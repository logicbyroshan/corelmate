using System;
using System.IO;
using System.Text;
using System.Xml;
using CorelMate.Core;

namespace CorelMate.Host;

public static class CorelMenuIntegration
{
    public const string DockerGuid = CorelWorkspacePatcher.DockerGuid;
    public const string DockerClassName = CorelWorkspacePatcher.DockerClassName;
    public const string DockerHostedItemGuid = CorelWorkspacePatcher.DockerHostedItemGuid;

    public const string MenuBarGuid = CorelWorkspacePatcher.MenuBarGuid;
    public const string CorelMateMenuFlyoutGuid = CorelWorkspacePatcher.CorelMateMenuFlyoutGuid;
    public const string CorelMateCommandBarGuid = CorelWorkspacePatcher.CorelMateCommandBarGuid;

    // Feature 1: Curves
    public const string FlyoutCurvesGuid        = CorelWorkspacePatcher.FlyoutCurvesGuid;
    public const string CBarCurvesGuid          = CorelWorkspacePatcher.CBarCurvesGuid;
    public const string CmdCurveCurrentPageGuid = CorelWorkspacePatcher.CmdCurveCurrentPageGuid;
    public const string CmdCurveAllPagesGuid    = CorelWorkspacePatcher.CmdCurveAllPagesGuid;
    public const string CmdCurveSelectionGuid   = CorelWorkspacePatcher.CmdCurveSelectionGuid;

    // Feature 2: Export
    public const string FlyoutExportGuid        = CorelWorkspacePatcher.FlyoutExportGuid;
    public const string CBarExportGuid          = CorelWorkspacePatcher.CBarExportGuid;
    public const string CmdExportIdCardGuid     = CorelWorkspacePatcher.CmdExportIdCardGuid;
    public const string CmdExportPrintGuid      = CorelWorkspacePatcher.CmdExportPrintGuid;

    // Feature 3: Color
    public const string FlyoutColorGuid         = CorelWorkspacePatcher.FlyoutColorGuid;
    public const string CBarColorGuid           = CorelWorkspacePatcher.CBarColorGuid;
    public const string CmdColorCmykGuid        = CorelWorkspacePatcher.CmdColorCmykGuid;
    public const string CmdColorRgbGuid         = CorelWorkspacePatcher.CmdColorRgbGuid;

    // Legacy Item GUIDs (retained for binary/library compatibility)
    public const string ItemShowDockerGuid        = CorelWorkspacePatcher.ItemShowDockerGuid;
    public const string ItemConvertPageCurvesGuid = CorelWorkspacePatcher.ItemConvertPageCurvesGuid;
    public const string ItemConvertAllCurvesGuid  = CorelWorkspacePatcher.ItemConvertAllCurvesGuid;
    public const string ItemConvertSelCurvesGuid  = CorelWorkspacePatcher.ItemConvertSelCurvesGuid;
    public const string ItemInspectorGuid         = CorelWorkspacePatcher.ItemInspectorGuid;
    public const string ItemBadgesGuid            = CorelWorkspacePatcher.ItemBadgesGuid;
    public const string ItemAboutGuid             = CorelWorkspacePatcher.ItemAboutGuid;

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

  <!-- Remove legacy CorelMate docker registration -->
  <xsl:template match=""uiConfig/dockers/dockerData[@guid='{A63F9C4C-64A3-4E91-9AC9-3A15C783D5D7}']""/>
  <xsl:template match=""//dockerData[@guidRef='{A63F9C4C-64A3-4E91-9AC9-3A15C783D5D7}']""/>
  <xsl:template match=""//item[@guidRef='a63f9c4c-item-dock-9ac9-3a15c783d5d4']""/>
  <xsl:template match=""//item[@guidRef='a63f9c4c-4e91-menu-9ac9-3a15c783d5d7']""/>
  <xsl:template match=""//commandBarData[@guid='a63f9c4c-cbef-4276-b686-adb4f77c80d6']""/>

  <!-- Register dynamic menu commands in items -->
  <xsl:template match=""uiConfig/items"">
    <xsl:copy>
      <xsl:apply-templates select=""node()|@*""/>

      <!-- Feature 1: Curves -->
      <xsl:if test=""not(./itemData[@guid='a63f9c4c-flyout-curves-3a15c783d5d1'])"">
        <itemData guid=""a63f9c4c-flyout-curves-3a15c783d5d1"" flyoutBarRef=""a63f9c4c-cbar-curves-3a15c783d5d1"" type=""flyout"" userCaption=""CorelMate Curves"" userToolTip=""Text-to-Curves Operations"" userCreated=""true"" noBmpOnMenu=""true""/>
      </xsl:if>
      <xsl:if test=""not(./itemData[@guid='a63f9c4c-cmd-cpage-3a15c783d5d1'])"">
        <itemData guid=""a63f9c4c-cmd-cpage-3a15c783d5d1"" type=""button"" dynamicCategory=""2cc24a3e-fe24-4708-9a74-9c75406eebcd"" dynamicCommand=""ConvertAllToCurves.Converter.CurveCurrentPage"" userCaption=""Curve Current Page"" userToolTip=""Convert all text on current page to curves"" noBmpOnMenu=""true""/>
      </xsl:if>
      <xsl:if test=""not(./itemData[@guid='a63f9c4c-cmd-callp-3a15c783d5d2'])"">
        <itemData guid=""a63f9c4c-cmd-callp-3a15c783d5d2"" type=""button"" dynamicCategory=""2cc24a3e-fe24-4708-9a74-9c75406eebcd"" dynamicCommand=""ConvertAllToCurves.Converter.CurveAllPages"" userCaption=""Curve All Pages"" userToolTip=""Convert all text across all pages to curves"" noBmpOnMenu=""true""/>
      </xsl:if>
      <xsl:if test=""not(./itemData[@guid='a63f9c4c-cmd-csele-3a15c783d5d3'])"">
        <itemData guid=""a63f9c4c-cmd-csele-3a15c783d5d3"" type=""button"" dynamicCategory=""2cc24a3e-fe24-4708-9a74-9c75406eebcd"" dynamicCommand=""ConvertAllToCurves.Converter.CurveSelection"" userCaption=""Curve Selected Objects"" userToolTip=""Convert selected text objects to curves"" noBmpOnMenu=""true""/>
      </xsl:if>

      <!-- Feature 2: Export -->
      <xsl:if test=""not(./itemData[@guid='a63f9c4c-flyout-export-3a15c783d5d2'])"">
        <itemData guid=""a63f9c4c-flyout-export-3a15c783d5d2"" flyoutBarRef=""a63f9c4c-cbar-export-3a15c783d5d2"" type=""flyout"" userCaption=""CorelMate Export"" userToolTip=""Export as ID Card or Print PDF"" userCreated=""true"" noBmpOnMenu=""true""/>
      </xsl:if>
      <xsl:if test=""not(./itemData[@guid='a63f9c4c-cmd-eidcd-3a15c783d5d4'])"">
        <itemData guid=""a63f9c4c-cmd-eidcd-3a15c783d5d4"" type=""button"" dynamicCategory=""2cc24a3e-fe24-4708-9a74-9c75406eebcd"" dynamicCommand=""ConvertAllToCurves.Converter.ExportAsIdCard"" userCaption=""Export As ID Card"" userToolTip=""Export ID card metadata (JSON) and editable vector PDF template"" noBmpOnMenu=""true""/>
      </xsl:if>
      <xsl:if test=""not(./itemData[@guid='a63f9c4c-cmd-eprnt-3a15c783d5d5'])"">
        <itemData guid=""a63f9c4c-cmd-eprnt-3a15c783d5d5"" type=""button"" dynamicCategory=""2cc24a3e-fe24-4708-9a74-9c75406eebcd"" dynamicCommand=""ConvertAllToCurves.Converter.ExportAsPrint"" userCaption=""Export As Print"" userToolTip=""Export print-ready CMYK PDF with text curved"" noBmpOnMenu=""true""/>
      </xsl:if>

      <!-- Feature 3: Color -->
      <xsl:if test=""not(./itemData[@guid='a63f9c4c-flyout-color-3a15c783d5d3'])"">
        <itemData guid=""a63f9c4c-flyout-color-3a15c783d5d3"" flyoutBarRef=""a63f9c4c-cbar-color-3a15c783d5d3"" type=""flyout"" userCaption=""CorelMate Color"" userToolTip=""Convert colors and bitmaps to CMYK or RGB"" userCreated=""true"" noBmpOnMenu=""true""/>
      </xsl:if>
      <xsl:if test=""not(./itemData[@guid='a63f9c4c-cmd-ccmyk-3a15c783d5d6'])"">
        <itemData guid=""a63f9c4c-cmd-ccmyk-3a15c783d5d6"" type=""button"" dynamicCategory=""2cc24a3e-fe24-4708-9a74-9c75406eebcd"" dynamicCommand=""ConvertAllToCurves.Converter.ConvertToCmyk"" userCaption=""Convert to CMYK"" userToolTip=""Convert all vector colors and bitmap images to CMYK"" noBmpOnMenu=""true""/>
      </xsl:if>
      <xsl:if test=""not(./itemData[@guid='a63f9c4c-cmd-crgbb-3a15c783d5d7'])"">
        <itemData guid=""a63f9c4c-cmd-crgbb-3a15c783d5d7"" type=""button"" dynamicCategory=""2cc24a3e-fe24-4708-9a74-9c75406eebcd"" dynamicCommand=""ConvertAllToCurves.Converter.ConvertToRgb"" userCaption=""Convert to RGB"" userToolTip=""Convert all vector colors and bitmap images to RGB"" noBmpOnMenu=""true""/>
      </xsl:if>
    </xsl:copy>
  </xsl:template>

  <!-- Append CommandBars for the 3 flyout menus -->
  <xsl:template match=""uiConfig/commandBars"">
    <xsl:copy>
      <xsl:apply-templates select=""node()|@*""/>

      <!-- Curves command bar -->
      <xsl:if test=""not(./commandBarData[@guid='a63f9c4c-cbar-curves-3a15c783d5d1'])"">
        <commandBarData guid=""a63f9c4c-cbar-curves-3a15c783d5d1"" type=""menu"" nonLocalizableName=""CorelMate Curves"" flyout=""true"">
          <menu>
            <item guidRef=""a63f9c4c-cmd-cpage-3a15c783d5d1""/>
            <item guidRef=""a63f9c4c-cmd-callp-3a15c783d5d2""/>
            <item guidRef=""a63f9c4c-cmd-csele-3a15c783d5d3""/>
          </menu>
        </commandBarData>
      </xsl:if>

      <!-- Export command bar -->
      <xsl:if test=""not(./commandBarData[@guid='a63f9c4c-cbar-export-3a15c783d5d2'])"">
        <commandBarData guid=""a63f9c4c-cbar-export-3a15c783d5d2"" type=""menu"" nonLocalizableName=""CorelMate Export"" flyout=""true"">
          <menu>
            <item guidRef=""a63f9c4c-cmd-eidcd-3a15c783d5d4""/>
            <item guidRef=""a63f9c4c-cmd-eprnt-3a15c783d5d5""/>
          </menu>
        </commandBarData>
      </xsl:if>

      <!-- Color command bar -->
      <xsl:if test=""not(./commandBarData[@guid='a63f9c4c-cbar-color-3a15c783d5d3'])"">
        <commandBarData guid=""a63f9c4c-cbar-color-3a15c783d5d3"" type=""menu"" nonLocalizableName=""CorelMate Color"" flyout=""true"">
          <menu>
            <item guidRef=""a63f9c4c-cmd-ccmyk-3a15c783d5d6""/>
            <item guidRef=""a63f9c4c-cmd-crgbb-3a15c783d5d7""/>
          </menu>
        </commandBarData>
      </xsl:if>
    </xsl:copy>
  </xsl:template>

  <!-- Append the 3 items to menubar -->
  <xsl:template match=""//commandBarData[@guid='f3016f3c-2847-4557-b61a-a2d05319cf18']/menubar/modeData"">
    <xsl:copy>
      <xsl:apply-templates select=""node()|@*""/>
      <xsl:if test=""not(./item[@guidRef='a63f9c4c-flyout-curves-3a15c783d5d1'])"">
        <item guidRef=""a63f9c4c-flyout-curves-3a15c783d5d1""/>
      </xsl:if>
      <xsl:if test=""not(./item[@guidRef='a63f9c4c-flyout-export-3a15c783d5d2'])"">
        <item guidRef=""a63f9c4c-flyout-export-3a15c783d5d2""/>
      </xsl:if>
      <xsl:if test=""not(./item[@guidRef='a63f9c4c-flyout-color-3a15c783d5d3'])"">
        <item guidRef=""a63f9c4c-flyout-color-3a15c783d5d3""/>
      </xsl:if>
    </xsl:copy>
  </xsl:template>
</xsl:stylesheet>";
    }

    public static string GenerateGmsMacroContent()
    {
        return @"' CorelMate GMS Integration Macro
' Auto-generated companion for CorelMate Main Menu Navigation & Features
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

Public Sub ConvertPageToCurves()
    If ActiveDocument Is Nothing Then MsgBox ""No active document in CorelDRAW."", vbExclamation, ""CorelMate"": Exit Sub
    ActiveDocument.BeginCommandGroup ""CorelMate Convert Page to Curves""
    Optimization = True
    EventsEnabled = False
    ActiveDocument.SaveSettings
    ActiveDocument.PreserveSelection = False
    On Error Resume Next
    ConvertShapesRangeToCurves ActivePage.Shapes.All
    ActiveDocument.PreserveSelection = True
    ActiveDocument.RestoreSettings
    EventsEnabled = True
    Optimization = False
    ActiveDocument.EndCommandGroup
    Application.CorelScript.RedrawScreen
    MsgBox ""Current page text converted to curves."", vbInformation, ""CorelMate""
End Sub

Public Sub ConvertAllPagesToCurves()
    Dim p As Page, c&
    If ActiveDocument Is Nothing Then MsgBox ""No active document in CorelDRAW."", vbExclamation, ""CorelMate"": Exit Sub
    ActiveDocument.BeginCommandGroup ""CorelMate Convert All Pages to Curves""
    Optimization = True
    EventsEnabled = False
    ActiveDocument.SaveSettings
    ActiveDocument.PreserveSelection = False
    On Error Resume Next
    For c = 1 To ActiveDocument.Pages.Count
        Set p = ActiveDocument.Pages(c)
        ConvertShapesRangeToCurves p.Shapes.All
    Next c
    ActiveDocument.PreserveSelection = True
    ActiveDocument.RestoreSettings
    EventsEnabled = True
    Optimization = False
    ActiveDocument.EndCommandGroup
    Application.CorelScript.RedrawScreen
    MsgBox ""All pages converted to curves."", vbInformation, ""CorelMate""
End Sub

Public Sub ConvertSelectionToCurves()
    If ActiveDocument Is Nothing Then MsgBox ""No active document in CorelDRAW."", vbExclamation, ""CorelMate"": Exit Sub
    If ActiveSelectionRange.Count = 0 Then MsgBox ""Select artwork first."", vbExclamation, ""CorelMate"": Exit Sub
    ActiveDocument.BeginCommandGroup ""CorelMate Convert Selection to Curves""
    Optimization = True
    EventsEnabled = False
    ActiveDocument.SaveSettings
    ActiveDocument.PreserveSelection = False
    On Error Resume Next
    ConvertShapesRangeToCurves ActiveSelectionRange
    ActiveDocument.PreserveSelection = True
    ActiveDocument.RestoreSettings
    EventsEnabled = True
    Optimization = False
    ActiveDocument.EndCommandGroup
    Application.CorelScript.RedrawScreen
    MsgBox ""Selected text converted to curves."", vbInformation, ""CorelMate""
End Sub

Private Sub ConvertShapesRangeToCurves(r As ShapeRange)
    Dim s As Shape
    On Error Resume Next
    For Each s In r
        If s.Type = cdrGroupShape Then
            ConvertShapesRangeToCurves s.Shapes.All
        ElseIf s.Type = cdrTextShape Then
            s.ConvertToCurves
        End If
        If Not s.PowerClip Is Nothing Then
            ConvertShapesRangeToCurves s.PowerClip.Shapes.All
        End If
    Next s
End Sub
";
    }

    public static void PatchWorkspaceXml(XmlDocument xml, string uiAssemblyPath)
        => CorelWorkspacePatcher.PatchWorkspaceXml(xml, uiAssemblyPath);

    public static bool PatchWorkspaceArchive(string cdwsPath, string uiAssemblyPath, out string message)
        => CorelWorkspacePatcher.PatchWorkspaceArchive(cdwsPath, uiAssemblyPath, out message);

    public static bool PatchAllUserWorkspaces(string uiAssemblyPath, out string message)
        => CorelWorkspacePatcher.PatchAllUserWorkspaces(uiAssemblyPath, out message);

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
