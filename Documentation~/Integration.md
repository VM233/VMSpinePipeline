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
Region records include authored rotation and scale. Region and Mesh records
include vertex/triangle counts; Mesh records report weighted and linked state.
Mesh width and height are optional authoring dimensions loaded in skeleton units,
not deformed bounds. Transform fields (x/y/rotation/scale) apply to Region
attachments and remain zero for Mesh attachments, which use vertex and bone data.
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

The two skeleton-data tools are read-only. Errors are returned through Automation's owner envelope;
transport success alone does not prove a successful inspection or sample.
Existing assets remain authoritative. Replacement and importer edits use the
generic Automation asset tools, with Spine's own importer owning the parsed data
and generated Mecanim clips.

`spine/sample-mecanim-state` samples an existing active SkeletonMecanim in paused
Play Mode. Supply its canonical Automation object ID, an exact Animator state
name and a normalized time between zero and one. The supported controller has
one flat layer and direct AnimationClip motions; override controllers, blend
trees and nested state machines are rejected before changing the pose.

This runtime tool clears pending Animator triggers, selects the requested state,
then invokes the real Animator and Spine update and mesh owners. Equipment
UpdateLocal callbacks run normally. The requested pose remains paused for a
Game-view capture. The result reports the confirmed state, rendered world bounds,
mesh vertex count, current slot offsets and canonical texture/material identities.
Both Region and Mesh material identities come from Spine's native texture-region
owner. Mesh topology flags distinguish weighted source geometry and linked runtime
equipment clones. The tool does not flatten or copy the mesh's authored weights.
It does not save a scene or asset. Resume ordinary gameplay, sample idle, or exit
Play Mode after the inspection. All skeleton metadata, controller records and
timeline counts share the explicit maxRecords budget; geometry and timeline frame
storage are limited to 65,536 elements each.

Enable the package's test assembly through the project's UPM `testables` list when
running the focused `VMSpinePipeline.Editor.Tests` fixtures.
Linked Mesh inspection tests use a texture-capable native Sprite shader and a
valid atlas region; the material's actual texture binding is asserted first.

## Mesh equipment

`Runtime/SpineMeshRectangle.Expand` prepares bone-driven meshes for rectangular
equipment Sprites. It preserves authored vertex records and triangles, adding
four outer vertices and exterior triangles. Pass an independent setup-pose Slot;
cache the result with the skeleton binding and remap Sprites through Spine's
native attachment tools. Vertex-deform timelines and sequences are explicitly
unsupported. The source skeleton and its assets are not modified.

`spine/inspect-mesh-sprite-coverage` compares native Sprite alpha coverage before
and after expansion and samples the original vertices under explicit animation
times. It reports the expanded UV topology and exact original-vertex errors.
These readers are read-only; the live Mecanim sampler changes only the paused
runtime pose, with an explicit component ID and state name.
See the [geometry contract and cost ledger](MeshRectangleCostLedger.md).
