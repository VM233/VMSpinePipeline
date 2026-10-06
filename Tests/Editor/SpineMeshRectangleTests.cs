using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Spine;
using UnityEngine;
using VMUnityAutomation.Editor;

namespace VMSpinePipeline.Editor.Tests
{
    public sealed class SpineMeshRectangleTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void ExpandedTopologyIsOneRectangleAndLeavesAuthoredMeshUntouched(bool linked)
        {
            Slot slot = CreateSlot(out MeshAttachment original);
            int[] bones = original.Bones;
            float[] vertices = original.Vertices, uvs = original.RegionUVs;
            int[] triangles = original.Triangles;
            MeshAttachment expanded = SpineMeshRectangle.Expand(linked ? original.NewLinkedMesh() : original, slot);
            Assert.That(expanded.WorldVerticesLength, Is.EqualTo(original.WorldVerticesLength + 8));
            Assert.That(expanded.Triangles.Length / 3, Is.EqualTo(14));
            Assert.That(expanded.HullLength, Is.EqualTo(8));
            Assert.That(expanded.ParentMesh, Is.Null);
            Assert.That(expanded.RegionUVs.Skip(8), Is.EqualTo(uvs));
            Assert.That(expanded.Triangles.Take(triangles.Length), Is.EqualTo(triangles.Select(i => i + 4)));
            Assert.That(original.Bones, Is.SameAs(bones));
            Assert.That(original.Vertices, Is.SameAs(vertices));
            Assert.That(original.RegionUVs, Is.SameAs(uvs));
            Assert.That(original.Triangles, Is.SameAs(triangles));

            double area = 0;
            var edges = new Dictionary<(int, int), int>();
            for (int i = 0; i < expanded.Triangles.Length; i += 3)
            {
                int a = expanded.Triangles[i], b = expanded.Triangles[i + 1], c = expanded.Triangles[i + 2];
                double signed = Cross(Point(expanded, a), Point(expanded, b), Point(expanded, c));
                Assert.That(signed, Is.GreaterThan(0));
                area += signed / 2;
                AddEdge(edges, a, b); AddEdge(edges, b, c); AddEdge(edges, c, a);
            }
            Assert.That(area, Is.EqualTo(1).Within(1e-7));
            Assert.That(edges.Values.Count(count => count == 1), Is.EqualTo(4));
            Assert.That(edges.Values.All(count => count == 1 || count == 2), Is.True);
            var pairs = edges.Keys.ToArray();
            foreach (var first in pairs)
                foreach (var second in pairs)
                {
                    if (first.Item1 == second.Item1 || first.Item1 == second.Item2 ||
                        first.Item2 == second.Item1 || first.Item2 == second.Item2) continue;
                    Vector2 a = Point(expanded, first.Item1), b = Point(expanded, first.Item2);
                    Vector2 c = Point(expanded, second.Item1), d = Point(expanded, second.Item2);
                    Assert.That(Cross(a, b, c) * Cross(a, b, d) < 0 && Cross(c, d, a) * Cross(c, d, b) < 0, Is.False);
                }
        }

        [Test]
        public void OriginalWeightedVerticesRemainIdenticalAcrossIndependentBonePoses()
        {
            Slot slot = CreateSlot(out MeshAttachment original);
            MeshAttachment expanded = SpineMeshRectangle.Expand(original, slot);
            float[] originalVertices = (float[])original.Vertices.Clone();
            var before = new float[original.WorldVerticesLength];
            var after = new float[expanded.WorldVerticesLength];
            float[] previous = null;
            foreach (float angle in new[] { 0f, 35f, -20f })
            {
                Skeleton skeleton = slot.Bone.Skeleton;
                skeleton.SetToSetupPose();
                skeleton.Bones.Items[1].Rotation += angle;
                skeleton.Bones.Items[2].Rotation -= angle;
                skeleton.UpdateWorldTransform(Skeleton.Physics.Pose);
                original.ComputeWorldVertices(slot, before);
                expanded.ComputeWorldVertices(slot, after);
                Assert.That(after.Skip(8), Is.EqualTo(before));
                Assert.That(after.All(value => !float.IsNaN(value) && !float.IsInfinity(value)), Is.True);
                if (previous != null) Assert.That(after.Take(8), Is.Not.EqualTo(previous.Take(8)));
                previous = (float[])after.Clone();
            }
            Assert.That(original.Vertices, Is.EqualTo(originalVertices));
        }

        [Test]
        public void AuthoredVertexDeformExposesAnExplicitUnsupportedDomain()
        {
            Slot slot = CreateSlot(out MeshAttachment original);
            var timeline = new DeformTimeline(1, 0, 0, original);
            timeline.SetFrame(0, 0, new float[original.Vertices.Length / 3 * 2]);
            var timelines = new ExposedList<Timeline>(); timelines.Add(timeline);
            slot.Bone.Skeleton.Data.Animations.Add(new Spine.Animation("vertex-deform", timelines, 0));
            var error = Assert.Throws<ArgumentException>(() => SpineMeshRectangle.Expand(original, slot));
            Assert.That(error.Message, Does.Contain("vertex-deform"));
            Assert.That(error.Message, Does.Contain("bone-driven"));
        }

        [Test]
        public void CoverageRouteUsesAClosedTypedContract()
        {
            var schema = VmJsonContract.CreateSchema(typeof(MeshSpriteCoverageRequest));
            Assert.That(schema["additionalProperties"], Is.False);
            Assert.That(schema["required"], Does.Contain("spriteAssetPath"));
            Assert.That(schema["required"], Does.Contain("attachmentName"));
        }

        private static Slot CreateSlot(out MeshAttachment mesh)
        {
            var data = new SkeletonData();
            var root = new BoneData(0, "root", null) { Rotation = 17 };
            data.Bones.Add(root);
            data.Bones.Add(new BoneData(1, "upper-limb", root) { X = 0.3f, Rotation = 23 });
            data.Bones.Add(new BoneData(2, "lower-limb", root) { Y = -0.2f, Rotation = -11 });
            data.Slots.Add(new SlotData(0, "weapon", root));
            var skeleton = new Skeleton(data);
            skeleton.UpdateWorldTransform(Skeleton.Physics.Pose);
            float[] uvs = { .2f, .2f, .8f, .2f, .8f, .8f, .5f, .8f, .5f, .5f, .2f, .5f };
            var vertices = new List<float>();
            var bones = new List<int>();
            for (int i = 0; i < 6; i++)
            {
                int boneIndex = i < 2 ? 0 : i < 4 ? 1 : 2;
                skeleton.Bones.Items[boneIndex].WorldToLocal(uvs[i * 2] * 2 - 1, 1 - uvs[i * 2 + 1] * 2, out float x, out float y);
                bones.Add(1); bones.Add(boneIndex); vertices.Add(x); vertices.Add(y); vertices.Add(1);
            }
            mesh = new MeshAttachment("weapon")
            {
                Region = new TextureRegion { width = 2, height = 2, u = 0, v = 0, u2 = 1, v2 = 1 },
                RegionUVs = uvs, Bones = bones.ToArray(), Vertices = vertices.ToArray(),
                WorldVerticesLength = 12, HullLength = 12, Triangles = new[] { 0, 1, 4, 1, 2, 4, 2, 3, 4, 0, 4, 5 }
            };
            mesh.UpdateRegion();
            return skeleton.Slots.Items[0];
        }

        private static Vector2 Point(MeshAttachment mesh, int index) => new(mesh.RegionUVs[index * 2], mesh.RegionUVs[index * 2 + 1]);
        private static double Cross(Vector2 a, Vector2 b, Vector2 c) =>
            ((double)b.x - a.x) * ((double)c.y - a.y) - ((double)b.y - a.y) * ((double)c.x - a.x);

        private static void AddEdge(Dictionary<(int, int), int> edges, int a, int b)
        {
            var edge = a < b ? (a, b) : (b, a);
            edges.TryGetValue(edge, out int count); edges[edge] = count + 1;
        }
    }
}
