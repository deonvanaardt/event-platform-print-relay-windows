using EventPlatform.PrintRelay.Core.Diagnostics;
using EventPlatform.PrintRelay.Core.Polling;

namespace EventPlatform.PrintRelay.App.Tray;

internal sealed class StatusForm : Form
{
    private readonly RelayRuntime _runtime;

    private readonly ListBox _checklistBox = new();
    private readonly ListBox _activityBox = new();
    private readonly DataGridView _recentJobsGrid = new();
    private readonly Panel _technicalPanel = new();
    private readonly Label _technicalLabel = new();
    private readonly CheckBox _technicalToggle = new();

    public StatusForm(RelayRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

        Text = "Print Relay — Status";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(640, 520);
        ClientSize = new Size(720, 560);

        BuildLayout();
        RefreshView();

        _runtime.SessionState.Changed += OnSessionChanged;
        FormClosed += (_, _) => _runtime.SessionState.Changed -= OnSessionChanged;
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(12),
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 35f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 35f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 30f));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var intro = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Text =
                "Use this panel while testing check-in. Desk ID appears when the platform sends a job to this relay secret.",
            Padding = new Padding(0, 0, 0, 8),
        };

        _checklistBox.Dock = DockStyle.Fill;
        _checklistBox.IntegralHeight = false;
        _checklistBox.Font = new Font(Font.FontFamily, 10f);

        _activityBox.Dock = DockStyle.Fill;
        _activityBox.IntegralHeight = false;
        _activityBox.Font = new Font(Font.FontFamily, 9.5f);

        _recentJobsGrid.Dock = DockStyle.Fill;
        _recentJobsGrid.ReadOnly = true;
        _recentJobsGrid.AllowUserToAddRows = false;
        _recentJobsGrid.AllowUserToDeleteRows = false;
        _recentJobsGrid.RowHeadersVisible = false;
        _recentJobsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _recentJobsGrid.Columns.Add("time", "Time");
        _recentJobsGrid.Columns.Add("job", "Job");
        _recentJobsGrid.Columns.Add("event", "Event");
        _recentJobsGrid.Columns.Add("outcome", "Outcome");

        _technicalToggle.AutoSize = true;
        _technicalToggle.Text = "Show technical details";
        _technicalToggle.CheckedChanged += (_, _) => RefreshView();

        _technicalPanel.Dock = DockStyle.Fill;
        _technicalPanel.Visible = false;
        _technicalPanel.Padding = new Padding(0, 8, 0, 0);

        _technicalLabel.AutoSize = false;
        _technicalLabel.Dock = DockStyle.Fill;
        _technicalLabel.Font = new Font("Consolas", 9f);
        _technicalPanel.Controls.Add(_technicalLabel);

        root.Controls.Add(intro, 0, 0);
        root.Controls.Add(CreateSection("Connection checklist", _checklistBox), 0, 1);
        root.Controls.Add(CreateSection("Live activity", _activityBox), 0, 2);
        root.Controls.Add(CreateSection("Recent jobs", _recentJobsGrid), 0, 3);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0),
        };

        var copyDiagnosticsButton = new Button
        {
            AutoSize = true,
            Text = "Export diagnostics",
        };
        copyDiagnosticsButton.Click += (_, _) => ExportDiagnostics();

        footer.Controls.Add(copyDiagnosticsButton);
        footer.Controls.Add(_technicalToggle);
        footer.Controls.Add(_technicalPanel);
        _technicalPanel.Dock = DockStyle.Top;
        root.Controls.Add(footer, 0, 4);

        Controls.Add(root);
    }

    private static GroupBox CreateSection(string title, Control content)
    {
        var group = new GroupBox
        {
            Text = title,
            Dock = DockStyle.Fill,
            Padding = new Padding(8),
        };
        group.Controls.Add(content);
        return group;
    }

    private void OnSessionChanged()
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(RefreshView);
            return;
        }

        RefreshView();
    }

    private void RefreshView()
    {
        var snapshot = _runtime.SessionState.GetSnapshot(_runtime.Settings);

        _checklistBox.Items.Clear();
        _checklistBox.Items.Add(ChecklistLine(true, "Setup complete"));
        _checklistBox.Items.Add(ChecklistLine(
            snapshot.LastSuccessfulPollUtc.HasValue,
            "API reachable (last poll succeeded or connection test)"));
        _checklistBox.Items.Add(ChecklistLine(
            snapshot.ConnectionState != PrintRelayPollConnectionState.AuthError,
            "Auth valid"));
        _checklistBox.Items.Add(ChecklistLine(snapshot.PrinterInstalled, "Printer installed"));
        _checklistBox.Items.Add(ChecklistLine(
            snapshot.JobsReceivedThisSession > 0,
            "Jobs received this session"));
        _checklistBox.Items.Add(
            $"Last job outcome: {snapshot.LastJob?.Outcome ?? "none"}");

        _activityBox.Items.Clear();

        foreach (var activity in snapshot.ActivityEvents)
        {
            _activityBox.Items.Add(
                $"{activity.TimestampUtc.ToLocalTime():HH:mm:ss} — {activity.Message}");
        }

        if (_activityBox.Items.Count > 0)
        {
            _activityBox.TopIndex = _activityBox.Items.Count - 1;
        }

        _recentJobsGrid.Rows.Clear();

        foreach (var job in snapshot.RecentJobs)
        {
            _recentJobsGrid.Rows.Add(
                job.ReceivedAtUtc.ToLocalTime().ToString("HH:mm:ss"),
                ShortId(job.JobId),
                ShortId(job.EventId),
                job.Outcome ?? "pending");
        }

        _technicalPanel.Visible = _technicalToggle.Checked;

        if (_technicalToggle.Checked)
        {
            _technicalLabel.Text =
                $"App version: {RelayAppInfo.AppVersion}{Environment.NewLine}" +
                $"API hostname: {snapshot.ApiHostname}{Environment.NewLine}" +
                $"Last poll: {FormatTimestamp(snapshot.LastSuccessfulPollUtc)} ({snapshot.LastPollLatencyMs} ms){Environment.NewLine}" +
                $"Pending jobs (last poll): {snapshot.LastPendingJobCount}{Environment.NewLine}" +
                $"WebView2: {RelayAppInfo.TryGetWebView2Version() ?? "unknown"}{Environment.NewLine}" +
                $"Desk ID: {snapshot.LastJob?.DeskId ?? "(waiting for first job)"}{Environment.NewLine}" +
                $"Event ID: {snapshot.LastJob?.EventId ?? "(waiting for first job)"}{Environment.NewLine}" +
                $"Job ID: {snapshot.LastJob?.JobId ?? "(none)"}{Environment.NewLine}" +
                $"Registration ID: {snapshot.LastJob?.RegistrationId ?? "(none)"}";
        }
    }

    private void ExportDiagnostics()
    {
        try
        {
            var json = _runtime.BuildDiagnosticsJson();
            var path = RelayDiagnosticsExporter.SaveExport(json);

            MessageBox.Show(
                this,
                $"Diagnostics saved to:{Environment.NewLine}{path}{Environment.NewLine}{Environment.NewLine}" +
                "Attach this file to a support ticket or open it and copy the contents.",
                "Print Relay",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Print Relay",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static string ChecklistLine(bool ok, string text) => $"{(ok ? "✓" : "○")} {text}";

    private static string ShortId(string value) =>
        value.Length <= 8 ? value : $"…{value[^8..]}";

    private static string FormatTimestamp(DateTimeOffset? value) =>
        value?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "never";
}
