using Godot;

namespace Overmatch.Game;

/// <summary>
/// Key layout after the original: arrows scroll, letters are orders. WASD used to pan the camera, which fought with
/// A (attack-move) and S (stop); the input map is rewritten here rather than in project.godot so it reads as code.
/// </summary>
public static class Hotkeys
{
    /// <summary>Shown on the Controls page of the settings panel.</summary>
    public static readonly (string Keys, string Does)[] Reference =
    {
        ("RIGHT-CLICK", "move, attack, enter, capture, repair, return to base"),
        ("A, THEN CLICK", "attack-move: fight anything on the way"),
        ("G, THEN CLICK", "guard an area: engage intruders, then return to post"),
        ("S", "stop"),
        ("X", "scatter"),
        ("R", "aircraft return to base"),
        ("V", "unload passengers"),
        ("Q", "select all combat units"),
        ("W", "select all aircraft"),
        ("E  /  E E", "select matching units on screen  /  everywhere"),
        ("DOUBLE-CLICK", "select matching units on screen"),
        ("I", "next idle builder"),
        ("CTRL + 0-9", "set control group"),
        ("0-9  /  TWICE", "select group  /  jump to it"),
        ("SHIFT + 0-9", "add group to selection"),
        ("ALT + 0-9", "look at group without selecting"),
        ("H", "headquarters"),
        ("SPACE", "jump to last alert"),
        ("ARROWS", "scroll"),
        ("[  ]", "rotate view"),
        ("SCROLL / PINCH", "zoom"),
        ("F9", "hide or show the interface"),
        ("M", "music on or off"),
        ("ESC", "cancel order  /  pause menu"),
    };

    public static void Install()
    {
        Rebind("camera_forward", Key.Up);
        Rebind("camera_back", Key.Down);
        Rebind("camera_left", Key.Left);
        Rebind("camera_right", Key.Right);
        Rebind("camera_rotate_left", Key.Bracketleft, Key.Kp4);
        Rebind("camera_rotate_right", Key.Bracketright, Key.Kp6);
    }

    private static void Rebind(string action, params Key[] keys)
    {
        if (!InputMap.HasAction(action)) InputMap.AddAction(action);
        InputMap.ActionEraseEvents(action);
        foreach (var k in keys) InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = k });
    }
}
