using System;
using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancer
{
    public class GPUInstancerShaderBindings : ScriptableObject
    {
        public List<ShaderInstance> shaderInstances;

        public Shader GetInstancedShader(string shaderName)
        {
            if (shaderName.Equals(GPUInstancerConstants.SHADER_UNITY_STANDARD))
                return Shader.Find(GPUInstancerConstants.SHADER_GPUI_STANDARD);
            else if (shaderName.Equals(GPUInstancerConstants.SHADER_UNITY_STANDARD_SPECULAR))
                return Shader.Find(GPUInstancerConstants.SHADER_GPUI_STANDARD_SPECULAR);
            else if (shaderName.Equals(GPUInstancerConstants.SHADER_UNITY_VERTEXLIT))
                return Shader.Find(GPUInstancerConstants.SHADER_GPUI_VERTEXLIT);
            else if(shaderName.Equals(GPUInstancerConstants.SHADER_GPUI_FOLIAGE))
                return Shader.Find(GPUInstancerConstants.SHADER_GPUI_FOLIAGE);
            

            foreach (ShaderInstance si in shaderInstances)
            {
                if (si.name.Equals(shaderName))
                    return si.instancedShader;
            }
            if (!shaderName.Equals(GPUInstancerConstants.SHADER_UNITY_STANDARD))
            {
                if (Application.isPlaying)
                    Debug.LogWarning("Can not find instanced shader for : " + shaderName + ". Using Standard shader instead.");
                return GetInstancedShader(GPUInstancerConstants.SHADER_UNITY_STANDARD);
            }
            Debug.LogWarning("Can not find instanced shader for : " + shaderName);
            return null;
        }

        public Material GetInstancedMaterial(Material originalMaterial)
        {
            Material instancedMaterial = new Material(GetInstancedShader(originalMaterial.shader.name));
            instancedMaterial.CopyPropertiesFromMaterial(originalMaterial);
            instancedMaterial.name = originalMaterial.name + "_GPUI";

            return instancedMaterial;
        }

        public void ResetShaderInstances()
        {
            if (shaderInstances == null)
                shaderInstances = new List<ShaderInstance>();
            else
                shaderInstances.Clear();

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public void ClearEmptyShaderInstances()
        {
            if (shaderInstances != null)
                if(shaderInstances.RemoveAll(si => si == null || si.instancedShader == null || string.IsNullOrEmpty(si.name)) > 0)
                {
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(this);
#endif
                }
        }

        public void AddShaderInstance(string name, Shader instancedShader)
        {
            shaderInstances.Add(new ShaderInstance(name, instancedShader));
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public bool IsShadersInstancedVersionExists(string shaderName)
        {
            if (shaderName.Equals(GPUInstancerConstants.SHADER_UNITY_STANDARD) 
                || shaderName.Equals(GPUInstancerConstants.SHADER_UNITY_STANDARD_SPECULAR)
                || shaderName.Equals(GPUInstancerConstants.SHADER_UNITY_VERTEXLIT)
                || shaderName.Equals(GPUInstancerConstants.SHADER_GPUI_FOLIAGE))
                return true;

            foreach (ShaderInstance si in shaderInstances)
            {
                if (si.name.Equals(shaderName))
                    return true;
            }
            return false;
        }
    }

    [Serializable]
    public class ShaderInstance
    {
        public string name;
        public Shader instancedShader;

        public ShaderInstance(string name, Shader instancedShader)
        {
            this.name = name;
            this.instancedShader = instancedShader;
        }
    }

}