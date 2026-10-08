using System;
using System.Collections.Generic;
using System.Linq;
using Engine;
using Engine.Graphics;
using Game;
using RealmEX.Core;

namespace RealmEX.Presets.Ponder
{
    /// <summary>Real sandbox Terrain meshes, cached per section and transformed by the tutorial clock.</summary>
    public sealed class RealmPonderBlockPresenter : IDisposable
    {
        private readonly Dictionary<string, DynamicArray<TerrainChunkGeometry.Buffer>> m_buffers = new(StringComparer.Ordinal);
        private readonly Dictionary<string, BlockMesh> m_models = new(StringComparer.Ordinal);
        private readonly PrimitivesRenderer3D m_actorRenderer = new();
        private SandboxRealm m_realm;
        private RealmPonderPlayer m_player;
        private RealmPonderState m_state;
        private int m_revision = -1;
        private Shader m_shader;
        private Shader m_transparentShader;
        public void Attach(SandboxRealm realm, RealmPonderPlayer player)
        {
            m_realm = realm ?? throw new ArgumentNullException(nameof(realm));
            m_player = player ?? throw new ArgumentNullException(nameof(player));
            m_realm.Profile.ParallelTick = false;
            m_realm.Viewport.AfterDraw = Draw;
            Display.DeviceReset += Invalidate;
        }
        public void Invalidate() => m_revision = -1;
        public void Synchronize()
        {
            RealmPonderState state = m_player.State;
            if (ReferenceEquals(m_state, state) && m_revision == state.GeometryRevision) return;
            Terrain terrain = m_realm.Project.FindSubsystem<SubsystemTerrain>(true).Terrain;
            DisposeBuffers();
            foreach (var section in state.Sections.Values) BuildSection(section, terrain, state);
            m_state = state; m_revision = state.GeometryRevision;
        }
        private void Draw(SandboxRealm realm, Camera camera)
        {
            Synchronize();
            m_shader ??= new Shader(ShaderCodeManager.GetFast("Shaders/RealmPonderBlocks.vsh"), ShaderCodeManager.GetFast("Shaders/RealmPonderBlocks.psh"), [new ShaderMacro("ALPHATESTED")]);
            m_transparentShader ??= new Shader(ShaderCodeManager.GetFast("Shaders/RealmPonderBlocks.vsh"), ShaderCodeManager.GetFast("Shaders/RealmPonderBlocks.psh"), [new ShaderMacro("TRANSPARENT")]);
            DrawSections(camera, false);
            DrawActors(camera);
            DrawSections(camera, true);
        }
        private void DrawSections(Camera camera, bool transparent)
        {
            Shader shader = transparent ? m_transparentShader : m_shader;
            var sections = m_player.State.Sections.Values.OrderByDescending(s => Vector3.DistanceSquared(Vector3.Transform(s.Pivot, s.Transform), camera.ViewPosition));
            foreach (var section in sections)
            {
                if (section.Opacity <= 0 || !m_buffers.TryGetValue(section.Id, out var buffers)) continue;
                shader.GetParameter("u_viewProjectionMatrix").SetValue(section.Transform * camera.ViewProjectionMatrix);
                shader.GetParameter("u_reveal").SetValue(section.Opacity);
                if (transparent) shader.GetParameter("u_viewPosition").SetValue(Vector3.Transform(camera.ViewPosition, Matrix.Invert(section.Transform)));
                shader.GetParameter("u_samplerState").SetValue(SamplerState.PointClamp);
                if (!transparent) shader.GetParameter("u_alphaThreshold").SetValue(0.5f);
                foreach (var buffer in buffers)
                {
                    int start = transparent ? buffer.SubsetIndexBufferStarts[6] : 0;
                    int end = buffer.SubsetIndexBufferEnds[transparent ? 6 : 5];
                    if (end <= start) continue;
                    Display.BlendState = BlendState.AlphaBlend;
                    Display.DepthStencilState = transparent ? DepthStencilState.DepthRead : DepthStencilState.Default;
                    Display.RasterizerState = RasterizerState.CullCounterClockwiseScissor;
                    shader.GetParameter("u_texture").SetValue(buffer.Texture);
                    Display.DrawIndexed(PrimitiveType.TriangleList, shader, buffer.VertexBuffer, buffer.IndexBuffer, start, end - start);
                }
            }
        }
        private void DrawActors(Camera camera)
        {
            DrawBlockEnvironmentData environment = new() { Light = 15, ViewProjectionMatrix = camera.ViewProjectionMatrix };
            foreach (var actor in m_player.State.Actors.Values)
            {
                Matrix transform = actor.Transform;
                if (actor.Kind == RealmPonderActorKind.Item)
                    BlocksManager.Blocks[Terrain.ExtractContents(actor.ItemValue)].DrawBlock(m_actorRenderer, Terrain.ReplaceLight(actor.ItemValue, 15), Color.White, 1, ref transform, environment);
                else
                {
                    if (!m_models.TryGetValue(actor.ModelAsset, out BlockMesh mesh))
                    {
                        mesh = new(); Model model = ContentManager.Get<Model>(actor.ModelAsset);
                        foreach (ModelMesh part in model.Meshes)
                            foreach (ModelMeshPart geometry in part.MeshParts)
                                mesh.AppendModelMeshPart(geometry, BlockMesh.GetBoneAbsoluteTransform(part.ParentBone), false, false, false, false, Color.White);
                        m_models.Add(actor.ModelAsset, mesh);
                    }
                    BlocksManager.DrawMeshBlock(m_actorRenderer, mesh, ContentManager.Get<Texture2D>(actor.TextureAsset), Color.White, 1, ref transform, environment);
                }
            }
            m_actorRenderer.Flush(Matrix.Identity);
        }
        private void BuildSection(RealmPonderSection section, Terrain source, RealmPonderState state)
        {
            SubsystemTerrain subsystem = m_realm.Project.FindSubsystem<SubsystemTerrain>(true);
            var main = GameManager.Project;
            Texture2D texture = main?.FindSubsystem<SubsystemAnimatedTextures>(false)?.AnimatedBlocksTexture ?? ContentManager.Get<Texture2D>("Textures/Blocks");
            using Terrain mesh = new();
            int shadow = Terrain.MakeBlockValue(ShadowBlock.Index, 15, 0);
            // Bright air around the selected cells. Each section owns complete faces, including hidden neighbours.
            foreach (Point3 p in section.Selection)
                for (int x = p.X - 1; x <= p.X + 1; x++)
                    for (int z = p.Z - 1; z <= p.Z + 1; z++)
                    {
                        EnsureChunk(mesh, x, z);
                        for (int y = Math.Max(0, p.Y - 1); y <= Math.Min(255, p.Y + 1); y++) mesh.SetCellValueFast(x, y, z, shadow);
                    }
            foreach (Point3 p in section.Selection)
            {
                int value = source.GetCellValue(p.X, p.Y, p.Z);
                if (Terrain.ExtractContents(value) != 0)
                    mesh.SetCellValueFast(p.X, p.Y, p.Z, Terrain.ReplaceLight(value, 15));
            }
            BlockGeometryGenerator generator = new(mesh, subsystem, new SubsystemElectricity { SubsystemTerrain = subsystem },
                main?.FindSubsystem<SubsystemFurnitureBlockBehavior>(false), null, main?.FindSubsystem<SubsystemPalette>(false));
            TerrainGeometry geometry = new(texture);
            foreach (Point3 p in section.Selection)
            {
                int value = source.GetCellValue(p.X, p.Y, p.Z);
                if (Terrain.ExtractContents(value) == 0) continue;
                BlocksManager.Blocks[Terrain.ExtractContents(value)].GenerateTerrainVertices(generator, geometry, Terrain.ReplaceLight(value, 15), p.X, p.Y, p.Z);
            }
            DynamicArray<TerrainChunkGeometry.Buffer> buffers = [];
            m_buffers.Add(section.Id, buffers);
            TerrainRenderer.CompileDrawSubsets([geometry], buffers);
            geometry.ClearGeometry();
        }
        private static void EnsureChunk(Terrain terrain, int x, int z)
        {
            if (terrain.GetChunkAtCell(x, z) == null) terrain.AllocateChunk(x >> 4, z >> 4).State = TerrainChunkState.Valid;
        }
        private void DisposeBuffers()
        {
            foreach (var buffers in m_buffers.Values) foreach (var buffer in buffers) buffer.Dispose();
            m_buffers.Clear();
        }
        public void Dispose()
        {
            Display.DeviceReset -= Invalidate;
            if (m_realm != null) m_realm.Viewport.AfterDraw = null;
            m_realm = null; m_player = null; m_state = null;
            DisposeBuffers(); m_shader?.Dispose(); m_shader = null; m_transparentShader?.Dispose(); m_transparentShader = null; m_models.Clear();
        }
    }
}
