using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using CorelMate.Badges;
using CorelMate.Core;
using CorelMate.Curves;
using CorelMate.Host;
using CorelMate.UI.Themes;

namespace CorelMate.UI;

public sealed partial class CorelMatePanel : UserControl
{
    private sealed class RowEditor
    {
        public RowEditor(StackPanel container, List<TextBox> valueBoxes, TextBox quantityBox)
        {
            Container = container;
            ValueBoxes = valueBoxes;
            QuantityBox = quantityBox;
        }

        public StackPanel Container { get; }
        public List<TextBox> ValueBoxes { get; }
        public TextBox QuantityBox { get; }
    }

    private readonly CorelDrawHost? host;
    private readonly CorelDrawBadgeGenerator? generator;
    private readonly CorelTextToCurvesConverter? curvesConverter;
    private readonly CorelDocumentInspector? documentInspector;
    private readonly List<RowEditor> rowEditors = new List<RowEditor>();
    private CorelBadgeMaster? master;
    private bool isGenerating;
    private int detectedMajorVersion = 27;
    private CorelEra currentEra = CorelEra.AdaptivePro2026;

    public string Title => "CorelMate";
    public string Caption => "CorelMate";

    public CorelMatePanel() : this("2026 / v27")
    {
    }

    public CorelMatePanel(string targetVersion)
    {
        InitializeComponent();
        try
        {
            host = CorelDrawHost.ConnectToRunningInstance();
            generator = new CorelDrawBadgeGenerator(host);
            curvesConverter = new CorelTextToCurvesConverter(host);
            documentInspector = new CorelDocumentInspector(host);

            // Version Detection & Compatibility Evaluation
            var rawVersion = host.HostVersionString;
            detectedMajorVersion = host.HostMajorVersion;
            if (detectedMajorVersion <= 0) detectedMajorVersion = 27;

            var compat = CorelCompatibilityRegistry.Evaluate(detectedMajorVersion, rawVersion);
            currentEra = CorelCompatibilityRegistry.GetEraForVersion(detectedMajorVersion);

            // Apply version-adaptive UI styling
            CorelMateThemeManager.ApplyTheme(this, currentEra);

            // Header & Status Updates
            HeaderEraText.Text = $"v{detectedMajorVersion} / {currentEra}";
            StatusText.Text = $"{compat.VersionDescription} connected. Ready.";
            StatusBadgeText.Text = "Connected";
            StatusIndicatorDot.Fill = (Brush)FindResource("CorelMateSuccessBrush");
            HostVersionText.Text = compat.VersionDescription;
            DashHostVersionText.Text = compat.VersionDescription;
            DashArchText.Text = Environment.Is64BitOperatingSystem ? "64-bit (x64)" : "32-bit (x86)";
            CompatHostText.Text = $"{compat.VersionDescription} ({rawVersion})";
            CompatStatusText.Text = compat.StatusMessage;
            CompatEraText.Text = $"{currentEra} Era Styling";

            UpdateActiveDocumentDashboard();
        }
        catch (Exception exception)
        {
            StatusText.Text = "CorelDRAW connection unavailable: " + exception.Message;
            StatusBadgeText.Text = "Offline";
            StatusIndicatorDot.Fill = (Brush)FindResource("CorelMateErrorBrush");
            HostVersionText.Text = "Disconnected (" + exception.Message + ")";
            DashHostVersionText.Text = "Disconnected";
            DashDocumentText.Text = "Active Document: Offline";
            CompatStatusText.Text = "Offline: " + exception.Message;
        }
    }

    private void UpdateActiveDocumentDashboard()
    {
        try
        {
            var summary = host?.ActiveDocumentSummary;
            DashDocumentText.Text = !string.IsNullOrEmpty(summary)
                ? $"Active Document: {summary}"
                : "Active Document: None (Open or create a document in CorelDRAW)";
        }
        catch
        {
            DashDocumentText.Text = "Active Document: None";
        }
    }

    private void TabRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (DashboardTabContent == null || BadgesTabContent == null || CurvesTabContent == null ||
            InspectorTabContent == null || CompatTabContent == null || SettingsTabContent == null) return;

        DashboardTabContent.Visibility = TabDashboardRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        BadgesTabContent.Visibility = TabBadgesRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        CurvesTabContent.Visibility = TabCurvesRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        InspectorTabContent.Visibility = TabInspectorRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        CompatTabContent.Visibility = TabCompatRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        SettingsTabContent.Visibility = TabInfoRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

        if (TabDashboardRadio.IsChecked == true) UpdateActiveDocumentDashboard();
        if (TabInspectorRadio.IsChecked == true) RefreshDocumentInspection();
    }

    private void DashGoBadges_Click(object sender, RoutedEventArgs e) { TabBadgesRadio.IsChecked = true; }
    private void DashGoCurves_Click(object sender, RoutedEventArgs e) { TabCurvesRadio.IsChecked = true; }
    private void DashGoInspector_Click(object sender, RoutedEventArgs e) { TabInspectorRadio.IsChecked = true; }
    private void DashGoCompat_Click(object sender, RoutedEventArgs e) { TabCompatRadio.IsChecked = true; }

    private void DashConvertPage_Click(object sender, RoutedEventArgs e)
    {
        TabCurvesRadio.IsChecked = true;
        if (ScopeCurrentPageRadio != null) ScopeCurrentPageRadio.IsChecked = true;
        ConvertCurvesButton_Click(sender, e);
    }

    #region Document Inspector Feature

    private void RefreshInspectorButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshDocumentInspection();
    }

    private void RefreshDocumentInspection()
    {
        try
        {
            if (documentInspector == null) throw new InvalidOperationException("CorelDRAW is not connected.");
            var metrics = documentInspector.InspectActiveDocument();

            InspDocNameText.Text = metrics.Name;
            InspDimensionsText.Text = $"{metrics.DimensionsString} ({metrics.ColorMode})";
            InspTotalShapesText.Text = metrics.TotalShapes.ToString(CultureInfo.InvariantCulture);
            InspTextShapesText.Text = metrics.TextShapes.ToString(CultureInfo.InvariantCulture);
            InspCurvesShapesText.Text = metrics.CurveShapes.ToString(CultureInfo.InvariantCulture);
            InspBitmapsShapesText.Text = metrics.BitmapShapes.ToString(CultureInfo.InvariantCulture);

            if (metrics.UniqueFonts.Count > 0)
            {
                InspFontsListText.Text = $"{metrics.UniqueFonts.Count} font(s) detected:\r\n• " + string.Join("\r\n• ", metrics.UniqueFonts);
            }
            else
            {
                InspFontsListText.Text = "No text objects with fonts detected on the active page.";
            }
        }
        catch (Exception ex)
        {
            InspDocNameText.Text = "Inspection unavailable";
            InspDimensionsText.Text = "-";
            InspFontsListText.Text = ex.Message;
        }
    }

    #endregion

    #region Version-Adaptive Theme Switching

    private void ThemeBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag)
        {
            var resolved = CorelMateThemeManager.ResolveEra(tag, detectedMajorVersion);
            currentEra = resolved;
            CorelMateThemeManager.ApplyTheme(this, resolved);
            HeaderEraText.Text = $"v{detectedMajorVersion} / {currentEra}";
            CompatEraText.Text = $"{currentEra} Era Styling";
        }
    }

    #endregion

    #region Badge Generator Feature

    private void UseSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (generator == null) throw new InvalidOperationException("CorelDRAW is not connected.");
            master = generator.CaptureSelectedMaster();
            UpdateVariablesDisplay();
            DimensionsText.Text = master.WidthMillimeters.ToString("0.##", CultureInfo.InvariantCulture) + " × " + master.HeightMillimeters.ToString("0.##", CultureInfo.InvariantCulture) + " mm";
            BadgeWidthText.Text = master.WidthMillimeters.ToString("0.###", CultureInfo.InvariantCulture);
            BadgeHeightText.Text = master.HeightMillimeters.ToString("0.###", CultureInfo.InvariantCulture);
            RowsPanel.Children.Clear();
            rowEditors.Clear();
            AddRowHeader();
            AddRow();
            SetMasterControlsEnabled(true);
            StatusText.Text = "Master artwork captured (" + master.Variables.Count + " variables). Enter data rows or import CSV/XLSX.";
            RefreshPreview();
        }
        catch (Exception exception)
        {
            ShowFriendlyError(exception);
            ClearMasterState();
        }
    }

    private void UpdateVariablesDisplay()
    {
        VariablesWrapPanel.Children.Clear();
        if (master == null || master.Variables.Count == 0)
        {
            var border = new Border { Style = GetResource<Style>("CorelMatePillStyle") };
            border.Child = new TextBlock { Text = "None detected", FontSize = 11, Foreground = (Brush)FindResource("CorelMateTextMutedBrush") };
            VariablesWrapPanel.Children.Add(border);
            return;
        }

        foreach (var variable in master.Variables)
        {
            var border = new Border { Style = GetResource<Style>("CorelMatePillStyle") };
            border.Child = new TextBlock
            {
                Text = "{{" + variable + "}}",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("CorelMateAccentBrush")
            };
            VariablesWrapPanel.Children.Add(border);
        }
    }

    private void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (generator == null || master == null) throw new InvalidOperationException("Capture master artwork first.");
            if (isGenerating) return;
            isGenerating = true;
            SetBusyState(true);
            var rows = ParseRows();
            var settings = CreateLayoutSettings();
            var result = generator.Generate(master, settings, rows);
            ResultText.Text = "✓ Successfully generated " + result.TotalBadges + " badges across " + result.PagesCreated + " page(s). Master artwork was preserved.";
            ResultText.Foreground = (Brush)FindResource("CorelMateSuccessBrush");
            StatusText.Text = "Generation complete (" + result.TotalBadges + " badges).";
        }
        catch (Exception exception)
        {
            ShowFriendlyError(exception);
        }
        finally
        {
            isGenerating = false;
            SetBusyState(false);
        }
    }

    private void AddRowButton_Click(object sender, RoutedEventArgs e) => AddRow();

    private void DeleteRowButton_Click(object sender, RoutedEventArgs e)
    {
        if (rowEditors.Count == 0) return;
        RowsPanel.Children.Remove(rowEditors[rowEditors.Count - 1].Container);
        rowEditors.RemoveAt(rowEditors.Count - 1);
        UpdateRowCountDisplay();
        RefreshPreview();
    }

    private void ImportDataButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (master == null) throw new InvalidOperationException("Capture the master artwork first.");
            var dialog = new OpenFileDialog
            {
                Filter = "Data files (*.csv;*.xlsx)|*.csv;*.xlsx|CSV files (*.csv)|*.csv|Excel workbooks (*.xlsx)|*.xlsx",
                CheckFileExists = true,
                Multiselect = false
            };
            if (dialog.ShowDialog() != true) return;

            var imported = string.Equals(System.IO.Path.GetExtension(dialog.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase)
                ? XlsxImportSource.Read(dialog.FileName)
                : CsvImportSource.Read(dialog.FileName);
            var validation = BadgeImportMapper.Validate(imported, master.Variables);
            if (!validation.IsValid)
            {
                var count = Math.Min(validation.Errors.Count, 10);
                var shownErrors = new List<string>();
                for (var index = 0; index < count; index++) shownErrors.Add(validation.Errors[index]);
                var message = "Import validation failed:\r\n" + string.Join("\r\n", shownErrors);
                if (validation.Errors.Count > count) message += "\r\n...and " + (validation.Errors.Count - count) + " more error(s).";
                ResultText.Text = message;
                ResultText.Foreground = (Brush)FindResource("CorelMateErrorBrush");
                StatusText.Text = "Import rejected; CorelDRAW was not modified.";
                ClearRowState();
                return;
            }

            ReplaceRows(validation.Rows);
            var sourceLabel = string.IsNullOrWhiteSpace(imported.WorksheetName) ? imported.SourceName : imported.SourceName + " / " + imported.WorksheetName;
            StatusText.Text = "Imported " + validation.Rows.Count + " rows from " + sourceLabel + ".";
            ResultText.Text = "✓ Mapped: " + string.Join(", ", validation.UsedColumns) + (validation.UnusedColumns.Count == 0 ? string.Empty : "\r\nUnused: " + string.Join(", ", validation.UnusedColumns));
            ResultText.Foreground = (Brush)FindResource("CorelMateSuccessBrush");
            RefreshPreview();
        }
        catch (Exception exception)
        {
            ShowFriendlyError(exception);
            ClearRowState();
        }
    }

    private void PreviewButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ParseRows();
            RefreshPreview();
            ResultText.Text = "Preview refreshed successfully.";
            ResultText.Foreground = (Brush)FindResource("CorelMateTextSecondaryBrush");
        }
        catch (Exception exception)
        {
            ShowFriendlyError(exception);
        }
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        master = null;
        rowEditors.Clear();
        RowsPanel.Children.Clear();
        DimensionsText.Text = "No artwork captured";
        UpdateVariablesDisplay();
        BadgeWidthText.Text = string.Empty;
        BadgeHeightText.Text = string.Empty;
        MetricTotalText.Text = "0";
        MetricColumnsText.Text = "-";
        MetricPerPageText.Text = "-";
        MetricPagesText.Text = "-";
        PreviewDetailText.Text = "Capture a master to preview the layout plan.";
        ResultText.Text = string.Empty;
        CurvesResultText.Text = "Select artwork in CorelDRAW and click Convert.";
        StatusText.Text = "Select a master badge template in CorelDRAW to begin.";
        ClearMasterState();
    }

    private List<BadgeDataRow> ParseRows()
    {
        var rows = new List<BadgeDataRow>();
        if (master == null) throw new InvalidOperationException("Capture the master artwork first.");
        foreach (var editor in rowEditors)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < master.Variables.Count; index++)
            {
                var value = editor.ValueBoxes[index].Text.Trim();
                if (value.Length == 0) throw new FormatException("Enter a value for {{" + master.Variables[index] + "}} in every row.");
                values[master.Variables[index]] = value;
            }

            if (!int.TryParse(editor.QuantityBox.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var quantity) || quantity < 0) throw new FormatException("Quantity must be a non-negative integer.");
            rows.Add(new BadgeDataRow(values, quantity));
        }

        if (rows.Count == 0) throw new FormatException("Add at least one data row.");
        return rows;
    }

    private void AddRow()
    {
        if (master == null) return;
        var container = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
        var valueBoxes = new List<TextBox>();
        foreach (var variable in master.Variables)
        {
            var box = new TextBox { Width = 96, ToolTip = variable, Style = GetResource<Style>("CorelMateTextBoxStyle") };
            box.TextChanged += RowInputChanged;
            valueBoxes.Add(box);
            container.Children.Add(box);
        }

        var quantityBox = new TextBox { Width = 50, Text = "1", ToolTip = "Quantity", Style = GetResource<Style>("CorelMateTextBoxStyle") };
        quantityBox.TextChanged += RowInputChanged;
        container.Children.Add(quantityBox);
        rowEditors.Add(new RowEditor(container, valueBoxes, quantityBox));
        RowsPanel.Children.Add(container);
        DeleteRowButton.IsEnabled = true;
        UpdateRowCountDisplay();
        RefreshPreview();
    }

    private void ReplaceRows(IReadOnlyList<BadgeDataRow> rows)
    {
        if (master == null) return;
        RowsPanel.Children.Clear();
        rowEditors.Clear();
        AddRowHeader();
        foreach (var row in rows)
        {
            var container = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
            var valueBoxes = new List<TextBox>();
            foreach (var variable in master.Variables)
            {
                var box = new TextBox { Width = 96, Text = row.GetValue(variable), ToolTip = variable, Style = GetResource<Style>("CorelMateTextBoxStyle") };
                box.TextChanged += RowInputChanged;
                valueBoxes.Add(box);
                container.Children.Add(box);
            }

            var quantityBox = new TextBox { Width = 50, Text = row.Quantity.ToString(CultureInfo.InvariantCulture), ToolTip = "Quantity", Style = GetResource<Style>("CorelMateTextBoxStyle") };
            quantityBox.TextChanged += RowInputChanged;
            container.Children.Add(quantityBox);
            rowEditors.Add(new RowEditor(container, valueBoxes, quantityBox));
            RowsPanel.Children.Add(container);
        }
        DeleteRowButton.IsEnabled = rowEditors.Count > 0;
        UpdateRowCountDisplay();
        RefreshPreview();
    }

    private void AddRowHeader()
    {
        if (master == null) return;
        var header = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
        foreach (var variable in master.Variables)
        {
            header.Children.Add(new TextBlock
            {
                Text = variable,
                Width = 96,
                Margin = new Thickness(4, 0, 0, 0),
                FontWeight = FontWeights.Bold,
                FontSize = 10.5,
                Foreground = (Brush)FindResource("CorelMateTextSecondaryBrush")
            });
        }
        header.Children.Add(new TextBlock
        {
            Text = "QTY",
            Width = 50,
            Margin = new Thickness(4, 0, 0, 0),
            FontWeight = FontWeights.Bold,
            FontSize = 10.5,
            Foreground = (Brush)FindResource("CorelMateTextSecondaryBrush")
        });
        RowsPanel.Children.Add(header);
    }

    private void UpdateRowCountDisplay()
    {
        RowCountText.Text = rowEditors.Count + (rowEditors.Count == 1 ? " row" : " rows");
    }

    private void RowInputChanged(object sender, TextChangedEventArgs e)
    {
        if (master != null) RefreshPreview();
    }

    private void LayoutTextChanged(object sender, TextChangedEventArgs e)
    {
        if (master != null) RefreshPreview();
    }

    private BadgeLayoutSettings CreateLayoutSettings()
    {
        if (master == null) throw new InvalidOperationException("Capture the master artwork first.");
        return new BadgeLayoutSettings
        {
            PageWidthMillimeters = master.PageWidthMillimeters,
            PageHeightMillimeters = master.PageHeightMillimeters,
            BadgeWidthMillimeters = ParseNumber(BadgeWidthText.Text, "badge width"),
            BadgeHeightMillimeters = ParseNumber(BadgeHeightText.Text, "badge height"),
            HorizontalGapMillimeters = ParseNumber(HorizontalGapText.Text, "horizontal gap"),
            VerticalGapMillimeters = ParseNumber(VerticalGapText.Text, "vertical gap"),
            LeftMarginMillimeters = ParseNumber(HorizontalMarginText.Text, "left/right margin"),
            RightMarginMillimeters = ParseNumber(HorizontalMarginText.Text, "left/right margin"),
            TopMarginMillimeters = ParseNumber(VerticalMarginText.Text, "top/bottom margin"),
            BottomMarginMillimeters = ParseNumber(VerticalMarginText.Text, "top/bottom margin")
        };
    }

    private int TotalQuantity()
    {
        var total = 0;
        foreach (var editor in rowEditors)
        {
            if (int.TryParse(editor.QuantityBox.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var quantity) && quantity >= 0) total += quantity;
        }

        return total;
    }

    private void RefreshPreview()
    {
        if (master == null) return;
        try
        {
            var settings = CreateLayoutSettings();
            var total = TotalQuantity();
            var plan = BadgeLayoutEngine.Plan(settings, total);
            MetricTotalText.Text = total.ToString(CultureInfo.InvariantCulture);
            MetricColumnsText.Text = plan.Columns.ToString(CultureInfo.InvariantCulture);
            MetricPerPageText.Text = plan.PerPage.ToString(CultureInfo.InvariantCulture);
            MetricPagesText.Text = plan.Pages.Count.ToString(CultureInfo.InvariantCulture);
            PreviewDetailText.Text = "Grid: " + plan.Columns + " col(s) × " + plan.RowsPerPage + " row(s) = " + plan.PerPage + " badges/page across " + plan.Pages.Count + " page(s).";
        }
        catch
        {
            var total = TotalQuantity();
            MetricTotalText.Text = total > 0 ? total.ToString(CultureInfo.InvariantCulture) : "0";
            MetricColumnsText.Text = "-";
            MetricPerPageText.Text = "-";
            MetricPagesText.Text = "-";
            PreviewDetailText.Text = "Preview unavailable until the layout and rows are valid.";
        }
    }

    private void SetMasterControlsEnabled(bool enabled)
    {
        AddRowButton.IsEnabled = enabled;
        DeleteRowButton.IsEnabled = enabled && rowEditors.Count > 0;
        ImportDataButton.IsEnabled = enabled;
        PreviewButton.IsEnabled = enabled;
        GenerateButton.IsEnabled = enabled;
    }

    private void SetBusyState(bool busy)
    {
        UseSelectedButton.IsEnabled = !busy;
        AddRowButton.IsEnabled = !busy && master != null;
        DeleteRowButton.IsEnabled = !busy && rowEditors.Count > 0;
        ImportDataButton.IsEnabled = !busy && master != null;
        PreviewButton.IsEnabled = !busy && master != null;
        GenerateButton.IsEnabled = !busy && master != null;
        ResetButton.IsEnabled = !busy;
    }

    private void ClearMasterState()
    {
        master = null;
        ClearRowState();
        DimensionsText.Text = "No artwork captured";
        BadgeWidthText.Text = string.Empty;
        BadgeHeightText.Text = string.Empty;
        SetMasterControlsEnabled(false);
        UpdateVariablesDisplay();
        MetricTotalText.Text = "0";
        MetricColumnsText.Text = "-";
        MetricPerPageText.Text = "-";
        MetricPagesText.Text = "-";
        PreviewDetailText.Text = "Capture a master to preview the layout plan.";
    }

    private void ClearRowState()
    {
        rowEditors.Clear();
        RowsPanel.Children.Clear();
        var emptyHint = new TextBlock
        {
            Text = "Capture master artwork to configure data rows or import CSV/XLSX.",
            Style = GetResource<Style>("CorelMateCaptionStyle"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 16, 0, 16)
        };
        RowsPanel.Children.Add(emptyHint);
        DeleteRowButton.IsEnabled = false;
        UpdateRowCountDisplay();
        MetricTotalText.Text = "0";
        MetricColumnsText.Text = "-";
        MetricPerPageText.Text = "-";
        MetricPagesText.Text = "-";
        PreviewDetailText.Text = master == null ? "Capture a master to preview the layout plan." : "Preview unavailable until the layout and rows are valid.";
    }

    #endregion

    #region Convert Text To Curves Feature

    private CurveConversionScope GetSelectedScope()
    {
        if (ScopeSelectionRadio?.IsChecked == true) return CurveConversionScope.SelectedObjects;
        if (ScopeAllPagesRadio?.IsChecked == true) return CurveConversionScope.AllPages;
        return CurveConversionScope.CurrentPage;
    }

    private void PreflightCurvesButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (curvesConverter == null) throw new InvalidOperationException("CorelDRAW is not connected.");
            var scope = GetSelectedScope();
            var preflight = curvesConverter.Preflight(scope);

            CurvesMetricsGrid.Visibility = Visibility.Visible;
            CurvesFoundCountText.Text = preflight.Summary.FoundTextObjects.ToString();
            CurvesConvertibleCountText.Text = preflight.Summary.ConvertibleTextObjects.ToString();
            CurvesGroupsCountText.Text = preflight.Summary.GroupsInspected.ToString();
            CurvesLockedCountText.Text = preflight.Summary.LockedTextObjects.ToString();

            var scopeDesc = scope == CurveConversionScope.CurrentPage ? "current page"
                : scope == CurveConversionScope.SelectedObjects ? "selection" : "entire document (" + preflight.Summary.PagesProcessed + " pages)";

            CurvesPreflightDetailText.Visibility = Visibility.Visible;
            CurvesPreflightDetailText.Text = "Scope: " + scopeDesc + " | Artistic: " + preflight.Summary.ArtisticTextCount + ", Paragraph: " + preflight.Summary.ParagraphTextCount + ", PowerClips: " + preflight.Summary.PowerClipsInspected;

            if (preflight.Summary.ConvertibleTextObjects == 0)
            {
                CurvesResultText.Text = "No convertible text found in " + scopeDesc + "." + (preflight.Summary.SkippedTextObjects > 0 ? " (" + preflight.Summary.SkippedTextObjects + " skipped/locked)" : "");
            }
            else
            {
                CurvesResultText.Text = "Preflight complete: " + preflight.Summary.ConvertibleTextObjects + " text object(s) ready for conversion in " + scopeDesc + ".";
            }
        }
        catch (Exception ex)
        {
            CurvesResultText.Text = ex is InvalidOperationException ? ex.Message : "Could not complete preflight: " + ex.Message;
        }
    }

    private void ConvertCurvesButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (curvesConverter == null) throw new InvalidOperationException("CorelDRAW is not connected.");
            var scope = GetSelectedScope();
            var preflight = curvesConverter.Preflight(scope);

            CurvesMetricsGrid.Visibility = Visibility.Visible;
            CurvesFoundCountText.Text = preflight.Summary.FoundTextObjects.ToString();
            CurvesConvertibleCountText.Text = preflight.Summary.ConvertibleTextObjects.ToString();
            CurvesGroupsCountText.Text = preflight.Summary.GroupsInspected.ToString();
            CurvesLockedCountText.Text = preflight.Summary.LockedTextObjects.ToString();

            var scopeDesc = scope == CurveConversionScope.CurrentPage ? "current page"
                : scope == CurveConversionScope.SelectedObjects ? "selection" : "all pages (" + preflight.Summary.PagesProcessed + " pages)";

            if (preflight.Summary.ConvertibleTextObjects == 0)
            {
                CurvesResultText.Text = "No convertible text objects found in " + scopeDesc + ".";
                if (preflight.Summary.SkippedTextObjects > 0) CurvesResultText.Text += " (" + preflight.Summary.SkippedTextObjects + " text object(s) skipped/locked).";
                return;
            }

            var saveCopy = CurvesSaveCopyCheck?.IsChecked == true;
            var prompt = "Convert " + preflight.Summary.ConvertibleTextObjects + " text object(s) to curves in " + scopeDesc + "?\r\n\r\n"
                       + (saveCopy ? "✓ A backup copy (_curves.cdr) will be saved first.\r\n\r\n" : "")
                       + "Note: Text will no longer be editable as text. You can undo this with Ctrl+Z.";

            var confirmation = MessageBox.Show(prompt, "CorelMate: Convert Text to Curves", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (confirmation != MessageBoxResult.OK) return;

            var options = new CurveConversionOptions
            {
                Scope = scope,
                SaveConvertedCopy = saveCopy
            };

            var result = curvesConverter.Convert(preflight, options);
            var msg = "✓ " + result.ConvertedTextObjects + " text object(s) successfully converted to curves in " + result.Summary.ExecutionDurationMs + "ms.";
            if (result.FailedTextObjects > 0) msg += " (" + result.FailedTextObjects + " failed).";
            if (result.Summary.SkippedTextObjects > 0) msg += " (" + result.Summary.SkippedTextObjects + " skipped/locked).";
            if (!string.IsNullOrEmpty(result.Summary.SavedCopyPath)) msg += "\r\n💾 Backup saved: " + Path.GetFileName(result.Summary.SavedCopyPath);

            CurvesResultText.Text = msg;
        }
        catch (Exception exception)
        {
            CurvesResultText.Text = exception is InvalidOperationException ? exception.Message : "CorelDRAW could not complete the text conversion: " + exception.Message;
        }
    }

    #endregion

    #region Helpers

    private void ShowFriendlyError(Exception exception)
    {
        var message = exception is FormatException || exception is InvalidOperationException ? exception.Message : "CorelDRAW could not complete the operation. Verify that the document and selected artwork are still available.";
        StatusText.Text = message;
        ResultText.Text = message;
        ResultText.Foreground = (Brush)FindResource("CorelMateErrorBrush");
    }

    private static double ParseNumber(string text, string label)
    {
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) throw new FormatException("Enter a valid number for " + label + ".");
        return value;
    }

    private T GetResource<T>(string key) where T : class
    {
        return (T)FindResource(key);
    }

    #endregion
}
