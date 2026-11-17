using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Corel.Interop.VGCore;
using CorelMate.Curves;
using CorelApplication = Corel.Interop.CorelDRAW.Application;
using CorelDocument = Corel.Interop.VGCore.Document;
using CorelShape = Corel.Interop.VGCore.Shape;
using CorelShapeRange = Corel.Interop.VGCore.ShapeRange;

namespace CorelMate.Host;

public sealed class CorelCurvePreflight
{
    internal CorelCurvePreflight(CorelDocument document, IReadOnlyList<CorelShape> targets, CurveConversionSummary summary)
    {
        Document = document;
        Targets = targets;
        Summary = summary;
    }

    internal CorelDocument Document { get; }
    internal IReadOnlyList<CorelShape> Targets { get; }
    public CurveConversionSummary Summary { get; }
}

public sealed class CorelCurveConversionResult
{
    internal CorelCurveConversionResult(int converted, int failed, CurveConversionSummary summary)
    {
        ConvertedTextObjects = converted;
        FailedTextObjects = failed;
        Summary = summary;
    }

    public int ConvertedTextObjects { get; }
    public int FailedTextObjects { get; }
    public CurveConversionSummary Summary { get; }
}

public sealed class CorelTextToCurvesConverter
{
    private readonly CorelDrawHost host;

    public CorelTextToCurvesConverter(CorelDrawHost host) => this.host = host ?? throw new ArgumentNullException(nameof(host));

    public CorelCurvePreflight Preflight(CurveConversionScope scope = CurveConversionScope.CurrentPage)
    {
        var document = host.ActiveDocument ?? throw new InvalidOperationException("CorelDRAW does not have an active document.");
        var summary = new CurveConversionSummary
        {
            Scope = scope
        };
        var metrics = new CorelShapeMetrics();
        var targets = new List<CorelShape>();
        IReadOnlyList<CorelShapeNode> nodes;

        switch (scope)
        {
            case CurveConversionScope.SelectedObjects:
                var selection = host.Application.ActiveSelectionRange;
                if (selection == null || selection.Count == 0)
                {
                    throw new InvalidOperationException("Select the artwork you want to convert first.");
                }
                summary.PagesProcessed = 1;
                nodes = CorelShapeTraversal.Flatten(selection, metrics);
                break;

            case CurveConversionScope.CurrentPage:
                var activePage = host.ActivePage ?? throw new InvalidOperationException("CorelDRAW has no active page.");
                summary.PagesProcessed = 1;
                nodes = CorelShapeTraversal.FlattenPage(activePage, metrics);
                break;

            case CurveConversionScope.AllPages:
                summary.PagesProcessed = document.Pages.Count;
                nodes = CorelShapeTraversal.FlattenDocument(document, metrics);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(scope));
        }

        summary.GroupsInspected = metrics.GroupsCount;
        summary.PowerClipsInspected = metrics.PowerClipsCount;
        summary.ArtisticTextCount = metrics.ArtisticTextCount;
        summary.ParagraphTextCount = metrics.ParagraphTextCount;
        summary.TextOnPathCount = metrics.TextOnPathCount;

        foreach (var node in nodes)
        {
            cdrShapeType shapeType;
            try { shapeType = node.Shape.Type; }
            catch { continue; }

            if (shapeType != cdrShapeType.cdrTextShape) continue;
            summary.FoundTextObjects++;

            if (node.IsLocked) summary.LockedTextObjects++;
            else if (node.IsHidden) summary.HiddenTextObjects++;
            else
            {
                summary.ConvertibleTextObjects++;
                targets.Add(node.Shape);
            }
        }

        return new CorelCurvePreflight(document, targets, summary);
    }

    public CorelCurvePreflight PreflightSelection() => Preflight(CurveConversionScope.SelectedObjects);

    public CorelCurveConversionResult Convert(CorelCurvePreflight preflight, CurveConversionOptions? options = null)
    {
        if (preflight == null) throw new ArgumentNullException(nameof(preflight));
        var activeDocument = host.ActiveDocument ?? throw new InvalidOperationException("CorelDRAW does not have an active document.");
        if (!ReferenceEquals(activeDocument, preflight.Document)) throw new InvalidOperationException("The selected document changed. Analyze the artwork again.");

        var opts = options ?? new CurveConversionOptions { Scope = preflight.Summary.Scope };
        var stopwatch = Stopwatch.StartNew();

        // 1. Optional document copy preservation
        if (opts.SaveConvertedCopy)
        {
            try
            {
                var fullPath = activeDocument.FullFileName;
                if (!string.IsNullOrWhiteSpace(fullPath))
                {
                    var dir = Path.GetDirectoryName(fullPath) ?? string.Empty;
                    var nameWithoutExt = Path.GetFileNameWithoutExtension(fullPath);
                    var ext = Path.GetExtension(fullPath);
                    if (string.IsNullOrEmpty(ext)) ext = ".cdr";
                    var copyName = $"{nameWithoutExt}{opts.CopySuffix}{ext}";
                    var copyPath = Path.Combine(dir, copyName);

                    try
                    {
                        activeDocument.SaveAsCopy(copyPath, null!);
                        preflight.Summary.SavedCopyPath = copyPath;
                    }
                    catch
                    {
                        if (File.Exists(fullPath))
                        {
                            File.Copy(fullPath, copyPath, true);
                            preflight.Summary.SavedCopyPath = copyPath;
                        }
                    }
                }
            }
            catch
            {
                // Non-fatal: if backup copy could not be written, continue with conversion
            }
        }

        // 2. Perform safe, atomic conversion with host performance optimizations
        var converted = 0;
        var failed = 0;
        var app = host.Application;

        activeDocument.BeginCommandGroup("CorelMate Convert Text to Curves");
        try
        {
            try
            {
                app.Optimization = true;
                app.EventsEnabled = false;
                activeDocument.PreserveSelection = false;
            }
            catch
            {
                // Optimization flags are best-effort across different CorelDRAW builds
            }

            foreach (var shape in preflight.Targets)
            {
                try
                {
                    shape.ConvertToCurves();
                    converted++;
                }
                catch
                {
                    // Fallback for nested shapes inside PowerClip containers
                    try
                    {
                        var parent = shape.PowerClipParent;
                        if (parent?.PowerClip != null)
                        {
                            parent.PowerClip.EnterEditMode();
                            shape.ConvertToCurves();
                            parent.PowerClip.LeaveEditMode();
                            converted++;
                        }
                        else
                        {
                            failed++;
                        }
                    }
                    catch
                    {
                        failed++;
                    }
                }
            }
        }
        finally
        {
            try
            {
                activeDocument.PreserveSelection = true;
                app.EventsEnabled = true;
                app.Optimization = false;
            }
            catch
            {
            }

            activeDocument.EndCommandGroup();

            try
            {
                app.ActiveWindow?.Refresh();
            }
            catch
            {
            }
        }

        stopwatch.Stop();
        preflight.Summary.ConvertedTextObjects = converted;
        preflight.Summary.FailedTextObjects = failed;
        preflight.Summary.ExecutionDurationMs = stopwatch.ElapsedMilliseconds;

        return new CorelCurveConversionResult(converted, failed, preflight.Summary);
    }
}
