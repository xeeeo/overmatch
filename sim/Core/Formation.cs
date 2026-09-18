namespace Overmatch.Sim;

/// <summary>Spreads a group move over a compact grid around the target so units don't all fight for one cell.</summary>
public static class Formation
{
    public static Vec2[] Spread(IReadOnlyList<Entity> units, Vec2 target)
    {
        var n = units.Count;
        var result = new Vec2[n];
        if (n == 1)
        {
            result[0] = target;
            return result;
        }

        var spacing = 0f;
        foreach (var u in units) spacing = MathF.Max(spacing, u.Radius * 2.4f);

        var cols = (int)MathF.Ceiling(MathF.Sqrt(n));
        var rows = (int)MathF.Ceiling(n / (float)cols);
        var origin = new Vec2(-(cols - 1) * spacing * 0.5f, -(rows - 1) * spacing * 0.5f);

        // Assign slots to units by sorting both by position so nearby units take nearby slots (fewer crossings).
        var slots = new List<Vec2>(n);
        for (var r = 0; r < rows && slots.Count < n; r++)
            for (var c = 0; c < cols && slots.Count < n; c++)
                slots.Add(target + origin + new Vec2(c * spacing, r * spacing));

        var unitOrder = Enumerable.Range(0, n).OrderBy(i => units[i].Pos.X + units[i].Pos.Y * 0.01f).ToArray();
        var slotOrder = Enumerable.Range(0, n).OrderBy(i => slots[i].X + slots[i].Y * 0.01f).ToArray();
        for (var k = 0; k < n; k++) result[unitOrder[k]] = slots[slotOrder[k]];
        return result;
    }
}
