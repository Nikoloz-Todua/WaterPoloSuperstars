using System.Runtime.InteropServices;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace ToduStudios.WaterPolo
{
    /// <summary>One transparent batch for surface detail and deterministic spray. No physics or Unity particle simulation.</summary>
    internal sealed class WaterContactMesh
    {
        internal const int MaximumSurfaces = 112, MaximumParticles = 192;
        internal const float LongestLifetime = 1.36f;
        private const float SurfaceOffset = .024f;
        private const int Capacity = MaximumSurfaces + MaximumParticles;
        private const MeshUpdateFlags Upload = MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices;
        private static readonly ProfilerMarker DrawMarker = new("WPS.Water.UploadContactMesh");
        private static readonly VertexAttributeDescriptor[] Layout =
        {
            new(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
            new(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
            new(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 2)
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct Vertex
        {
            public Vector3 Position;
            public Color32 Color;
            public Vector2 UV, Shape;
        }

        private readonly Vertex[] vertices = new Vertex[Capacity * 4];
        private readonly Mesh mesh;
        private readonly MeshRenderer renderer;
        private Camera camera;
        private Vector3 eye, right, up;
        private float rangeSquared, line;
        private int count, surfaceBudget, particleBudget;
        private WaterEffectsQuality quality;
        internal int SurfaceCount { get; private set; }
        internal int ParticleCount { get; private set; }
        internal int ImpactSurfaceCount { get; private set; }

        internal WaterContactMesh(Transform parent, Material material)
        {
            var root = new GameObject("Batched pool contacts", typeof(MeshFilter), typeof(MeshRenderer));
            root.transform.SetParent(parent, true);
            renderer = root.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.enabled = false;
            mesh = new Mesh { name = "Pooled water surface and spray" };
            mesh.MarkDynamic();
            mesh.SetVertexBufferParams(vertices.Length, Layout);
            var indices = new ushort[Capacity * 6];
            for (int i = 0; i < Capacity; i++)
            {
                int v = 4 * i, t = 6 * i;
                indices[t] = (ushort)v;
                indices[t + 1] = (ushort)(v + 1);
                indices[t + 2] = (ushort)(v + 2);
                indices[t + 3] = (ushort)v;
                indices[t + 4] = (ushort)(v + 2);
                indices[t + 5] = (ushort)(v + 3);
            }
            mesh.SetIndexBufferParams(indices.Length, IndexFormat.UInt16);
            mesh.SetIndexBufferData(indices, 0, 0, indices.Length, Upload);
            mesh.subMeshCount = 1;
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(200f, 30f, 200f));
            root.GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        internal void Begin(Camera view, int surfaces, int particles, float range, float waterline, WaterEffectsQuality tier)
        {
            count = SurfaceCount = ParticleCount = ImpactSurfaceCount = 0;
            camera = view;
            var lens = view.transform;
            eye = lens.position;
            right = lens.right;
            up = lens.up;
            quality = tier;
            surfaceBudget = surfaces;
            particleBudget = particles;
            rangeSquared = range * range;
            line = waterline;
        }

        internal void Append(in WaterContactStamp stamp, int seed, float age)
        {
            if (SurfaceCount >= surfaceBudget && ParticleCount >= particleBudget) return;
            bool low = quality == WaterEffectsQuality.Low;
            if (low && (stamp.Kind == WaterContactKind.Pull || stamp.Kind == WaterContactKind.Kick || stamp.Kind == WaterContactKind.Exit ||
                        stamp.Kind == WaterContactKind.Release || (stamp.Kind == WaterContactKind.Wake && (seed & 1) != 0))) return;
            bool wake = stamp.Kind == WaterContactKind.Wake || stamp.Kind == WaterContactKind.Bow;
            float lifetime = wake ? LongestLifetime : stamp.Kind == WaterContactKind.Impact ? 1.19f : .87f;
            if (age >= lifetime) return;
            float distance = (stamp.Point - eye).sqrMagnitude;
            if (distance > rangeSquared) return;
            var viewport = camera.WorldToViewportPoint(stamp.Point);
            if (viewport.z <= 0f || viewport.x < -.12f || viewport.x > 1.12f || viewport.y < -.12f || viewport.y > 1.12f) return;

            Vector3 travel = stamp.Velocity;
            travel.y = 0f;
            Vector3 axis = travel.sqrMagnitude > .03f ? travel.normalized : Vector3.forward;
            Vector3 across = Vector3.Cross(Vector3.up, axis);
            float progress = age / lifetime;
            float fade = 1f - progress;
            if (stamp.Kind != WaterContactKind.Release && stamp.Kind != WaterContactKind.Exit && SurfaceCount < surfaceBudget)
            {
                float size = stamp.Radius * (.38f + 1.37f * Mathf.Sqrt(progress));
                Vector3 center = stamp.Point;
                center.y = line + SurfaceOffset;
                if (wake) center -= axis * (.16f + age * .27f);
                float opacity = Mathf.Clamp01((.15f + stamp.Strength * .63f) * fade * (.72f + .28f * fade));
                float mode = wake ? 1f : stamp.Kind == WaterContactKind.Kick || stamp.Kind == WaterContactKind.Pull ? 2f : 0f;
                Quad(center, across * (wake ? size * .63f : size), axis * (wake ? size * 1.24f : size),
                    Tint(wake ? new Color32(191, 236, 245, 255) : new Color32(202, 243, 253, 255), opacity), mode, progress);
                SurfaceCount++;
                if (stamp.Kind == WaterContactKind.Impact) ImpactSurfaceCount++;
            }

            if (ParticleCount >= particleBudget) return;
            bool bubble = stamp.Kind == WaterContactKind.Pull || stamp.Kind == WaterContactKind.Kick;
            bool wet = stamp.Kind == WaterContactKind.Impact || stamp.Kind == WaterContactKind.Entry || stamp.Kind == WaterContactKind.Exit ||
                       stamp.Kind == WaterContactKind.Keeper || stamp.Kind == WaterContactKind.Gather || stamp.Kind == WaterContactKind.Release ||
                       stamp.Kind == WaterContactKind.Celebration;
            if ((!wet && !bubble) || (low && stamp.Kind != WaterContactKind.Impact && stamp.Kind != WaterContactKind.Entry)) return;
            if (age >= (bubble ? .54f : .55f) || distance > rangeSquared * .54f) return;

            int density = quality == WaterEffectsQuality.High ? 8 : quality == WaterEffectsQuality.Standard ? 6 : 3;
            int particles = bubble ? 2 : Mathf.RoundToInt(density * (.57f + stamp.Strength * .9f));
            if (stamp.Kind == WaterContactKind.Impact) particles += density;
            if (stamp.Kind == WaterContactKind.Exit || stamp.Kind == WaterContactKind.Release) particles = (particles + 1) / 2;
            for (int i = 0; i < particles && ParticleCount < particleBudget; i++)
            {
                int key = seed * 27 + i * 117;
                float t = age - Random01(key + 4) * .064f;
                if (t < 0f) continue;
                var direction = across * (Random01(key + 1) * 2f - 1f) + axis * (Random01(key + 7) * 1.5f - .34f);
                float height = (.83f + Random01(key + 9) * 2.15f) * (.52f + stamp.Strength * .85f);
                Vector3 position = stamp.Point + direction * ((.24f + stamp.Strength * .95f) * t + .045f);
                if (bubble)
                    position.y = line - .12f + t * .20f;
                else
                {
                    position.y += .033f + height * t - 5.8f * t * t;
                    if (position.y < line + .016f) continue;
                }
                float size = (.012f + .033f * Random01(key + 18)) * (.82f + stamp.Strength * .83f);
                float opacity = Mathf.Clamp01((1f - t / (bubble ? .50f : .54f)) * (bubble ? .33f : .94f));
                Quad(position, right * size, up * size * (bubble ? 1f : 1.65f),
                    Tint(new Color32(213, 246, 252, 255), opacity), 3f, 0f);
                ParticleCount++;
            }
        }

        private static Color32 Tint(Color32 color, float alpha)
        {
            color.a = (byte)(alpha * 255f);
            return color;
        }

        private static float Random01(int key)
        {
            uint n = (uint)key + 0x9e3779b9u;
            n = (n ^ (n >> 16)) * 0x21f0aaadu;
            n = (n ^ (n >> 15)) * 0x735a2d97u;
            return ((n ^ (n >> 15)) & 0x00ffffffu) / 16777215f;
        }

        private void Quad(Vector3 center, Vector3 x, Vector3 y, Color32 color, float mode, float age)
        {
            int at = count++ * 4;
            var v = new Vertex { Color = color, Shape = new Vector2(mode, age) };
            v.Position = center - x - y; v.UV = new Vector2(-1f, -1f); vertices[at] = v;
            v.Position = center + x - y; v.UV = new Vector2( 1f, -1f); vertices[at + 1] = v;
            v.Position = center + x + y; v.UV = new Vector2( 1f,  1f); vertices[at + 2] = v;
            v.Position = center - x + y; v.UV = new Vector2(-1f,  1f); vertices[at + 3] = v;
        }

        internal void Flush()
        {
            renderer.enabled = count > 0;
            if (count == 0) return;
            using (DrawMarker.Auto())
            {
                mesh.SetVertexBufferData(vertices, 0, 0, count * 4, 0, Upload);
                mesh.SetSubMesh(0, new SubMeshDescriptor(0, count * 6)
                {
                    firstVertex = 0, vertexCount = count * 4, bounds = mesh.bounds
                }, Upload);
            }
        }

        internal void Clear()
        {
            renderer.enabled = false;
            count = SurfaceCount = ParticleCount = ImpactSurfaceCount = 0;
        }

        internal void Dispose()
        {
            Object.Destroy(renderer.gameObject);
            Object.Destroy(mesh);
        }
    }
}
