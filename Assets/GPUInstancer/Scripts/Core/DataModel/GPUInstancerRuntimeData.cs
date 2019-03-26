using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace GPUInstancer
{
    public class GPUInstancerRuntimeData
    {
        public GPUInstancerPrototype prototype;

        // Mesh - Material - LOD info
        public List<GPUInstancerPrototypeLOD> instanceLODs;
        public Bounds instanceBounds;
        public Vector4 lodSizes;

        // Instance Data
        [HideInInspector]
        public Matrix4x4[] instanceDataArray;
        // Currently instanced count
        public int instanceCount;
        // Buffer size
        public int bufferSize;

        // Buffers Data
        public ComputeBuffer transformationMatrixVisibilityBuffer;
        public ComputeBuffer argsBuffer; // for multiple material (submesh) rendering
        public uint[] args;

        // Runtime data modifications
        public bool modified;

        public Material shadowCasterMaterial;
        public MaterialPropertyBlock shadowCasterMPB;
        public ComputeBuffer shadowAppendBuffer;
        public ComputeBuffer shadowArgsBuffer;

        public GPUInstancerRuntimeData(GPUInstancerPrototype prototype)
        {
            this.prototype = prototype;
            modified = true;
        }

        /// <summary>
        /// Registers an LOD to the prototype. LODs contain the renderers for instance prototypes,
        /// so even if no LOD is being used, the prototype must be registered as LOD0 using this method.
        /// </summary>
        /// <param name="screenRelativeTransitionHeight">if not defined, will default to 0</param>
        public void AddLod(float screenRelativeTransitionHeight = -1)
        {
            if (instanceLODs == null)
            {
                instanceLODs = new List<GPUInstancerPrototypeLOD>();
                lodSizes = new Vector4(-1, -1, -1, -1);
            }

            instanceLODs.Add(new GPUInstancerPrototypeLOD());

            // Ensure the LOD will render if this is the first LOD and lodDistance is not set.
            if (instanceLODs.Count == 1 && screenRelativeTransitionHeight < 0f)
                lodSizes.x = 0;

            // Do not modify the lodDistances vector if LOD distance is not supplied.
            if (screenRelativeTransitionHeight < 0f)
                return;

            switch (instanceLODs.Count)
            {
                case 1: //LOD 0
                    lodSizes.x = screenRelativeTransitionHeight;
                    break;
                case 2: //LOD 1
                    lodSizes.y = screenRelativeTransitionHeight;
                    break;
                case 3: //LOD 2
                    lodSizes.z = screenRelativeTransitionHeight;
                    break;
                case 4: //LOD 3
                    lodSizes.w = screenRelativeTransitionHeight;
                    break;
            }
        }

        /// <summary>
        /// Adds a new LOD and creates a single renderer for it.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="materials"></param>
        /// <param name="lodSize"></param>
        public void AddLod(Mesh mesh, List<Material> materials, MaterialPropertyBlock mpb, float lodSize = -1)
        {
            AddLod(lodSize);
            AddRenderer(instanceLODs.Count - 1, mesh, materials, Matrix4x4.identity, mpb);
        }

        /// <summary>
        /// Adds a renderer to an LOD. Renderers define the meshes and materials to render for a given instance prototype LOD.
        /// </summary>
        /// <param name="lod">The LOD to add this renderer to. LOD indices start from 0.</param>
        /// <param name="mesh">The mesh that this renderer will use.</param>
        /// <param name="materials">The list of materials that this renderer will use (must be GPU Instancer compatible materials)</param>
        /// <param name="transformOffset">The transformation matrix that represents a change in position, rotation and scale 
        /// for this renderer as an offset from the instance prototype. This matrix will be applied to the prototype instance 
        /// matrix for final rendering calculations in the shader. Use Matrix4x4.Identity if no offset is desired.</param>
        public void AddRenderer(int lod, Mesh mesh, List<Material> materials, Matrix4x4 transformOffset, MaterialPropertyBlock mpb)
        {

            if (instanceLODs == null || instanceLODs.Count <= lod || instanceLODs[lod] == null)
            {
                Debug.LogError("Can't add renderer: Invalid LOD");
                return;
            }

            if (mesh == null)
            {
                Debug.LogError("Can't add renderer: mesh is null");
                return;
            }

            if (materials == null || materials.Count == 0)
            {
                Debug.LogError("Can't add renderer: no materials");
                return;
            }

            if (instanceLODs[lod].renderers == null)
                instanceLODs[lod].renderers = new List<GPUInstancerRenderer>();

            GPUInstancerRenderer renderer = new GPUInstancerRenderer
            {
                mesh = mesh,
                materials = materials,
                transformOffset = transformOffset,
                mpb = mpb
            };

            instanceLODs[lod].renderers.Add(renderer);
            CalculateBounds();
        }

        /// <summary>
        /// Generates all LOD and render data from the supplied Unity LODGroup. Deletes all existing LOD data.
        /// </summary>
        /// <param name="lodGroup">Unity LODGroup</param>
        /// <param name="settings">GPU Instancer settings to find appropriate shader for materials</param> 
        private void GenerateLODsFromLODGroup(LODGroup lodGroup, GPUInstancerShaderBindings shaderBindings)
        {
            if (instanceLODs == null)
                instanceLODs = new List<GPUInstancerPrototypeLOD>();
            else
                instanceLODs.Clear();

            for (int lod = 0; lod < lodGroup.GetLODs().Length; lod++)
            {
                List<Renderer> lodRenderers = lodGroup.GetLODs()[lod].renderers.Where(r => r.GetComponent<MeshFilter>() != null).ToList();

                if (!lodRenderers.Any())
                    continue;

                AddLod(lodGroup.GetLODs()[lod].screenRelativeTransitionHeight);

                for (int r = 0; r < lodRenderers.Count; r++)
                {
                    List<Material> instanceMaterials = new List<Material>();
                    for (int m = 0; m < lodRenderers[r].sharedMaterials.Length; m++)
                    {
                        instanceMaterials.Add(shaderBindings.GetInstancedMaterial(lodRenderers[r].sharedMaterials[m]));
                    }

                    Matrix4x4 transformOffset = Matrix4x4.identity;
                    Transform currentTransform = lodRenderers[r].gameObject.transform;
                    while (currentTransform != lodGroup.gameObject.transform)
                    {
                        transformOffset = Matrix4x4.TRS(currentTransform.localPosition, currentTransform.localRotation, currentTransform.localScale) * transformOffset;
                        currentTransform = currentTransform.parent;
                    }

                    MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                    lodRenderers[r].GetPropertyBlock(mpb);
                    AddRenderer(lod, lodRenderers[r].GetComponent<MeshFilter>().sharedMesh, instanceMaterials, transformOffset, mpb);
                }
            }
        }

        /// <summary>
        /// Generates instancing renderer data for a given GameObject, at the first LOD level.
        /// </summary>
        /// <param name="gameObject">GameObject</param>
        /// <param name="settings">GPU Instancer settings to find appropriate shader for materials</param> 
        /// <param name="includeChildren">if true, renderers for all found children of this gameObject will be created as well</param>
        public void CreateRenderersFromGameObject(GameObject gameObject, GPUInstancerShaderBindings shaderBindings, bool includeChildren = true)
        {
            if (gameObject.GetComponent<LODGroup>() != null)
                GenerateLODsFromLODGroup(gameObject.GetComponent<LODGroup>(), shaderBindings);
            else
            {
                if (instanceLODs == null || instanceLODs.Count == 0)
                    AddLod();
                CreateRenderersFromGameObject(0, gameObject, shaderBindings, true);
            }

        }

        /// <summary>
        /// Generates instancing renderer data for a given GameObject, at the given LOD level.
        /// </summary>
        /// <param name="lod">Which LOD level to generate renderers in</param>
        /// <param name="gameObject">GameObject</param>
        /// <param name="settings">GPU Instancer settings to find appropriate shader for materials</param> 
        /// <param name="includeChildren">if true, renderers for all found children of this gameObject will be created as well</param>
        private void CreateRenderersFromGameObject(int lod, GameObject gameObject, GPUInstancerShaderBindings shaderBindings, bool includeChildren)
        {
            if (instanceLODs == null || instanceLODs.Count <= lod || instanceLODs[lod] == null)
            {
                Debug.LogError("Can't create renderer(s): Invalid LOD");
                return;
            }

            if (!gameObject)
            {
                Debug.LogError("Can't create renderer(s): reference GameObject is null");
                return;
            }

            MeshRenderer[] meshRenderers = gameObject.GetComponentsInChildren<MeshRenderer>();

            if (meshRenderers == null || meshRenderers.Length == 0)
            {
                Debug.LogError("Can't create renderer(s): no MeshRenderers found in the reference GameObject or any of its children");
                return;
            }


            if (!includeChildren)
            {
                List<Material> instanceMaterials = new List<Material>();
                for (int m = 0; m < gameObject.GetComponent<MeshRenderer>().sharedMaterials.Length; m++)
                {
                    instanceMaterials.Add(shaderBindings.GetInstancedMaterial(gameObject.GetComponent<MeshRenderer>().sharedMaterials[m]));
                }

                MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                gameObject.GetComponent<MeshRenderer>().GetPropertyBlock(mpb);
                AddRenderer(lod, gameObject.GetComponent<MeshFilter>().sharedMesh, instanceMaterials, Matrix4x4.identity, mpb);
            }
            else
            {
                foreach (MeshRenderer meshRenderer in meshRenderers)
                {
                    if (meshRenderer.GetComponent<MeshFilter>() == null)
                    {
                        Debug.LogWarning("MeshRenderer with no MeshFilter found on GameObject <" + gameObject.name +
                            "> (Child: <" + meshRenderer.gameObject + ">). Are you missing a component?");
                        continue;
                    }

                    List<Material> instanceMaterials = new List<Material>();

                    for (int m = 0; m < meshRenderer.sharedMaterials.Length; m++)
                    {
                        instanceMaterials.Add(shaderBindings.GetInstancedMaterial(meshRenderer.sharedMaterials[m]));
                    }

                    Matrix4x4 transformOffset = Matrix4x4.identity;
                    Transform currentTransform = meshRenderer.gameObject.transform;
                    while (currentTransform != gameObject.transform)
                    {
                        transformOffset = Matrix4x4.TRS(currentTransform.localPosition, currentTransform.localRotation, currentTransform.localScale) * transformOffset;
                        currentTransform = currentTransform.parent;
                    }

                    MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                    meshRenderer.GetPropertyBlock(mpb);
                    AddRenderer(lod, meshRenderer.GetComponent<MeshFilter>().sharedMesh, instanceMaterials, transformOffset, mpb);
                }
            }
        }

        public void CalculateBounds()
        {
            if (instanceLODs == null || instanceLODs.Count == 0 || instanceLODs[0].renderers == null ||
                instanceLODs[0].renderers.Count == 0)
                return;

            instanceBounds = instanceLODs[0].renderers[0].mesh.bounds;

            for (int lod = 0; lod < instanceLODs.Count; lod++)
            {
                for (int r = 0; r < instanceLODs[lod].renderers.Count; r++)
                {
                    if (lod == 0 && r == 0)
                        continue;
                    instanceBounds.Encapsulate(new Bounds(instanceLODs[lod].renderers[r].mesh.bounds.center + (Vector3)instanceLODs[lod].renderers[r].transformOffset.GetColumn(3),
                        new Vector3(
                        instanceLODs[lod].renderers[r].mesh.bounds.size.x * instanceLODs[lod].renderers[r].transformOffset.GetRow(0).magnitude,
                        instanceLODs[lod].renderers[r].mesh.bounds.size.y * instanceLODs[lod].renderers[r].transformOffset.GetRow(1).magnitude,
                        instanceLODs[lod].renderers[r].mesh.bounds.size.z * instanceLODs[lod].renderers[r].transformOffset.GetRow(2).magnitude)));
                }
            }
        }
    }

    public class GPUInstancerPrototypeLOD
    {
        // Prototype Data
        public List<GPUInstancerRenderer> renderers; // support for multiple mesh renderers

        // Buffers Data
        public ComputeBuffer transformationMatrixAppendBuffer;
        public int argsBufferOffset { get { return renderers == null ? -1 : renderers[0].argsBufferOffset; } }
    }

    public class GPUInstancerRenderer
    {
        public Mesh mesh;
        public List<Material> materials; // support for multiple submeshes.
        public Matrix4x4 transformOffset;
        public int argsBufferOffset;
        public MaterialPropertyBlock mpb;
    }
}
