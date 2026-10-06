using System.ComponentModel;
using VMUnityAutomation.Editor;

namespace VMSpinePipeline.Editor
{
    public sealed class MeshSpriteCoverageRequest : SkeletonDataRequest
    {
        [VmRequired, VmMinLength(1)] public string skinName;
        [VmRequired, VmMinLength(1)] public string slotName;
        [VmRequired, VmMinLength(1)] public string attachmentName;
        [VmRequired, VmMinLength(1)] public string spriteAssetPath;

        [Description("Optional exact animation name. Empty samples setup pose at time zero.")]
        public string animationName = "";

        [Description("One to sixteen explicit sample times in seconds; defaults to setup time zero.")]
        public float[] times = { 0 };
    }
}
