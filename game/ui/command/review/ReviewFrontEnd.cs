using Godot;

namespace Overmatch.Game.UiReview;

/// <summary>A staged title and deployment interface. Options never alter a match.</summary>
public partial class ReviewFrontEnd : Control
{
    public Action? DeployRequested;
    public Action? HudRequested;
    public bool SetupInitially { get; set; }

    private Control _page = null!;
    private bool _setup;
    private readonly int[] _controllers = { 0, 1, 2, 2 };
    private readonly int[] _factions = { 0, 1, 2, 0 };
    private readonly int[] _difficulty = { 1, 1, 1, 1 };
    private int _cash = 1;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        Resized += () => ShowPage(_setup);
        ShowPage(SetupInitially);
    }

    private void ShowPage(bool setup)
    {
        _setup = setup;
        if (_page is not null)
        {
            RemoveChild(_page);
            _page.QueueFree();
        }
        _page = new Control { Name = setup ? "DeploymentSetup" : "TitleMenu", MouseFilter = MouseFilterEnum.Ignore };
        _page.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_page);
        if (setup) BuildSetup(); else BuildTitle();
        Footer();
        QueueRedraw();
    }

    public override void _Draw()
    {
        float w = Mathf.Max(Size.X, 1600), h = Mathf.Max(Size.Y, 900);
        DrawRect(new Rect2(0, 0, w, h), new Color("0d1417"));
        // A calm graphite falloff, built as vector strips rather than an imported image.
        for (int x = 0; x < w; x += 8)
        {
            var t = x / w;
            var glow = Mathf.Sin(t * Mathf.Pi) * .035f;
            DrawRect(new Rect2(x, 0, 8, h), new Color(.045f + glow, .064f + glow, .07f + glow));
        }
        for (int x = 40; x < w; x += 80)
            DrawLine(new Vector2(x, 0), new Vector2(x, h), new Color(.65f, .75f, .75f, .025f));
        for (int y = 20; y < h; y += 80)
            DrawLine(new Vector2(0, y), new Vector2(w, y), new Color(.65f, .75f, .75f, .025f));
        // Offset contour lines recall a command-room tactical plot without changing terrain art.
        for (int i = 0; i < 7; i++)
        {
            float d = i * 29;
            Vector2[] contour = { new(570 + d, h), new(640 + d, 700), new(770 + d, 640),
                new(740 + d, 480), new(850 + d, 360), new(920 + d, 140), new(1100 + d, 0) };
            DrawPolyline(contour, new Color(.5f, .61f, .61f, .045f), 1, true);
        }
        DrawLine(new Vector2(64, 54), new Vector2(1536, 54), CommandTheme.Line.Darkened(.48f), 1);
        DrawLine(new Vector2(64, h - 68), new Vector2(1536, h - 68), CommandTheme.Line.Darkened(.48f), 1);
        DrawLine(new Vector2(64, 54), new Vector2(146, 54), CommandTheme.Gold, 3);
        DrawLine(new Vector2(64, h - 68), new Vector2(146, h - 68), CommandTheme.Gold.Darkened(.25f), 2);
        if (!_setup)
        {
            DrawLine(new Vector2(825, 126), new Vector2(825, h - 126), CommandTheme.Line.Darkened(.45f), 1);
            DrawCircle(new Vector2(1186, 315), 165, new Color(.45f, .65f, .72f, .025f));
            DrawArc(new Vector2(1186, 315), 175, 0, Mathf.Tau, 96, new Color(.45f, .65f, .72f, .1f), 1, true);
            DrawArc(new Vector2(1186, 315), 183, -.35f, .9f, 24, new Color(.91f, .74f, .4f, .55f), 2, true);
        }
    }

    private void BuildTitle()
    {
        LabelAt("REAL-TIME STRATEGY  /  2030s", 70, 108, 670, 30, 19, CommandTheme.Gold, true);
        LabelAt("OVERMATCH", 60, 141, 735, 154, 132, CommandTheme.Text, true);
        LabelAt("THE NEXT WAR IS ALREADY HERE.", 70, 298, 680, 36, 27, CommandTheme.Muted, true);
        LabelAt("Three doctrines. One battlefield.\nEstablish your command and decide what survives.",
            72, 352, 630, 74, 21, CommandTheme.Muted);

        ButtonAt("", 72, 481, 598, 76, () => ShowPage(true), true);
        LabelAt("01    SKIRMISH", 103, 489, 470, 33, 25, CommandTheme.Gold, true);
        LabelAt("Configure a local engagement", 103, 523, 440, 24, 16, CommandTheme.Muted);
        LabelAt("›", 617, 489, 32, 45, 34, CommandTheme.Gold, true);

        ButtonAt("", 72, 573, 598, 76, () => HudRequested?.Invoke());
        LabelAt("02    FIELD CONSOLE", 103, 581, 480, 33, 25, CommandTheme.Text, true);
        LabelAt("Inspect the battlefield command interface", 103, 615, 482, 24, 16, CommandTheme.Muted);
        LabelAt("›", 617, 581, 32, 45, 34, CommandTheme.Muted, true);
        LabelAt("COMMAND INTERFACE  /  DESIGN STUDY 01", 73, 687, 650, 27, 16, CommandTheme.Muted.Darkened(.15f), true);

        LabelAt("FACTION INTELLIGENCE", 940, 111, 492, 30, 19, CommandTheme.Muted, true);
        LabelAt("01 / 03", 1383, 114, 80, 25, 17, CommandTheme.Gold, true, HorizontalAlignment.Right);
        ImageAt("coalition", 1083, 193, 206, 238, CommandTheme.Text);
        LabelAt("COALITION", 932, 499, 524, 60, 56, CommandTheme.Text, true, HorizontalAlignment.Center);
        LabelAt("PRECISION. SUPERIORITY. CONTROL.", 912, 567, 564, 30, 20, CommandTheme.Blue, true, HorizontalAlignment.Center);
        LabelAt("An elite force built around air power, precision fires\nand advanced battlefield systems.",
            930, 615, 532, 69, 21, CommandTheme.Muted, false, HorizontalAlignment.Center);

        Doctrine(918, 710, "airfield", "AIR POWER");
        Doctrine(1110, 710, "attack", "PRECISION");
        Doctrine(1302, 710, "satellite", "INTELLIGENCE");
    }

    private void BuildSetup()
    {
        LabelAt("OPERATIONS / LOCAL ENGAGEMENT", 68, 86, 990, 28, 18, CommandTheme.Gold, true);
        LabelAt("SKIRMISH", 63, 113, 1120, 82, 70, CommandTheme.Text, true);
        LabelAt("Set the battlefield. Choose your doctrine.", 68, 198, 1000, 31, 22, CommandTheme.Muted);

        PanelAt(64, 255, 500, 475, true);
        PanelAt(592, 255, 944, 475, false);
        LabelAt("01 / BATTLEFIELD", 90, 273, 430, 28, 19, CommandTheme.Gold, true);
        LabelAt("02 / COMMANDERS", 618, 273, 640, 28, 19, CommandTheme.Gold, true);

        var map = new BattlefieldSchematic { Position = new Vector2(90, 320), Size = new Vector2(446, 252) };
        _page.AddChild(map);
        LabelAt("DRY PLAIN", 90, 593, 430, 44, 34, CommandTheme.Text, true);
        LabelAt("4 POSITIONS   /   96 × 96   /   OPEN TERRAIN", 92, 641, 438, 27, 16, CommandTheme.Muted, true);
        LabelAt("Central crossings. Distributed supply. Room to manoeuvre.",
            92, 679, 430, 40, 17, CommandTheme.Muted);

        LabelAt("SLOT", 619, 322, 67, 27, 15, CommandTheme.Muted, true);
        LabelAt("COMMAND", 701, 322, 190, 27, 15, CommandTheme.Muted, true);
        LabelAt("FACTION", 929, 322, 230, 27, 15, CommandTheme.Muted, true);
        LabelAt("DIFFICULTY", 1190, 322, 270, 27, 15, CommandTheme.Muted, true);
        for (int i = 0; i < 4; i++) BuildSlot(i, 359 + i * 70);

        LabelAt("STARTING FUNDS", 619, 663, 203, 28, 16, CommandTheme.Muted, true);
        var cash = Picker(new[] { "$ 5,000", "$ 10,000", "$ 20,000", "$ 50,000" }, _cash, 809, 653, 194);
        cash.ItemSelected += n => _cash = (int)n;
        LabelAt("PRESENTATION PRESET", 1038, 663, 467, 27, 15, CommandTheme.Muted.Darkened(.1f), true,
            HorizontalAlignment.Right);

        ButtonAt("‹    BACK", 64, 761, 212, 64, () => ShowPage(false));
        LabelAt("STAGED DEPLOYMENT", 626, 765, 490, 27, 18, CommandTheme.Text, true);
        LabelAt("Preview the field console with the existing battlefield assets.",
            626, 793, 540, 28, 17, CommandTheme.Muted);
        ButtonAt("PREVIEW DEPLOYMENT    ›", 1194, 761, 342, 64, () => DeployRequested?.Invoke(), true);
    }

    private void BuildSlot(int index, float y)
    {
        var active = _controllers[index] != 2;
        var colour = index == 0 ? CommandTheme.Blue : index == 1 ? CommandTheme.Red : CommandTheme.Muted;
        var badge = new ColorRect { Position = new Vector2(619, y + 9), Size = new Vector2(5, 34), Color = active ? colour : colour.Darkened(.55f), MouseFilter = MouseFilterEnum.Ignore };
        _page.AddChild(badge);
        LabelAt($"0{index + 1}", 640, y + 5, 50, 40, 24, active ? CommandTheme.Text : CommandTheme.Muted.Darkened(.3f), true);
        var command = Picker(new[] { "Human", "AI Commander", "Closed" }, _controllers[index], 701, y, 203);
        if (index == 0) command.Disabled = true;
        else command.SetItemDisabled(0, true);
        command.ItemSelected += n => { _controllers[index] = (int)n; ShowPage(true); };
        var faction = Picker(new[] { "Coalition", "Directorate", "Network", "Random" }, _factions[index], 929, y, 236);
        faction.Disabled = !active;
        faction.ItemSelected += n => _factions[index] = (int)n;
        var difficulty = Picker(new[] { "Easy", "Medium", "Hard", "Brutal" }, _difficulty[index], 1190, y, 318);
        difficulty.Disabled = _controllers[index] != 1;
        if (_controllers[index] == 0) difficulty.Text = "PLAYER CONTROL";
        if (!active) difficulty.Text = "—";
        difficulty.ItemSelected += n => _difficulty[index] = (int)n;
    }

    private void Footer()
    {
        float y = Mathf.Max(Size.Y, 900) - 52;
        LabelAt("OVERMATCH  /  COMMAND SYSTEMS", 65, 18, 720, 27, 16, CommandTheme.Muted, true);
        LabelAt("UI REVIEW  /  STAGED SCENE", 1030, 18, 505, 27, 16, CommandTheme.Gold, true, HorizontalAlignment.Right);
        LabelAt("INTERFACE PROTOTYPE", 65, y, 410, 27, 15, CommandTheme.Muted, true);
        LabelAt("LOCAL REVIEW   ·   ORIGINAL ART DIRECTION", 893, y, 643, 27, 15, CommandTheme.Muted, true, HorizontalAlignment.Right);
    }

    private void Doctrine(float x, float y, string icon, string text)
    {
        ImageAt(icon, x + 63, y, 30, 30, CommandTheme.Gold.Darkened(.1f));
        LabelAt(text, x, y + 44, 158, 27, 16, CommandTheme.Muted, true, HorizontalAlignment.Center);
    }

    private void PanelAt(float x, float y, float w, float h, bool accent)
    {
        _page.AddChild(new ConsolePanel { Position = new Vector2(x, y), Size = new Vector2(w, h),
            Fill = new Color("172124"), Edge = CommandTheme.Line.Darkened(.25f), Accent = accent });
    }

    private Label LabelAt(string text, float x, float y, float w, float h, int size, Color color,
        bool display = false, HorizontalAlignment align = HorizontalAlignment.Left)
    {
        var label = new Label { Text = text, Position = new Vector2(x, y), Size = new Vector2(w, h),
            HorizontalAlignment = align, VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        label.AddThemeFontOverride("font", display ? CommandTheme.Display : CommandTheme.Body);
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        _page.AddChild(label);
        return label;
    }

    private Button ButtonAt(string text, float x, float y, float w, float h, Action action, bool accent = false)
    {
        var button = new Button { Text = text, Position = new Vector2(x, y), Size = new Vector2(w, h), MouseDefaultCursorShape = CursorShape.PointingHand };
        CommandTheme.Style(button, accent);
        button.AddThemeFontSizeOverride("font_size", 24);
        button.Pressed += action;
        _page.AddChild(button);
        return button;
    }

    private OptionButton Picker(string[] items, int selected, float x, float y, float width)
    {
        var option = new OptionButton { Position = new Vector2(x, y), Size = new Vector2(width, 51),
            MouseDefaultCursorShape = CursorShape.PointingHand };
        foreach (var item in items) option.AddItem(item);
        option.Select(selected);
        CommandTheme.Style(option);
        option.AddThemeFontOverride("font", CommandTheme.Body);
        option.AddThemeFontSizeOverride("font_size", 21);
        option.AddThemeConstantOverride("arrow_margin", 12);
        var popup = option.GetPopup();
        popup.AddThemeFontOverride("font", CommandTheme.Body);
        popup.AddThemeFontSizeOverride("font_size", 21);
        popup.AddThemeColorOverride("font_color", CommandTheme.Text);
        popup.AddThemeColorOverride("font_hover_color", CommandTheme.Gold);
        popup.AddThemeStyleboxOverride("panel", CommandTheme.Box(CommandTheme.Panel, CommandTheme.Line));
        popup.AddThemeStyleboxOverride("hover", CommandTheme.Box(CommandTheme.Raised, CommandTheme.Gold));
        popup.AddThemeConstantOverride("v_separation", 15);
        _page.AddChild(option);
        return option;
    }

    private void ImageAt(string name, float x, float y, float w, float h, Color color)
    {
        _page.AddChild(new TextureRect { ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, Texture = CommandTheme.Icon(name), Position = new Vector2(x, y),
            Size = new Vector2(w, h),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, Modulate = color,
            MouseFilter = MouseFilterEnum.Ignore });
    }
}

/// <summary>Original map-selection schematic based on the existing Dry Plain geometry.</summary>
internal partial class BattlefieldSchematic : Control
{
    public override void _Ready() { MouseFilter = MouseFilterEnum.Ignore; }
    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color("101a1e"));
        const float scale = 2.24f;
        var origin = new Vector2((Size.X - 96 * scale) * .5f, 18);
        Vector2 P(float x, float y) => origin + new Vector2(x, 96 - y) * scale;
        void Block(float x, float y, float w, float h, Color color)
            => DrawRect(new Rect2(P(x, y + h), new Vector2(w, h) * scale), color);
        var terrain = new Color("293330");
        DrawRect(new Rect2(origin, new Vector2(96, 96) * scale), terrain);
        Block(0, 46, 96, 3, new Color("687365"));
        Block(60, 10, 20, 14, new Color("43483b"));
        Block(10, 62, 18, 16, new Color("43483b"));
        Block(40, 70, 14, 8, new Color("264650"));
        foreach (var rect in new[] { new Rect2(30, 30, 4, 14), new Rect2(34, 30, 10, 3), new Rect2(62, 52, 4, 14), new Rect2(52, 63, 10, 3), new Rect2(50, 20, 3, 5), new Rect2(20, 50, 5, 3), new Rect2(78, 36, 4, 4), new Rect2(14, 84, 6, 3) })
            Block(rect.Position.X, rect.Position.Y, rect.Size.X, rect.Size.Y, new Color("758076"));
        for (int c = 0; c <= 96; c += 16)
        {
            DrawLine(P(c, 0), P(c, 96), new Color(1, 1, 1, .055f));
            DrawLine(P(0, c), P(96, c), new Color(1, 1, 1, .055f));
        }
        foreach (var point in new[] { new Vector2(26, 8), new Vector2(6, 26), new Vector2(70, 88), new Vector2(90, 70), new Vector2(6, 70), new Vector2(26, 90), new Vector2(90, 26), new Vector2(70, 6), new Vector2(40, 40), new Vector2(56, 56) })
            DrawRect(new Rect2(P(point.X, point.Y) - new Vector2(2, 2), new Vector2(4, 4)), CommandTheme.Gold);
        var spawns = new[] { P(16, 16), P(80, 80), P(16, 80), P(80, 16) };
        for (int i = 0; i < spawns.Length; i++)
        {
            Color colour = i == 0 ? CommandTheme.Blue : i == 1 ? CommandTheme.Red : CommandTheme.Muted;
            DrawCircle(spawns[i], 11, CommandTheme.Ink);
            DrawArc(spawns[i], 11, 0, Mathf.Tau, 24, colour, 1, true);
            DrawString(CommandTheme.Display, spawns[i] + new Vector2(-4, 6), $"{i + 1}", HorizontalAlignment.Left, -1, 17, colour);
        }
        DrawRect(new Rect2(origin, new Vector2(96, 96) * scale), CommandTheme.Line, false);
        DrawString(CommandTheme.Display, new Vector2(16, 24), "N", HorizontalAlignment.Left, -1, 17, CommandTheme.Muted);
        DrawLine(new Vector2(20, 33), new Vector2(20, 56), CommandTheme.Muted);
        DrawLine(new Vector2(20, 33), new Vector2(15, 41), CommandTheme.Muted);
        DrawLine(new Vector2(20, 33), new Vector2(25, 41), CommandTheme.Muted);
        DrawString(CommandTheme.Display, new Vector2(Size.X - 90, Size.Y - 12), "SCHEMATIC", HorizontalAlignment.Left, -1, 12, CommandTheme.Muted);
    }
}
