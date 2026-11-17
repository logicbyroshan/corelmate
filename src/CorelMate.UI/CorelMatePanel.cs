using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
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

    private CorelDrawHost? host;
    private CorelDrawBadgeGenerator? generator;
    private CorelTextToCurvesConverter? curvesConverter;
    private CorelDocumentInspector? documentInspector;
    private readonly List<RowEditor> rowEditors = new List<RowEditor>();
    private CorelBadgeMaster? master;
    private bool isGenerating;
    private int detectedMajorVersion = 27;
    private CorelEra currentEra = CorelEra.AdaptivePro2026;
    private DispatcherTimer? retryTimer;
    private int retryCount;

    public string Title => "CorelMate";
    public string Caption => "CorelMate";

    public CorelMatePanel() : this("2026 / v27")
    {
    }

    public CorelMatePanel(string targetVersion)
    {
        InitializeComponent();

        // 1. Auto-detect and apply theme (Light vs Dark) matching CorelDRAW / Windows
        ApplyThemeMode(ThemeMode.Auto);

        // 2. Initial connection attempt
        var connected = TryConnectHost();
        if (!connected)
        {
            StartDeferredConnectionRetry();
        }

        // 3. Register Loaded event to ensure active document refresh
        Loaded += OnPanelLoaded;
    }

    private void OnPanelLoaded(object sender, RoutedEventArgs e)
    {
        if (host == null)
        {
            TryConnectHost();
        }
        else
        {
            UpdateActiveDocumentSummary();
        }
    }

    #region Host Connection & Auto-Reconnect

    private bool TryConnectHost()
    {
        try
        {
            host = CorelDrawHost.ConnectToRunningInstance();
            generator = new CorelDrawBadgeGenerator(host);
            curvesConverter = new CorelTextToCurvesConverter(host);
            documentInspector = new CorelDocumentInspector(host);

            var rawVersion = host.HostVersionString;
            detectedMajorVersion = host.HostMajorVersion;
            if (detectedMajorVersion <= 0) detectedMajorVersion = 27;

            var compat = CorelCompatibilityRegistry.Evaluate(detectedMajorVersion, rawVersion);
            currentEra = CorelCompatibilityRegistry.GetEraForVersion(detectedMajorVersion);

            HeaderEraText.Text = $"v{detectedMajorVersion}";
            StatusBadgeText.Text = "Connected";
            StatusIndicatorDot.Fill = (Brush)FindResource("CorelMateSuccessBrush");
            HostVersionText.Text = compat.VersionDescription;
            CompatStatusText.Text = "Connected (Ready)";
            DashArchText.Text = Environment.Is64BitOperatingSystem ? "64-bit (x64)" : "32-bit (x86)";

            UpdateActiveDocumentSummary();

            if (retryTimer != null)
            {
                retryTimer.Stop();
                retryTimer = null;
            }

            return true;
        }
        catch (Exception ex)
        {
            StatusBadgeText.Text = "Offline";
            StatusIndicatorDot.Fill = (Brush)FindResource("CorelMateErrorBrush");
            DashDocumentText.Text = "CorelDRAW is not connected. (Click to reconnect)";
            CompatStatusText.Text = "Disconnected: " + ex.Message;
            return false;
        }
    }

    private void StartDeferredConnectionRetry()
    {
        StatusBadgeText.Text = "Connecting...";
        StatusIndicatorDot.Fill = (Brush)FindResource("CorelMateWarningBrush");

        retryCount = 0;
        retryTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000) };
        retryTimer.Tick += (s, e) =>
        {
            retryCount++;
            if (TryConnectHost())
            {
                retryTimer.Stop();
                retryTimer = null;
            }
            else if (retryCount >= 10)
            {
                retryTimer.Stop();
                retryTimer = null;
                StatusBadgeText.Text = "Offline";
                StatusIndicatorDot.Fill = (Brush)FindResource("CorelMateErrorBrush");
            }
        };
        retryTimer.Start();
    }

    private bool EnsureConnected()
    {
        if (host != null && curvesConverter != null) return true;
        return TryConnectHost();
    }

    private void UpdateActiveDocumentSummary()
    {
        try
        {
            if (host == null)
            {
                DashDocumentText.Text = "CorelDRAW is not connected.";
                return;
            }

            var summary = host.ActiveDocumentSummary;
            if (!string.IsNullOrEmpty(summary))
            {
                DashDocumentText.Text = "📄 " + summary;
            }
            else
            {
                DashDocumentText.Text = "No active document. (Open or create a file in CorelDRAW)";
            }
        }
        catch
        {
            DashDocumentText.Text = "No active document.";
        }
    }

    private void RefreshDoc_Click(object sender, RoutedEventArgs e)
    {
        EnsureConnected();
        UpdateActiveDocumentSummary();
    }

    private void ConnectionBadge_Click(object sender, MouseButtonEventArgs e)
    {
        if (TryConnectHost())
        {
            MessageBox.Show("CorelMate is successfully connected to CorelDRAW!\r\n" + HostVersionText.Text, "CorelMate Connection", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show("Could not connect to CorelDRAW. Please ensure CorelDRAW is running and open.", "CorelMate Connection", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Reconnect_Click(object sender, RoutedEventArgs e)
    {
        if (TryConnectHost())
        {
            MessageBox.Show("Connected successfully to CorelDRAW!", "CorelMate", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show("CorelDRAW could not be reached. Ensure a document is open.", "CorelMate", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    #endregion

    #region Theme & Appearance

    private void ApplyThemeMode(ThemeMode mode)
    {
        CorelMateThemeManager.ApplyTheme(this, mode);
        ThemeToggleButton.Content = CorelMateThemeManager.IsDarkActive ? "🌙" : "☀️";
        ThemeToggleButton.ToolTip = CorelMateThemeManager.IsDarkActive ? "Dark Theme Active (Click for Light)" : "Light Theme Active (Click for Dark)";
    }

    private void ThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        var newMode = CorelMateThemeManager.IsDarkActive ? ThemeMode.Light : ThemeMode.Dark;
        ApplyThemeMode(newMode);
    }

    private void ThemeLight_Click(object sender, RoutedEventArgs e)
    {
        ApplyThemeMode(ThemeMode.Light);
    }

    private void ThemeDark_Click(object sender, RoutedEventArgs e)
    {
        ApplyThemeMode(ThemeMode.Dark);
    }

    private void ThemeAuto_Click(object sender, RoutedEventArgs e)
    {
        ApplyThemeMode(ThemeMode.Auto);
    }

    #endregion

    #region Streamlined Tab Navigation (3 Tabs)

    private void TabRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (CurvesTabContent == null || BadgesTabContent == null || InspectorTabContent == null) return;

        CurvesTabContent.Visibility = TabCurvesRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        BadgesTabContent.Visibility = TabBadgesRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        InspectorTabContent.Visibility = TabInspectorRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

        if (TabCurvesRadio.IsChecked == true)
        {
            UpdateActiveDocumentSummary();
        }
        else if (TabInspectorRadio.IsChecked == true)
        {
            RefreshDocumentInspection();
        }
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
            if (!EnsureConnected()) throw new InvalidOperationException("CorelDRAW is not connected. Please open CorelDRAW.");
            if (curvesConverter == null) throw new InvalidOperationException("CorelDRAW is not connected.");

            var scope = GetSelectedScope();
            var unlock = CurvesUnlockShapesCheck?.IsChecked == true;
            var opts = new CurveConversionOptions { Scope = scope, UnlockLockedShapes = unlock };
            var preflight = curvesConverter.Preflight(scope, opts);

            CurvesMetricsGrid.Visibility = Visibility.Visible;
            CurvesFoundCountText.Text = preflight.Summary.FoundTextObjects.ToString(CultureInfo.InvariantCulture);
            CurvesConvertibleCountText.Text = preflight.Summary.ConvertibleTextObjects.ToString(CultureInfo.InvariantCulture);
            CurvesGroupsCountText.Text = preflight.Summary.GroupsInspected.ToString(CultureInfo.InvariantCulture);
            CurvesLockedCountText.Text = preflight.Summary.LockedTextObjects.ToString(CultureInfo.InvariantCulture);

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
            if (!EnsureConnected()) throw new InvalidOperationException("CorelDRAW is not connected. Please open CorelDRAW.");
            if (curvesConverter == null) throw new InvalidOperationException("CorelDRAW is not connected.");

            var scope = GetSelectedScope();
            var unlock = CurvesUnlockShapesCheck?.IsChecked == true;
            var opts = new CurveConversionOptions { Scope = scope, UnlockLockedShapes = unlock };
            var preflight = curvesConverter.Preflight(scope, opts);

            CurvesMetricsGrid.Visibility = Visibility.Visible;
            CurvesFoundCountText.Text = preflight.Summary.FoundTextObjects.ToString(CultureInfo.InvariantCulture);
            CurvesConvertibleCountText.Text = preflight.Summary.ConvertibleTextObjects.ToString(CultureInfo.InvariantCulture);
            CurvesGroupsCountText.Text = preflight.Summary.GroupsInspected.ToString(CultureInfo.InvariantCulture);
            CurvesLockedCountText.Text = preflight.Summary.LockedTextObjects.ToString(CultureInfo.InvariantCulture);

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
                       + "Note: Text will be converted to vector curves in-place. You can undo this with Ctrl+Z.";

            var confirmation = MessageBox.Show(prompt, "CorelMate: Convert Text to Curves", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (confirmation != MessageBoxResult.OK) return;

            var options = new CurveConversionOptions
            {
                Scope = scope,
                SaveConvertedCopy = saveCopy,
                UnlockLockedShapes = unlock
            };

            var result = curvesConverter.Convert(preflight, options);
            var msg = "✓ " + result.ConvertedTextObjects + " text object(s) successfully converted to curves in " + result.Summary.ExecutionDurationMs + "ms.";
            if (result.FailedTextObjects > 0) msg += " (" + result.FailedTextObjects + " failed).";
            if (result.Summary.SkippedTextObjects > 0) msg += " (" + result.Summary.SkippedTextObjects + " skipped/locked).";
            if (!string.IsNullOrEmpty(result.Summary.SavedCopyPath)) msg += "\r\n💾 Backup saved: " + Path.GetFileName(result.Summary.SavedCopyPath);

            CurvesResultText.Text = msg;
            UpdateActiveDocumentSummary();
        }
        catch (Exception exception)
        {
            CurvesResultText.Text = exception is InvalidOperationException ? exception.Message : "CorelDRAW could not complete the text conversion: " + exception.Message;
        }
    }

    #endregion

    #region Document Inspector Feature

    private void RefreshInspectorButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshDocumentInspection();
    }

    private void RefreshDocumentInspection()
    {
        try
        {
            if (!EnsureConnected()) throw new InvalidOperationException("CorelDRAW is not connected.");
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
                InspFontsListText.Text = string.Join(", ", metrics.UniqueFonts);
            }
            else
            {
                InspFontsListText.Text = "No fonts detected on active page.";
            }
        }
        catch (Exception ex)
        {
            InspDocNameText.Text = "Inspection unavailable";
            InspDimensionsText.Text = ex.Message;
            InspFontsListText.Text = "Could not inspect active document: " + ex.Message;
        }
    }

    #endregion

    #region Badge Generator Feature

    private void UseSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!EnsureConnected()) throw new InvalidOperationException("CorelDRAW is not connected.");
            if (generator == null) throw new InvalidOperationException("CorelDRAW is not connected.");

            master = generator.CaptureSelectedMaster();
            DimensionsText.Text = $"{master.WidthMillimeters:F1} × {master.HeightMillimeters:F1} mm";
            BadgeWidthText.Text = master.WidthMillimeters.ToString("0.##", CultureInfo.InvariantCulture);
            BadgeHeightText.Text = master.HeightMillimeters.ToString("0.##", CultureInfo.InvariantCulture);

            SetMasterControlsEnabled(true);
            UpdateVariablesDisplay();
            RebuildRowEditors();
            RecalculatePlan();

            StatusText.Text = master.Variables.Count == 0
                ? "Artwork captured without placeholders (static duplication mode)."
                : $"Captured master with {master.Variables.Count} variable(s). Ready.";
        }
        catch (Exception exception)
        {
            ShowFriendlyError(exception);
        }
    }

    private void AddRowButton_Click(object sender, RoutedEventArgs e)
    {
        if (master == null) return;
        AddRowEditor(CreateDefaultRow());
        UpdateRowCountDisplay();
        RecalculatePlan();
    }

    private void DeleteRowButton_Click(object sender, RoutedEventArgs e)
    {
        if (rowEditors.Count == 0) return;
        var last = rowEditors[rowEditors.Count - 1];
        rowEditors.RemoveAt(rowEditors.Count - 1);
        RowsPanel.Children.Remove(last.Container);
        DeleteRowButton.IsEnabled = rowEditors.Count > 0;
        UpdateRowCountDisplay();
        RecalculatePlan();
    }

    private void ImportDataButton_Click(object sender, RoutedEventArgs e)
    {
        if (master == null) return;

        var dialog = new OpenFileDialog
        {
            Title = "Import Badge Data",
            Filter = "CSV & Delimited Text (*.csv;*.txt)|*.csv;*.txt|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            var lines = File.ReadAllLines(dialog.FileName);
            if (lines.Length < 2) throw new InvalidOperationException("The imported file must contain a header row and at least one data row.");

            var delimiter = dialog.FileName.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase) ? '\t' : ',';
            var headers = lines[0].Split(delimiter);

            var rows = new List<BadgeDataRow>();
            for (var i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                var cols = line.Split(delimiter);
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (var j = 0; j < Math.Min(headers.Length, cols.Length); j++)
                {
                    dict[headers[j].Trim()] = cols[j].Trim();
                }
                rows.Add(new BadgeDataRow(dict, 1));
            }

            rowEditors.Clear();
            RowsPanel.Children.Clear();
            foreach (var row in rows)
            {
                AddRowEditor(row);
            }

            DeleteRowButton.IsEnabled = rowEditors.Count > 0;
            UpdateRowCountDisplay();
            RecalculatePlan();
            StatusText.Text = $"Imported {rows.Count} data row(s) from {Path.GetFileName(dialog.FileName)}.";
        }
        catch (Exception exception)
        {
            ShowFriendlyError(exception);
        }
    }

    private void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        if (master == null || isGenerating) return;

        try
        {
            if (!EnsureConnected()) throw new InvalidOperationException("CorelDRAW is not connected.");
            if (generator == null) throw new InvalidOperationException("CorelDRAW is not connected.");

            var rows = CollectRows();
            var settings = ReadSettings();
            var totalCount = 0;
            foreach (var r in rows) totalCount += r.Quantity;
            var plan = BadgeLayoutEngine.Plan(settings, totalCount);

            var prompt = $"Generate {totalCount} badge(s) across {plan.Pages.Count} page(s)?";
            if (MessageBox.Show(prompt, "CorelMate Badge Generation", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;

            SetBusyState(true);
            var result = generator.Generate(master, settings, rows);
            ResultText.Text = $"✓ Generated {result.TotalBadges} badge(s) across {result.PagesCreated} page(s).";
            ResultText.Foreground = (Brush)FindResource("CorelMateSuccessBrush");
            StatusText.Text = "Badge generation completed successfully.";
            UpdateActiveDocumentSummary();
        }
        catch (Exception exception)
        {
            ShowFriendlyError(exception);
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private void PreviewButton_Click(object sender, RoutedEventArgs e)
    {
        RecalculatePlan();
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        ClearMasterState();
        ResultText.Text = string.Empty;
        StatusText.Text = "Select a master badge template in CorelDRAW to begin.";
    }

    private void LayoutTextChanged(object sender, TextChangedEventArgs e)
    {
        RecalculatePlan();
    }

    private void SetMasterControlsEnabled(bool enabled)
    {
        AddRowButton.IsEnabled = enabled;
        ImportDataButton.IsEnabled = enabled;
        PreviewButton.IsEnabled = enabled;
        GenerateButton.IsEnabled = enabled && rowEditors.Count > 0;
    }

    private void UpdateVariablesDisplay()
    {
        VariablesWrapPanel.Children.Clear();
        if (master == null || master.Variables.Count == 0)
        {
            VariablesWrapPanel.Children.Add(new Border
            {
                Style = GetResource<Style>("CorelMatePillStyle"),
                Child = new TextBlock
                {
                    Text = master == null ? "None detected" : "Static (no placeholders)",
                    FontSize = 11,
                    Foreground = (Brush)FindResource("CorelMateTextMutedBrush")
                }
            });
            return;
        }

        foreach (var v in master.Variables)
        {
            var border = new Border { Style = GetResource<Style>("CorelMatePillStyle") };
            border.Child = new TextBlock
            {
                Text = v,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("CorelMateAccentBrush")
            };
            VariablesWrapPanel.Children.Add(border);
        }
    }

    private void RebuildRowEditors()
    {
        rowEditors.Clear();
        RowsPanel.Children.Clear();
        if (master == null) return;

        AddRowEditor(CreateDefaultRow());
        DeleteRowButton.IsEnabled = true;
        UpdateRowCountDisplay();
    }

    private BadgeDataRow CreateDefaultRow()
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (master != null)
        {
            foreach (var v in master.Variables)
            {
                values[v] = v;
            }
        }
        return new BadgeDataRow(values, 1);
    }

    private void AddRowEditor(BadgeDataRow row)
    {
        if (master == null) return;

        var container = new StackPanel { Margin = new Thickness(0, 0, 0, 4) };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50, GridUnitType.Pixel) });

        var valuesPanel = new WrapPanel();
        var valueBoxes = new List<TextBox>();

        foreach (var v in master.Variables)
        {
            var box = new TextBox
            {
                Style = GetResource<Style>("CorelMateTextBoxStyle"),
                MinWidth = 75,
                ToolTip = v,
                Text = row.Values.TryGetValue(v, out var val) ? val : v
            };
            box.TextChanged += (s, e) => RecalculatePlan();
            valuesPanel.Children.Add(box);
            valueBoxes.Add(box);
        }

        var qtyBox = new TextBox
        {
            Style = GetResource<Style>("CorelMateTextBoxStyle"),
            Text = row.Quantity.ToString(CultureInfo.InvariantCulture),
            ToolTip = "Quantity"
        };
        qtyBox.TextChanged += (s, e) => RecalculatePlan();

        Grid.SetColumn(valuesPanel, 0);
        Grid.SetColumn(qtyBox, 1);
        grid.Children.Add(valuesPanel);
        grid.Children.Add(qtyBox);
        container.Children.Add(grid);

        rowEditors.Add(new RowEditor(container, valueBoxes, qtyBox));
        RowsPanel.Children.Add(container);
        DeleteRowButton.IsEnabled = true;
    }

    private void UpdateRowCountDisplay()
    {
        RowCountText.Text = $"{rowEditors.Count} row(s)";
        GenerateButton.IsEnabled = master != null && rowEditors.Count > 0;
    }

    private List<BadgeDataRow> CollectRows()
    {
        var rows = new List<BadgeDataRow>();
        if (master == null) return rows;

        foreach (var editor in rowEditors)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < master.Variables.Count && i < editor.ValueBoxes.Count; i++)
            {
                dict[master.Variables[i]] = editor.ValueBoxes[i].Text;
            }
            var qty = int.TryParse(editor.QuantityBox.Text, out var q) && q > 0 ? q : 1;
            rows.Add(new BadgeDataRow(dict, qty));
        }
        return rows;
    }

    private BadgeLayoutSettings ReadSettings()
    {
        var w = ParseNumber(BadgeWidthText.Text, "Badge Width");
        var h = ParseNumber(BadgeHeightText.Text, "Badge Height");
        var hGap = ParseNumber(HorizontalGapText.Text, "Horizontal Gap");
        var vGap = ParseNumber(VerticalGapText.Text, "Vertical Gap");
        var hMargin = ParseNumber(HorizontalMarginText.Text, "Horizontal Margin");
        var vMargin = ParseNumber(VerticalMarginText.Text, "Vertical Margin");

        return new BadgeLayoutSettings
        {
            PageWidthMillimeters = 210,
            PageHeightMillimeters = 297,
            BadgeWidthMillimeters = w,
            BadgeHeightMillimeters = h,
            HorizontalGapMillimeters = hGap,
            VerticalGapMillimeters = vGap,
            LeftMarginMillimeters = hMargin,
            RightMarginMillimeters = hMargin,
            TopMarginMillimeters = vMargin,
            BottomMarginMillimeters = vMargin
        };
    }

    private void RecalculatePlan()
    {
        if (master == null) return;

        try
        {
            var rows = CollectRows();
            var settings = ReadSettings();
            var totalCount = 0;
            foreach (var r in rows) totalCount += r.Quantity;
            var plan = BadgeLayoutEngine.Plan(settings, totalCount);

            MetricTotalText.Text = totalCount.ToString(CultureInfo.InvariantCulture);
            MetricColumnsText.Text = plan.Columns.ToString(CultureInfo.InvariantCulture);
            MetricPerPageText.Text = plan.PerPage.ToString(CultureInfo.InvariantCulture);
            MetricPagesText.Text = plan.Pages.Count.ToString(CultureInfo.InvariantCulture);
            PreviewDetailText.Text = $"Layout: {plan.Columns} col × {plan.RowsPerPage} row = {plan.PerPage}/page across {plan.Pages.Count} page(s).";
            GenerateButton.IsEnabled = totalCount > 0;
        }
        catch
        {
            MetricTotalText.Text = "0";
            MetricColumnsText.Text = "-";
            MetricPerPageText.Text = "-";
            MetricPagesText.Text = "-";
            PreviewDetailText.Text = "Check badge dimensions and spacing numbers.";
            GenerateButton.IsEnabled = false;
        }
    }

    private void SetBusyState(bool busy)
    {
        isGenerating = busy;
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
            Text = "Capture master artwork to configure data rows or import CSV.",
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
