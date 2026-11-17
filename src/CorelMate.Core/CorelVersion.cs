using System;

namespace CorelMate.Core;

public enum CorelEra
{
    Classic2017,
    Charcoal2019,
    Teal2021,
    Azure2023,
    AdaptivePro2026
}

public sealed class CorelVersionInfo
{
    public CorelVersionInfo(
        int majorVersion,
        string releaseYear,
        string marketingName,
        CorelEra era,
        bool isSupported)
    {
        MajorVersion = majorVersion;
        ReleaseYear = releaseYear ?? throw new ArgumentNullException(nameof(releaseYear));
        MarketingName = marketingName ?? throw new ArgumentNullException(nameof(marketingName));
        Era = era;
        IsSupported = isSupported;
    }

    public int MajorVersion { get; }
    public string ReleaseYear { get; }
    public string MarketingName { get; }
    public CorelEra Era { get; }
    public bool IsSupported { get; }

    public string DisplayName => $"{MarketingName} (v{MajorVersion})";

    public override string ToString() => DisplayName;
}

