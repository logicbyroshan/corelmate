using System;
using System.IO;
using System.Text;
using CorelMate.Core;

namespace CorelMate.Host;

public static class CorelMenuIntegration
{
    public const string MenuBarGuid = "f3016f3c-2847-4557-b61a-a2d05319cf18";
    public const string CorelMateMenuFlyoutGuid = "a63f9c4c-4e91-menu-9ac9-3a15c783d5d7";
    public const string CorelMateCommandBarGuid = "a63f9c4c-cbef-4276-b686-adb4f77c80d6";

    public static string GenerateUserDrawXslt()
    {
        return @"<?xml version=""1.0""?>
<xsl:stylesheet version=""1.0""
    xmlns:xsl=""http://www.w3.org/1999/XSL/Transform""
    xmlns:frmwrk=""Corel Framework Data""
    exclude-result-prefixes=""frmwrk"">
  <xsl:output method=""xml"" encoding=""UTF-8"" indent=""yes""/>

  <frmwrk:uiconfig>
    <frmwrk:applicationInfo name=""CorelDRAW"" framework=""CorelDRAW"" userConfiguration=""true"" />
  </frmwrk:uiconfig>

  <!-- Identity template: copy everything untouched -->
  <xsl:template match=""node()|@*"">
    <xsl:copy>
      <xsl:apply-templates select=""node()|@*""/>
    </xsl:copy>
  </xsl:template>

  <!-- Append CorelMate as the LAST main navigation menu item in the Menu Bar -->
  <xsl:template match=""//commandBarData[@guid='f3016f3c-2847-4557-b61a-a2d05319cf18']/menubar/modeData"">
    <xsl:copy>
      <xsl:apply-templates select=""node()|@*""/>
      <xsl:if test=""not(./item[@guidRef='a63f9c4c-4e91-menu-9ac9-3a15c783d5d7'])"">
        <item guidRef=""a63f9c4c-4e91-menu-9ac9-3a15c783d5d7""/>
      </xsl:if>
    </xsl:copy>
  </xsl:template>

</xsl:stylesheet>";
    }

    public static string GenerateGmsMacroContent()
    {
        return @"' CorelMate GMS Integration Macro
' Auto-generated companion for CorelMate Main Menu Navigation
Option Explicit

Private Const DOCKER_GUID As String = ""{A63F9C4C-64A3-4E91-9AC9-3A15C783D5D7}""

Public Sub ShowDashboard()
    On Error Resume Next
    CorelDRAW.FrameWork.ShowDocker DOCKER_GUID
End Sub

Public Sub ShowBadges()
    On Error Resume Next
    CorelDRAW.FrameWork.ShowDocker DOCKER_GUID
End Sub

Public Sub ShowCurves()
    On Error Resume Next
    CorelDRAW.FrameWork.ShowDocker DOCKER_GUID
End Sub

Public Sub ShowInspector()
    On Error Resume Next
    CorelDRAW.FrameWork.ShowDocker DOCKER_GUID
End Sub

Public Sub ShowCompatibility()
    On Error Resume Next
    CorelDRAW.FrameWork.ShowDocker DOCKER_GUID
End Sub

Public Sub ShowSettings()
    On Error Resume Next
    CorelDRAW.FrameWork.ShowDocker DOCKER_GUID
End Sub

Public Sub ShowAbout()
    On Error Resume Next
    CorelDRAW.FrameWork.ShowDocker DOCKER_GUID
End Sub
";
    }

    public static bool DeployAddon(string programsDir, out string message)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(programsDir) || !Directory.Exists(programsDir))
            {
                message = $"Programs directory '{programsDir}' does not exist.";
                return false;
            }

            var addonsRoot = Path.Combine(programsDir, "Addons");
            var corelMateAddonDir = Path.Combine(addonsRoot, "CorelMate");
            Directory.CreateDirectory(corelMateAddonDir);

            // 1. Zero-byte Coreldrw.addon marker file
            var markerPath = Path.Combine(corelMateAddonDir, "Coreldrw.addon");
            if (!File.Exists(markerPath))
            {
                File.WriteAllBytes(markerPath, Array.Empty<byte>());
            }

            // 2. UserDraw.xslt stylesheet
            var xsltPath = Path.Combine(corelMateAddonDir, "UserDraw.xslt");
            File.WriteAllText(xsltPath, GenerateUserDrawXslt(), Encoding.UTF8);

            message = $"CorelMate Addon successfully deployed to '{corelMateAddonDir}'.";
            return true;
        }
        catch (Exception ex)
        {
            message = $"Failed to deploy CorelMate Addon: {ex.Message}";
            return false;
        }
    }

    public static bool RemoveAddon(string programsDir, out string message)
    {
        try
        {
            var corelMateAddonDir = Path.Combine(programsDir, "Addons", "CorelMate");
            if (Directory.Exists(corelMateAddonDir))
            {
                Directory.Delete(corelMateAddonDir, true);
                message = "CorelMate Addon removed.";
                return true;
            }

            message = "CorelMate Addon was not present.";
            return true;
        }
        catch (Exception ex)
        {
            message = $"Failed to remove Addon: {ex.Message}";
            return false;
        }
    }

    public static bool DeployGmsMacro(string gmsDir, out string message)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(gmsDir))
            {
                message = "GMS directory path was not provided.";
                return false;
            }

            Directory.CreateDirectory(gmsDir);
            var gmsPath = Path.Combine(gmsDir, "CorelMate.gms");
            File.WriteAllText(gmsPath, GenerateGmsMacroContent(), Encoding.ASCII);

            message = $"CorelMate macro installed to '{gmsPath}'.";
            return true;
        }
        catch (Exception ex)
        {
            message = $"Failed to install GMS macro: {ex.Message}";
            return false;
        }
    }

    public static bool RemoveGmsMacro(string gmsDir, out string message)
    {
        try
        {
            var gmsPath = Path.Combine(gmsDir, "CorelMate.gms");
            if (File.Exists(gmsPath))
            {
                File.Delete(gmsPath);
                message = "CorelMate GMS macro removed.";
                return true;
            }

            message = "CorelMate GMS macro was not present.";
            return true;
        }
        catch (Exception ex)
        {
            message = $"Failed to remove GMS macro: {ex.Message}";
            return false;
        }
    }
}

