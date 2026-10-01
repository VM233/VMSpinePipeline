# Integration

Install Spine-Unity 4.2 with assembly definitions enabled. Resolve this package
and the project's pinned VM Unity Automation dependency, refresh the project,
and verify compilation before bounded CLI discovery.

The package registers project tools in the `spine` module. It adds no server,
transport, port or top-level CLI commands. Input and output schemas are generated
by Automation from the same typed request/result classes used by the handlers.
Use the catalog as the authoritative list and schema reference.

Skeleton inspection reads one `SkeletonDataAsset`, its parsed Spine data, explicit
atlas/material references and the clips stored in its assigned Animator controller.
It does not reimport data, regenerate clips, repair references or create resources.
An empty controller path is reported when the authoring asset has no controller.

Pose sampling constructs a separate Spine skeleton and applies the requested
animation independently at each explicit time. Samples are in skeleton-local
world units after the SkeletonDataAsset import scale; they exclude the scene
Transform and renderer, and do not simulate physics. Bounds use Spine's unclipped
attachment geometry. Loaded scene instances, equipment remapping, clipping,
rendered pixels and gameplay require their own production validation.
When every slot has no renderable attachment, `hasGeometry` is false and the
reported bounds are zero. Bone poses still describe the sampled skeleton.
Sampled bone rotation is the world X-axis rotation in this same coordinate space.

The sample skin, animation and time range must exist in the selected data.
`maxRecords` is an explicit report budget: the operation fails rather than
silently truncating. Pose requests accept at most 16 times. Geometry and selected
timeline frame storage are limited to 65,536 elements each before sampling.

Both tools are read-only. Errors are returned through Automation's owner envelope;
transport success alone does not prove a successful inspection or sample.
Existing assets remain authoritative. Replacement and importer edits use the
generic Automation asset tools, with Spine's own importer owning the parsed data
and generated Mecanim clips.

Enable the package's test assembly through the project's UPM `testables` list when
running the focused `VMSpinePipeline.Editor.Tests` fixtures.
