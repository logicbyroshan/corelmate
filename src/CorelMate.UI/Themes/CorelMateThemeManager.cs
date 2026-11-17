using System;
using System.Windows;
using System.Windows.Controls;
using CorelMate.Core;

namespace CorelMate.UI.Themes;

public static class CorelMateThemeManager
{
    public static CorelEra ResolveEra(string preference, int detectedMajorVersion)
    {
        if (string.Equals(preference, "Classic2017", StringComparison.OrdinalIgnoreCase)) return CorelEra.Classic2017;
        if (string.Equals(preference, "Charcoal2019", StringComparison.OrdinalIgnoreCase)) return CorelEra.Charcoal2019;
        if (string.Equals(preference, "Teal2021", StringComparison.OrdinalIgnoreCase)) return CorelEra.Teal2021;
        if (string.Equals(preference, "Azure2023", StringComparison.OrdinalIgnoreCase)) return CorelEra.Azure2023;
        if (string.Equals(preference, "AdaptivePro2026", StringComparison.OrdinalIgnoreCase)) return CorelEra.AdaptivePro2026;

        return CorelCompatibilityRegistry.GetEraForVersion(detectedMajorVersion);
    }

    public static void ApplyTheme(UserControl control, CorelEra era)
    {
        if (control == null) throw new ArgumentNullException(nameof(control));

        var uriString = era switch
        {
            CorelEra.Classic2017 => "/CorelMate.UI;component/Themes/Theme2017.xaml",
            CorelEra.Charcoal2019 => "/CorelMate.UI;component/Themes/Theme2019.xaml",
            CorelEra.Teal2021 => "/CorelMate.UI;component/Themes/Theme2021.xaml",
            CorelEra.Azure2023 => "/CorelMate.UI;component/Themes/Theme2023.xaml",
            _ => "/CorelMate.UI;component/Themes/Theme2026.xaml"
        };

        try
        {
            var dict = new ResourceDictionary { Source = new Uri(uriString, UriKind.RelativeOrAbsolute) };
            // Clear existing era dictionary if any and insert the new one
            var resources = control.Resources.MergedDictionaries;
            for (var index = resources.Count - 1; index >= 0; index--)
            {
                var src = resources[index].Source?.OriginalString ?? string.Empty;
                if (src.IndexOf("Theme20", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    resources.RemoveAt(index);
                }
            }

            resources.Add(dict);
        }
        catch
        {
            // Fallback gracefully to default CorelMateTheme if pack URI fails
        }
    }
}

