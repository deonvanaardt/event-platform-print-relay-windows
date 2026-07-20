using EventPlatform.PrintRelay.App;
using EventPlatform.PrintRelay.App.Printing;
using EventPlatform.PrintRelay.Core.Settings;

namespace EventPlatform.PrintRelay.App.Tray;

internal sealed class SettingsForm : Form
{
    private readonly RelayRuntime _runtime;
    private readonly Action<RelayRestartReason> _requestRestart;

    private readonly Label _deskNameLabel = new();
    private readonly ComboBox _printerComboBox = new();
    private readonly Label _versionLabel = new();
    private readonly Button _saveButton = new();
    private readonly Button _rerunSetupButton = new();

    public SettingsForm(RelayRuntime runtime, Action<RelayRestartReason> requestRestart)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _requestRestart = requestRestart ?? throw new ArgumentNullException(nameof(requestRestart));

        Text = RelayProductName.Title("Settings");
        Icon = RelayAppIcons.LoadAppIcon();
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(480, 280);

        BuildLayout();
        LoadValues();
    }

    private void BuildLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(16),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        layout.Controls.Add(new Label { Text = "Desk:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        _deskNameLabel.AutoSize = true;
        _deskNameLabel.Anchor = AnchorStyles.Left;
        layout.Controls.Add(_deskNameLabel, 1, 0);

        layout.Controls.Add(new Label { Text = "Printer:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        _printerComboBox.Dock = DockStyle.Fill;
        _printerComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        layout.SetColumnSpan(_printerComboBox, 1);
        layout.Controls.Add(_printerComboBox, 1, 1);

        layout.Controls.Add(new Label { Text = "Version:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
        _versionLabel.AutoSize = true;
        _versionLabel.Anchor = AnchorStyles.Left;
        layout.Controls.Add(_versionLabel, 1, 2);

        _saveButton.Text = "Save printer";
        _saveButton.AutoSize = true;
        _saveButton.Click += async (_, _) => await SaveAsync().ConfigureAwait(true);

        _rerunSetupButton.Text = "Re-run setup wizard";
        _rerunSetupButton.AutoSize = true;
        _rerunSetupButton.Click += (_, _) => RerunSetup();

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(16, 0, 16, 16),
            AutoSize = true,
        };
        footer.Controls.Add(_saveButton);
        footer.Controls.Add(_rerunSetupButton);

        Controls.Add(layout);
        Controls.Add(footer);
    }

    private void LoadValues()
    {
        _deskNameLabel.Text = _runtime.Settings.DeskName;
        _versionLabel.Text = RelayAppInfo.AppVersion;

        _printerComboBox.Items.Clear();

        foreach (var printer in InstalledPrinters.List())
        {
            _printerComboBox.Items.Add(printer);
        }

        var selectedIndex = _printerComboBox.Items.IndexOf(_runtime.Settings.PrinterName);

        if (selectedIndex >= 0)
        {
            _printerComboBox.SelectedIndex = selectedIndex;
        }
        else if (_printerComboBox.Items.Count > 0)
        {
            _printerComboBox.SelectedIndex = 0;
        }
    }

    private async Task SaveAsync()
    {
        if (_printerComboBox.SelectedItem is not string printerName)
        {
            MessageBox.Show(
                "Please select a printer.",
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        _saveButton.Enabled = false;

        try
        {
            await _runtime.UpdatePrinterAsync(printerName).ConfigureAwait(true);

            MessageBox.Show(
                $"Printer saved. {RelayProductName.DisplayName} will restart to apply the change.",
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            _requestRestart(RelayRestartReason.Reload);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _saveButton.Enabled = true;
        }
    }

    private void RerunSetup()
    {
        var result = MessageBox.Show(
            "This clears saved settings and opens the setup wizard again. Continue?",
            Text,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
        {
            return;
        }

        _requestRestart(RelayRestartReason.ResetSetup);
    }
}
