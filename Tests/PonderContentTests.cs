using System.Text;
using Engine;
using Game;
using RealmEX.Presets.Ponder;
using Xunit;

namespace RealmEX.Tests;

public sealed class PonderContentTests
{
    const string Manifest = """
        <PonderPack Version="1" Namespace="example">
          <Palette><Block Id="stone" Value="3" /></Palette>
          <Tag Id="building" Zh="建造" En="Building" />
          <Tutorial Id="machine" Zh="机器" En="Machine" Script="machine.pjs" Subjects="stone" Tags="building" />
        </PonderPack>
        """;
    const string Script = """
        if (typeof System !== 'undefined' || typeof importNamespace !== 'undefined') throw new Error('CLR exposed');
        scene.keyframe(['搭建', 'Build']);
        for (let x = 0; x < 3; x++) scene.fill([x, 1, 0], [x, 1, 0], 'stone');
        scene.show('base', [0, -1, 0], 10);
        scene.idle(20);
        scene.keyframe(['移动', 'Move']);
        scene.move('base', [3, 0, 0], 20);
        scene.text('help', ['完成', 'Complete'], [0, 1, 0], 20);
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
        var mod = new Mod(new() { ["Ponder/content.ponder.xml"] = Manifest, ["Ponder/machine.pjs"] = Script }) { modInfo = new() { PackageName = "example" } };
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
        var good = new Mod(new() { ["Ponder/content.ponder.xml"] = Manifest, ["Ponder/machine.pjs"] = Script }) { modInfo = new() { PackageName = "a-good" } };
        string brokenXml = Manifest.Replace("example", "broken").Replace("</PonderPack>", "<Tutorial Id='bad' Zh='坏' En='Bad' Script='bad.pjs'/></PonderPack>");
        var bad = new Mod(new() { ["Ponder/content.ponder.xml"] = brokenXml, ["Ponder/machine.pjs"] = Script, ["Ponder/bad.pjs"] = "scene.nonexistent();" }) { modInfo = new() { PackageName = "z-bad" } };
        List<string> errors = [];
        var registry = RealmPonderContentLoader.Discover([good, bad], errors.Add, new());
        Assert.Single(errors); Assert.Single(registry.Search()); Assert.Single(registry.Tags);
        Assert.Equal("example:machine", registry.Search()[0].Tutorial.Id);
    }
    [Theory]
    [InlineData("while (true) {}")]
    [InlineData("scene.show('missing', [0,0,0], 20)")]
    [InlineData("scene.uiText('label', 'Must first show UI')")]
    [InlineData("scene.idle(24001)")]
    [InlineData("scene.fill([0,-1,0],[0,-1,0], 'stone')")]
    public void InvalidScriptsCannotPublishPartialTutorials(string script)
    {
        var registry = new RealmPonderRegistry();
        Assert.ThrowsAny<Exception>(() => RealmPonderContentLoader.Load(registry, "Ponder/content.ponder.xml", Manifest, _ => script, _ => 3));
        Assert.Empty(registry.Search()); Assert.Empty(registry.Tags);
    }
    [Theory]
    [InlineData("../machine.pjs")]
    [InlineData("machine.js")]
    [InlineData("/machine.pjs")]
    public void ResourcePathsStayWithinThePackAndDoNotUseHostAutoExecutedJs(string path)
    {
        var registry = new RealmPonderRegistry();
        Assert.Throws<FormatException>(() => RealmPonderContentLoader.Load(registry, "Ponder/content.ponder.xml", Manifest.Replace("machine.pjs", path), _ => Script, _ => 3));
        Assert.Empty(registry.Search());
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
