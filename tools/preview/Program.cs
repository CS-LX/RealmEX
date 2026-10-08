using System.Reflection;
using System.Xml.Linq;
using Engine;
using Engine.Graphics;
using Engine.Media;
using Game;
using RealmEX.Core;
using RealmEX.Presets.Ponder;

string root = Path.GetFullPath(args[0]);
string output = Path.GetFullPath(args[1]);
Directory.CreateDirectory(output);
bool done = false;
Window.Frame += () => {
    if (done) return;
    done = true;
    try {
        ContentManager.Initialize();
        new SurvivalCraftModEntity().CombineContent();
        LanguageControl.CurrentLanguageName = "zh-CN";
        LanguageControl.jsonNode = System.Text.Json.Nodes.JsonNode.Parse(ContentManager.Get<System.Text.Json.JsonDocument>("Lang/zh-CN").RootElement.GetRawText());
        LanguageControl.englishJsonNode = System.Text.Json.Nodes.JsonNode.Parse(ContentManager.Get<System.Text.Json.JsonDocument>("Lang/en-US").RootElement.GetRawText());
        foreach (string path in Directory.EnumerateFiles(Path.Combine(root, "Assets"), "*", SearchOption.AllDirectories)) {
            ContentInfo content = new(Path.GetRelativePath(Path.Combine(root, "Assets"), path).Replace('\\', '/'));
            content.SetContentStream(new MemoryStream(File.ReadAllBytes(path))); ContentManager.Add(content);
        }
        LightingManager.Initialize(); TextureAtlasManager.Initialize(); BlocksTexturesManager.Initialize(); BlocksManager.CalculateSlotTexCoordTables();
        for (int i = 0; i < BlocksManager.Blocks.Length; i++) BlocksManager.Blocks[i] = new AirBlock { BlockIndex = i, IsTransparent = true };
        foreach (Type type in typeof(Block).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Block)) && !t.IsAbstract)) {
            var field = type.GetField("Index", BindingFlags.Public | BindingFlags.Static);
            if (field?.GetValue(null) is int index && index >= 0 && index < BlocksManager.Blocks.Length)
                BlocksManager.Blocks[index] = (Block)Activator.CreateInstance(type)!;
        }
        BlocksManager.LoadBlocksData(ContentManager.Get<string>("BlocksData"));
        foreach (int index in new[] { AirBlock.Index, ShadowBlock.Index, PlanksBlock.Index, AndGateBlock.Index, NotGateBlock.Index, SwitchBlock.Index, WireBlock.Index, LedBlock.Index, DirtBlock.Index, SoilBlock.Index, PumpkinBlock.Index, JackOLanternBlock.Index, GlassBlock.Index, WaterBlock.Index })
            BlocksManager.Blocks[index].Initialize();
        BlocksManager.FluidBlocks[WaterBlock.Index] = (FluidBlock)BlocksManager.Blocks[WaterBlock.Index];
        XElement database = new(ContentManager.Get<XElement>("Database"));
        using (var stream = File.OpenRead(Path.Combine(root, "Assets/RealmEX/PonderSandboxProjectTemplate.xdb"))) ModsManager.CombineDataBase(database, stream, "RealmEX");
        database.Element("DatabaseObjects")!.Add(XElement.Parse("""
            <EntityTemplate Name="PonderPreviewProbe" Guid="a3bdfe28-a5a8-4a18-9c39-60a604676d9e">
              <MemberComponentTemplate Name="Frame" Guid="d99d9d93-297c-4734-92e3-d0c06724166a">
                <Parameter Name="IsOptional" Guid="1542b6d6-4a1a-4876-8065-992c731904ed" Type="bool" Value="False" />
                <Parameter Name="LoadOrder" Guid="fef22b31-307e-46d6-bbcb-24430df3c1a7" Type="int" Value="0" />
                <Parameter Name="Class" Guid="d9f88582-5419-4ae1-aa28-b008a2da3713" Type="string" Value="Game.ComponentFrame" />
                <Parameter Name="Position" Guid="f3c778f3-6547-47c5-97b9-469ed90721c7" Type="Vector3" Value="0,0,0" />
                <Parameter Name="Rotation" Guid="1521e3cd-7543-4925-bdad-c4639090ee06" Type="Quaternion" Value="0,0,0,1" />
              </MemberComponentTemplate>
              <MemberComponentTemplate Name="Probe" Guid="b7fe833c-5cff-4b42-84c2-3c9a46650e70">
                <Parameter Name="IsOptional" Guid="d0fac8c1-3016-4dab-a3a3-f5978c160ce9" Type="bool" Value="False" />
                <Parameter Name="LoadOrder" Guid="e7e3562c-793c-42d3-a7b0-544597c8260c" Type="int" Value="1" />
                <Parameter Name="Class" Guid="c27711f1-0d86-44f5-bb2e-d05729665e4e" Type="string" Value="PonderPreviewProbe" />
              </MemberComponentTemplate>
            </EntityTemplate>
            """));
        DatabaseManager.LoadDataBaseFromXml(database);
        VerifyRuntime();
        VerifyMaterials();
        // Same lazy bootstrap, player binding, actual widgets, sandbox terrain and shader as F6.
        foreach (string language in new[] { "zh-CN", "en-US" }) {
            LanguageControl.CurrentLanguageName = language;
            LanguageControl.jsonNode = System.Text.Json.Nodes.JsonNode.Parse(ContentManager.Get<System.Text.Json.JsonDocument>("Lang/" + language).RootElement.GetRawText());
            var registry = RealmPonderSamples.CreateRegistry();
            foreach (var tutorial in registry.Search().Select(e => e.Tutorial)) {
                var dialog = new RealmPonderDialog(tutorial, registry);
                dialog.Update();
                if (RealmHost.ActiveRealms.Count != 1) throw new InvalidOperationException("Dialog bootstrap failed; see engine log.");
                foreach (int step in Enumerable.Range(0, tutorial.Keyframes.Count)) {
                    dialog.Player.Seek(tutorial.Keyframes[step].Tick + 25); dialog.Player.IsPaused = true; dialog.Update();
                    Save(dialog, 1280, 720, $"{tutorial.Id.Split(':')[1]}-{language}-step{step + 1}");
                }
                if (tutorial.Id == "realmex:and_gate") {
                    VerifyControls(dialog);
                    Save(dialog, 640, 480, $"compact-{language}");
                    Save(dialog, 390, 844, $"portrait-{language}");
                }
                dialog.Close();
                if (RealmHost.ActiveRealms.Count != 0) throw new InvalidOperationException("Realm leak after closing.");
            }
            var indexDialog = new RealmPonderIndexDialog(registry, _ => {}, () => {});
            Save(indexDialog, 800, 700, $"index-{language}"); VerifyIndex(indexDialog, language); indexDialog.Close();
        }
        Console.WriteLine("Actual Ponder dialogs rendered in both languages, including the normal bootstrap, player, sandbox Terrain and section shader. All realms released.");
    }
    catch (Exception ex) { Console.Error.WriteLine(ex); Environment.ExitCode = 1; }
    finally { RealmHost.DisposeAll(); Window.Close(); }
};
Window.Run(64, 64, WindowMode.Fixed, "Ponder render verification");

void Save(Widget widget, int width, int height, string name) {
    using var target = new RenderTarget2D(width, height, 1, ColorFormat.Rgba8888, DepthFormat.Depth24Stencil8);
    Display.RenderTarget = target; Display.Viewport = new(0, 0, width, height); Display.ScissorRectangle = new(0, 0, width, height);
    Display.Clear(new Color(16, 22, 34), 1, 0);
    CanvasWidget rootWidget = new(); rootWidget.Children.Add(widget);
    for (int pass = 0; pass < 3; pass++) Widget.LayoutWidgetsHierarchy(rootWidget, new(width, height));
    Widget.DrawWidgetsHierarchy(rootWidget);
    RenderTarget2D.Save(target, Path.Combine(output, name + ".png"), ImageFileFormat.Png, true);
    Display.RenderTarget = null;
    rootWidget.Children.Remove(widget);
}

void VerifyRuntime() {
    Point3 cell = new(8, 1, 8);
    var originalProject = GameManager.Project;
    var overrides = new TemplatesDatabase.ValuesDictionary();
    var frameValues = new TemplatesDatabase.ValuesDictionary(); frameValues.SetValue("Position", Vector3.One); overrides.SetValue("Frame", frameValues);
    var chestOverrides = new TemplatesDatabase.ValuesDictionary();
    var blockValues = new TemplatesDatabase.ValuesDictionary(); blockValues.SetValue("Coordinates", new Point3(8, 2, 8)); chestOverrides.SetValue("BlockEntity", blockValues);
    var b = new RealmPonderSceneBuilder("preview:runtime", "Runtime", new(new Dictionary<Point3, int> { [cell] = PlanksBlock.Index }));
    b.ShowSection("base", duration: 0).World(world => { world.CreateEntity("probe", "PonderPreviewProbe", overrides); world.CreateEntity("chest", "Chest", chestOverrides); })
        .Idle(10).World(world => world.Entity("probe").FindComponent<ComponentFrame>(true).Position = new(2, 3, 4))
        .World(world => world.ModifyBlockEntity<ComponentChest>(new(8, 2, 8), chest => chest.AddSlotItems(0, PlanksBlock.Index, 3)))
        .Idle(5).World(world => world.Project.FindSubsystem<SubsystemTerrain>(true).ChangeCell(cell.X, cell.Y, cell.Z, DirtBlock.Index)).Idle(15);
    var player = new RealmPonderPlayer(b.Build());
    using var session = new RealmPonderSession(player);
    player.Seek(20);
    if (session.World.Entity("probe").FindComponent<PonderPreviewProbe>(true).Ticks != 20 || Math.Abs(session.Realm.Project.FindSubsystem<SubsystemTime>(true).GameTime - 1) > 1e-5)
        throw new InvalidOperationException("Sandbox simulation must tick exactly once per 20 Hz timeline step.");
    if (player.State.Blocks[cell] != DirtBlock.Index) throw new InvalidOperationException("World instructions must feed rendered/inspected block state.");
    if (session.World.Entity("chest").FindComponent<ComponentChest>(true).GetSlotCount(0) != 3) throw new InvalidOperationException("Block entity instructions failed.");
    var previousRealm = session.Realm; player.Seek(5);
    if (!previousRealm.IsDisposed || session.World.Entity("probe").FindComponent<ComponentFrame>(true).Position != Vector3.One || session.World.Entity("probe").FindComponent<PonderPreviewProbe>(true).Ticks != 5)
        throw new InvalidOperationException("Backward seek must reconstruct entities, callbacks and ticks.");
    if (player.State.Blocks[cell] != PlanksBlock.Index) throw new InvalidOperationException("Backward seek did not restore Terrain.");
    if (session.World.Entity("chest").FindComponent<ComponentChest>(true).GetSlotCount(0) != 0) throw new InvalidOperationException("Backward seek did not restore block entity inventory.");
    player.IsPaused = true; player.Advance(5);
    if (session.World.Entity("probe").FindComponent<PonderPreviewProbe>(true).Ticks != 5) throw new InvalidOperationException("Paused sandbox kept ticking.");
    var shared = DatabaseManager.FindEntityValuesDictionary("PonderPreviewProbe", true).GetValue<TemplatesDatabase.ValuesDictionary>("Frame");
    if (shared.GetValue<Vector3>("Position") != Vector3.Zero || !ReferenceEquals(originalProject, GameManager.Project)) throw new InvalidOperationException("Runtime escaped sandbox isolation.");
    Console.WriteLine("Runtime: entity and chest inventory replay, isolated template overrides, terrain restoration, 20 Hz subsystem ticks and pause passed.");
}

void VerifyMaterials() {
    var cells = new Dictionary<Point3, int>();
    foreach (var p in RealmPonderSelection.Box(new(7, 1, 7), new(9, 1, 9))) cells[p] = PlanksBlock.Index;
    cells[new(7, 2, 8)] = GlassBlock.Index; cells[new(9, 2, 8)] = WaterBlock.Index;
    var b = new RealmPonderSceneBuilder("preview:materials", "Glass, water and model", new(cells));
    b.ConfigureCamera(new(8.5f, 2, 8.5f), 7).ShowSection("base", duration: 0)
        .CreateModel("model", "Models/Switch", "Textures/Blocks", new(8.5f, 3, 8.5f))
        .Controls("input", RealmPonderInput.Interact, new(7.5f, 2.5f, 8.5f), 60, GlassBlock.Index)
        .RotateActor("model", new(0, 180, 0), 50).ChaseBounds("glass", new(new Vector3(7, 2, 8), new Vector3(8, 3, 9)), 60, 10, new(158, 199, 234)).Idle(60);
    var dialog = new RealmPonderDialog(b.Build()); dialog.Update(); dialog.Player.Seek(25); dialog.Player.IsPaused = true; dialog.Update();
    Save(dialog, 960, 640, "materials-model-bounds"); dialog.Close();
}

void VerifyControls(RealmPonderDialog dialog) {
    var input = new WidgetInput(WidgetInputDevice.None); dialog.WidgetsHierarchyInput = input;
    Widget.LayoutWidgetsHierarchy(dialog, new(1280, 720));
    void Click(string name) {
        var button = dialog.Children.Find<RealmPonderButtonWidget>(name);
        Vector2 point = (button.GlobalBounds.Min + button.GlobalBounds.Max) / 2;
        input.Click = new Segment2(point, point); button.Update(); dialog.Update(); input.Click = null; button.Update();
    }
    Click("Ponder.Restart");
    Click("Ponder.Play"); if (!dialog.Player.IsPaused) throw new InvalidOperationException("Pause button failed.");
    Click("Ponder.Next"); if (dialog.Player.KeyframeIndex != 1) throw new InvalidOperationException("Next keyframe button failed.");
    Click("Ponder.Previous"); if (dialog.Player.KeyframeIndex != 0) throw new InvalidOperationException("Previous keyframe button failed.");
    Click("Ponder.Inspect"); if (!dialog.Children.Find<RealmPonderWidget>("Ponder.Viewport").InspectMode || !dialog.Player.IsPaused) throw new InvalidOperationException("Inspection must pause.");
    Click("Ponder.Inspect"); Click("Ponder.Reading"); if (!dialog.Player.ComfyReading) throw new InvalidOperationException("Reading control failed.");
    dialog.Player.Seek(dialog.Player.Tutorial.Keyframes[^1].Tick + 25); dialog.Update();
    Save(dialog, 1280, 720, $"and_gate-{LanguageControl.CurrentLanguageName}-step8");
    var viewport = dialog.Children.Find<RealmPonderWidget>("Ponder.Viewport");
    viewport.InspectMode = true; viewport.RefreshPresentation();
    Vector3 clip = Vector3.Transform(new Vector3(8.5f, 2.2f, 8.5f), viewport.Realm.Viewport.Camera.ViewProjectionMatrix);
    input.Tap = viewport.WidgetToScreen(new Vector2((clip.X + 1) * viewport.ActualSize.X / 2, (1 - clip.Y) * viewport.ActualSize.Y / 2));
    viewport.Update();
    if (string.IsNullOrEmpty(viewport.InspectedName)) throw new InvalidOperationException("Viewport unprojection did not identify the rendered gate.");
    input.Tap = null; viewport.InspectMode = false; viewport.RefreshPresentation();
    Console.WriteLine("UI: pointer hit tests, rendered-block inspection, pause/replay/keyframe/reading button updates passed.");
}

void VerifyIndex(RealmPonderIndexDialog dialog, string language) {
    var search = dialog.Children.Find<TextBoxWidget>("Index.Search");
    var scenes = dialog.Children.Find<ListPanelWidget>("Index.Scenes");
    search.Text = language == "zh-CN" ? "非门" : "NOT"; dialog.Update();
    if (scenes.Items.Count != 1 || ((RealmPonderTutorial)scenes.Items[0]).Id != "realmex:not_gate") throw new InvalidOperationException("Index search failed.");
    search.Text = ""; dialog.Update();
    var input = new WidgetInput(WidgetInputDevice.None); dialog.WidgetsHierarchyInput = input;
    var tags = dialog.Children.Find<ListPanelWidget>("Index.Tags"); tags.PlayClickSound = false;
    Vector2 point = tags.GlobalBounds.Min + new Vector2(tags.ItemSize * 2.5f, tags.ActualSize.Y / 2);
    input.Tap = point; input.Click = new(point, point); tags.Update(); input.Tap = null; input.Click = null;
    if (scenes.Items.Count != 1 || ((RealmPonderTutorial)scenes.Items[0]).Id != "realmex:pumpkin") throw new InvalidOperationException("Index tag filter failed.");
    Console.WriteLine("Index: localized search and actual tag pointer selection passed.");
}

public sealed class PonderPreviewProbe : GameEntitySystem.Component, IUpdateable {
    public int Ticks { get; private set; }
    public void Update(float dt) { if (Math.Abs(dt - 0.05f) > 1e-6f) throw new InvalidOperationException("Unexpected simulation delta."); Ticks++; }
}
