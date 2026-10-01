using System.ComponentModel;
using VMUnityAutomation.Editor;

namespace VMSpinePipeline.Editor
{
    public class SkeletonDataRequest
    {
        [VmRequired, VmMinLength(1)]
        [Description("Exact project path of the Spine SkeletonDataAsset.")]
        public string assetPath;

        [VmRange(1, 8192)]
        [Description("Maximum metadata or sample records. Defaults to 2048; exceeding this budget is an error.")]
        public int maxRecords = 2048;
    }

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
