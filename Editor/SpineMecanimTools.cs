using System;
using System.Linq;
using Spine;
using Spine.Unity;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VMUnityAutomation.Editor;

namespace VMSpinePipeline.Editor
{
    public static class SpineMecanimTools
    {
        [VmProjectTool("spine/sample-mecanim-state",
            Description = "Apply one paused, live SkeletonMecanim state through its real Animator and Spine owners. Supports a single flat AnimatorController layer with direct clips. Clears pending triggers, leaves the sampled pose for capture and reports rendered bounds plus attachment/material identities. Does not save assets or scenes.",
            ModuleId = "spine", MutatesRuntime = true, RequiresPlayMode = true,
            SideEffects = VmProjectToolSideEffect.ChangesRuntimeState,
            Preconditions = new[] { "pausedPlayMode", "singleFlatAnimatorLayer" },
            CompletionEvidence = "Requested Animator state confirmed, Spine update callbacks applied, mesh generated and all current slot identities reported.",
            ErrorCodes = new[] { "spine_paused_play_mode_required", "spine_component_invalid", "spine_version_mismatch", "spine_controller_unsupported", "spine_state_not_found", "spine_animation_not_found", "spine_sample_time_invalid", "spine_budget_exceeded", "spine_state_mismatch" })]
        public static MecanimPoseResult Sample(MecanimPoseRequest request)
        {
            if (!EditorApplication.isPlaying || !EditorApplication.isPaused)
                throw new VmProjectToolException("spine_paused_play_mode_required", "Live Mecanim pose sampling requires paused Play Mode.");
            var component = VmObjectId.ToObject(request.instanceId) as SkeletonMecanim;
            if (component == null || !component.isActiveAndEnabled || !component.valid ||
                component.UpdateMode != UpdateMode.FullUpdate)
                throw new VmProjectToolException("spine_component_invalid", $"Object '{request.instanceId}' must be an active, valid, fully updating SkeletonMecanim.");
            SkeletonData data = component.Skeleton.Data;
            if (data.Version == null || !data.Version.StartsWith("4.2.", StringComparison.Ordinal))
                throw new VmProjectToolException("spine_version_mismatch", $"Expected Spine 4.2 data; received '{data.Version}'.");
            int records = SpinePoseSampler.CountRecords(data);
            SpinePoseSampler.RequireBudget(records, request.maxRecords);
            var animator = component.GetComponent<Animator>();
            var controller = animator?.runtimeAnimatorController as AnimatorController;
            if (animator == null || !animator.isActiveAndEnabled || controller == null || controller.layers.Length != 1)
                throw new VmProjectToolException("spine_controller_unsupported", "Sampling requires an active Animator with one AnimatorController layer; override controllers and multiple layers are outside this tool's domain.");
            AnimatorControllerLayer layer = controller.layers[0];
            ChildAnimatorState[] states = layer.stateMachine.states;
            AnimatorControllerParameter[] parameters = controller.parameters;
            records += states.Length + parameters.Length + data.Slots.Count + 1;
            SpinePoseSampler.RequireBudget(records, request.maxRecords);
            if (layer.stateMachine.stateMachines.Length != 0 || states.Any(s => s.state.motion is not AnimationClip))
                throw new VmProjectToolException("spine_controller_unsupported", "The layer must be flat and every state must have a direct AnimationClip motion.");
            AnimatorState state = states.Select(s => s.state).SingleOrDefault(s => s.name == request.stateName);
            if (state == null)
                throw new VmProjectToolException("spine_state_not_found", $"State '{request.stateName}' does not exist in layer '{layer.name}'.");
            var clip = (AnimationClip)state.motion;
            Spine.Animation animation = data.FindAnimation(clip.name);
            if (animation == null)
                throw new VmProjectToolException("spine_animation_not_found", $"State '{state.name}' uses clip '{clip.name}', which has no Spine animation.");
            foreach (Spine.Animation entry in data.Animations)
                records += entry.Timelines.Count;
            SpinePoseSampler.RequireBudget(records, request.maxRecords);
            SpinePoseSampler.RequireGeometryBudget(data, data.Animations);
            if (float.IsNaN(request.normalizedTime) || float.IsInfinity(request.normalizedTime) ||
                request.normalizedTime < 0 || request.normalizedTime > 1)
                throw new VmProjectToolException("spine_sample_time_invalid", "normalizedTime must be finite and within 0..1.");
            int stateHash = Animator.StringToHash(layer.name + "." + state.name);
            foreach (var parameter in parameters)
                if (parameter.type == AnimatorControllerParameterType.Trigger)
                    animator.ResetTrigger(parameter.nameHash);
            component.Skeleton.SetToSetupPose();
            animator.Play(stateHash, 0, request.normalizedTime);
            animator.Update(0);
            component.Update(0);
            component.LateUpdateMesh();
            AnimatorStateInfo actual = animator.GetCurrentAnimatorStateInfo(0);
            if (actual.fullPathHash != stateHash)
                throw new VmProjectToolException("spine_state_mismatch", $"Animator did not retain requested state '{state.name}'; actual state hash is {actual.fullPathHash}.");
            Bounds bounds = component.GetComponent<MeshRenderer>().bounds;
            return new MecanimPoseResult
            {
                instanceId = VmObjectId.Get(component), stateName = state.name, animationName = animation.Name,
                normalizedTime = actual.normalizedTime, animationDuration = animation.Duration,
                recordCount = records, meshVertexCount = component.GetComponent<MeshFilter>().sharedMesh.vertexCount,
                worldBounds = new BoundsRecord { x = bounds.min.x, y = bounds.min.y, width = bounds.size.x, height = bounds.size.y },
                slots = component.Skeleton.Slots.Select(ReadSlot).ToArray()
            };
        }

        private static MecanimSlotRecord ReadSlot(Slot slot)
        {
            var attachment = slot.Attachment as RegionAttachment;
            var region = attachment?.Region as AtlasRegion;
            var material = region?.page?.rendererObject as Material;
            var texture = material?.mainTexture;
            return new MecanimSlotRecord
            {
                name = slot.Data.Name, boneName = slot.Bone.Data.Name,
                hasAttachment = slot.Attachment != null, attachmentName = slot.Attachment?.Name ?? "",
                attachmentType = slot.Attachment?.GetType().Name ?? "",
                x = attachment?.X ?? 0, y = attachment?.Y ?? 0, rotation = attachment?.Rotation ?? 0,
                scaleX = attachment?.ScaleX ?? 0, scaleY = attachment?.ScaleY ?? 0,
                width = attachment?.Width ?? 0, height = attachment?.Height ?? 0,
                textureName = texture?.name ?? "", textureInstanceId = texture == null ? "" : VmObjectId.Get(texture),
                materialInstanceId = material == null ? "" : VmObjectId.Get(material)
            };
        }
    }
}
