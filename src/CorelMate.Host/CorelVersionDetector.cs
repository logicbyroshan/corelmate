using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using CorelMate.Core;

namespace CorelMate.Host;

public static class CorelVersionDetector
{
    public static IReadOnlyList<CorelInstallationInfo> DetectAllInstallations()
    {
        var installations = new Dictionary<string, CorelInstallationInfo>(StringComparer.OrdinalIgnoreCase);

        // 1. Scan 64-bit and 32-bit Registry
        ScanRegistry(RegistryView.Registry64, installations);
        ScanRegistry(RegistryView.Registry32, installations);

        // 2. Scan Standard Program Files locations if registry was incomplete
        ScanCommonDirectories(installations);

        // 3. Scan running COM instance if active
        ScanRunningComInstance(installations);

        var list = new List<CorelInstallationInfo>(installations.Values);
        list.Sort((a, b) => b.MajorVersion.CompareTo(a.MajorVersion)); // Descending by version
        return list;
    }

    private static void ScanRegistry(RegistryView view, Dictionary<string, CorelInstallationInfo> list)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
            using var corelDrawKey = baseKey.OpenSubKey(@"SOFTWARE\Corel\CorelDRAW");
            if (corelDrawKey == null) return;

            foreach (var subKeyName in corelDrawKey.GetSubKeyNames())
            {
                if (!double.TryParse(subKeyName, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var verNum))
                {
                    continue;
                }

                var major = (int)Math.Floor(verNum);
                using var verKey = corelDrawKey.OpenSubKey(subKeyName);
                if (verKey == null) continue;

                var progDir = verKey.GetValue("ProgramsDir") as string ?? string.Empty;
                var pluginDir = verKey.GetValue("PluginDir") as string ?? string.Empty;

                if (string.IsNullOrWhiteSpace(progDir) && !string.IsNullOrWhiteSpace(pluginDir))
                {
                    progDir = Path.Combine(Path.GetDirectoryName(pluginDir.TrimEnd('\\', '/')) ?? string.Empty, "Programs64");
                }

                if (string.IsNullOrWhiteSpace(progDir) || !Directory.Exists(progDir)) continue;

                var exePath = Path.Combine(progDir, "CorelDRW.exe");
                var is64 = view == RegistryView.Registry64 || progDir.IndexOf("64", StringComparison.OrdinalIgnoreCase) >= 0;
                var versionStr = $"{major}.0";

                if (File.Exists(exePath))
                {
                    try
                    {
                        var fvi = FileVersionInfo.GetVersionInfo(exePath);
                        if (!string.IsNullOrWhiteSpace(fvi.FileVersion)) versionStr = fvi.FileVersion;
                    }
                    catch { }
                }

                var key = $"{major}-{is64}-{progDir}";
                if (!list.ContainsKey(key))
                {
                    list[key] = new CorelInstallationInfo(
                        major,
                        versionStr,
                        progDir,
                        exePath,
                        is64,
                        $"CorelDRAW.Application.{major}",
                        $"Registry {view}");
                }
            }
        }
        catch
        {
            // Ignore registry scan permission limitations
        }
    }

    private static void ScanCommonDirectories(Dictionary<string, CorelInstallationInfo> list)
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var baseDir = Path.Combine(programFiles, "Corel");
        if (!Directory.Exists(baseDir)) return;

        try
        {
            foreach (var dir in Directory.GetDirectories(baseDir))
            {
                var dirName = Path.GetFileName(dir);
                // Look for "CorelDRAW Graphics Suite <Year>" or "CorelDRAW Graphics Suite\27"
                var programs64 = Path.Combine(dir, "Programs64");
                if (!Directory.Exists(programs64))
                {
                    programs64 = Path.Combine(dir, "Programs");
                }

                if (!Directory.Exists(programs64)) continue;

                var exePath = Path.Combine(programs64, "CorelDRW.exe");
                if (!File.Exists(exePath)) continue;

                try
                {
                    var fvi = FileVersionInfo.GetVersionInfo(exePath);
                    var major = fvi.ProductMajorPart > 0 ? fvi.ProductMajorPart : fvi.FileMajorPart;
                    if (major <= 0) continue;

                    var is64 = programs64.IndexOf("64", StringComparison.OrdinalIgnoreCase) >= 0;
                    var key = $"{major}-{is64}-{programs64}";
                    if (!list.ContainsKey(key))
                    {
                        list[key] = new CorelInstallationInfo(
                            major,
                            fvi.FileVersion ?? $"{major}.0",
                            programs64,
                            exePath,
                            is64,
                            $"CorelDRAW.Application.{major}",
                            "File System");
                    }
                }
                catch { }
            }
        }
        catch { }
    }

    private static void ScanRunningComInstance(Dictionary<string, CorelInstallationInfo> list)
    {
        try
        {
            var comObj = Marshal.GetActiveObject("CorelDRAW.Application");
            if (comObj == null) return;

            var comType = comObj.GetType();
            string rawVersion = comType.InvokeMember("Version", System.Reflection.BindingFlags.GetProperty, null, comObj, null)?.ToString() ?? string.Empty;
            // E.g. "Version 27.1.0.129" or "27.1"
            var parsedMajor = ExtractMajorVersion(rawVersion);
            if (parsedMajor > 0)
            {
                string path = string.Empty;
                try { path = comType.InvokeMember("Path", System.Reflection.BindingFlags.GetProperty, null, comObj, null)?.ToString() ?? string.Empty; } catch { }
                var is64 = Environment.Is64BitOperatingSystem;

                var key = $"Running-{parsedMajor}";
                list[key] = new CorelInstallationInfo(
                    parsedMajor,
                    rawVersion,
                    path,
                    Path.Combine(path, "CorelDRW.exe"),
                    is64,
                    "CorelDRAW.Application",
                    "Active COM Instance");
            }
        }
        catch
        {
            // No active running COM instance, that is completely normal
        }
    }

    public static int ExtractMajorVersion(string versionText)
    {
        return CorelCompatibilityRegistry.ExtractMajorVersion(versionText);
    }
}
