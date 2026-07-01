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
    private readonly TextBox _setupCodeTextBox;
    private readonly Label _errorLabel;
    private readonly Button _continueButton;
    private readonly Label _deskNameLabel;
    private readonly ComboBox _printerComboBox;
    private readonly Button _finishButton;

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
        ClientSize = new Size(520, 380);
        MinimumSize = new Size(520, 380);

        _step1Panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
        };

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
            MaximumSize = new Size(460, 0),
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

        _step2Panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            Visible = false,
        };

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

        var step2Body = new Panel
        {
            Dock = DockStyle.Fill,
        };

        var deskNameCaption = new Label
        {
            Text = "Desk:",
            AutoSize = true,
            Location = new Point(0, 0),
        };

        _deskNameLabel = new Label
        {
            AutoSize = true,
            Location = new Point(48, 0),
        };

        var printerCaption = new Label
        {
            Text = "Printer:",
            AutoSize = true,
            Location = new Point(0, 36),
        };

        _printerComboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(0, 60),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Width = 460,
        };

        step2Body.Controls.Add(deskNameCaption);
        step2Body.Controls.Add(_deskNameLabel);
        step2Body.Controls.Add(printerCaption);
        step2Body.Controls.Add(_printerComboBox);

        _step2Panel.Controls.Add(step2Body);
        _step2Panel.Controls.Add(step2Footer);
        _step2Panel.Controls.Add(step2Title);

        Controls.Add(_step1Panel);
        Controls.Add(_step2Panel);
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
    }

    private void ClearError()
    {
        _errorLabel.Text = string.Empty;
        _errorLabel.Visible = false;
    }
}
