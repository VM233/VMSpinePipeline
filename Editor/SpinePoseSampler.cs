using System;
using System.Collections.Generic;
using System.Linq;
using Spine;
using VMUnityAutomation.Editor;

namespace VMSpinePipeline.Editor
{
    internal static class SpinePoseSampler
    {
        private const int MaximumGeometryElements = 65536;
        private const int MaximumTimes = 16;

        internal static int CountRecords(SkeletonData data)
        {
            long count = data.Bones.Count + (long)data.Slots.Count + data.Skins.Count + data.Animations.Count;
            RequireBudget((int)Math.Min(count, int.MaxValue), 8192);
            foreach (Skin skin in data.Skins)
            {
                count += skin.Attachments.Count;
                RequireBudget((int)Math.Min(count, int.MaxValue), 8192);
            }
            return (int)count;
        }

        internal static void RequireBudget(int records, int maximum)
        {
            if (maximum < 1 || maximum > 8192 || records > maximum)
                throw new VmProjectToolException("spine_budget_exceeded", $"Operation requires {records} records; maxRecords is {maximum}, within 1..8192.");
        }

        internal static SkeletonPoseResult Sample(SkeletonData data, SkeletonPoseRequest request)
        {
            Skin skin = data.FindSkin(request.skinName);
            if (skin == null)
                throw new VmProjectToolException("spine_skin_not_found", $"Skin '{request.skinName}' does not exist in '{request.assetPath}'.");
            Spine.Animation animation = data.FindAnimation(request.animationName);
            if (animation == null)
                throw new VmProjectToolException("spine_animation_not_found", $"Animation '{request.animationName}' does not exist in '{request.assetPath}'.");
            if (request.times == null || request.times.Length < 1 || request.times.Length > MaximumTimes)
                throw new VmProjectToolException("spine_budget_exceeded", "Pose sampling requires 1..16 times.");
            int records = CountRecords(data) + animation.Timelines.Count +
                          request.times.Length * (data.Bones.Count + data.Slots.Count + 2);
            RequireBudget(records, request.maxRecords);
            RequireGeometryBudget(data, new[] { animation });
            foreach (float time in request.times)
                if (float.IsNaN(time) || float.IsInfinity(time) || time < 0 || time > animation.Duration)
                    throw new VmProjectToolException("spine_sample_time_invalid", $"Sample time {time} is outside animation duration 0..{animation.Duration}.");

            var skeleton = new Skeleton(data);
            skeleton.SetSkin(skin);
            float[] vertices = Array.Empty<float>();
            var poses = new PoseRecord[request.times.Length];
            for (int index = 0; index < poses.Length; index++)
            {
                skeleton.SetToSetupPose();
                animation.Apply(skeleton, -1, request.times[index], false, null, 1, MixBlend.Setup, MixDirection.In);
                skeleton.UpdateWorldTransform(Skeleton.Physics.Pose);
                skeleton.GetBounds(out float x, out float y, out float width, out float height, ref vertices);
                bool hasGeometry = skeleton.Slots.Any(s => s.Attachment is RegionAttachment || s.Attachment is MeshAttachment);
                poses[index] = new PoseRecord
                {
                    time = request.times[index],
                    hasGeometry = hasGeometry,
                    bounds = hasGeometry
                        ? new BoundsRecord { x = x, y = y, width = width, height = height }
                        : new BoundsRecord(),
                    bones = skeleton.Bones.Select(b => new BonePoseRecord
                    {
                        name = b.Data.Name, worldX = b.WorldX, worldY = b.WorldY, rotation = b.WorldRotationX
                    }).ToArray(),
                    slots = skeleton.Slots.Select(s => new SlotPoseRecord
                    {
                        name = s.Data.Name, boneName = s.Bone.Data.Name,
                        attachmentName = s.Attachment?.Name ?? "", hasAttachment = s.Attachment != null
                    }).ToArray()
                };
            }
            return new SkeletonPoseResult
            {
                assetPath = request.assetPath, skinName = request.skinName,
                animationName = request.animationName, animationDuration = animation.Duration,
                recordCount = records, poses = poses
            };
        }

        internal static void RequireGeometryBudget(SkeletonData data, IEnumerable<Spine.Animation> animations)
        {
            long vertices = 0;
            foreach (Skin skin in data.Skins)
                foreach (Skin.SkinEntry entry in skin.Attachments)
                    if (entry.Attachment is VertexAttachment vertex)
                        vertices += vertex.Vertices.Length + (vertex.Bones?.Length ?? 0);
                    else if (entry.Attachment is RegionAttachment)
                        vertices += 8;
            long frames = 0;
            foreach (Spine.Animation animation in animations)
                foreach (Timeline timeline in animation.Timelines)
                    frames += timeline.Frames.Length;
            if (vertices > MaximumGeometryElements || frames > MaximumGeometryElements)
                throw new VmProjectToolException("spine_budget_exceeded", $"Geometry elements {vertices} and timeline frame elements {frames} must each be <= {MaximumGeometryElements}.");
        }
    }
}
