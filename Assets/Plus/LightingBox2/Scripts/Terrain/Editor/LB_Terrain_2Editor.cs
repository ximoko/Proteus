using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LB_Terrain_2))]
public class LB_Terrain_2Editor : Editor 
{
	LB_Terrain_2 targetObject;

	int currentLayer;

	void OnEnable()
	{
	}

	public override void OnInspectorGUI()
	{
		
		serializedObject.Update ();

		targetObject = (LB_Terrain_2)target;

		targetObject.LoadSplats ();

		if (!targetObject.initializd || targetObject.failed) {
			targetObject.terrainType = (TerrainType)EditorGUILayout.EnumPopup ("Terrain Mode", targetObject.terrainType, GUILayout.Width (343));

			if (GUILayout.Button ("Initialize")) {
				targetObject.Initalize ();
			}
			return;
		} else {
			if (GUILayout.Button ("Update Layers")) 
			{
				targetObject.UpdateTerrain ();
			}
		}

		////------------------------------------------------------------------

		GUILayout.BeginVertical ("", GUI.skin.box);

		EditorGUILayout.LabelField ("Tessellation",GUI.skin.box);

		var tessIntensityRef = targetObject.tessIntensity;
		var minTessRef = targetObject.minTess;
		var maxTessRef = targetObject.maxTess;

		targetObject.tessIntensity = EditorGUILayout.Slider ("Intensity", targetObject.tessIntensity,1,32);

		targetObject.minTess = EditorGUILayout.Slider ("Mix", targetObject.minTess,1,300);
		targetObject.maxTess = EditorGUILayout.Slider ("Max", targetObject.maxTess,1,300);

		if (tessIntensityRef != targetObject.tessIntensity || minTessRef != targetObject.minTess
		    || maxTessRef != targetObject.maxTess)
			targetObject.UpdateTessellation ();
		

		EditorGUILayout.Space ();

		EditorGUILayout.EndHorizontal ();
		////------------------------------------------------------------------

		GUILayout.BeginVertical ("", GUI.skin.box);

		EditorGUILayout.LabelField ("Layers",GUI.skin.box);


		//-----------------------------------------------------------------------------------------
		EditorGUILayout.BeginHorizontal ();

		for (int a = 0; a < targetObject.splats.Length; a++) {
			if (GUILayout.Button (targetObject.splats [a].texture, GUILayout.Width (43), GUILayout.Height (43)))
				currentLayer = a;
		}

		EditorGUILayout.EndHorizontal ();

		EditorGUILayout.Space ();

		//-----------------------------------------------------------------------------------------
		// ... your box content ...
		GUILayout.EndVertical ();
		//-----------------------------------------------------------------------------------------

		//-----------------------------------------------------------------------------------------
		GUILayout.BeginVertical ("", GUI.skin.box);

		EditorGUILayout.LabelField ("Properties",GUI.skin.box);

		var uvRef = targetObject.uvS[currentLayer];
		var smoothnessValueRef = targetObject.smoothnessValue[currentLayer];
		var disValueRef = targetObject.disValue[currentLayer];
		var normalPowerRef = targetObject.normalPower[currentLayer];
		var specularColorRef = targetObject.specularColor[currentLayer];

		targetObject.uvS [currentLayer] = EditorGUILayout.Slider ("UV Tile", targetObject.uvS [currentLayer],1,300);
		targetObject.smoothnessValue [currentLayer] = EditorGUILayout.Slider ("Smoothness", targetObject.smoothnessValue [currentLayer], 0, 10);
		targetObject.disValue [currentLayer] = EditorGUILayout.Slider ("Displacement", targetObject.disValue [currentLayer], 0, 3);
		targetObject.normalPower [currentLayer] = EditorGUILayout.Slider ("Normal Power", targetObject.normalPower [currentLayer], 0, 1);
		targetObject.specularColor [currentLayer] = EditorGUILayout.ColorField ("Specular", targetObject.specularColor [currentLayer]);

		if (uvRef != targetObject.uvS[currentLayer] || smoothnessValueRef != targetObject.smoothnessValue[currentLayer] || disValueRef != targetObject.disValue[currentLayer]
			|| normalPowerRef != targetObject.normalPower[currentLayer] || specularColorRef != targetObject.specularColor[currentLayer]) {
			targetObject.UpdateSettings ();
		}

		EditorGUILayout.Space ();

		GUILayout.EndVertical ();

		serializedObject.ApplyModifiedProperties ();
	}
}
