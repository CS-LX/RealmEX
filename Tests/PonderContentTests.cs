using System.Text;
using Engine;
using Game;
using RealmEX.Presets.Ponder;
using Xunit;

namespace RealmEX.Tests;

public sealed class PonderContentTests
{
    const string English = """{"building":"Building","machine":"Machine","build":"Build","move":"Move","complete":"Complete"}""";
    const string Chinese = """{"building":"建造","machine":"机器","build":"搭建","move":"移动","complete":"完成"}""";
    static string Read(string path, string script) => path.EndsWith("en-US.json") ? English : path.EndsWith("zh-CN.json") ? Chinese : script;
    const string Manifest = """
        <PonderPack Version="2" Namespace="example">
          <Locale Name="en-US" File="en-US.json"/><Locale Name="zh-CN" File="zh-CN.json"/>
          <Palette><Block Id="stone" Value="3" /></Palette>
          <Tag Id="building" Text="building" />
          <Tutorial Id="machine" Text="machine" Script="machine.pjs" Subjects="stone" Tags="building" />
        </PonderPack>
        """;
    const string Script = """
        if (typeof System !== 'undefined' || typeof importNamespace !== 'undefined') throw new Error('CLR exposed');
        scene.keyframe('build');
        for (let x = 0; x < 3; x++) scene.fill([x, 1, 0], [x, 1, 0], 'stone');
        scene.show('base', [0, -1, 0], 10);
        scene.idle(20);
        scene.keyframe('move');
        scene.move('base', [3, 0, 0], 20);
        scene.text('help', 'complete', [0, 1, 0], 20);
        scene.idle(20);
        """;
    sealed class Mod(Dictionary<string, string> files) : ModEntity
    {
        public Mod() : this([]) { }
        public override void GetFiles(string extension, Action<string, Stream> action)
        {
            foreach (var file in files.Where(f => f.Key.EndsWith(extension, StringComparison.Ordinal)))
                using (MemoryStream stream = new(Encoding.UTF8.GetBytes(file.Value))) action(file.Key, stream);
        }
        public override bool GetFile(string name, Action<Stream> action)
        {
            if (files.TryGetValue(name, out string data)) { using MemoryStream stream = new(Encoding.UTF8.GetBytes(data)); action(stream); }
            return false; // The production loader cannot use this bool to infer success.
        }
    }
    [Fact]
    public void LoadedModIsDiscoveredAndJavascriptLogicCompilesIntoReplayableScenes()
    {
        var mod = new Mod(new() { ["Ponder/content.ponder.xml"] = Manifest, ["Ponder/en-US.json"] = English, ["Ponder/zh-CN.json"] = Chinese, ["Ponder/machine.pjs"] = Script }) { modInfo = new() { PackageName = "example" } };
        List<string> errors = [];
        var registry = RealmPonderContentLoader.Discover([mod], errors.Add, new());
        Assert.Empty(errors);
        var entry = Assert.Single(registry.Search("机器", language: "zh-CN"));
        Assert.Equal("example:machine", entry.Tutorial.Id);
        Assert.Single(registry.ForSubject(3));
        var player = new RealmPonderPlayer(entry.Tutorial);
        player.Seek(30);
        Assert.Equal(3, player.State.Blocks.Count);
        Assert.Equal(1.5f, player.State.Sections["base"].Offset.X);
        player.Seek(5); Assert.Equal(0, player.State.Sections["base"].Offset.X);
        player.Seek(30); Assert.Equal(1.5f, player.State.Sections["base"].Offset.X);
    }
    [Fact]
    public void BrokenPackRollsBackWithoutHidingAnotherModsTutorial()
    {
        var good = new Mod(new() { ["Ponder/content.ponder.xml"] = Manifest, ["Ponder/en-US.json"] = English, ["Ponder/zh-CN.json"] = Chinese, ["Ponder/machine.pjs"] = Script }) { modInfo = new() { PackageName = "a-good" } };
        string brokenXml = Manifest.Replace("example", "broken").Replace("</PonderPack>", "<Tutorial Id='bad' Text='machine' Script='bad.pjs'/></PonderPack>");
        var bad = new Mod(new() { ["Ponder/content.ponder.xml"] = brokenXml, ["Ponder/en-US.json"] = English, ["Ponder/zh-CN.json"] = Chinese, ["Ponder/machine.pjs"] = Script, ["Ponder/bad.pjs"] = "scene.nonexistent();" }) { modInfo = new() { PackageName = "z-bad" } };
        List<string> errors = [];
        var registry = RealmPonderContentLoader.Discover([good, bad], errors.Add, new());
        Assert.Single(errors); Assert.Single(registry.Search()); Assert.Single(registry.Tags);
        Assert.Equal("example:machine", registry.Search()[0].Tutorial.Id);
    }
    [Theory]
    [InlineData("while (true) {}")]
    [InlineData("scene.show('missing', [0,0,0], 20)")]
    [InlineData("scene.uiText('label', 'machine')")]
    [InlineData("scene.idle(24001)")]
    [InlineData("scene.fill([0,-1,0],[0,-1,0], 'stone')")]
    public void InvalidScriptsCannotPublishPartialTutorials(string script)
    {
        var registry = new RealmPonderRegistry();
        Assert.ThrowsAny<Exception>(() => RealmPonderContentLoader.Load(registry, "Ponder/content.ponder.xml", Manifest, path => Read(path, script), _ => 3));
        Assert.Empty(registry.Search()); Assert.Empty(registry.Tags);
    }
    [Theory]
    [InlineData("../machine.pjs")]
    [InlineData("machine.js")]
    [InlineData("/machine.pjs")]
    public void ResourcePathsStayWithinThePackAndDoNotUseHostAutoExecutedJs(string path)
    {
        var registry = new RealmPonderRegistry();
        Assert.Throws<FormatException>(() => RealmPonderContentLoader.Load(registry, "Ponder/content.ponder.xml", Manifest.Replace("machine.pjs", path), path => Read(path, Script), _ => 3));
        Assert.Empty(registry.Search());
    }
    [Fact]
    public void SeriesLoadIndependentChaptersWithSharedScriptIncludes()
    {
        string manifest = Manifest.Replace("<Tutorial", "<Series Id='course' Text='building'/><Tutorial Series='course' Includes='common.pjs'", StringComparison.Ordinal)
            .Replace("</PonderPack>", "<Tutorial Id='second' Text='move' Script='second.pjs' Includes='common.pjs' Series='course' Order='1'/></PonderPack>");
        var registry = new RealmPonderRegistry();
        RealmPonderContentLoader.Load(registry, "Ponder/content.ponder.xml", manifest,
            path => path.EndsWith(".json") ? Read(path, "") : path.EndsWith("common.pjs")
                ? "let counter = 0; function build() { scene.fill([0,1,0],[0,1,0], ++counter); scene.idle(20); }" : "build();", _ => 3);
        Assert.Equal("example:course", Assert.Single(registry.Series).Id);
        Assert.Equal(new[] { "example:machine", "example:second" }, registry.Sequence("example:machine").Select(t => t.Id));
        foreach (var entry in registry.Search("建造", language: "zh-CN"))
            Assert.Equal(1, new RealmPonderPlayer(entry.Tutorial).State.Blocks[new(0, 1, 0)]);
        Assert.Equal(2, registry.Search("建造", language: "zh-CN").Count);
    }
    [Fact]
    public void ScriptCanMergeAConnectedAssemblyAndReplayBeforeTheMerge()
    {
        var builder = new RealmPonderSceneBuilder("test:assembly", "Assembly", new([]));
        RealmPonderScript.Compile(builder, """
            scene.fill([0,1,0],[1,1,0], 3);
            scene.section('pipe', [0,1,0], [0,1,0]);
            scene.section('port', [1,1,0], [1,1,0]);
            scene.show('pipe', [0,0,0], 0); scene.idle(10);
            scene.merge('port', 'pipe'); scene.idle(10);
            """, "assembly.pjs", new Dictionary<string, int>(), new Dictionary<string, RealmPonderUiDefinition>());
        var player = new RealmPonderPlayer(builder.Build()); player.Seek(15);
        Assert.False(player.State.Sections.ContainsKey("port")); Assert.Equal(2, player.State.Sections["pipe"].Selection.Count);
        Assert.Equal(1, player.State.Sections["pipe"].Opacity);
        player.Seek(5); Assert.True(player.State.Sections.ContainsKey("port")); Assert.Single(player.State.Sections["pipe"].Selection);
    }
    [Theory]
    [InlineData("Series='missing'")]
    [InlineData("Includes='../common.pjs'")]
    [InlineData("Includes='common.js'")]
    public void InvalidChapterReferencesRollBackTheSeries(string attributes)
    {
        var registry = new RealmPonderRegistry();
        string manifest = Manifest.Replace("<Tutorial", "<Series Id='course' Text='building'/><Tutorial " + attributes, StringComparison.Ordinal);
        Assert.ThrowsAny<Exception>(() => RealmPonderContentLoader.Load(registry, "Ponder/content.ponder.xml", manifest, path => Read(path, Script), _ => 3));
        Assert.Empty(registry.Series); Assert.Empty(registry.Search()); Assert.Empty(registry.Tags);
    }
    sealed class DeviceBlock : AirBlock
    {
        public override string GetCraftingId(int value) => "device-" + (Terrain.ExtractData(value) >> 3);
    }
    [Fact]
    public void SubjectLookupDistinguishesDevicesSharingOneBlockAndIgnoresOrientation()
    {
        const int index = 900;
        Block previous = BlocksManager.Blocks[index];
        try
        {
            BlocksManager.Blocks[index] = new DeviceBlock();
            var registry = new RealmPonderRegistry();
            var tutorial = new RealmPonderSceneBuilder("test:device", "Device", new([])).Build();
            registry.Register(tutorial, subjects: [Terrain.MakeBlockValue(index, 0, 16)]);
            Assert.Single(registry.ForSubject(Terrain.MakeBlockValue(index, 15, 18)));
            Assert.Empty(registry.ForSubject(Terrain.MakeBlockValue(index, 0, 24)));
        }
        finally { BlocksManager.Blocks[index] = previous; }
    }
}
