using System;

namespace CorelMate.Core;

public sealed class CorelInstallationInfo
{
    public CorelInstallationInfo(
        int majorVersion,
        string versionString,
        string installationPath,
        string executablePath,
        bool is64Bit,
        string progId,
        string detectionSource)
    {
        MajorVersion = majorVersion;
        VersionString = versionString ?? string.Empty;
        InstallationPath = installationPath ?? string.Empty;
        ExecutablePath = executablePath ?? string.Empty;
        Is64Bit = is64Bit;
        ProgId = progId ?? string.Empty;
        DetectionSource = detectionSource ?? string.Empty;
    }

    public int MajorVersion { get; }
    public string VersionString { get; }
    public string InstallationPath { get; }
    public string ExecutablePath { get; }
    public bool Is64Bit { get; }
    public string ProgId { get; }
    public string DetectionSource { get; }

    public CorelVersionInfo? KnownVersion => CorelCompatibilityRegistry.GetVersionInfo(MajorVersion);
    public bool IsSupported => KnownVersion?.IsSupported == true;
    public string DisplayName => KnownVersion != null ? KnownVersion.DisplayName : $"CorelDRAW v{MajorVersion}";
    public string ArchitectureString => Is64Bit ? "64-bit (x64)" : "32-bit (x86)";

    public override string ToString() => $"{DisplayName} [{ArchitectureString}] at {InstallationPath}";
}

