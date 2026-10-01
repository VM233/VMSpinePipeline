using System.ComponentModel;
using VMUnityAutomation.Editor;

namespace VMSpinePipeline.Editor
{
    public sealed class MecanimPoseRequest
    {
        [VmRequired, VmMinLength(1)]
        [Description("Exact SkeletonMecanim component object ID as returned by Automation's VmObjectId owner.")]
        public string instanceId;

        [VmRequired, VmMinLength(1)]
        [Description("Exact state name in the controller's single flat layer.")]
        public string stateName;

        [VmRange(0, 1)]
        [Description("Normalized state time within 0..1. Defaults to 0.")]
        public float normalizedTime;

        [VmRange(1, 8192)]
        [Description("Combined metadata, controller, timeline and output record budget. Defaults to 2048.")]
        public int maxRecords = 2048;
    }
}
