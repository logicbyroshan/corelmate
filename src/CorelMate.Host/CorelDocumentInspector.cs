using System;
using System.Collections.Generic;
using Corel.Interop.VGCore;
using CorelMate.Core;
using CorelApplication = Corel.Interop.CorelDRAW.Application;
using CorelDocument = Corel.Interop.VGCore.Document;
using CorelShape = Corel.Interop.VGCore.Shape;

namespace CorelMate.Host;

public sealed class CorelDocumentInspector
{
    private readonly CorelDrawHost host;

    public CorelDocumentInspector(CorelDrawHost host)
    {
        this.host = host ?? throw new ArgumentNullException(nameof(host));
    }

    public DocumentMetrics InspectActiveDocument()
    {
        var doc = host.ActiveDocument ?? throw new InvalidOperationException("CorelDRAW does not have an active document.");
        var page = doc.ActivePage ?? throw new InvalidOperationException("CorelDRAW does not have an active page.");

        var name = doc.Name;
        var filePath = doc.FullFileName;
        var pageCount = doc.Pages.Count;

        var widthMm = doc.ToUnits(page.SizeWidth, cdrUnit.cdrMillimeter);
        var heightMm = doc.ToUnits(page.SizeHeight, cdrUnit.cdrMillimeter);
        var colorMode = "Unknown";
        try
        {
            colorMode = doc.ColorContext?.BlendingColorModel.ToString() ?? "Default";
        }
        catch { }

        var totalShapes = 0;
        var textShapes = 0;
        var curveShapes = 0;
        var rectangleShapes = 0;
        var ellipseShapes = 0;
        var bitmapShapes = 0;
        var groupShapes = 0;
        var fontSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Inspect shapes on active page
        var shapes = page.Shapes.All();
        var nodes = CorelShapeTraversal.Flatten(shapes);
        totalShapes = nodes.Count;

        foreach (var node in nodes)
        {
            var shape = node.Shape;
            switch (shape.Type)
            {
                case cdrShapeType.cdrTextShape:
                    textShapes++;
                    try
                    {
                        var fontName = shape.Text.Story.Font;
                        if (!string.IsNullOrWhiteSpace(fontName)) fontSet.Add(fontName);
                    }
                    catch { }
                    break;
                case cdrShapeType.cdrCurveShape:
                    curveShapes++;
                    break;
                case cdrShapeType.cdrRectangleShape:
                    rectangleShapes++;
                    break;
                case cdrShapeType.cdrEllipseShape:
                    ellipseShapes++;
                    break;
                case cdrShapeType.cdrBitmapShape:
                    bitmapShapes++;
                    break;
                case cdrShapeType.cdrGroupShape:
                    groupShapes++;
                    break;
            }
        }

        var layersCount = 0;
        try { layersCount = page.Layers.Count; } catch { }

        var fontList = new List<string>(fontSet);
        fontList.Sort(StringComparer.OrdinalIgnoreCase);

        return new DocumentMetrics(
            name,
            filePath,
            pageCount,
            widthMm,
            heightMm,
            colorMode,
            totalShapes,
            textShapes,
            curveShapes,
            rectangleShapes,
            ellipseShapes,
            bitmapShapes,
            groupShapes,
            layersCount,
            fontList);
    }
}
