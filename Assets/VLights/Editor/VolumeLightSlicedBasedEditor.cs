using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

/*
 * VLight
 * Copyright Brian Su 2011-2014
*/
[CustomEditor(typeof(VLight))]
[CanEditMultipleObjects]
public class VolumeLightSlicedBasedEditor : Editor
{
    public VLight Light
    {
        get { return target as VLight; }
    }
    

    public override void OnInspectorGUI()
    {
		var property = serializedObject.FindProperty("renderWireFrame");
		
		property.boolValue = GUILayout.Toggle(property.boolValue, "Render wireframe");
		EditorUtility.SetSelectedWireframeHidden(Light.GetComponent<Renderer>(), !property.boolValue);
		
		serializedObject.ApplyModifiedProperties();		
		
        base.OnInspectorGUI();
		
		EditorGUILayout.HelpBox("Texture parameters can be set on the material.", MessageType.Info); 
    }
}