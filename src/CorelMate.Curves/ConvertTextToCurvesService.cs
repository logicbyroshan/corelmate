using System;

namespace CorelMate.Curves;

public enum CurveConversionScope
{
    SelectedObjects,
    CurrentPage,
    AllPages
}

public enum CurveSkipReason
{
    Locked,
    Hidden,
    Unsupported
}

public sealed class CurveConversionOptions
{
    public CurveConversionScope Scope { get; set; } = CurveConversionScope.CurrentPage;
    public bool SaveConvertedCopy { get; set; }
    public string CopySuffix { get; set; } = "_curves";
    public bool UnlockLockedShapes { get; set; }
}

public sealed class CurveConversionSummary
{
    public CurveConversionScope Scope { get; set; } = CurveConversionScope.CurrentPage;
    public int PagesProcessed { get; set; }
    public int FoundTextObjects { get; set; }
    public int ArtisticTextCount { get; set; }
    public int ParagraphTextCount { get; set; }
    public int TextOnPathCount { get; set; }
    public int GroupsInspected { get; set; }
    public int PowerClipsInspected { get; set; }
    public int ConvertibleTextObjects { get; set; }
    public int LockedTextObjects { get; set; }
    public int HiddenTextObjects { get; set; }
    public int UnsupportedTextObjects { get; set; }
    public int ConvertedTextObjects { get; set; }
    public int FailedTextObjects { get; set; }
    public string? SavedCopyPath { get; set; }
    public long ExecutionDurationMs { get; set; }

    public int SkippedTextObjects => LockedTextObjects + HiddenTextObjects + UnsupportedTextObjects;
    public bool HasTargets => ConvertibleTextObjects > 0;
}
