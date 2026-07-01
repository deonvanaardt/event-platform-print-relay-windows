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
        ClientSize = new Size(480, 280);

        _step1Panel = new Panel
        {
            Dock = DockStyle.Fill,
        };

        var step1Title = new Label
        {
            Text = "Paste your desk setup code",
            AutoSize = true,
            Location = new Point(16, 16),
            Font = new Font(Font, FontStyle.Bold),
        };

        _setupCodeTextBox = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Location = new Point(16, 48),
            Size = new Size(448, 140),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };

        _errorLabel = new Label
        {
            AutoSize = true,
            ForeColor = Color.DarkRed,
            Location = new Point(16, 196),
            MaximumSize = new Size(448, 0),
            Visible = false,
        };

        _continueButton = new Button
        {
            Text = "Continue",
            Location = new Point(368, 232),
            Size = new Size(96, 32),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
        };
        _continueButton.Click += ContinueButton_Click;

        _step1Panel.Controls.Add(step1Title);
        _step1Panel.Controls.Add(_setupCodeTextBox);
        _step1Panel.Controls.Add(_errorLabel);
        _step1Panel.Controls.Add(_continueButton);

        _step2Panel = new Panel
        {
            Dock = DockStyle.Fill,
            Visible = false,
        };

        var step2Title = new Label
        {
            Text = "Select printer",
            AutoSize = true,
            Location = new Point(16, 16),
            Font = new Font(Font, FontStyle.Bold),
        };

        var deskNameCaption = new Label
        {
            Text = "Desk:",
            AutoSize = true,
            Location = new Point(16, 52),
        };

        _deskNameLabel = new Label
        {
            AutoSize = true,
            Location = new Point(56, 52),
        };

        var printerCaption = new Label
        {
            Text = "Printer:",
            AutoSize = true,
            Location = new Point(16, 88),
        };

        _printerComboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(16, 112),
            Size = new Size(448, 28),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };

        _finishButton = new Button
        {
            Text = "Finish",
            Location = new Point(368, 232),
            Size = new Size(96, 32),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
        };
        _finishButton.Click += FinishButton_Click;

        _step2Panel.Controls.Add(step2Title);
        _step2Panel.Controls.Add(deskNameCaption);
        _step2Panel.Controls.Add(_deskNameLabel);
        _step2Panel.Controls.Add(printerCaption);
        _step2Panel.Controls.Add(_printerComboBox);
        _step2Panel.Controls.Add(_finishButton);

        Controls.Add(_step1Panel);
        Controls.Add(_step2Panel);
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
