using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;
using Game;
using GameEntitySystem;
using RealmEX.Core;

namespace RealmEX.Presets.Ponder
{
    /// <summary>
    /// 从沙箱 Terrain 生成真实方块网格并画到 Realm 离屏相机（Create 式 Ponder 呈现）。
    /// </summary>
    public sealed class RealmPonderBlockPresenter : IDisposable
    {
        private readonly DynamicArray<TerrainChunkGeometry.Buffer> m_buffers = [];
        private readonly DynamicArray<TerrainChunkGeometry.Buffer> m_revealBuffers = [];
        private readonly Dictionary<Point3, int> m_previousCells = [];
        private BlockGeometryGenerator m_generator;
        private TerrainGeometry m_geometry;
        private TerrainGeometry m_revealGeometry;
        private double m_revealStartTime;
        private Shader m_shader;
        private bool m_needsUpdate = true;
        private SandboxRealm m_realm;

        public void Attach(SandboxRealm realm)
        {
            m_realm = realm ?? throw new ArgumentNullException(nameof(realm));
            m_realm.Viewport.AfterDraw = Draw;
            Display.DeviceReset += Invalidate;
            m_needsUpdate = true;
        }

        public void Invalidate()
        {
            m_needsUpdate = true;
        }

        public void Dispose()
        {
            Display.DeviceReset -= Invalidate;
            if (m_realm != null)
            {
                m_realm.Viewport.AfterDraw = null;
                m_realm = null;
            }

            DisposeBuffers();
            m_geometry?.ClearGeometry();
            m_revealGeometry?.ClearGeometry();
            m_previousCells.Clear();
            m_generator?.Terrain?.Dispose();
            m_generator = null;
            m_geometry = null;
            m_revealGeometry = null;
            m_shader?.Dispose();
            m_shader = null;
        }

        private void Draw(SandboxRealm realm, Camera camera)
        {
            EnsureResources(realm);
            if (m_needsUpdate)
            {
                RebuildGeometry(realm);
                DisposeBuffers();
                TerrainRenderer.CompileDrawSubsets([m_geometry], m_buffers);
                TerrainRenderer.CompileDrawSubsets([m_revealGeometry], m_revealBuffers);
                m_revealStartTime = Time.FrameStartTime;
                m_needsUpdate = false;
            }

            DrawBuffers(m_buffers, camera, 1f);
            float progress = Math.Clamp((float)(Time.FrameStartTime - m_revealStartTime) / 0.45f, 0f, 1f);
            DrawBuffers(m_revealBuffers, camera, 1f - MathF.Pow(1f - progress, 3f));
        }

        private void DrawBuffers(DynamicArray<TerrainChunkGeometry.Buffer> buffers, Camera camera, float reveal)
        {
            m_shader.GetParameter("u_viewProjectionMatrix").SetValue(camera.ViewProjectionMatrix);
            m_shader.GetParameter("u_verticalOffset").SetValue((1f - reveal) * 0.6f);
            m_shader.GetParameter("u_reveal").SetValue(reveal);
            for (int i = 0; i < buffers.Count; i++)
            {
                TerrainChunkGeometry.Buffer buffer = buffers[i];
                Display.BlendState = BlendState.AlphaBlend;
                Display.DepthStencilState = DepthStencilState.Default;
                Display.RasterizerState = RasterizerState.CullCounterClockwiseScissor;
                m_shader.GetParameter("u_texture").SetValue(buffer.Texture);
                m_shader.GetParameter("u_samplerState").SetValue(SamplerState.PointClamp);
                m_shader.GetParameter("u_alphaThreshold").SetValue(0.5f);
                Display.DrawIndexed(
                    PrimitiveType.TriangleList,
                    m_shader,
                    buffer.VertexBuffer,
                    buffer.IndexBuffer,
                    0,
                    buffer.IndexBuffer.IndicesCount);
            }
        }

        private void EnsureResources(SandboxRealm realm)
        {
            if (m_shader != null && m_generator != null)
            {
                return;
            }

            Project main = GameManager.Project
                ?? throw new InvalidOperationException("Main project is required for ponder block textures.");
            SubsystemAnimatedTextures animatedTextures =
                main.FindSubsystem<SubsystemAnimatedTextures>(true);
            SubsystemFurnitureBlockBehavior furniture =
                main.FindSubsystem<SubsystemFurnitureBlockBehavior>(true);
            SubsystemPalette palette = main.FindSubsystem<SubsystemPalette>(true);
            SubsystemTerrain sandboxTerrain = realm.Project.FindSubsystem<SubsystemTerrain>(true);

            Terrain meshTerrain = new();
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    meshTerrain.AllocateChunk(i, j).State = TerrainChunkState.Valid;
                }
            }

            // GenerateWireVertices 在 SubsystemElectricity==null 时直接 return；
            // 只需挂上沙箱 Terrain，供邻接查询画出导线/器件端子，不跑仿真。
            SubsystemElectricity electricityForMeshes = new()
            {
                SubsystemTerrain = sandboxTerrain
            };

            m_generator = new BlockGeometryGenerator(
                meshTerrain,
                sandboxTerrain,
                electricityForMeshes,
                furniture,
                null,
                palette);
            m_geometry = new TerrainGeometry(animatedTextures.AnimatedBlocksTexture);
            m_revealGeometry = new TerrainGeometry(animatedTextures.AnimatedBlocksTexture);
            m_shader = new Shader(
                ShaderCodeManager.GetFast("Shaders/RealmPonderBlocks.vsh"),
                ShaderCodeManager.GetFast("Shaders/RealmPonderBlocks.psh"),
                [new ShaderMacro("ALPHATESTED")]);
        }

        private void RebuildGeometry(SandboxRealm realm)
        {
            SubsystemTerrain sandboxTerrain = realm.Project.FindSubsystem<SubsystemTerrain>(true);
            Terrain source = sandboxTerrain.Terrain;
            Terrain mesh = m_generator.Terrain;

            foreach (TerrainChunk chunk in mesh.AllocatedChunks)
            {
                chunk.State = TerrainChunkState.Valid;
            }

            const int min = 4;
            const int max = 13;
            const int yMin = 0;
            const int yMax = 8;
            int shadow = Terrain.MakeBlockValue(ShadowBlock.Index, 15, 0);
            for (int x = min - 1; x <= max + 1; x++)
            {
                for (int z = min - 1; z <= max + 1; z++)
                {
                    for (int y = yMin; y <= yMax; y++)
                    {
                        if (mesh.IsCellValid(x, y, z))
                        {
                            mesh.SetCellValueFast(x, y, z, shadow);
                        }
                    }
                }
            }

            for (int x = min; x <= max; x++)
            {
                for (int z = min; z <= max; z++)
                {
                    for (int y = yMin; y <= yMax; y++)
                    {
                        if (!source.IsCellValid(x, y, z) || !mesh.IsCellValid(x, y, z))
                        {
                            continue;
                        }

                        int value = source.GetCellValueFast(x, y, z);
                        if (Terrain.ExtractContents(value) == 0)
                        {
                            continue;
                        }

                        int light = mesh.GetCellLightFast(x, y, z);
                        mesh.SetCellValueFast(x, y, z, Terrain.ReplaceLight(value, light));
                    }
                }
            }

            m_generator.ResetCache();
            m_geometry.ClearGeometry();
            m_revealGeometry.ClearGeometry();
            Dictionary<Point3, int> currentCells = [];
            for (int x = min; x <= max; x++)
            {
                for (int z = min; z <= max; z++)
                {
                    for (int y = yMin; y <= yMax; y++)
                    {
                        int value = mesh.GetCellValueFast(x, y, z);
                        int content = Terrain.ExtractContents(value);
                        if (content == 0 || content == ShadowBlock.Index)
                        {
                            continue;
                        }

                        Point3 position = new(x, y, z);
                        int sourceValue = source.GetCellValueFast(x, y, z);
                        currentCells[position] = sourceValue;
                        bool changed = !m_previousCells.TryGetValue(position, out int oldValue) || oldValue != sourceValue;
                        BlocksManager.Blocks[content].GenerateTerrainVertices(
                            m_generator,
                            changed ? m_revealGeometry : m_geometry,
                            value,
                            x,
                            y,
                            z);
                    }
                }
            }
            m_previousCells.Clear();
            foreach (var cell in currentCells) m_previousCells.Add(cell.Key, cell.Value);
        }

        private void DisposeBuffers()
        {
            foreach (TerrainChunkGeometry.Buffer buffer in m_buffers)
            {
                buffer.Dispose();
            }
            m_buffers.Clear();
            foreach (TerrainChunkGeometry.Buffer buffer in m_revealBuffers) buffer.Dispose();
            m_revealBuffers.Clear();
        }
    }
}
