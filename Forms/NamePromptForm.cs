namespace HS175D
{
    /// <summary>Small reusable name-entry dialog for the survey "Mark Point" trigger
    /// (<see cref="AirSimPanel"/>). The dropdown lists existing survey group names so re-marking
    /// another corner of the same target is a pick, not a retype - typing a new name still starts a
    /// new group.</summary>
    public partial class NamePromptForm : Form
    {
        public string EnteredName { get; private set; } = "";

        readonly IReadOnlyList<string> _existingNames;

        public NamePromptForm(IReadOnlyList<string> existingNames, string? defaultText = null)
        {
            _existingNames = existingNames;
            InitializeComponent();
            Theme.Apply(this);
            cboName.Text = defaultText ?? "";
        }

        void NamePromptForm_Load(object? sender, EventArgs e)
        {
            cboName.Items.AddRange(_existingNames.ToArray());
            cboName.SelectAll();
            cboName.Focus();
        }

        void btnOk_Click(object? sender, EventArgs e)
        {
            EnteredName = cboName.Text.Trim();
            if (EnteredName.Length == 0) DialogResult = DialogResult.None;   // empty name - stay open
        }
    }
}
