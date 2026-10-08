using System.Runtime.CompilerServices;
using Engine;
using Engine.Media;
using Game;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace RealmEX.Tests;
internal static class PonderTestResources
{
    [ModuleInitializer]
    public static void Initialize()
    {
        LabelWidget.BitmapFont = new BitmapFont(null,
            [new BitmapFont.Glyph('?', Vector2.Zero, Vector2.One, Vector2.Zero, 24f)], '?', 28f, Vector2.Zero, 1f);
        var assembly = typeof(PonderTestResources).Assembly;
        foreach (string name in assembly.GetManifestResourceNames().Where(n => n.StartsWith("PonderLang.")))
        {
            using var input = assembly.GetManifestResourceStream(name)!;
            MemoryStream stream = new(); input.CopyTo(stream); stream.Position = 0;
            ContentInfo content = new("RealmEX/Lang/" + name["PonderLang.".Length..]);
            content.SetContentStream(stream); ContentManager.Add(content);
        }
    }
}
