using System;
using Spine;
using UnityEditor;
using UnityEngine;
using VMUnityAutomation.Editor;

namespace VMSpinePipeline.Editor
{
    public static class SpineMeshCoverageTools
    {
        [VmProjectTool("spine/inspect-mesh-sprite-coverage",
            Description = "Read one exact Sprite and Spine mesh, compare opaque pixel coverage before/after bone-driven rectangle expansion, and prove original vertex positions are preserved at explicit animation times. Source assets remain unchanged.",
            ModuleId = "spine", ReadOnly = true,
            SideEffects = VmProjectToolSideEffect.ReadsProjectState,
            CompletionEvidence = "Native Sprite/GUID, complete expanded UV topology, opaque pixel coverage and original-vertex error per sampled pose.",
            ErrorCodes = new[] { "spine_asset_not_found", "spine_data_invalid", "spine_version_mismatch", "spine_skin_not_found", "spine_animation_not_found", "spine_sample_time_invalid", "spine_budget_exceeded", "spine_mesh_expansion_invalid", "spine_sprite_invalid" })]
        public static MeshSpriteCoverageResult Inspect(MeshSpriteCoverageRequest request)
        {
            SkeletonData data = SpineSkeletonDataTools.Read(SpineSkeletonDataTools.Load(request.assetPath));
            int dataRecords = SpinePoseSampler.CountRecords(data) + data.IkConstraints.Count +
                              data.TransformConstraints.Count + data.PathConstraints.Count + data.PhysicsConstraints.Count;
            SpinePoseSampler.RequireBudget(dataRecords, request.maxRecords);
            Skin skin = data.FindSkin(request.skinName) ??
                throw new VmProjectToolException("spine_skin_not_found", $"Skin '{request.skinName}' does not exist.");
            var skeleton = new Skeleton(data);
            skeleton.SetSkin(skin);
            skeleton.SetSlotsToSetupPose();
            skeleton.UpdateWorldTransform(Skeleton.Physics.Pose);
            Slot slot = skeleton.FindSlot(request.slotName) ??
                throw new VmProjectToolException("spine_mesh_expansion_invalid", $"Slot '{request.slotName}' does not exist.");
            if (skin.GetAttachment(slot.Data.Index, request.attachmentName) is not MeshAttachment original)
                throw new VmProjectToolException("spine_mesh_expansion_invalid", $"Attachment '{request.attachmentName}' is not a mesh in slot '{request.slotName}'.");
            MeshAttachment expanded;
            try { expanded = SpineMeshRectangle.Expand(original, slot); }
            catch (ArgumentException error) { throw new VmProjectToolException("spine_mesh_expansion_invalid", error.Message); }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(request.spriteAssetPath);
            if (sprite == null || sprite.packed || !sprite.texture.isReadable ||
                (long)sprite.texture.width * sprite.texture.height > 65536 || sprite.rect.width * sprite.rect.height > 4096)
                throw new VmProjectToolException("spine_sprite_invalid", $"'{request.spriteAssetPath}' must be one unpacked readable Sprite, <=4096 Sprite pixels and <=65536 texture pixels.");
            Rect rect = sprite.rect;
            int width = (int)rect.width, height = (int)rect.height;
            Color32[] pixels = sprite.texture.GetPixels32();
            int opaque = 0, missingOriginal = 0, missingExpanded = 0;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    if (pixels[((int)rect.y + y) * sprite.texture.width + (int)rect.x + x].a == 0) continue;
                    opaque++;
                    var uv = new Vector2((x + 0.5f) / width, 1 - (y + 0.5f) / height);
                    if (!SpineMeshRectangle.Covers(original, uv)) missingOriginal++;
                    if (!SpineMeshRectangle.Covers(expanded, uv)) missingExpanded++;
                }

            Spine.Animation animation = string.IsNullOrEmpty(request.animationName) ? null : data.FindAnimation(request.animationName) ??
                throw new VmProjectToolException("spine_animation_not_found", $"Animation '{request.animationName}' does not exist.");
            if (request.times == null || request.times.Length < 1 || request.times.Length > 16)
                throw new VmProjectToolException("spine_sample_time_invalid", "Mesh coverage accepts one to sixteen explicit sample times.");
            SpinePoseSampler.RequireGeometryBudget(data, animation == null ? Array.Empty<Spine.Animation>() : new[] { animation });
            int outputRecords = expanded.WorldVerticesLength + expanded.Triangles.Length + request.times.Length;
            SpinePoseSampler.RequireBudget(outputRecords, request.maxRecords);
            var poses = new MeshPosePreservationRecord[request.times.Length];
            var before = new float[original.WorldVerticesLength];
            var after = new float[expanded.WorldVerticesLength];
            for (int sample = 0; sample < request.times.Length; sample++)
            {
                float time = request.times[sample];
                if (float.IsNaN(time) || float.IsInfinity(time) || time < 0 || time > (animation?.Duration ?? 0))
                    throw new VmProjectToolException("spine_sample_time_invalid", $"Time {time} is outside the requested animation domain.");
                skeleton.SetToSetupPose();
                animation?.Apply(skeleton, -1, time, false, null, 1, MixBlend.Setup, MixDirection.In);
                skeleton.UpdateWorldTransform(Skeleton.Physics.Pose);
                original.ComputeWorldVertices(slot, before);
                expanded.ComputeWorldVertices(slot, after);
                double error = 0;
                bool finite = true;
                for (int i = 0; i < before.Length; i++)
                    error = Math.Max(error, Math.Abs((double)before[i] - after[i + SpineMeshRectangle.OriginalVertexOffset * 2]));
                foreach (float value in after) finite &= !float.IsNaN(value) && !float.IsInfinity(value);
                poses[sample] = new MeshPosePreservationRecord { time = time, maxOriginalVertexError = error, allVerticesFinite = finite };
            }
            return new MeshSpriteCoverageResult
            {
                success = true, spriteAssetPath = request.spriteAssetPath,
                spriteGuid = AssetDatabase.AssetPathToGUID(request.spriteAssetPath),
                opaquePixels = opaque, uncoveredOriginalPixels = missingOriginal, uncoveredExpandedPixels = missingExpanded,
                originalVertexOffset = SpineMeshRectangle.OriginalVertexOffset,
                originalVertexCount = original.WorldVerticesLength / 2, expandedVertexCount = expanded.WorldVerticesLength / 2,
                originalTriangleCount = original.Triangles.Length / 3, expandedTriangleCount = expanded.Triangles.Length / 3,
                expandedRegionUVs = expanded.RegionUVs, expandedTriangles = expanded.Triangles, poses = poses
            };
        }
    }
}
