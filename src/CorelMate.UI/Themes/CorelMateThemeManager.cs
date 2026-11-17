using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using CorelMate.Core;

namespace CorelMate.UI.Themes;

public enum ThemeMode
{
    Auto,
    Light,
    Dark
}

public static class CorelMateThemeManager
{
    public static ThemeMode CurrentMode { get; private set; } = ThemeMode.Auto;
    public static bool IsDarkActive { get; private set; }

    public static ThemeMode DetectSystemTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key != null)
            {
                var val = key.GetValue("AppsUseLightTheme");
                if (val is int intVal)
                {
                    return intVal == 0 ? ThemeMode.Dark : ThemeMode.Light;
                }
            }
        }
        catch
        {
        }

        // Default to Light mode matching standard CorelDRAW default installation
        return ThemeMode.Light;
    }

    public static void ApplyTheme(UserControl control, ThemeMode mode)
    {
        if (control == null) throw new ArgumentNullException(nameof(control));

        CurrentMode = mode;
        var effectiveMode = mode == ThemeMode.Auto ? DetectSystemTheme() : mode;
        IsDarkActive = effectiveMode == ThemeMode.Dark;

        var uriString = effectiveMode == ThemeMode.Dark
            ? "/CorelMate.UI;component/Themes/DarkTheme.xaml"
            : "/CorelMate.UI;component/Themes/LightTheme.xaml";

        try
        {
            var dict = new ResourceDictionary { Source = new Uri(uriString, UriKind.RelativeOrAbsolute) };
            var resources = control.Resources.MergedDictionaries;

            // Remove any previous theme dictionaries
            for (var index = resources.Count - 1; index >= 0; index--)
            {
                var src = resources[index].Source?.OriginalString ?? string.Empty;
                if (src.IndexOf("Theme", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    src.IndexOf("CorelMateTheme.xaml", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    resources.RemoveAt(index);
                }
            }

            resources.Add(dict);
        }
        catch
        {
            // Fallback gracefully
        }
    }

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
        // Era theme call delegates to system-aware light/dark theme adaptation
        var detected = DetectSystemTheme();
        ApplyTheme(control, detected);
    }
}
