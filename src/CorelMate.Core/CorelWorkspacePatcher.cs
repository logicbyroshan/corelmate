using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace CorelMate.Core;

public static class CorelWorkspacePatcher
{
    public const string DockerGuid = "{A63F9C4C-64A3-4E91-9AC9-3A15C783D5D7}";
    public const string DockerClassName = "CorelMate.UI.CorelMatePanel";
    public const string DockerHostedItemGuid = "eaa2648a-69d1-46dd-8527-9cd9a00c467f";

    public const string MenuBarGuid = "f3016f3c-2847-4557-b61a-a2d05319cf18";
    public const string CorelMateMenuFlyoutGuid = "a63f9c4c-4e91-menu-9ac9-3a15c783d5d7";
    public const string CorelMateCommandBarGuid = "a63f9c4c-cbef-4276-b686-adb4f77c80d6";

    // Legacy Item GUIDs (retained for binary/library compatibility)
    public const string ItemShowDockerGuid        = "a63f9c4c-item-dock-9ac9-3a15c783d5d4";
    public const string ItemConvertPageCurvesGuid = "a63f9c4c-cmd-cpage-3a15c783d5d1";
    public const string ItemConvertAllCurvesGuid  = "a63f9c4c-cmd-callp-3a15c783d5d2";
    public const string ItemConvertSelCurvesGuid  = "a63f9c4c-cmd-csele-3a15c783d5d3";
    public const string ItemInspectorGuid         = "a63f9c4c-item-inspect-9ac9-3a15c783d5d5";
    public const string ItemBadgesGuid            = "a63f9c4c-item-badge-9ac9-3a15c783d5d6";
    public const string ItemAboutGuid             = "a63f9c4c-item-about-9ac9-3a15c783d5d7";

    // Feature 1: Curves
    public const string FlyoutCurvesGuid        = "a63f9c4c-flyout-curves-3a15c783d5d1";
    public const string CBarCurvesGuid          = "a63f9c4c-cbar-curves-3a15c783d5d1";
    public const string CmdCurveCurrentPageGuid = "a63f9c4c-cmd-cpage-3a15c783d5d1";
    public const string CmdCurveAllPagesGuid    = "a63f9c4c-cmd-callp-3a15c783d5d2";
    public const string CmdCurveSelectionGuid   = "a63f9c4c-cmd-csele-3a15c783d5d3";

    // Feature 2: Export
    public const string FlyoutExportGuid        = "a63f9c4c-flyout-export-3a15c783d5d2";
    public const string CBarExportGuid          = "a63f9c4c-cbar-export-3a15c783d5d2";
    public const string CmdExportIdCardGuid     = "a63f9c4c-cmd-eidcd-3a15c783d5d4";
    public const string CmdExportPrintGuid      = "a63f9c4c-cmd-eprnt-3a15c783d5d5";

    // Feature 3: Color
    public const string FlyoutColorGuid         = "a63f9c4c-flyout-color-3a15c783d5d3";
    public const string CBarColorGuid           = "a63f9c4c-cbar-color-3a15c783d5d3";
    public const string CmdColorCmykGuid        = "a63f9c4c-cmd-ccmyk-3a15c783d5d6";
    public const string CmdColorRgbGuid         = "a63f9c4c-cmd-crgbb-3a15c783d5d7";

    // Macro Category GUID in CorelDRAW
    public const string MacroCategoryGuid = "2cc24a3e-fe24-4708-9a74-9c75406eebcd";

    // CorelDRAW Workspace Menubar & DockHost GUIDs
    public const string ModeWelcomeGuid = "6d1d9e94-13f3-4517-a976-ba9154671db4";
    public const string ModeDocOpenGuid = "76d73481-9076-44c9-821c-52de9408cce2";
    public const string HelpItemGuid    = "71f8ffd6-46bd-43a3-8256-5412bc2d7741";
    public const string WindowMenuModeGuid = "6c91d5ab-d648-4364-96fb-3e71bcfaf70d";
    public const string RightDockTrayGuid = "bf2668a0-d25d-4eb8-97c6-c0f9fe6e7516";
    public const string SeparatorItemGuid = "266435b4-6e53-460f-9fa7-f45be187d400";

    public static void PatchWorkspaceXml(XmlDocument xml, string uiAssemblyPath)
    {
        if (xml == null) throw new ArgumentNullException(nameof(xml));

        // 1. Explicitly REMOVE any Docker registration (No Docker required)
        var dockerNode = xml.SelectSingleNode($"//dockers/dockerData[@guid='{DockerGuid}']");
        dockerNode?.ParentNode?.RemoveChild(dockerNode);

        var trayDocker = xml.SelectSingleNode($"//viewHost[@guid='{RightDockTrayGuid}']/dockerData[@guidRef='{DockerGuid}']");
        trayDocker?.ParentNode?.RemoveChild(trayDocker);

        var winDockerItem = xml.SelectSingleNode($"//modeData[@guid='{WindowMenuModeGuid}']/item[@guidRef='{ItemShowDockerGuid}']");
        winDockerItem?.ParentNode?.RemoveChild(winDockerItem);

        // 2. Ensure <items> exists
        var itemsNode = xml.SelectSingleNode("//items") as XmlElement;
        if (itemsNode == null)
        {
            itemsNode = xml.CreateElement("items");
            xml.DocumentElement?.AppendChild(itemsNode);
        }

        if (itemsNode != null)
        {
            // Remove all legacy docker and obsolete menu items
            string[] legacyItemGuids = {
                DockerHostedItemGuid,
                ItemShowDockerGuid,
                "a63f9c4c-item-page-9ac9-3a15c783d5d1",
                "a63f9c4c-item-pall-9ac9-3a15c783d5d2",
                "a63f9c4c-item-psel-9ac9-3a15c783d5d3",
                "a63f9c4c-item-inspect-9ac9-3a15c783d5d5",
                "a63f9c4c-item-badge-9ac9-3a15c783d5d6",
                "a63f9c4c-item-about-9ac9-3a15c783d5d7"
            };
            foreach (var legId in legacyItemGuids)
            {
                var oldNode = itemsNode.SelectSingleNode($"itemData[@guid='{legId}']");
                oldNode?.ParentNode?.RemoveChild(oldNode);
            }

            // Master CorelMate flyout item (on Main Navbar)
            EnsureItemData(itemsNode, CorelMateMenuFlyoutGuid, new Dictionary<string, string>
            {
                ["type"] = "flyout",
                ["flyoutBarRef"] = CorelMateCommandBarGuid,
                ["userCaption"] = "CorelMate",
                ["userToolTip"] = "CorelMate Productivity Suite",
                ["userCreated"] = "true"
            }, CorelMateIcons.CorelMate);

            // --- FEATURE 1: CURVES ---
            EnsureItemData(itemsNode, FlyoutCurvesGuid, new Dictionary<string, string>
            {
                ["type"] = "flyout",
                ["flyoutBarRef"] = CBarCurvesGuid,
                ["userCaption"] = "Curves",
                ["userToolTip"] = "Text-to-Curves Operations",
                ["userCreated"] = "true"
            }, CorelMateIcons.Curves);
            EnsureItemData(itemsNode, CmdCurveCurrentPageGuid, new Dictionary<string, string>
            {
                ["type"] = "button",
                ["dynamicCommand"] = "GlobalMacros.CorelMate.CurveCurrentPage",
                ["dynamicCategory"] = MacroCategoryGuid,
                ["userCaption"] = "Curve Current Page",
                ["userToolTip"] = "Convert all text on current page to curves"
            }, CorelMateIcons.CurveCurrentPage);
            EnsureItemData(itemsNode, CmdCurveAllPagesGuid, new Dictionary<string, string>
            {
                ["type"] = "button",
                ["dynamicCommand"] = "GlobalMacros.CorelMate.CurveAllPages",
                ["dynamicCategory"] = MacroCategoryGuid,
                ["userCaption"] = "Curve All Pages",
                ["userToolTip"] = "Convert all text across all pages to curves"
            }, CorelMateIcons.CurveAllPages);
            EnsureItemData(itemsNode, CmdCurveSelectionGuid, new Dictionary<string, string>
            {
                ["type"] = "button",
                ["dynamicCommand"] = "GlobalMacros.CorelMate.CurveSelection",
                ["dynamicCategory"] = MacroCategoryGuid,
                ["userCaption"] = "Curve Selected Objects",
                ["userToolTip"] = "Convert selected text objects to curves"
            }, CorelMateIcons.CurveSelection);

            // --- FEATURE 2: EXPORT ---
            EnsureItemData(itemsNode, FlyoutExportGuid, new Dictionary<string, string>
            {
                ["type"] = "flyout",
                ["flyoutBarRef"] = CBarExportGuid,
                ["userCaption"] = "Export",
                ["userToolTip"] = "Export as ID Card or Print PDF",
                ["userCreated"] = "true"
            }, CorelMateIcons.Export);
            EnsureItemData(itemsNode, CmdExportIdCardGuid, new Dictionary<string, string>
            {
                ["type"] = "button",
                ["dynamicCommand"] = "GlobalMacros.CorelMate.ExportAsIdCard",
                ["dynamicCategory"] = MacroCategoryGuid,
                ["userCaption"] = "Export As ID Card",
                ["userToolTip"] = "Export ID card template metadata (JSON) and editable vector PDF"
            }, CorelMateIcons.ExportIdCard);
            EnsureItemData(itemsNode, CmdExportPrintGuid, new Dictionary<string, string>
            {
                ["type"] = "button",
                ["dynamicCommand"] = "GlobalMacros.CorelMate.ExportAsPrint",
                ["dynamicCategory"] = MacroCategoryGuid,
                ["userCaption"] = "Export As Print",
                ["userToolTip"] = "Export print-ready CMYK PDF with text curved"
            }, CorelMateIcons.ExportPrint);

            // --- FEATURE 3: COLOR ---
            EnsureItemData(itemsNode, FlyoutColorGuid, new Dictionary<string, string>
            {
                ["type"] = "flyout",
                ["flyoutBarRef"] = CBarColorGuid,
                ["userCaption"] = "Color",
                ["userToolTip"] = "Convert colors and bitmaps to CMYK or RGB",
                ["userCreated"] = "true"
            }, CorelMateIcons.Color);
            EnsureItemData(itemsNode, CmdColorCmykGuid, new Dictionary<string, string>
            {
                ["type"] = "button",
                ["dynamicCommand"] = "GlobalMacros.CorelMate.ConvertToCmyk",
                ["dynamicCategory"] = MacroCategoryGuid,
                ["userCaption"] = "Convert to CMYK",
                ["userToolTip"] = "Convert all vector colors and bitmap images to CMYK"
            }, CorelMateIcons.ConvertCmyk);
            EnsureItemData(itemsNode, CmdColorRgbGuid, new Dictionary<string, string>
            {
                ["type"] = "button",
                ["dynamicCommand"] = "GlobalMacros.CorelMate.ConvertToRgb",
                ["dynamicCategory"] = MacroCategoryGuid,
                ["userCaption"] = "Convert to RGB",
                ["userToolTip"] = "Convert all vector colors and bitmap images to RGB"
            }, CorelMateIcons.ConvertRgb);
        }

        // 3. Ensure <commandBars> and flyout menus exist
        var cbs = xml.SelectSingleNode("//commandBars") as XmlElement;
        if (cbs == null)
        {
            cbs = xml.CreateElement("commandBars");
            xml.DocumentElement?.AppendChild(cbs);
        }

        if (cbs != null)
        {
            // 3a. Master CorelMate command bar (contains Curves, Export, Color)
            EnsureCommandBarMenu(cbs, CorelMateCommandBarGuid, "CorelMate", new[]
            {
                FlyoutCurvesGuid,
                FlyoutExportGuid,
                FlyoutColorGuid
            });

            // 3b. Curves command bar
            EnsureCommandBarMenu(cbs, CBarCurvesGuid, "Curves", new[]
            {
                CmdCurveCurrentPageGuid,
                CmdCurveAllPagesGuid,
                CmdCurveSelectionGuid
            });

            // 3c. Export command bar
            EnsureCommandBarMenu(cbs, CBarExportGuid, "Export", new[]
            {
                CmdExportIdCardGuid,
                CmdExportPrintGuid
            });

            // 3d. Color command bar
            EnsureCommandBarMenu(cbs, CBarColorGuid, "Color", new[]
            {
                CmdColorCmykGuid,
                CmdColorRgbGuid
            });
        }

        // 4. Inject single 'CorelMate' menu into Navigation Bar BEFORE Help
        string[] modes = { ModeWelcomeGuid, ModeDocOpenGuid };
        string[] separateFlyouts = { FlyoutCurvesGuid, FlyoutExportGuid, FlyoutColorGuid };
        foreach (var mGuid in modes)
        {
            var modeNode = xml.SelectSingleNode($"//commandBarData[@guid='{MenuBarGuid}']/menubar/modeData[@guid='{mGuid}']") as XmlElement;
            if (modeNode != null)
            {
                // Remove individual flyouts if previously added to main menubar
                foreach (var sepId in separateFlyouts)
                {
                    var sepNode = modeNode.SelectSingleNode($"item[@guidRef='{sepId}']");
                    sepNode?.ParentNode?.RemoveChild(sepNode);
                }

                // Remove existing CorelMate item so we re-insert cleanly before Help
                var existingMaster = modeNode.SelectSingleNode($"item[@guidRef='{CorelMateMenuFlyoutGuid}']");
                existingMaster?.ParentNode?.RemoveChild(existingMaster);

                var newMaster = xml.CreateElement("item");
                newMaster.SetAttribute("guidRef", CorelMateMenuFlyoutGuid);

                // Find Help item to insert BEFORE it
                var helpNode = modeNode.SelectSingleNode($"item[@guidRef='{HelpItemGuid}']");
                if (helpNode != null)
                {
                    modeNode.InsertBefore(newMaster, helpNode);
                }
                else
                {
                    modeNode.AppendChild(newMaster);
                }
            }
        }
    }

    private static void EnsureCommandBarMenu(XmlElement commandBarsNode, string guid, string name, string[] itemGuids)
    {
        var cbNode = commandBarsNode.SelectSingleNode($"commandBarData[@guid='{guid}']") as XmlElement;
        if (cbNode == null)
        {
            cbNode = commandBarsNode.OwnerDocument.CreateElement("commandBarData");
            cbNode.SetAttribute("guid", guid);
            cbNode.SetAttribute("type", "menu");
            cbNode.SetAttribute("nonLocalizableName", name);
            cbNode.SetAttribute("flyout", "true");
            commandBarsNode.AppendChild(cbNode);
        }

        var menuNode = cbNode.SelectSingleNode("menu") as XmlElement;
        if (menuNode == null)
        {
            menuNode = cbNode.OwnerDocument.CreateElement("menu");
            cbNode.AppendChild(menuNode);
        }

        // Rebuild items in correct order
        menuNode.RemoveAll();
        foreach (var id in itemGuids)
        {
            var it = cbNode.OwnerDocument.CreateElement("item");
            it.SetAttribute("guidRef", id);
            menuNode.AppendChild(it);
        }
    }

    private static void EnsureItemData(XmlElement parent, string guid, Dictionary<string, string> attrs, string? iconBase64 = null)
    {
        var item = parent.SelectSingleNode($"itemData[@guid='{guid}']") as XmlElement;
        if (item == null)
        {
            item = parent.OwnerDocument.CreateElement("itemData");
            item.SetAttribute("guid", guid);
            parent.AppendChild(item);
        }
        foreach (var kvp in attrs)
        {
            item.SetAttribute(kvp.Key, kvp.Value);
        }

        // Ensure noBmpOnMenu is removed so CorelDRAW displays the icon on the menu
        item.RemoveAttribute("noBmpOnMenu");

        if (!string.IsNullOrEmpty(iconBase64))
        {
            var bmpNode = item.SelectSingleNode("userSmallBitmap") as XmlElement;
            if (bmpNode == null)
            {
                bmpNode = item.OwnerDocument.CreateElement("userSmallBitmap");
                bmpNode.SetAttribute("xmlns:dt", "urn:schemas-microsoft-com:datatypes");
                bmpNode.SetAttribute("dt:dt", "bin.base64");
                item.AppendChild(bmpNode);
            }
            bmpNode.InnerText = iconBase64!;
        }
    }

    public static bool PatchWorkspaceArchive(string cdwsPath, string uiAssemblyPath, out string message)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(cdwsPath) || !File.Exists(cdwsPath))
            {
                message = $"Workspace file '{cdwsPath}' does not exist.";
                return false;
            }

            var xml = new XmlDocument();
            using (var zip = ZipFile.Open(cdwsPath, ZipArchiveMode.Update))
            {
                var entry = zip.GetEntry("content/workspace.xml");
                if (entry == null)
                {
                    message = $"Archive '{cdwsPath}' does not contain 'content/workspace.xml'.";
                    return false;
                }

                using (var stream = entry.Open())
                {
                    xml.Load(stream);
                }

                PatchWorkspaceXml(xml, uiAssemblyPath);

                entry.Delete();

                var newEntry = zip.CreateEntry("content/workspace.xml", CompressionLevel.Optimal);
                using (var stream = newEntry.Open())
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    xml.Save(writer);
                }
            }

            message = $"Successfully patched workspace '{cdwsPath}'.";
            return true;
        }
        catch (Exception ex)
        {
            message = $"Failed to patch workspace '{cdwsPath}': {ex.Message}";
            return false;
        }
    }

    public static bool PatchAllUserWorkspaces(string uiAssemblyPath, out string message)
    {
        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var corelRoot = Path.Combine(appData, "Corel");
            if (!Directory.Exists(corelRoot))
            {
                message = "Corel AppData directory not found.";
                return false;
            }

            var cdwsFiles = Directory.GetFiles(corelRoot, "_default.cdws", SearchOption.AllDirectories);
            if (cdwsFiles.Length == 0)
            {
                message = "No active _default.cdws workspace files found in Corel AppData.";
                return false;
            }

            int count = 0;
            var sb = new StringBuilder();
            foreach (var file in cdwsFiles)
            {
                if (PatchWorkspaceArchive(file, uiAssemblyPath, out var patchMsg))
                {
                    count++;
                    sb.AppendLine(patchMsg);
                }
            }

            message = $"Patched {count} workspace file(s).\n{sb}";
            return count > 0;
        }
        catch (Exception ex)
        {
            message = $"Error during workspace scanning: {ex.Message}";
            return false;
        }
    }
}
