using System;
using System.Collections.Generic;

namespace CorelMate.Core;

public sealed class DocumentMetrics
{
    public DocumentMetrics(
        string name,
        string filePath,
        int pageCount,
        double pageWidthMillimeters,
        double pageHeightMillimeters,
        string colorMode,
        int totalShapes,
        int textShapes,
        int curveShapes,
        int rectangleShapes,
        int ellipseShapes,
        int bitmapShapes,
        int groupShapes,
        int layerCount,
        IReadOnlyList<string> uniqueFonts)
    {
        Name = name ?? string.Empty;
        FilePath = filePath ?? string.Empty;
        PageCount = pageCount;
        PageWidthMillimeters = pageWidthMillimeters;
        PageHeightMillimeters = pageHeightMillimeters;
        ColorMode = colorMode ?? string.Empty;
        TotalShapes = totalShapes;
        TextShapes = textShapes;
        CurveShapes = curveShapes;
        RectangleShapes = rectangleShapes;
        EllipseShapes = ellipseShapes;
        BitmapShapes = bitmapShapes;
        GroupShapes = groupShapes;
        LayerCount = layerCount;
        UniqueFonts = uniqueFonts ?? Array.Empty<string>();
        InspectedAt = DateTime.Now;
    }

    public string Name { get; }
    public string FilePath { get; }
    public int PageCount { get; }
    public double PageWidthMillimeters { get; }
    public double PageHeightMillimeters { get; }
    public string ColorMode { get; }
    public int TotalShapes { get; }
    public int TextShapes { get; }
    public int CurveShapes { get; }
    public int RectangleShapes { get; }
    public int EllipseShapes { get; }
    public int BitmapShapes { get; }
    public int GroupShapes { get; }
    public int LayerCount { get; }
    public IReadOnlyList<string> UniqueFonts { get; }
    public DateTime InspectedAt { get; }

    public string DimensionsString => $"{PageWidthMillimeters:0.##} × {PageHeightMillimeters:0.##} mm";
}

