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

    // Item GUIDs for menu commands & dockers
    public const string ItemShowDockerGuid        = "a63f9c4c-item-dock-9ac9-3a15c783d5d4";
    public const string ItemConvertPageCurvesGuid = "a63f9c4c-item-page-9ac9-3a15c783d5d1";
    public const string ItemConvertAllCurvesGuid  = "a63f9c4c-item-pall-9ac9-3a15c783d5d2";
    public const string ItemConvertSelCurvesGuid  = "a63f9c4c-item-psel-9ac9-3a15c783d5d3";
    public const string ItemInspectorGuid         = "a63f9c4c-item-inspect-9ac9-3a15c783d5d5";
    public const string ItemBadgesGuid            = "a63f9c4c-item-badge-9ac9-3a15c783d5d6";
    public const string ItemAboutGuid             = "a63f9c4c-item-about-9ac9-3a15c783d5d7";

    // Standard CorelDRAW Menubar & DockHost GUIDs
    public const string ModeWelcomeGuid = "6d1d9e94-13f3-4517-a976-ba9154671db4";
    public const string ModeDocOpenGuid = "76d73481-9076-44c9-821c-52de9408cce2";
    public const string WindowMenuModeGuid = "6c91d5ab-d648-4364-96fb-3e71bcfaf70d";
    public const string RightDockTrayGuid = "bf2668a0-d25d-4eb8-97c6-c0f9fe6e7516";
    public const string SeparatorItemGuid = "266435b4-6e53-460f-9fa7-f45be187d400";

    public static void PatchWorkspaceXml(XmlDocument xml, string uiAssemblyPath)
    {
        if (xml == null) throw new ArgumentNullException(nameof(xml));

        // 1. Update or create <dockers>/<dockerData>
        var dockerNode = xml.SelectSingleNode($"//dockers/dockerData[@guid='{DockerGuid}']") as XmlElement;
        if (dockerNode != null)
        {
            dockerNode.SetAttribute("userCaption", "CorelMate");
            dockerNode.SetAttribute("caption", "CorelMate");
            dockerNode.SetAttribute("focusStyle", "noThrow");
        }
        else
        {
            var dockersNode = xml.SelectSingleNode("//dockers") as XmlElement;
            if (dockersNode == null)
            {
                dockersNode = xml.CreateElement("dockers");
                xml.DocumentElement?.AppendChild(dockersNode);
            }
            if (dockersNode != null)
            {
                var d = xml.CreateElement("dockerData");
                d.SetAttribute("guid", DockerGuid);
                d.SetAttribute("userCaption", "CorelMate");
                d.SetAttribute("caption", "CorelMate");
                d.SetAttribute("dock", "fill");
                d.SetAttribute("focusStyle", "noThrow");
                var c = xml.CreateElement("container");
                var it = xml.CreateElement("item");
                it.SetAttribute("dock", "fill");
                it.SetAttribute("guidRef", DockerHostedItemGuid);
                c.AppendChild(it);
                d.AppendChild(c);
                dockersNode.AppendChild(d);
            }
        }

        // 2. Ensure <items>
        var itemsNode = xml.SelectSingleNode("//items") as XmlElement;
        if (itemsNode == null)
        {
            itemsNode = xml.CreateElement("items");
            xml.DocumentElement?.AppendChild(itemsNode);
        }

        if (itemsNode != null)
        {
            EnsureItemData(itemsNode, DockerHostedItemGuid, new Dictionary<string, string>
            {
                ["type"] = "wpfhost",
                ["hostedType"] = $"{uiAssemblyPath},{DockerClassName}"
            });

            EnsureItemData(itemsNode, ItemShowDockerGuid, new Dictionary<string, string>
            {
                ["type"] = "checkButton",
                ["check"] = $"*Docker('{DockerGuid}')",
                ["userCaption"] = "CorelMate Docker",
                ["userToolTip"] = "Open or Toggle CorelMate Docker",
                ["noBmpOnMenu"] = "true"
            });

            EnsureItemData(itemsNode, CorelMateMenuFlyoutGuid, new Dictionary<string, string>
            {
                ["type"] = "flyout",
                ["flyoutBarRef"] = CorelMateCommandBarGuid,
                ["userCaption"] = "CorelMate",
                ["userToolTip"] = "CorelMate Productivity Suite",
                ["userCreated"] = "true",
                ["noBmpOnMenu"] = "true"
            });

            EnsureItemData(itemsNode, ItemConvertPageCurvesGuid, new Dictionary<string, string>
            {
                ["type"] = "button",
                ["userCaption"] = "Convert Page to Curves",
                ["userToolTip"] = "Convert all text objects on the current page to curves",
                ["noBmpOnMenu"] = "true"
            });

            EnsureItemData(itemsNode, ItemConvertAllCurvesGuid, new Dictionary<string, string>
            {
                ["type"] = "button",
                ["userCaption"] = "Convert All Pages to Curves",
                ["userToolTip"] = "Convert all text objects across all pages to curves",
                ["noBmpOnMenu"] = "true"
            });

            EnsureItemData(itemsNode, ItemConvertSelCurvesGuid, new Dictionary<string, string>
            {
                ["type"] = "button",
                ["userCaption"] = "Convert Selection to Curves",
                ["userToolTip"] = "Convert selected text objects to curves",
                ["noBmpOnMenu"] = "true"
            });

            EnsureItemData(itemsNode, ItemInspectorGuid, new Dictionary<string, string>
            {
                ["type"] = "button",
                ["userCaption"] = "Document Inspector",
                ["userToolTip"] = "Inspect document metrics and objects",
                ["noBmpOnMenu"] = "true"
            });

            EnsureItemData(itemsNode, ItemBadgesGuid, new Dictionary<string, string>
            {
                ["type"] = "button",
                ["userCaption"] = "Badge Generator",
                ["userToolTip"] = "Open Badge Generator",
                ["noBmpOnMenu"] = "true"
            });

            EnsureItemData(itemsNode, ItemAboutGuid, new Dictionary<string, string>
            {
                ["type"] = "button",
                ["userCaption"] = "About CorelMate",
                ["userToolTip"] = "About CorelMate",
                ["noBmpOnMenu"] = "true"
            });
        }

        // 3. Create or update flyout menu <commandBarData>
        var cbNode = xml.SelectSingleNode($"//commandBars/commandBarData[@guid='{CorelMateCommandBarGuid}']") as XmlElement;
        if (cbNode == null)
        {
            var cbs = xml.SelectSingleNode("//commandBars") as XmlElement;
            if (cbs == null)
            {
                cbs = xml.CreateElement("commandBars");
                xml.DocumentElement?.AppendChild(cbs);
            }

            if (cbs != null)
            {
                cbNode = xml.CreateElement("commandBarData");
                cbNode.SetAttribute("guid", CorelMateCommandBarGuid);
                cbNode.SetAttribute("type", "menu");
                cbNode.SetAttribute("nonLocalizableName", "CorelMate");
                cbNode.SetAttribute("flyout", "true");
                var m = xml.CreateElement("menu");

                string[] itemGuids =
                {
                    ItemShowDockerGuid,
                    SeparatorItemGuid,
                    ItemConvertPageCurvesGuid,
                    ItemConvertAllCurvesGuid,
                    ItemConvertSelCurvesGuid,
                    SeparatorItemGuid,
                    ItemInspectorGuid,
                    ItemBadgesGuid,
                    SeparatorItemGuid,
                    ItemAboutGuid
                };

                foreach (var id in itemGuids)
                {
                    var it = xml.CreateElement("item");
                    it.SetAttribute("guidRef", id);
                    m.AppendChild(it);
                }

                cbNode.AppendChild(m);
                cbs.AppendChild(cbNode);
            }
        }

        // 4. Inject into Main Navigation Bar modes (ModeWelcomeGuid & ModeDocOpenGuid)
        string[] modes = { ModeWelcomeGuid, ModeDocOpenGuid };
        foreach (var mGuid in modes)
        {
            var modeNode = xml.SelectSingleNode($"//commandBarData[@guid='{MenuBarGuid}']/menubar/modeData[@guid='{mGuid}']") as XmlElement;
            if (modeNode != null)
            {
                var existing = modeNode.SelectSingleNode($"item[@guidRef='{CorelMateMenuFlyoutGuid}']");
                if (existing == null)
                {
                    var newIt = xml.CreateElement("item");
                    newIt.SetAttribute("guidRef", CorelMateMenuFlyoutGuid);
                    modeNode.AppendChild(newIt);
                }
            }
        }

        // 5. Inject into Window menu modeData
        var winMode = xml.SelectSingleNode($"//modeData[@guid='{WindowMenuModeGuid}']") as XmlElement;
        if (winMode != null)
        {
            var existing = winMode.SelectSingleNode($"item[@guidRef='{ItemShowDockerGuid}']");
            if (existing == null)
            {
                var newIt = xml.CreateElement("item");
                newIt.SetAttribute("guidRef", ItemShowDockerGuid);
                winMode.AppendChild(newIt);
            }
        }

        // 6. Ensure in right dockers tray
        var tray = xml.SelectSingleNode($"//viewHost[@guid='{RightDockTrayGuid}']") as XmlElement;
        if (tray != null)
        {
            var existing = tray.SelectSingleNode($"dockerData[@guidRef='{DockerGuid}']");
            if (existing == null)
            {
                var dRef = xml.CreateElement("dockerData");
                dRef.SetAttribute("guidRef", DockerGuid);
                dRef.SetAttribute("category", "view+docker");
                tray.AppendChild(dRef);
            }
        }
    }

    private static void EnsureItemData(XmlElement parent, string guid, Dictionary<string, string> attrs)
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
