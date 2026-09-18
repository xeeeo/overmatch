using Overmatch.Sim.Data;

namespace Overmatch.Sim.Tests;

public class RulesTests
{
    [Fact]
    public void Load_ParsesUnitsAndFactions()
    {
        var rules = TestRules.Basic();
        Assert.Single(rules.Units);
        Assert.Equal(4f, rules.Unit("tank").Speed);
        Assert.Equal("Test", rules.Factions["test"].Name);
    }

    [Fact]
    public void Load_RejectsUnitWithoutId()
    {
        Assert.Throws<InvalidDataException>(() => GameRules.Load(new[] { new DataFile("units/x.json", "{ \"name\": \"no id\" }") }));
    }

    [Fact]
    public void Unit_UnknownThrows()
    {
        Assert.Throws<KeyNotFoundException>(() => TestRules.Basic().Unit("nope"));
    }
}
