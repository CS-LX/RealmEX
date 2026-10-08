using System;
using System.Collections.Generic;
using System.Text.Json;
using Engine;
using Jint;

namespace RealmEX.Presets.Ponder
{
    /// <summary>Compiles bounded JavaScript into the existing deterministic scene instructions. No global SC engine or CLR access.</summary>
    public static class RealmPonderScript
    {
        public static void Compile(RealmPonderSceneBuilder builder, string source, string sourceName,
            IReadOnlyDictionary<string, int> palette, IReadOnlyDictionary<string, RealmPonderUiDefinition> interfaces)
        {
            int commands = 0;
            using var engine = new Jint.Engine(options => options.MaxStatements(100000).LimitMemory(16 * 1024 * 1024).TimeoutInterval(TimeSpan.FromSeconds(3)).LimitRecursion(64));
            engine.SetValue("__emit", new Action<string>(json =>
            {
                if (++commands > 10000) throw new InvalidOperationException("Tutorial command limit exceeded.");
                using JsonDocument document = JsonDocument.Parse(json);
                var command = document.RootElement;
                string op = command.GetProperty("op").GetString();
                var args = command.GetProperty("args");
                string S(int n) => args[n].GetString() ?? throw new FormatException("Expected a string.");
                int I(int n) => args[n].GetInt32();
                float F(int n) { float value = args[n].GetSingle(); return float.IsFinite(value) ? value : throw new FormatException("Expected a finite number."); }
                int T(int n) { int ticks = I(n); return ticks is >= 0 and <= 24000 ? ticks : throw new FormatException("Duration must be 0..24000 ticks."); }
                bool B(int n) => args[n].GetBoolean();
                Vector3 V(int n) => new(args[n][0].GetSingle(), args[n][1].GetSingle(), args[n][2].GetSingle());
                Point3 P(int n) => new(args[n][0].GetInt32(), args[n][1].GetInt32(), args[n][2].GetInt32());
                RealmPonderSelection Box(int n) => RealmPonderSelection.Box(P(n), P(n + 1));
                RealmPonderText Text(int n) => args[n].ValueKind == JsonValueKind.String ? S(n) : new RealmPonderText(args[n][0].GetString(), args[n][1].GetString());
                int Value(int n) => args[n].ValueKind == JsonValueKind.String ? palette[S(n)] : I(n);
                Color Color(int n) => args.GetArrayLength() <= n ? new Color(230, 210, 130) : new Color(args[n][0].GetByte(), args[n][1].GetByte(), args[n][2].GetByte());
                switch (op)
                {
                    case "idle": builder.Idle(T(0)); break;
                    case "keyframe": builder.Keyframe(Text(0)); break;
                    case "camera": builder.ConfigureCamera(V(0), F(1), F(2)); break;
                    case "rotateCamera": builder.RotateCamera(F(0), T(1)); break;
                    case "section": builder.IndependentSection(S(0), Box(1)); break;
                    case "show": builder.ShowSection(S(0), V(1), T(2)); break;
                    case "hide": builder.HideSection(S(0), V(1), T(2)); break;
                    case "move": builder.MoveSection(S(0), V(1), T(2)); break;
                    case "rotate": builder.RotateSection(S(0), V(1), T(2)); break;
                    case "fill": builder.SetBlocks(Box(0), Value(2)); break;
                    case "restore": builder.RestoreBlocks(Box(0)); break;
                    case "text": builder.Text(S(0), Text(1), V(2), T(3)); break;
                    case "outline": builder.Outline(S(0), Box(1), T(3), Color(4)); break;
                    case "line": builder.Line(S(0), V(1), V(2), T(3), Color(4)); break;
                    case "removeOverlay": builder.RemoveOverlay(S(0)); break;
                    case "item": builder.CreateItem(S(0), Value(1), V(2)); break;
                    case "moveItem": builder.MoveActor(S(0), V(1), T(2)); break;
                    case "removeItem": builder.RemoveActor(S(0)); break;
                    case "success": builder.Success(V(0), T(1)); break;
                    case "finish": builder.MarkAsFinished(); break;
                    case "uiShow": builder.ShowUi(interfaces[S(0)]); break;
                    case "uiHide": builder.HideUi(); break;
                    case "uiText": builder.UiText(S(0), Text(1)); break;
                    case "uiFont": builder.UiFont(S(0), S(1)); break;
                    case "uiButtonColor": builder.UiButtonColor(S(0), Color(1)); break;
                    case "uiEnabled": builder.UiEnabled(S(0), B(1)); break;
                    case "uiVisible": builder.UiVisible(S(0), B(1)); break;
                    case "uiValue": builder.UiValue(S(0), F(1)); break;
                    case "uiConfigure":
                        Dictionary<string, string> properties = new(StringComparer.Ordinal);
                        foreach (var property in args[1].EnumerateObject()) properties.Add(property.Name, property.Value.GetString());
                        builder.UiConfigure(S(0), properties); break;
                    case "uiAnimateValue": builder.UiAnimateValue(S(0), F(1), F(2), T(3)); break;
                    case "uiInventory": builder.UiInventory(S(0)); break;
                    case "uiItem": builder.UiItem(S(0), Value(1), I(2)); break;
                    case "uiButton": builder.UiButton(S(0), S(1), Text(2), I(3), I(4), new(args[5][0].GetSingle(), args[5][1].GetSingle())); break;
                    case "uiPoint": builder.UiPoint(S(0), T(1)); break;
                    case "uiClick": builder.UiClick(S(0), T(1)); break;
                    case "uiDrag": builder.UiDrag(S(0), S(1), T(2)); break;
                    case "uiScroll": builder.UiScroll(S(0), F(1), T(2)); break;
                    default: throw new FormatException($"Unknown Ponder command '{op}'.");
                }
                if (builder.CurrentTick > 24000) throw new FormatException("Tutorials may not exceed 20 minutes.");
            }));
            engine.Execute("""
                const scene = (() => {
                    const emit = __emit;
                    delete globalThis.__emit;
                    const api = {};
                    for (const op of ['idle','keyframe','camera','rotateCamera','section','show','hide','move','rotate','fill','restore','text','outline','line','removeOverlay','item','moveItem','removeItem','success','finish','uiShow','uiHide','uiText','uiFont','uiButtonColor','uiEnabled','uiVisible','uiValue','uiConfigure','uiAnimateValue','uiInventory','uiItem','uiButton','uiPoint','uiClick','uiDrag','uiScroll'])
                        api[op] = (...args) => emit(JSON.stringify({op, args}));
                    let seed = 1;
                    Math.random = () => { seed = (Math.imul(seed, 1664525) + 1013904223) >>> 0; return seed / 4294967296; };
                    globalThis.Date = undefined;
                    return Object.freeze(api);
                })();
                """);
            engine.Execute(source, sourceName);
            if (builder.Build().Duration > 24000) throw new FormatException("Tutorials may not exceed 20 minutes.");
        }
    }
}
