using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DockyJumpList.Services
{
    /// <summary>
    /// Custom renderer that gives the tray context menu a clean dark appearance
    /// matching a modern Windows tool aesthetic.
    /// </summary>
    public class DockyMenuRenderer : ToolStripProfessionalRenderer
    {
        private static readonly Color BackgroundColor   = Color.FromArgb(28,  28,  30);
        private static readonly Color HoverColor        = Color.FromArgb(44,  44,  48);
        private static readonly Color TextColor         = Color.FromArgb(240, 240, 245);
        private static readonly Color SeparatorColor    = Color.FromArgb(55,  55,  60);
        private static readonly Color AccentColor       = Color.FromArgb(0,   120, 212);
        private static readonly Color BorderColor       = Color.FromArgb(50,  50,  55);

        public DockyMenuRenderer() : base(new DockyColorTable()) { }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            e.Graphics.FillRectangle(new SolidBrush(BackgroundColor), e.AffectedBounds);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var rect = new Rectangle(4, 0, e.Item.Width - 8, e.Item.Height);
            if (e.Item.Selected && e.Item.Enabled)
            {
                using var brush = new SolidBrush(HoverColor);
                using var path  = RoundedRect(rect, 5);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillPath(brush, path);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? TextColor : Color.FromArgb(100, 100, 105);
            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            var y = e.Item.Height / 2;
            using var pen = new Pen(SeparatorColor);
            e.Graphics.DrawLine(pen, 8, y, e.Item.Width - 8, y);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            var rect = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
            using var pen = new Pen(BorderColor);
            e.Graphics.DrawRectangle(pen, rect);
        }

        protected override void OnRenderItemImage(ToolStripItemImageRenderEventArgs e)
        {
            if (e.Image == null) return;
            var rect = e.ImageRectangle;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.DrawImage(e.Image, rect);
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal class DockyColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Color.FromArgb(28, 28, 30);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(44, 44, 48);
        public override Color MenuItemSelectedGradientEnd   => Color.FromArgb(44, 44, 48);
        public override Color MenuItemBorder                => Color.FromArgb(50, 50, 55);
        public override Color MenuBorder                    => Color.FromArgb(50, 50, 55);
        public override Color ImageMarginGradientBegin      => Color.FromArgb(35, 35, 38);
        public override Color ImageMarginGradientMiddle     => Color.FromArgb(35, 35, 38);
        public override Color ImageMarginGradientEnd        => Color.FromArgb(35, 35, 38);
        public override Color SeparatorDark                 => Color.FromArgb(55, 55, 60);
        public override Color SeparatorLight                => Color.FromArgb(55, 55, 60);
    }
}
