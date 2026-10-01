using System;
using NUnit.Framework;
using Spine;
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
