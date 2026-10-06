using System;
using NUnit.Framework;
using Spine;
using UnityEngine;
using VMUnityAutomation.Editor;

namespace VMSpinePipeline.Editor.Tests
{
    public sealed class SpinePoseSamplerTests
    {
        [Test]
        public void AttachmentTimelineSelectsTheNewAttachmentAtEveryIndependentSample()
        {
            SkeletonData data = CreateData();
            SkeletonPoseResult result = SpinePoseSampler.Sample(data, Request(0, 0.5f, 1));
            Assert.That(result.poses.Length, Is.EqualTo(3));
            foreach (PoseRecord pose in result.poses)
            {
                Assert.That(pose.slots[0].attachmentName, Is.EqualTo("attack-weapon"));
                Assert.That(pose.bounds.width, Is.EqualTo(2).Within(0.0001f));
                Assert.That(pose.bounds.height, Is.EqualTo(2).Within(0.0001f));
            }
            Assert.That(data.Slots.Items[0].AttachmentName, Is.EqualTo("idle-weapon"));
        }

        [TestCase("skin", "spine_skin_not_found")]
        [TestCase("animation", "spine_animation_not_found")]
        [TestCase("time", "spine_sample_time_invalid")]
        [TestCase("budget", "spine_budget_exceeded")]
        public void InvalidAuthoringAndRequestDomainsExposeTheOwnerError(string invalid, string code)
        {
            SkeletonPoseRequest request = Request(0.5f);
            if (invalid == "skin") request.skinName = "removed-skin";
            if (invalid == "animation") request.animationName = "removed-animation";
            if (invalid == "time") request.times = new[] { 2f };
            if (invalid == "budget") request.maxRecords = 1;
            var error = Assert.Throws<VmProjectToolException>(() => SpinePoseSampler.Sample(CreateData(), request));
            Assert.That(error.ErrorCode, Is.EqualTo(code));
        }

        [Test]
        public void BonePoseRotationIncludesTheParentRotation()
        {
            SkeletonData data = CreateData();
            BoneData root = data.Bones.Items[0];
            root.Rotation = 21;
            data.Bones.Add(new BoneData(1, "child", root) { Rotation = 35 });
            PoseRecord pose = SpinePoseSampler.Sample(data, Request(0.5f)).poses[0];
            Assert.That(pose.bones[1].rotation, Is.EqualTo(56).Within(0.0001f));
        }

        [Test]
        public void HiddenSkeletonPoseReportsEmptyGeometryWithoutNonFiniteBounds()
        {
            SkeletonData data = CreateData();
            ((AttachmentTimeline)data.Animations.Items[0].Timelines.Items[0]).SetFrame(0, 0, null);
            PoseRecord pose = SpinePoseSampler.Sample(data, Request(0.5f)).poses[0];
            Assert.That(pose.hasGeometry, Is.False);
            Assert.That(pose.bounds.width, Is.Zero);
            Assert.That(pose.bounds.x, Is.Zero);
            Assert.That(pose.slots[0].hasAttachment, Is.False);
        }

        [Test]
        public void RequestSchemaComesFromTheTypedContractAndIsClosed()
        {
            var schema = VmJsonContract.CreateSchema(typeof(SkeletonPoseRequest));
            Assert.That(schema["additionalProperties"], Is.False);
            Assert.That(schema["required"], Does.Contain("assetPath"));
            Assert.That(schema["required"], Does.Contain("skinName"));
            Assert.That(schema["required"], Does.Contain("animationName"));
            Assert.That(schema["required"], Does.Contain("times"));
        }

        [Test]
        public void WeightedMeshMetadataAndIndependentBoundsUseNativeGeometry()
        {
            SkeletonData data = CreateData();
            MeshAttachment mesh = WeightedMesh();
            data.DefaultSkin.SetAttachment(0, "attack-weapon", mesh);
            foreach (var entry in data.DefaultSkin.Attachments)
            {
                if (entry.Name != "attack-weapon") continue;
                AttachmentRecord record = SpineSkeletonDataTools.ReadAttachment(data, entry);
                Assert.That(record.type, Is.EqualTo("MeshAttachment"));
                Assert.That(record.weighted, Is.True);
                Assert.That(record.linkedMesh, Is.False);
                Assert.That(record.vertexCount, Is.EqualTo(3));
                Assert.That(record.triangleCount, Is.EqualTo(1));
                Assert.That(record.width, Is.EqualTo(2));
                Assert.That(record.height, Is.EqualTo(3));
            }
            PoseRecord pose = SpinePoseSampler.Sample(data, Request(0.5f)).poses[0];
            Assert.That(pose.hasGeometry, Is.True);
            Assert.That(pose.bounds.width, Is.EqualTo(2).Within(0.0001f));
            Assert.That(pose.bounds.height, Is.EqualTo(2).Within(0.0001f));
            Assert.That(mesh.Vertices, Is.EqualTo(new[] { -1f, -1f, 1f, -1f, 1f, 1f, 1f, 1f, 1f }));
        }

        [Test]
        public void LinkedWeightedMeshSlotReportsItsActualMaterialAndTexture()
        {
            var shader = Shader.Find("Sprites/Default");
            Assert.That(shader, Is.Not.Null);
            var texture = new Texture2D(2, 2) { name = "Spine Mesh Inspection Test" };
            var material = new Material(shader);
            try
            {
                Assert.That(material.HasProperty("_MainTex"), Is.True);
                material.mainTexture = texture;
                Assert.That(material.mainTexture, Is.SameAs(texture));
                MeshAttachment source = WeightedMesh();
                source.Region = new AtlasRegion
                {
                    width = 2, height = 2, originalWidth = 2, originalHeight = 2,
                    u = 0, v = 0, u2 = 1, v2 = 1,
                    page = new AtlasPage { width = 2, height = 2, rendererObject = material }
                };
                MeshAttachment linked = source.NewLinkedMesh();
                Slot slot = new Skeleton(CreateData()).Slots.Items[0];
                slot.Attachment = linked;
                MecanimSlotRecord record = SpineMecanimTools.ReadSlot(slot);
                Assert.That(record.weighted, Is.True);
                Assert.That(record.linkedMesh, Is.True);
                Assert.That(record.vertexCount, Is.EqualTo(3));
                Assert.That(record.triangleCount, Is.EqualTo(1));
                Assert.That(record.textureName, Is.EqualTo(texture.name));
                Assert.That(record.materialInstanceId, Is.EqualTo(VmObjectId.Get(material)));
                Assert.That(record.textureInstanceId, Is.EqualTo(VmObjectId.Get(texture)));
                Assert.That(linked.Bones, Is.SameAs(source.Bones));
                Assert.That(linked.Vertices, Is.SameAs(source.Vertices));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static MeshAttachment WeightedMesh()
        {
            return new MeshAttachment("attack-weapon")
            {
                Width = 2, Height = 3, WorldVerticesLength = 6,
                Bones = new[] { 1, 0, 1, 0, 1, 0 },
                Vertices = new[] { -1f, -1f, 1f, -1f, 1f, 1f, 1f, 1f, 1f },
                RegionUVs = new[] { 0f, 0f, 0f, 1f, 1f, 1f },
                Triangles = new[] { 0, 1, 2 }
            };
        }

        private static SkeletonPoseRequest Request(params float[] times)
        {
            return new SkeletonPoseRequest
            {
                assetPath = "test-skeleton", skinName = "default", animationName = "attack",
                times = times, maxRecords = 100
            };
        }

        private static SkeletonData CreateData()
        {
            var data = new SkeletonData();
            var bone = new BoneData(0, "root", null);
            data.Bones.Add(bone);
            data.Slots.Add(new SlotData(0, "Weapon", bone) { AttachmentName = "idle-weapon" });
            var skin = new Skin("default");
            skin.SetAttachment(0, "idle-weapon", Region("idle-weapon"));
            skin.SetAttachment(0, "attack-weapon", Region("attack-weapon"));
            data.Skins.Add(skin);
            data.DefaultSkin = skin;
            var timeline = new AttachmentTimeline(1, 0);
            timeline.SetFrame(0, 0, "attack-weapon");
            var timelines = new ExposedList<Timeline>();
            timelines.Add(timeline);
            data.Animations.Add(new Spine.Animation("attack", timelines, 1));
            return data;
        }

        private static RegionAttachment Region(string name)
        {
            var attachment = new RegionAttachment(name);
            Array.Copy(new[] { -1f, -1f, -1f, 1f, 1f, 1f, 1f, -1f }, attachment.Offset, 8);
            return attachment;
        }
    }
}
