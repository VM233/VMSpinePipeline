using System;
using System.Linq;
using Spine;
using Spine.Unity;
using UnityEditor;
using UnityEngine;
using VMUnityAutomation.Editor;

namespace VMSpinePipeline.Editor
{
    public static class SpineSkeletonDataTools
    {
        [VmProjectTool("spine/inspect-skeleton-data",
            Description = "Inspect one Spine 4.2 SkeletonDataAsset, its bones, slots, skin attachment geometry, atlas materials, animations and embedded Mecanim clips.",
            ModuleId = "spine", ReadOnly = true,
            SideEffects = VmProjectToolSideEffect.ReadsProjectState,
            CompletionEvidence = "Exact parsed skeleton data, resolved resource paths and controller clip identities.",
            ErrorCodes = new[] { "spine_asset_not_found", "spine_data_invalid", "spine_version_mismatch", "spine_resource_missing", "spine_budget_exceeded" })]
        public static SkeletonDataResult Inspect(SkeletonDataRequest request)
        {
            SkeletonDataAsset asset = Load(request.assetPath);
            SkeletonData data = Read(asset);
            int records = SpinePoseSampler.CountRecords(data);
            SpinePoseSampler.RequireBudget(records, request.maxRecords);
            if (asset.atlasAssets == null)
                throw new VmProjectToolException("spine_resource_missing", "SkeletonDataAsset has no atlas reference array.");
            records += asset.atlasAssets.Length;
            SpinePoseSampler.RequireBudget(records, request.maxRecords);
            foreach (AtlasAssetBase atlas in asset.atlasAssets)
            {
                if (atlas == null)
                    throw new VmProjectToolException("spine_resource_missing", "SkeletonDataAsset contains a missing atlas reference.");
                records += atlas.Materials.Count();
                SpinePoseSampler.RequireBudget(records, request.maxRecords);
            }
            UnityEngine.Object[] controllerObjects = asset.controller == null
                ? Array.Empty<UnityEngine.Object>()
                : AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(asset.controller));
            AnimationClip[] clips = controllerObjects.OfType<AnimationClip>().ToArray();
            records += clips.Length;
            SpinePoseSampler.RequireBudget(records, request.maxRecords);
            AtlasRecord[] atlases = asset.atlasAssets.Select(ReadAtlas).ToArray();
            return new SkeletonDataResult
            {
                assetPath = request.assetPath,
                guid = AssetDatabase.AssetPathToGUID(request.assetPath),
                sourcePath = AssetDatabase.GetAssetPath(asset.skeletonJSON),
                spineVersion = data.Version,
                scale = asset.scale,
                recordCount = records,
                bones = data.Bones.Select(b => new BoneRecord
                {
                    name = b.Name, parent = b.Parent?.Name ?? "", x = b.X, y = b.Y,
                    rotation = b.Rotation, scaleX = b.ScaleX, scaleY = b.ScaleY
                }).ToArray(),
                slots = data.Slots.Select(s => new SlotRecord
                {
                    index = s.Index, name = s.Name, boneName = s.BoneData.Name,
                    setupAttachmentName = s.AttachmentName ?? ""
                }).ToArray(),
                skins = data.Skins.Select(s => new SkinRecord
                {
                    name = s.Name,
                    attachments = s.Attachments.Select(a => ReadAttachment(data, a)).ToArray()
                }).ToArray(),
                animations = data.Animations.Select(a => new AnimationRecord
                {
                    name = a.Name, duration = a.Duration, timelineCount = a.Timelines.Count
                }).ToArray(),
                atlases = atlases,
                controllerPath = AssetDatabase.GetAssetPath(asset.controller),
                controllerClips = clips.Select(c => ReadClip(data, c)).ToArray()
            };
        }

        [VmProjectTool("spine/sample-skeleton-animation",
            Description = "Sample a Spine 4.2 animation at explicit times on an independent skeleton. Return local bounds, bone poses and selected slot attachments without changing scene objects or authoring data.",
            ModuleId = "spine", ReadOnly = true,
            SideEffects = VmProjectToolSideEffect.ReadsProjectState,
            CompletionEvidence = "One independently reset Spine pose per requested time, with complete bone and slot observations.",
            ErrorCodes = new[] { "spine_asset_not_found", "spine_data_invalid", "spine_version_mismatch", "spine_skin_not_found", "spine_animation_not_found", "spine_sample_time_invalid", "spine_budget_exceeded" })]
        public static SkeletonPoseResult Sample(SkeletonPoseRequest request)
        {
            return SpinePoseSampler.Sample(Read(Load(request.assetPath)), request);
        }

        private static SkeletonDataAsset Load(string path)
        {
            SkeletonDataAsset asset = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(path);
            if (asset == null)
                throw new VmProjectToolException("spine_asset_not_found", $"No SkeletonDataAsset exists at '{path}'.");
            return asset;
        }

        private static SkeletonData Read(SkeletonDataAsset asset)
        {
            SkeletonData data = asset.GetSkeletonData(false);
            if (data == null)
                throw new VmProjectToolException("spine_data_invalid", $"Spine could not parse '{AssetDatabase.GetAssetPath(asset)}'.");
            if (data.Version == null || !data.Version.StartsWith("4.2.", StringComparison.Ordinal))
                throw new VmProjectToolException("spine_version_mismatch", $"Expected a Spine 4.2 export; received '{data.Version}'.");
            return data;
        }

        private static AtlasRecord ReadAtlas(AtlasAssetBase atlas)
        {
            if (atlas == null)
                throw new VmProjectToolException("spine_resource_missing", "SkeletonDataAsset contains a missing atlas reference.");
            return new AtlasRecord
            {
                assetPath = AssetDatabase.GetAssetPath(atlas),
                materials = atlas.Materials.Select(m =>
                {
                    if (m == null || m.mainTexture == null || m.shader == null)
                        throw new VmProjectToolException("spine_resource_missing", $"Atlas '{atlas.name}' has an unresolved material, texture or shader.");
                    return new MaterialRecord
                    {
                        assetPath = AssetDatabase.GetAssetPath(m),
                        texturePath = AssetDatabase.GetAssetPath(m.mainTexture),
                        shaderName = m.shader.name
                    };
                }).ToArray()
            };
        }

        internal static AttachmentRecord ReadAttachment(SkeletonData data, Skin.SkinEntry entry)
        {
            var result = new AttachmentRecord
            {
                slotIndex = entry.SlotIndex, slotName = data.Slots.Items[entry.SlotIndex].Name,
                name = entry.Name, type = entry.Attachment.GetType().Name
            };
            if (entry.Attachment is RegionAttachment region)
            {
                result.x = region.X; result.y = region.Y;
                result.width = region.Width; result.height = region.Height;
                result.rotation = region.Rotation;
                result.scaleX = region.ScaleX; result.scaleY = region.ScaleY;
                result.vertexCount = 4; result.triangleCount = 2;
            }
            else if (entry.Attachment is MeshAttachment mesh)
            {
                result.width = mesh.Width; result.height = mesh.Height;
                result.weighted = mesh.Bones != null;
                result.linkedMesh = mesh.ParentMesh != null;
                result.vertexCount = mesh.WorldVerticesLength / 2;
                result.triangleCount = mesh.Triangles.Length / 3;
            }
            return result;
        }

        private static ClipRecord ReadClip(SkeletonData data, AnimationClip clip)
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(clip, out string _, out long id);
            return new ClipRecord
            {
                name = clip.name, localFileId = id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                duration = clip.length, hasSpineAnimation = data.FindAnimation(clip.name) != null
            };
        }
    }
}
