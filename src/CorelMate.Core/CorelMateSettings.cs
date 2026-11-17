using System;

namespace CorelMate.Core;

public sealed class CorelMateSettings
{
    public double HorizontalBadgeGapMillimeters { get; set; } = 3.0;
    public double VerticalBadgeGapMillimeters { get; set; } = 3.0;
    public double PageMarginMillimeters { get; set; } = 5.0;
    public string ThemePreference { get; set; } = "Auto"; // "Auto", "Classic2017", "Charcoal2019", "Teal2021", "Azure2023", "AdaptivePro2026"
    public bool EnableFileLogging { get; set; } = true;
    public string PreferredAiProvider { get; set; } = string.Empty;
    public string AiModel { get; set; } = string.Empty;
}
