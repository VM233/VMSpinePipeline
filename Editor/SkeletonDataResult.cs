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
}
