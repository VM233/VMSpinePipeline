using System.ComponentModel;
using VMUnityAutomation.Editor;

namespace VMSpinePipeline.Editor
{
    public sealed class SkeletonPoseRequest : SkeletonDataRequest
    {
        [VmRequired, VmMinLength(1)]
        [Description("Exact skin name from skeleton-data inspection.")]
        public string skinName;

        [VmRequired, VmMinLength(1)]
        [Description("Exact Spine animation name, not an Animator state name.")]
        public string animationName;

        [VmRequired, VmMinItems(1)]
        [Description("At most 16 explicit times in seconds, each within the animation duration.")]
        public float[] times;
    }
}
