namespace VanDao.Desktop;

internal static class Theme
{
    public static readonly Color Ink = Color.FromArgb(14, 20, 22);
    public static readonly Color Bar = Color.FromArgb(20, 29, 32);
    public static readonly Color Line = Color.FromArgb(43, 58, 62);
    public static readonly Color Hover = Color.FromArgb(36, 50, 54);
    public static readonly Color Paper = Color.FromArgb(235, 228, 211);
    public static readonly Color Muted = Color.FromArgb(151, 165, 162);
    public static readonly Color Gold = Color.FromArgb(230, 196, 106);

    public static ToolStripRenderer Renderer { get; } = new DarkRenderer();

    private sealed class DarkRenderer : ToolStripProfessionalRenderer
    {
        public DarkRenderer() : base(new DarkColors()) => RoundedEdges = false;

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? Paper : Muted;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = Paper;
            base.OnRenderArrow(e);
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            // dấu tích vàng, dễ thấy trên nền tối
            var r = e.ImageRectangle;
            using var pen = new Pen(Gold, 2f);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.DrawLines(pen, new[]
            {
                new PointF(r.Left + r.Width * .2f, r.Top + r.Height * .55f),
                new PointF(r.Left + r.Width * .42f, r.Top + r.Height * .75f),
                new PointF(r.Left + r.Width * .8f, r.Top + r.Height * .3f),
            });
        }
    }

    private sealed class DarkColors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Bar;
        public override Color ImageMarginGradientBegin => Bar;
        public override Color ImageMarginGradientMiddle => Bar;
        public override Color ImageMarginGradientEnd => Bar;
        public override Color MenuBorder => Line;
        public override Color MenuItemBorder => Line;
        public override Color MenuItemSelected => Hover;
        public override Color MenuItemSelectedGradientBegin => Hover;
        public override Color MenuItemSelectedGradientEnd => Hover;
        public override Color MenuItemPressedGradientBegin => Hover;
        public override Color MenuItemPressedGradientEnd => Hover;
        public override Color CheckBackground => Bar;
        public override Color CheckSelectedBackground => Hover;
        public override Color CheckPressedBackground => Hover;
        public override Color SeparatorDark => Line;
        public override Color SeparatorLight => Bar;
    }
}
