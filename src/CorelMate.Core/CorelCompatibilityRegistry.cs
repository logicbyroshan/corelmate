using System;
using System.Collections.Generic;

namespace CorelMate.Core;

public enum CompatibilityStatus
{
    Supported,
    UnsupportedLegacy,
    UnsupportedFuture,
    Unknown
}

public sealed class CompatibilityCheckResult
{
    public CompatibilityCheckResult(
        CompatibilityStatus status,
        int majorVersion,
        string versionDescription,
        string statusMessage,
        bool canRun)
    {
        Status = status;
        MajorVersion = majorVersion;
        VersionDescription = versionDescription;
        StatusMessage = statusMessage;
        CanRun = canRun;
    }

    public CompatibilityStatus Status { get; }
    public int MajorVersion { get; }
    public string VersionDescription { get; }
    public string StatusMessage { get; }
    public bool CanRun { get; }
}

public static class CorelCompatibilityRegistry
{
    public const int MinSupportedMajorVersion = 19; // CorelDRAW 2017
    public const int MaxSupportedMajorVersion = 28; // CorelDRAW 2026+

    private static readonly Dictionary<int, CorelVersionInfo> KnownVersions = new Dictionary<int, CorelVersionInfo>
    {
        [18] = new CorelVersionInfo(18, "2016", "CorelDRAW Graphics Suite X8", CorelEra.Classic2017, false),
        [19] = new CorelVersionInfo(19, "2017", "CorelDRAW Graphics Suite 2017", CorelEra.Classic2017, true),
        [20] = new CorelVersionInfo(20, "2018", "CorelDRAW Graphics Suite 2018", CorelEra.Charcoal2019, true),
        [21] = new CorelVersionInfo(21, "2019", "CorelDRAW Graphics Suite 2019", CorelEra.Charcoal2019, true),
        [22] = new CorelVersionInfo(22, "2020", "CorelDRAW Graphics Suite 2020", CorelEra.Teal2021, true),
        [23] = new CorelVersionInfo(23, "2021", "CorelDRAW Graphics Suite 2021", CorelEra.Teal2021, true),
        [24] = new CorelVersionInfo(24, "2022", "CorelDRAW Graphics Suite 2022", CorelEra.Azure2023, true),
        [25] = new CorelVersionInfo(25, "2023", "CorelDRAW Graphics Suite 2023", CorelEra.Azure2023, true),
        [26] = new CorelVersionInfo(26, "2024", "CorelDRAW Graphics Suite 2024", CorelEra.AdaptivePro2026, true),
        [27] = new CorelVersionInfo(27, "2025/2026", "CorelDRAW Graphics Suite 2025/2026", CorelEra.AdaptivePro2026, true),
        [28] = new CorelVersionInfo(28, "2026+", "CorelDRAW Graphics Suite 2026+", CorelEra.AdaptivePro2026, true),
    };

    public static IReadOnlyCollection<CorelVersionInfo> AllKnownVersions => KnownVersions.Values;

    public static CorelVersionInfo? GetVersionInfo(int majorVersion)
    {
        return KnownVersions.TryGetValue(majorVersion, out var info) ? info : null;
    }

    public static CompatibilityCheckResult Evaluate(int majorVersion, string rawVersionString = "")
    {
        var info = GetVersionInfo(majorVersion);
        var label = info?.DisplayName ?? $"CorelDRAW v{majorVersion}" + (string.IsNullOrWhiteSpace(rawVersionString) ? string.Empty : $" ({rawVersionString})");

        if (majorVersion < MinSupportedMajorVersion)
        {
            return new CompatibilityCheckResult(
                CompatibilityStatus.UnsupportedLegacy,
                majorVersion,
                label,
                $"CorelDRAW v{majorVersion} is not supported. CorelMate requires CorelDRAW 2017 (v19) or newer.",
                false);
        }

        if (majorVersion > MaxSupportedMajorVersion)
        {
            return new CompatibilityCheckResult(
                CompatibilityStatus.UnsupportedFuture,
                majorVersion,
                label,
                $"CorelDRAW v{majorVersion} is newer than the tested range (2017-2026). An explicit compatibility update is required.",
                false);
        }

        return new CompatibilityCheckResult(
            CompatibilityStatus.Supported,
            majorVersion,
            label,
            $"{label} is fully supported by CorelMate.",
            true);
    }

    public static CorelEra GetEraForVersion(int majorVersion)
    {
        var info = GetVersionInfo(majorVersion);
        if (info != null) return info.Era;

        if (majorVersion <= 19) return CorelEra.Classic2017;
        if (majorVersion <= 21) return CorelEra.Charcoal2019;
        if (majorVersion <= 23) return CorelEra.Teal2021;
        if (majorVersion <= 25) return CorelEra.Azure2023;
        return CorelEra.AdaptivePro2026;
    }

    public static int ExtractMajorVersion(string versionText)
    {
        if (string.IsNullOrWhiteSpace(versionText)) return 0;

        var parts = versionText.Split(new[] { ' ', '.', ',', 'v', 'V' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            if (int.TryParse(part, out var num) && num >= 10 && num <= 50)
            {
                return num;
            }
        }

        return 0;
    }
}
