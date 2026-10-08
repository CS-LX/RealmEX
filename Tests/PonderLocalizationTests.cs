using RealmEX.Presets.Ponder;
using Xunit;

namespace RealmEX.Tests;
public sealed class PonderLocalizationTests
{
    static RealmPonderLocalization Catalog() => new(new Dictionary<string, string> {
        ["en-US"] = """{"title":"Reactor","heat":"Heat {0} T","onlyEnglish":"Fallback"}""",
        ["zh-CN"] = """{"title":"反应堆","heat":"温度 {0} T"}""",
        ["fr"] = """{"title":"Réacteur"}""" });
    [Fact]
    public void LocaleParentsAndMissingTranslationsFallBackWithoutExposingKeys()
    {
        var catalog = Catalog();
        Assert.Equal("反应堆", catalog.Text("title").Resolve("zh-CN"));
        Assert.Equal("Réacteur", catalog.Text("title").Resolve("fr-CA"));
        Assert.Equal("Reactor", catalog.Text("title").Resolve("es-419"));
        Assert.Equal("Fallback", catalog.Text("onlyEnglish").Resolve("zh-CN"));
        Assert.Throws<FormatException>(() => catalog.Text("missing"));
    }
    [Fact]
    public void ParametersAreCapturedAndReorderedByTranslatedFormat()
    {
        var catalog = new RealmPonderLocalization(new Dictionary<string, string> {
            ["en-US"] = """{"pair":"{0} then {1}"}""", ["zh-CN"] = """{"pair":"{1} 在 {0} 之后"}""" });
        object[] args = ["A", "B"];
        var text = catalog.Text("pair", args); args[0] = "changed";
        Assert.Equal("A then B", text.Resolve("en-US")); Assert.Equal("B 在 A 之后", text.Resolve("zh-CN"));
        Assert.Throws<FormatException>(() => catalog.Text("pair", "only one"));
    }
    [Theory]
    [InlineData("{\"title\":1}")]
    [InlineData("{\"title\":\"A\",\"title\":\"B\"}")]
    public void InvalidCatalogsAreRejected(string json)
        => Assert.Throws<FormatException>(() => new RealmPonderLocalization(new Dictionary<string, string> { ["en-US"] = json }));
    [Fact]
    public void ScriptUsesCatalogKeysAndCapturesDynamicValuesAcrossReplay()
    {
        var builder = new RealmPonderSceneBuilder("test:locale", Catalog().Text("title"), new([]));
        RealmPonderScript.Compile(builder, "let heat=120; scene.text('readout',scene.t('heat',heat),[0,1,0],20); heat=900; scene.idle(20);",
            "locale.pjs", new Dictionary<string, int>(), new Dictionary<string, RealmPonderUiDefinition>(), Catalog());
        var player = new RealmPonderPlayer(builder.Build());
        Assert.Equal("温度 120 T", player.State.Overlays["readout"].Text.Resolve("zh-CN"));
        player.Seek(20); player.Seek(0);
        Assert.Equal("Heat 120 T", player.State.Overlays["readout"].Text.Resolve("en-US"));
    }
    [Theory]
    [InlineData("'missing'")]
    [InlineData("['旧文本','Old text']")]
    [InlineData("scene.t('heat')")]
    public void BadKeysAndLegacyLiteralPairsCannotCompile(string text)
    {
        var builder = new RealmPonderSceneBuilder("test:invalid-locale", "Test", new([]));
        Assert.ThrowsAny<Exception>(() => RealmPonderScript.Compile(builder, $"scene.keyframe({text});", "bad.pjs",
            new Dictionary<string, int>(), new Dictionary<string, RealmPonderUiDefinition>(), Catalog()));
    }
}
