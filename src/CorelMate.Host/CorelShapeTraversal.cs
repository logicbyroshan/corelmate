using System;
using System.Collections.Generic;
using Corel.Interop.VGCore;
using CorelShape = Corel.Interop.VGCore.Shape;
using CorelPage = Corel.Interop.VGCore.Page;
using CorelDocument = Corel.Interop.VGCore.Document;

namespace CorelMate.Host;

public sealed class CorelShapeMetrics
{
    public int GroupsCount { get; set; }
    public int PowerClipsCount { get; set; }
    public int ArtisticTextCount { get; set; }
    public int ParagraphTextCount { get; set; }
    public int TextOnPathCount { get; set; }
}

public sealed class CorelShapeNode
{
    internal CorelShapeNode(CorelShape shape, bool hiddenByAncestor, bool lockedByAncestor)
    {
        Shape = shape;
        HiddenByAncestor = hiddenByAncestor;
        LockedByAncestor = lockedByAncestor;
    }

    public CorelShape Shape { get; }
    public bool HiddenByAncestor { get; }
    public bool LockedByAncestor { get; }
    public bool IsHidden => HiddenByAncestor || !Shape.Visible;
    public bool IsLocked => LockedByAncestor || Shape.Locked;
}

public static class CorelShapeTraversal
{
    public static IReadOnlyList<CorelShapeNode> Flatten(ShapeRange? selection, CorelShapeMetrics? metrics = null)
    {
        var nodes = new List<CorelShapeNode>();
        if (selection == null) return nodes;

        for (var index = 1; index <= selection.Count; index++)
        {
            AddShape(selection[index], false, false, nodes, metrics);
        }
        return nodes;
    }

    public static IReadOnlyList<CorelShapeNode> FlattenPage(CorelPage? page, CorelShapeMetrics? metrics = null)
    {
        var nodes = new List<CorelShapeNode>();
        if (page == null) return nodes;

        for (var layerIndex = 1; layerIndex <= page.Layers.Count; layerIndex++)
        {
            var layer = page.Layers[layerIndex];
            if (layer.IsSpecialLayer) continue;

            var layerHidden = !layer.Visible;
            var layerLocked = !layer.Editable;

            for (var shapeIndex = 1; shapeIndex <= layer.Shapes.Count; shapeIndex++)
            {
                AddShape(layer.Shapes[shapeIndex], layerHidden, layerLocked, nodes, metrics);
            }
        }
        return nodes;
    }

    public static IReadOnlyList<CorelShapeNode> FlattenDocument(CorelDocument? document, CorelShapeMetrics? metrics = null)
    {
        var nodes = new List<CorelShapeNode>();
        if (document == null) return nodes;

        for (var pageIndex = 1; pageIndex <= document.Pages.Count; pageIndex++)
        {
            var page = document.Pages[pageIndex];
            var pageNodes = FlattenPage(page, metrics);
            nodes.AddRange(pageNodes);
        }
        return nodes;
    }

    private static void AddShape(CorelShape? shape, bool hiddenByAncestor, bool lockedByAncestor, ICollection<CorelShapeNode> nodes, CorelShapeMetrics? metrics)
    {
        if (shape == null) return;

        var hidden = hiddenByAncestor || !shape.Visible;
        var locked = lockedByAncestor || shape.Locked;

        nodes.Add(new CorelShapeNode(shape, hiddenByAncestor, lockedByAncestor));

        // Track text statistics
        if (shape.Type == cdrShapeType.cdrTextShape && metrics != null)
        {
            try
            {
                if (shape.Text.Type == cdrTextType.cdrArtisticText)
                {
                    metrics.ArtisticTextCount++;
                }
                else if (shape.Text.Type == cdrTextType.cdrParagraphText)
                {
                    metrics.ParagraphTextCount++;
                }

                if (shape.Effects != null && shape.Effects.Count > 0)
                {
                    for (var effIndex = 1; effIndex <= shape.Effects.Count; effIndex++)
                    {
                        var eff = shape.Effects[effIndex];
                        if (eff.Type == cdrEffectType.cdrTextOnPath)
                        {
                            metrics.TextOnPathCount++;
                            break;
                        }
                    }
                }
            }
            catch
            {
                // Protected from COM interop anomalies on mixed or custom shapes
            }
        }

        // Recursively traverse groups without destructive ungrouping
        if (shape.Type == cdrShapeType.cdrGroupShape)
        {
            if (metrics != null) metrics.GroupsCount++;
            for (var index = 1; index <= shape.Shapes.Count; index++)
            {
                AddShape(shape.Shapes[index], hidden, locked, nodes, metrics);
            }
        }

        // Recursively traverse PowerClip containers
        try
        {
            if (shape.PowerClip != null)
            {
                if (metrics != null) metrics.PowerClipsCount++;
                var pcShapes = shape.PowerClip.Shapes;
                if (pcShapes != null)
                {
                    for (var pcIndex = 1; pcIndex <= pcShapes.Count; pcIndex++)
                    {
                        AddShape(pcShapes[pcIndex], hidden, locked, nodes, metrics);
                    }
                }
            }
        }
        catch
        {
            // Shape does not support PowerClip or has empty container
        }
    }
}
