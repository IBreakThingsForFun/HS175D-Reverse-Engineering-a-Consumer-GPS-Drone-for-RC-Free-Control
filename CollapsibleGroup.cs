using System.ComponentModel;

namespace HS175D
{
    /// <summary>
    /// A titled, collapsible container: a clickable header ("▾ Title" / "▸ Title") that toggles
    /// Content's visibility and the group's own Height between "just the header" and "header +
    /// Content". Raises ExpandedChanged so a parent holding several of these stacked vertically can
    /// re-run its own stacking whenever one of them toggles - the group itself has no idea what's
    /// above or below it, it just reports its own size changing.
    ///
    /// Content uses the same DesignerSerializationVisibility.Content technique as FlatScrollHost's own
    /// Content (see FlatScrollHost.cs's comment) - a plain get-only Panel property wouldn't survive the
    /// Designer's regenerate pass otherwise.
    /// </summary>
    public class CollapsibleGroup : Panel
    {
        public const int HeaderHeight = 24;

        readonly Button _header = new();

        /// <summary>The clickable header itself, for styling from the outside (BackColor/ForeColor/
        /// Font) - construction/parenting/the click-to-toggle wiring all happen once, here.</summary>
        public Button Header => _header;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Panel Content { get; } = new();

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string Title { get => _title; set { _title = value; UpdateHeaderText(); } }
        string _title = "";

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool Expanded
        {
            get => _expanded;
            set
            {
                if (_expanded == value) return;
                _expanded = value;
                Apply();
                ExpandedChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        bool _expanded = true;

        public event EventHandler? ExpandedChanged;

        public CollapsibleGroup()
        {
            DoubleBuffered = true;
            _header.Name = "_header";
            _header.FlatStyle = FlatStyle.Flat;
            _header.TextAlign = ContentAlignment.MiddleLeft;
            _header.TabStop = false;
            _header.Location = new Point(0, 0);
            _header.FlatAppearance.BorderSize = 0;
            _header.Click += (s, e) => Expanded = !Expanded;
            Content.Location = new Point(0, HeaderHeight);
            Controls.Add(Content);
            Controls.Add(_header);
            UpdateHeaderText();
            Apply();
        }

        void UpdateHeaderText() => _header.Text = (_expanded ? "▾ " : "▸ ") + _title;

        void Apply()
        {
            UpdateHeaderText();
            Content.Visible = _expanded;
            Height = HeaderHeight + (_expanded ? Content.Height : 0);
        }

        /// <summary>Call after changing Content's own children/height so the group's total Height (and,
        /// via ExpandedChanged, whatever's stacked below it) catches up.</summary>
        public void RecalcHeight()
        {
            Apply();
            ExpandedChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            _header.Width = Width;
            Content.Width = Width;
        }
    }
}
