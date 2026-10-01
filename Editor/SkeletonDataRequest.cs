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
}
