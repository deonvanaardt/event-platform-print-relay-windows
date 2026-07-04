using EventPlatform.PrintRelay.App.Printing;
using EventPlatform.PrintRelay.Core.Setup;
using EventPlatform.PrintRelay.Core.SetupCode;
using EventPlatform.PrintRelay.Core.Settings;

namespace EventPlatform.PrintRelay.App.Setup;

internal sealed class SetupWizardForm : Form
{
    private readonly HttpClient _http;
    private readonly string _settingsPath;

    private readonly Panel _step1Panel;
    private readonly Panel _step2Panel;
    private TextBox _setupCodeTextBox = null!;
    private Label _errorLabel = null!;
    private Button _continueButton = null!;
    private Label _deskNameLabel = null!;
    private ComboBox _printerComboBox = null!;
    private Button _finishButton = null!;

    private DeskSetupCodePayload? _validatedPayload;

    public SetupWizardForm(HttpClient http, string settingsPath)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _settingsPath = settingsPath ?? throw new ArgumentNullException(nameof(settingsPath));

        Text = "Event Platform Print Relay — Setup";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(560, 400);
        MinimumSize = new Size(560, 400);

        _step1Panel = CreateStepPanel();
        _step2Panel = CreateStepPanel(visible: false);

        BuildStep1();
        BuildStep2();

        Controls.Add(_step1Panel);
        Controls.Add(_step2Panel);
    }

    private static Panel CreateStepPanel(bool visible = true)
    {
        return new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            Visible = visible,
        };
    }

    private void BuildStep1()
    {
        var step1Title = new Label
        {
            Text = "Paste your desk setup code",
            AutoSize = true,
            Dock = DockStyle.Top,
            Font = new Font(Font, FontStyle.Bold),
            Padding = new Padding(0, 0, 0, 12),
        };

        _continueButton = new Button
        {
            Text = "Continue",
            AutoSize = true,
            MinimumSize = new Size(96, 32),
        };
        _continueButton.Click += ContinueButton_Click;

        var step1Footer = CreateButtonFooter(_continueButton);

        _errorLabel = new Label
        {
            AutoSize = true,
            ForeColor = Color.DarkRed,
            Dock = DockStyle.Top,
            Padding = new Padding(0, 0, 0, 8),
            Visible = false,
        };

        _setupCodeTextBox = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill,
        };

        _step1Panel.Controls.Add(_setupCodeTextBox);
        _step1Panel.Controls.Add(_errorLabel);
        _step1Panel.Controls.Add(step1Footer);
        _step1Panel.Controls.Add(step1Title);
    }

    private void BuildStep2()
    {
        var step2Title = new Label
        {
            Text = "Select printer",
            AutoSize = true,
            Dock = DockStyle.Top,
            Font = new Font(Font, FontStyle.Bold),
            Padding = new Padding(0, 0, 0, 12),
        };

        _finishButton = new Button
        {
            Text = "Finish",
            AutoSize = true,
            MinimumSize = new Size(96, 32),
        };
        _finishButton.Click += FinishButton_Click;

        var step2Footer = CreateButtonFooter(_finishButton);

        var step2Body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            AutoSize = false,
        };
        step2Body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        step2Body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        step2Body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        step2Body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        step2Body.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

        var deskNameCaption = new Label
        {
            Text = "Desk:",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 0, 8, 12),
        };

        _deskNameLabel = new Label
        {
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 0, 0, 12),
        };

        var printerCaption = new Label
        {
            Text = "Printer:",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 0, 8, 8),
        };

        _printerComboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 8),
            IntegralHeight = false,
        };

        step2Body.Controls.Add(deskNameCaption, 0, 0);
        step2Body.Controls.Add(_deskNameLabel, 1, 0);
        step2Body.Controls.Add(printerCaption, 0, 1);
        step2Body.SetColumnSpan(printerCaption, 2);
        step2Body.Controls.Add(_printerComboBox, 0, 2);
        step2Body.SetColumnSpan(_printerComboBox, 2);

        _step2Panel.Controls.Add(step2Body);
        _step2Panel.Controls.Add(step2Footer);
        _step2Panel.Controls.Add(step2Title);
        _step2Panel.Resize += (_, _) => UpdatePrinterComboBoxLayout();
    }

    private static Panel CreateButtonFooter(Button button)
    {
        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            Padding = new Padding(0, 8, 0, 0),
        };

        var buttonBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
        };
        buttonBar.Controls.Add(button);
        footer.Controls.Add(buttonBar);

        return footer;
    }

    private void UpdatePrinterComboBoxLayout()
    {
        var availableWidth = _step2Panel.ClientSize.Width - _step2Panel.Padding.Horizontal;
        if (availableWidth <= 0)
        {
            return;
        }

        _printerComboBox.Width = availableWidth;
        _printerComboBox.DropDownWidth = availableWidth;
    }

    private async void ContinueButton_Click(object? sender, EventArgs e)
    {
        ClearError();
        SetStep1Enabled(false);

        try
        {
            var result = await SetupCodeValidation.ValidateAsync(
                _setupCodeTextBox.Text,
                _http,
                CancellationToken.None).ConfigureAwait(true);

            if (!result.Success)
            {
                ShowError(result.OperatorMessage ?? SetupValidationMessages.InvalidSetupCode);
                SetStep1Enabled(true);
                return;
            }

            _validatedPayload = result.Payload;
            ShowStep2();
        }
        catch (Exception)
        {
            ShowError(SetupValidationMessages.CouldNotConnect);
            SetStep1Enabled(true);
        }
    }

    private async void FinishButton_Click(object? sender, EventArgs e)
    {
        if (_validatedPayload is null)
        {
            ShowStep1();
            return;
        }

        if (_printerComboBox.SelectedItem is not string printerName)
        {
            MessageBox.Show(
                "Please select a printer.",
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        _finishButton.Enabled = false;

        try
        {
            var settings = new RelaySettings
            {
                Secret = _validatedPayload.Secret,
                ApiUrl = _validatedPayload.ApiUrl,
                DeskName = _validatedPayload.DeskName,
                PrinterName = printerName,
            };

            await RelaySettingsStore.SaveAsync(settings, _settingsPath).ConfigureAwait(true);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            _finishButton.Enabled = true;
        }
    }

    private void ShowStep2()
    {
        _deskNameLabel.Text = _validatedPayload?.DeskName ?? string.Empty;

        _printerComboBox.Items.Clear();
        foreach (var printer in InstalledPrinters.List())
        {
            _printerComboBox.Items.Add(printer);
        }

        if (_printerComboBox.Items.Count > 0)
        {
            _printerComboBox.SelectedIndex = 0;
        }

        _step1Panel.Visible = false;
        _step2Panel.Visible = true;
        UpdatePrinterComboBoxLayout();
    }

    private void ShowStep1()
    {
        _step2Panel.Visible = false;
        _step1Panel.Visible = true;
        SetStep1Enabled(true);
    }

    private void SetStep1Enabled(bool enabled)
    {
        _setupCodeTextBox.Enabled = enabled;
        _continueButton.Enabled = enabled;
    }

    private void ShowError(string message)
    {
        _errorLabel.Text = message;
        _errorLabel.Visible = true;
        _errorLabel.MaximumSize = new Size(
            _step1Panel.ClientSize.Width - _step1Panel.Padding.Horizontal,
            0);
    }

    private void ClearError()
    {
        _errorLabel.Text = string.Empty;
        _errorLabel.Visible = false;
    }
}
