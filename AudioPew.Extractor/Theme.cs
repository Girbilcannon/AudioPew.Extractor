namespace AudioPew.Extractor;

internal static class Theme
{
    public static readonly Color WindowBg = Color.FromArgb(24, 26, 31);
    public static readonly Color PanelBg = Color.FromArgb(32, 35, 42);
    public static readonly Color InputBg = Color.FromArgb(17, 19, 24);
    public static readonly Color Border = Color.FromArgb(51, 56, 68);
    public static readonly Color Text = Color.FromArgb(240, 240, 240);
    public static readonly Color MutedText = Color.FromArgb(168, 173, 183);
    public static readonly Color Primary = Color.FromArgb(45, 125, 255);
    public static readonly Color PrimaryHover = Color.FromArgb(60, 140, 255);
    public static readonly Color Success = Color.FromArgb(76, 195, 111);
    public static readonly Color Warning = Color.FromArgb(229, 184, 76);
    public static readonly Color Error = Color.FromArgb(224, 98, 98);

    public static void ApplyForm(Form form)
    {
        form.BackColor = WindowBg;
        form.ForeColor = Text;
        form.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
    }

    public static void ApplyPanel(Control panel)
    {
        panel.BackColor = PanelBg;
        panel.ForeColor = Text;
    }

    public static void ApplyInput(Control control)
    {
        control.BackColor = InputBg;
        control.ForeColor = Text;
        control.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
    }

    public static void ApplyButton(Button button, bool primary = false)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = primary ? Primary : Border;
        button.FlatAppearance.MouseOverBackColor = primary ? PrimaryHover : Color.FromArgb(42, 46, 55);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(35, 38, 45);
        button.BackColor = primary ? Primary : Color.FromArgb(38, 42, 50);
        button.ForeColor = Color.White;
        button.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        button.Height = 34;
    }

    public static Label Label(string text, int x, int y, int width, int height = 22, bool muted = false)
    {
        return new Label
        {
            Text = text,
            Left = x,
            Top = y,
            Width = width,
            Height = height,
            ForeColor = muted ? MutedText : Text,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft
        };
    }
}
