GPU Instancer v0.8
Copyright ©2018 GurBu Technologies
---------------------------------
Thank you for supporting GPU Instancer!

---------------------------------
DOCUMENTATION
---------------------------------
Please read our online documentation for more in-depth explanations and customization options at:
http://gurbu.com/

---------------------------------
SETUP
---------------------------------
1. Add one or both Managers to your scene

1.1. Add Prefab Manager
GPU Instancer -> Add Prefab Manager

1.2. Add Detail Managers to your scene (Requires at least one Unity terrain present in the scene)
GPU Instancer -> Add Detail Manager For Terrains

2. In the Inspector window, press the "?" button at the top-right corner to get detailed information about setting up the manager.

---------------------------------
SUPPORT
---------------------------------
If you have any questions, requests or bug reports, please email us at: support@gurbu.com
Unity Forum Thread: https://forum.unity.com/threads/gpu-instancer.529746/

---------------------------------
DESCRIPTION
---------------------------------
GPU Instancer is an out of the box solution to display extreme numbers of objects on screen with high performance. 
With a few mouse clicks, you can instance your prefabs and Unity terrain details.

GPU Instancer provides user friendly tools to allow everyone to use Indirect GPU Instancing without having to go through 
the deep learning curve of Compute Shaders and GPU infrastructure. Also, an API with extensive documentation is provided 
to manage runtime changes.

To provide the fastest possible performance, GPU Instancer utilizes Indirect GPU Instancing using Unity's 
DrawMeshInstancedIndirect method and Compute Shaders.

GPU Instancing results in magnitudes of performance improvement over static batching and mesh combining. Also, other available 
solutions for GPU Instancing (including Unity's material option and the DrawMeshInstanced method) fail short on limited buffer 
sizes and therefore result in more draw calls and less performance. By using the indirect method GPU Instancer aims to provide 
the ultimate solution for this, and increases performance considerably while rendering the same mesh multiple times.

GPU Instancer consists of two main Monobehavior classes named "GPU Instancer Prefab Manager" and "GPU Insancer Detail Manager".

GPU Instancer Prefab Manager
---------------------------------
By adding your prefabs to the Prefab Manager, the prefab instances you add to your scenes are automatically rendered by GPU Instancer.
It also provides additional functionality such as adding/removing instances at runtime, changing material properties per instance, 
and enabling/disabling GPU Instancing and Rigidbodies on instanced objects (at a specified area) at runtime.

Note that prefab manager only accepts user created prefabs. It will not accept prefabs that are generated when importing your 3D model assets.

GPU Instancer Detail Manager
---------------------------------
Detail Manager takes over rendering of detail prototypes added to your Unity terrain.
It comes with a grass shader which gives you the ability to customize how your grass will look on your terrain with a set of shader 
properties that can be edited from the manager.

---------------------------------
FEATURES
---------------------------------
- Out of the box solution for complex GPU Instancing.
- Easy to use interface.
- Tens of thousands of objects rendered lightning fast in a single draw call.
- GPU frustum culling.
- Automatically configured custom shader support
- Complex hierarchies of prefabs instanced with a single click.
- Multiple sub-meshes support.
- LOD Group support.
- Shadows casting and receiving support for instances (frustum culled instances still can cast shadows).
- Unity 5.6 support.
- Well documented API for procedural scenes and runtime modifications (Examples included).
- Example scenes that showcase GPU Instancer capabilities.

Prefab Instancing Features:
- Ability to automatically instance prefabs at your scene that you distribute with your favorite prefab painting tool.
- Area localized rigidbody and physics support. 
- Instance based material variations through material property blocks.
- Enabling and disabling instancing at runtime per instance basis.
- API to manage instanced prefabs at runtime.

Detail Instancing Features:
- Dense grass fields and vegetation with very high frame rates.
- Included vegetation shader with wind, shadows, AO, billboarding and various other properties.
- Support for custom shaders and materials.
- Cross quadding support: automatically turns grass textures to crossed quads.
- Ability to paint prefabs with custom materials on Unity terrain (with Unity terrain tools).
- Ability to use prefabs with LOD Groups on Unity terrain.
- Further performance improvements with automatic spatial partitioning.
- API to manage instanced terrain detail prototypes at runtime (Examples included).
- Editor GPU Instancing simulation 

Planned Features:
- GPU Occlusion culling (hi-z occlusion culling)
- Support for animation baking and skinned mesh renderers.


---------------------------------
REQUIREMENTS
---------------------------------
- Graphics hardware with Shader Model 5.0


---------------------------------
DEMO SCENES
---------------------------------
You can find demo scenes that showcase GPU Instancer capabilities in the "GPUInstancer/Demos" folder. 
These scenes are only for demonstration and you can safely remove this folder from your builds.
Some demos also have a "PostProcessing" folder. For best visual results, you can add a "Post-Processing Behavior" 
script to the scene cameras and add the setting object to this script from this folder.

