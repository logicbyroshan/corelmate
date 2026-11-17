using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using CorelMate.Badges;
using CorelMate.Core;
using CorelMate.Curves;
using CorelMate.Infrastructure;

// 1. Badge Grid calculation
var grid = BadgeGrid.ForQuantity(5, 2);
if (grid.Rows != 3 || grid.Capacity != 6) throw new InvalidOperationException("Badge grid calculation failed.");

// 2. Placeholder parser & replacement
var placeholders = PlaceholderParser.Parse("{{title}} - {{STANDARD}} - {{TITLE}}");
if (placeholders.Count != 3 || placeholders[0].Name != "TITLE") throw new InvalidOperationException("Placeholder parsing failed.");
var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["TITLE"] = "MONITOR", ["STANDARD"] = "NURSERY" };
if (PlaceholderParser.Replace("{{TITLE}} / {{STANDARD}}", values) != "MONITOR / NURSERY") throw new InvalidOperationException("Placeholder replacement failed.");

// 3. Badge Layout Engine planning
var settings = new BadgeLayoutSettings
{
	PageWidthMillimeters = 210,
	PageHeightMillimeters = 297,
	BadgeWidthMillimeters = 80,
	BadgeHeightMillimeters = 25,
	HorizontalGapMillimeters = 3,
	VerticalGapMillimeters = 3,
	LeftMarginMillimeters = 5,
	RightMarginMillimeters = 5,
	TopMarginMillimeters = 5,
	BottomMarginMillimeters = 5
};
var layout = BadgeLayoutEngine.Plan(settings, 18);
if (layout.Columns != 2 || layout.RowsPerPage != 10 || layout.Pages.Count != 1 || layout.Pages[0].Positions.Count != 18) throw new InvalidOperationException("Badge layout calculation failed.");

var multiPage = BadgeLayoutEngine.Plan(settings, 21);
if (multiPage.Pages.Count != 2 || multiPage.Pages[1].Positions.Count != 1) throw new InvalidOperationException("Multi-page layout calculation failed.");

try { PlaceholderParser.Parse("{{student name}}"); throw new InvalidOperationException("Invalid placeholder was accepted."); } catch (FormatException) { }
try { new BadgeDataRow(values, -1); throw new InvalidOperationException("Negative quantity was accepted."); } catch (ArgumentOutOfRangeException) { }

// 4. Text to Curves Preflight & Scope Models
var curveSummary = new CurveConversionSummary
{
	Scope = CurveConversionScope.CurrentPage,
	PagesProcessed = 1,
	FoundTextObjects = 12,
	ArtisticTextCount = 7,
	ParagraphTextCount = 5,
	TextOnPathCount = 2,
	GroupsInspected = 4,
	PowerClipsInspected = 2,
	ConvertibleTextObjects = 9,
	LockedTextObjects = 2,
	HiddenTextObjects = 1,
	UnsupportedTextObjects = 0,
	ConvertedTextObjects = 9,
	FailedTextObjects = 0,
	SavedCopyPath = @"C:\Artwork\Doc_curves.cdr",
	ExecutionDurationMs = 120
};
if (curveSummary.SkippedTextObjects != 3) throw new InvalidOperationException("Curve conversion skipped count failed.");
if (!curveSummary.HasTargets) throw new InvalidOperationException("Curve conversion HasTargets failed.");
if (curveSummary.Scope != CurveConversionScope.CurrentPage) throw new InvalidOperationException("Curve conversion scope failed.");

var options = new CurveConversionOptions
{
	Scope = CurveConversionScope.AllPages,
	SaveConvertedCopy = true,
	CopySuffix = "_backup",
	UnlockLockedShapes = true
};
if (options.Scope != CurveConversionScope.AllPages || !options.SaveConvertedCopy || options.CopySuffix != "_backup" || !options.UnlockLockedShapes)
{
	throw new InvalidOperationException("Curve conversion options initialization failed.");
}

var emptySummary = new CurveConversionSummary();
if (emptySummary.HasTargets || emptySummary.SkippedTextObjects != 0) throw new InvalidOperationException("Empty curve summary state failed.");

// 5. Version Compatibility Registry & Version Extraction Tests
if (CorelCompatibilityRegistry.ExtractMajorVersion("Version 27.1.0.129") != 27) throw new InvalidOperationException("Version extraction failed for v27.1.");
if (CorelCompatibilityRegistry.ExtractMajorVersion("CorelDRAW Graphics Suite 2024 v26.0") != 26) throw new InvalidOperationException("Version extraction failed for v26.");
if (CorelCompatibilityRegistry.ExtractMajorVersion("19.1.0.448") != 19) throw new InvalidOperationException("Version extraction failed for v19.");
if (CorelCompatibilityRegistry.ExtractMajorVersion("unknown") != 0) throw new InvalidOperationException("Version extraction should return 0 for unknown string.");

// Evaluate 2017 (v19) through 2026 (v27/v28)
for (var ver = 19; ver <= 28; ver++)
{
	var eval = CorelCompatibilityRegistry.Evaluate(ver);
	if (!eval.CanRun || eval.Status != CompatibilityStatus.Supported) throw new InvalidOperationException($"Supported version v{ver} was rejected.");
}

// Evaluate legacy (< 19)
var legacy = CorelCompatibilityRegistry.Evaluate(18);
if (legacy.CanRun || legacy.Status != CompatibilityStatus.UnsupportedLegacy) throw new InvalidOperationException("Legacy version v18 was unexpectedly supported.");

// Evaluate future (> 28)
var future = CorelCompatibilityRegistry.Evaluate(29);
if (future.CanRun || future.Status != CompatibilityStatus.UnsupportedFuture) throw new InvalidOperationException("Future version v29 was unexpectedly supported without compatibility update.");

// Era resolution
if (CorelCompatibilityRegistry.GetEraForVersion(19) != CorelEra.Classic2017) throw new InvalidOperationException("v19 era resolution failed.");
if (CorelCompatibilityRegistry.GetEraForVersion(21) != CorelEra.Charcoal2019) throw new InvalidOperationException("v21 era resolution failed.");
if (CorelCompatibilityRegistry.GetEraForVersion(23) != CorelEra.Teal2021) throw new InvalidOperationException("v23 era resolution failed.");
if (CorelCompatibilityRegistry.GetEraForVersion(25) != CorelEra.Azure2023) throw new InvalidOperationException("v25 era resolution failed.");
if (CorelCompatibilityRegistry.GetEraForVersion(27) != CorelEra.AdaptivePro2026) throw new InvalidOperationException("v27 era resolution failed.");

// 6. Document Metrics model tests
var metrics = new DocumentMetrics(
	"TestDoc.cdr",
	@"C:\Docs\TestDoc.cdr",
	3,
	210.0,
	297.0,
	"CMYK",
	45,
	12,
	20,
	5,
	3,
	5,
	2,
	4,
	new[] { "Arial", "Impact", "Segoe UI" });
if (metrics.TotalShapes != 45 || metrics.UniqueFonts.Count != 3 || !metrics.DimensionsString.Contains("210")) throw new InvalidOperationException("Document metrics failed.");

// 7. Logger tests
var tempRoot = Path.Combine(Path.GetTempPath(), "CorelMate-tests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(tempRoot);
try
{
	var logFile = Path.Combine(tempRoot, "test.log");
	var logger = new FileCorelMateLogger(logFile);
	logger.Info("Test info message");
	logger.Error("Test error message", new InvalidOperationException("Sample failure"));
	if (!File.Exists(logFile)) throw new InvalidOperationException("Log file was not created.");
	var logText = File.ReadAllText(logFile);
	if (!logText.Contains("Test info message") || !logText.Contains("Sample failure")) throw new InvalidOperationException("Log contents verification failed.");

	// 8. CSV & XLSX Import Tests
	var csvPath = Path.Combine(tempRoot, "badges.csv");
	File.WriteAllText(csvPath, "TITLE,STANDARD,QUANTITY,EXTRA\r\n\"Senior, Monitor\",\"वरिष्ठ मॉनिटर\",2,ignored\r\n\"Monitor\",Monitor,8,ignored\r\n\r\n\"Say \"\"Hi\"\"\",Test,1,ignored\r\n", new UTF8Encoding(false));
	var csv = CsvImportSource.Read(csvPath);
	var csvValidation = BadgeImportMapper.Validate(csv, new[] { "TITLE", "STANDARD" });
	if (!csvValidation.IsValid || csvValidation.Rows.Count != 3 || csvValidation.Rows[0].GetValue("TITLE") != "Senior, Monitor" || csvValidation.Rows[0].GetValue("STANDARD") != "वरिष्ठ मॉनिटर" || csvValidation.UnusedColumns.Count != 1) throw new InvalidOperationException("CSV import failed.");

	var duplicatePath = Path.Combine(tempRoot, "duplicate.csv");
	File.WriteAllText(duplicatePath, "TITLE,TITLE,QUANTITY\r\nA,B,1\r\n", new UTF8Encoding(false));
	var duplicateValidation = BadgeImportMapper.Validate(CsvImportSource.Read(duplicatePath), new[] { "TITLE" });
	if (duplicateValidation.IsValid || duplicateValidation.Errors.Count == 0) throw new InvalidOperationException("Duplicate CSV header validation failed.");

	var invalidPath = Path.Combine(tempRoot, "invalid.csv");
	File.WriteAllText(invalidPath, "TITLE,STANDARD,QUANTITY\nA,,2.5\n", new UTF8Encoding(false));
	var invalidValidation = BadgeImportMapper.Validate(CsvImportSource.Read(invalidPath), new[] { "TITLE", "STANDARD" });
	if (invalidValidation.IsValid || invalidValidation.Errors.Count != 2) throw new InvalidOperationException("CSV row validation failed.");

	var xlsxPath = Path.Combine(tempRoot, "badges.xlsx");
	CreateSyntheticXlsx(xlsxPath);
	var xlsx = XlsxImportSource.Read(xlsxPath);
	var xlsxValidation = BadgeImportMapper.Validate(xlsx, new[] { "TITLE", "STANDARD" });
	if (!xlsxValidation.IsValid || xlsx.WorksheetName != "Badges" || xlsxValidation.Rows.Count != 2 || xlsxValidation.Rows[1].GetValue("TITLE") != "मॉनिटर") throw new InvalidOperationException("XLSX import failed.");

	// 9. Workspace Patcher XML & Archive Tests
	var rawWsXml = @"<uiConfig>
	<commandBars>
		<commandBarData guid=""f3016f3c-2847-4557-b61a-a2d05319cf18"">
			<menubar>
				<modeData guid=""6d1d9e94-13f3-4517-a976-ba9154671db4""><item guidRef=""7aa80134-8e64-42d1-be0a-c7ec7f6c441f"" /><item guidRef=""71f8ffd6-46bd-43a3-8256-5412bc2d7741"" /></modeData>
				<modeData guid=""76d73481-9076-44c9-821c-52de9408cce2""><item guidRef=""7aa80134-8e64-42d1-be0a-c7ec7f6c441f"" /><item guidRef=""71f8ffd6-46bd-43a3-8256-5412bc2d7741"" /></modeData>
			</menubar>
		</commandBarData>
	</commandBars>
	<commandBars><modeData guid=""6c91d5ab-d648-4364-96fb-3e71bcfaf70d""><item guidRef=""existing-win"" /></modeData></commandBars>
	<layout><dockHost><viewHost guid=""bf2668a0-d25d-4eb8-97c6-c0f9fe6e7516"" category=""view+docker"" /></dockHost></layout>
	<dockers><dockerData guid=""{A63F9C4C-64A3-4E91-9AC9-3A15C783D5D7}"" userCaption=""CorelMate.UI.CorelMatePanel"" dock=""fill"" /></dockers>
	<items><itemData guid=""eaa2648a-69d1-46dd-8527-9cd9a00c467f"" type=""wpfhost"" /></items>
</uiConfig>";
	var wsXmlDoc = new System.Xml.XmlDocument();
	wsXmlDoc.LoadXml(rawWsXml);
	CorelWorkspacePatcher.PatchWorkspaceXml(wsXmlDoc, @"C:\CorelMate\CorelMate.UI.dll");

	var dNode = wsXmlDoc.SelectSingleNode($"//dockers/dockerData[@guid='{CorelWorkspacePatcher.DockerGuid}']") as System.Xml.XmlElement;
	if (dNode != null) throw new InvalidOperationException("Workspace dockerData should be removed for zero-docker mode.");

	var menubarMaster = wsXmlDoc.SelectSingleNode($"//commandBarData[@guid='{CorelWorkspacePatcher.MenuBarGuid}']/menubar/modeData[@guid='{CorelWorkspacePatcher.ModeDocOpenGuid}']/item[@guidRef='{CorelWorkspacePatcher.CorelMateMenuFlyoutGuid}']") as System.Xml.XmlElement;
	if (menubarMaster == null) throw new InvalidOperationException("Workspace menubar CorelMate master flyout item patch failed.");

	// Verify CorelMate is placed BEFORE Help
	var nextSibling = menubarMaster.NextSibling as System.Xml.XmlElement;
	if (nextSibling == null || nextSibling.GetAttribute("guidRef") != CorelWorkspacePatcher.HelpItemGuid)
	{
		throw new InvalidOperationException("CorelMate master menu item must be placed immediately before Help.");
	}

	// Verify master command bar contains the 3 sub-flyouts
	var masterCb = wsXmlDoc.SelectSingleNode($"//commandBars/commandBarData[@guid='{CorelWorkspacePatcher.CorelMateCommandBarGuid}']/menu");
	if (masterCb == null || masterCb.ChildNodes.Count != 3)
	{
		throw new InvalidOperationException("Master CorelMate command bar must contain the 3 sub-flyouts (Curves, Export, Color).");
	}

	var cmdItem = wsXmlDoc.SelectSingleNode($"//items/itemData[@guid='{CorelWorkspacePatcher.CmdCurveCurrentPageGuid}']") as System.Xml.XmlElement;
	if (cmdItem == null || cmdItem.GetAttribute("dynamicCommand") != "GlobalMacros.CorelMate.CurveCurrentPage")
	{
		throw new InvalidOperationException("Workspace dynamic macro command item patch failed.");
	}

	var bmpNode = cmdItem.SelectSingleNode("userSmallBitmap") as System.Xml.XmlElement;
	if (bmpNode == null || string.IsNullOrWhiteSpace(bmpNode.InnerText))
	{
		throw new InvalidOperationException("Item icon userSmallBitmap was not attached.");
	}

	// 10. Workspace Archive .cdws In-Place Patch Test
	var syntheticCdws = Path.Combine(tempRoot, "test_ws.cdws");
	using (var arch = ZipFile.Open(syntheticCdws, ZipArchiveMode.Create))
	{
		AddEntry(arch, "mimetype", "application/vnd.corel.draw.workspace");
		AddEntry(arch, "content/workspace.xml", rawWsXml);
	}
	if (!CorelWorkspacePatcher.PatchWorkspaceArchive(syntheticCdws, @"C:\CorelMate\CorelMate.UI.dll", out var patchMsg))
	{
		throw new InvalidOperationException("Workspace archive patch returned false: " + patchMsg);
	}

	using (var readArch = ZipFile.OpenRead(syntheticCdws))
	{
		var entry = readArch.GetEntry("content/workspace.xml");
		if (entry == null) throw new InvalidOperationException("Patched archive missing content/workspace.xml");
		using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
		var patchedContent = reader.ReadToEnd();
		if (!patchedContent.Contains(CorelWorkspacePatcher.CorelMateMenuFlyoutGuid))
		{
			throw new InvalidOperationException("Patched archive content verification failed.");
		}
	}
}
finally
{
	if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
}

Console.WriteLine("All CorelMate.Tests passed successfully.");

static void CreateSyntheticXlsx(string path)
{
	using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
	{
		AddEntry(archive, "xl/workbook.xml", "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Badges\" sheetId=\"1\" r:id=\"rId1\" /></sheets></workbook>");
		AddEntry(archive, "xl/_rels/workbook.xml.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Target=\"worksheets/sheet1.xml\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" /></Relationships>");
		AddEntry(archive, "xl/worksheets/sheet1.xml", "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData><row r=\"1\"><c r=\"A1\" t=\"inlineStr\"><is><t>TITLE</t></is></c><c r=\"B1\" t=\"inlineStr\"><is><t>STANDARD</t></is></c><c r=\"C1\" t=\"inlineStr\"><is><t>QUANTITY</t></is></c></row><row r=\"2\"><c r=\"A2\" t=\"inlineStr\"><is><t>MONITOR</t></is></c><c r=\"B2\" t=\"inlineStr\"><is><t>NURSERY</t></is></c><c r=\"C2\"><v>2</v></c></row><row r=\"3\"><c r=\"A3\" t=\"inlineStr\"><is><t>मॉनिटर</t></is></c><c r=\"B3\" t=\"inlineStr\"><is><t>वरिष्ठ</t></is></c><c r=\"C3\"><v>1</v></c></row></sheetData></worksheet>");
	}
}

static void AddEntry(ZipArchive archive, string name, string content)
{
	using (var writer = new StreamWriter(archive.CreateEntry(name).Open(), new UTF8Encoding(false))) writer.Write(content);
}
