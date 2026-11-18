namespace HS175D.Forms
{
    // Real WinForms-Designer-style companion to WelcomeForm.cs: every control is a plain field,
    // built with a bare "new Type()" and given its properties one flat statement at a time, with
    // absolute Locations baked in - same convention as AirSimForm.Designer.cs / Nrf24Panel.Designer.cs.
    public sealed partial class WelcomeForm
    {
        Label _title;
        Label _subtitle;
        Button _btnStudio;
        Label _lblStudioDesc;
        Button _btnWorkbench;
        Label _lblWorkbenchDesc;

        System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        void InitializeComponent()
        {
            _title = new Label();
            _subtitle = new Label();
            _btnStudio = new Button();
            _lblStudioDesc = new Label();
            _btnWorkbench = new Button();
            _lblWorkbenchDesc = new Label();
            SuspendLayout();
            // 
            // _title
            // 
            _title.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            _title.Location = new Point(20, 18);
            _title.Name = "_title";
            _title.Size = new Size(320, 25);
            _title.TabIndex = 0;
            _title.Text = "HS175D";
            _title.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _subtitle
            // 
            _subtitle.Font = new Font("Segoe UI", 8.25F);
            _subtitle.Location = new Point(21, 43);
            _subtitle.Name = "_subtitle";
            _subtitle.Size = new Size(320, 18);
            _subtitle.TabIndex = 1;
            _subtitle.Text = "Open:";
            _subtitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _btnStudio
            // 
            _btnStudio.FlatStyle = FlatStyle.Flat;
            _btnStudio.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            _btnStudio.Location = new Point(20, 70);
            _btnStudio.Name = "_btnStudio";
            _btnStudio.Padding = new Padding(10, 0, 0, 0);
            _btnStudio.Size = new Size(322, 34);
            _btnStudio.TabIndex = 2;
            _btnStudio.Text = "Drone RE Studio";
            _btnStudio.TextAlign = ContentAlignment.MiddleLeft;
            _btnStudio.Click += OnStudioClick;
            // 
            // _lblStudioDesc
            // 
            _lblStudioDesc.Font = new Font("Segoe UI", 8F);
            _lblStudioDesc.Location = new Point(22, 105);
            _lblStudioDesc.Name = "_lblStudioDesc";
            _lblStudioDesc.Size = new Size(320, 17);
            _lblStudioDesc.TabIndex = 3;
            _lblStudioDesc.Text = "Main cockpit — live drone control, video, telemetry.";
            _lblStudioDesc.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _btnWorkbench
            // 
            _btnWorkbench.FlatStyle = FlatStyle.Flat;
            _btnWorkbench.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            _btnWorkbench.Location = new Point(20, 131);
            _btnWorkbench.Name = "_btnWorkbench";
            _btnWorkbench.Padding = new Padding(10, 0, 0, 0);
            _btnWorkbench.Size = new Size(322, 34);
            _btnWorkbench.TabIndex = 4;
            _btnWorkbench.Text = "RE Workbench";
            _btnWorkbench.TextAlign = ContentAlignment.MiddleLeft;
            _btnWorkbench.Click += OnWorkbenchClick;
            // 
            // _lblWorkbenchDesc
            // 
            _lblWorkbenchDesc.Font = new Font("Segoe UI", 8F);
            _lblWorkbenchDesc.Location = new Point(22, 167);
            _lblWorkbenchDesc.Name = "_lblWorkbenchDesc";
            _lblWorkbenchDesc.Size = new Size(320, 17);
            _lblWorkbenchDesc.TabIndex = 5;
            _lblWorkbenchDesc.Text = "Monitor, inject, replay, packet catalog.";
            _lblWorkbenchDesc.TextAlign = ContentAlignment.MiddleLeft;
            //
            // WelcomeForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(362, 316);
            Controls.Add(_title);
            Controls.Add(_subtitle);
            Controls.Add(_btnStudio);
            Controls.Add(_lblStudioDesc);
            Controls.Add(_btnWorkbench);
            Controls.Add(_lblWorkbenchDesc);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "WelcomeForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "HS175D — Launcher";
            ResumeLayout(false);
        }
    }
}
