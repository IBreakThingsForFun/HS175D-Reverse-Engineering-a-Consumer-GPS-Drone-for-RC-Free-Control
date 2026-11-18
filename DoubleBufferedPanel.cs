namespace HS175D
{
    /// <summary>Plain Panel with double-buffering turned on - Panel doesn't expose that property
    /// itself, and a custom-paint canvas (line/dot plotting) flickers badly without it.</summary>
    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        }
    }
}
