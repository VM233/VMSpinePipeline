using VMUnityAutomation.Editor;

namespace VMSpinePipeline.Editor
{
    public sealed class SkeletonDataResult
    {
        [VmRequired] public string assetPath;
        [VmRequired] public string guid;
        [VmRequired] public string sourcePath;
        [VmRequired] public string spineVersion;
        [VmRequired] public float scale;
        [VmRequired] public int recordCount;
        [VmRequired] public BoneRecord[] bones;
        [VmRequired] public SlotRecord[] slots;
        [VmRequired] public SkinRecord[] skins;
        [VmRequired] public AnimationRecord[] animations;
        [VmRequired] public AtlasRecord[] atlases;
        [VmRequired] public string controllerPath;
        [VmRequired] public ClipRecord[] controllerClips;
    }

    public sealed class BoneRecord
    {
        [VmRequired] public string name;
        [VmRequired] public string parent;
        [VmRequired] public float x;
        [VmRequired] public float y;
        [VmRequired] public float rotation;
        [VmRequired] public float scaleX;
        [VmRequired] public float scaleY;
    }

    public sealed class SlotRecord
    {
        [VmRequired] public int index;
        [VmRequired] public string name;
        [VmRequired] public string boneName;
        [VmRequired] public string setupAttachmentName;
    }

    public sealed class SkinRecord
    {
        [VmRequired] public string name;
        [VmRequired] public AttachmentRecord[] attachments;
    }

    public sealed class AttachmentRecord
    {
        [VmRequired] public int slotIndex;
        [VmRequired] public string slotName;
        [VmRequired] public string name;
        [VmRequired] public string type;
        [VmRequired] public float x;
        [VmRequired] public float y;
        [VmRequired] public float width;
        [VmRequired] public float height;
    }

    public sealed class AnimationRecord
    {
        [VmRequired] public string name;
        [VmRequired] public float duration;
        [VmRequired] public int timelineCount;
    }

    public sealed class AtlasRecord
    {
        [VmRequired] public string assetPath;
        [VmRequired] public MaterialRecord[] materials;
    }

    public sealed class MaterialRecord
    {
        [VmRequired] public string assetPath;
        [VmRequired] public string texturePath;
        [VmRequired] public string shaderName;
    }

    public sealed class ClipRecord
    {
        [VmRequired] public string name;
        [VmRequired] public string localFileId;
        [VmRequired] public float duration;
        [VmRequired] public bool hasSpineAnimation;
    }

    public sealed class SkeletonPoseResult
    {
        [VmRequired] public string assetPath;
        [VmRequired] public string skinName;
        [VmRequired] public string animationName;
        [VmRequired] public float animationDuration;
        [VmRequired] public int recordCount;
        [VmRequired] public PoseRecord[] poses;
    }

    public sealed class PoseRecord
    {
        [VmRequired] public float time;
        [VmRequired] public bool hasGeometry;
        [VmRequired] public BoundsRecord bounds;
        [VmRequired] public BonePoseRecord[] bones;
        [VmRequired] public SlotPoseRecord[] slots;
    }

    public sealed class BoundsRecord
    {
        [VmRequired] public float x;
        [VmRequired] public float y;
        [VmRequired] public float width;
        [VmRequired] public float height;
    }

    public sealed class BonePoseRecord
    {
        [VmRequired] public string name;
        [VmRequired] public float worldX;
        [VmRequired] public float worldY;
        [VmRequired] public float rotation;
    }

    public sealed class SlotPoseRecord
    {
        [VmRequired] public string name;
        [VmRequired] public string boneName;
        [VmRequired] public string attachmentName;
        [VmRequired] public bool hasAttachment;
    }
}
