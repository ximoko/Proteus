using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

/*
 * VLight
 * Copyright Brian Su 2011-2014
*/
public class VolumeLightAbout : EditorWindow
{
    private Texture _logoImage;

    [MenuItem("Help/About V-Lights...", false, 1010)]
    private static void Init()
    {
        VolumeLightAbout window = EditorWindow.CreateInstance<VolumeLightAbout>();
        window.ShowUtility();
        window.title = "About V-Lights";
    }

    private void OnGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Space(20);

        GUILayout.BeginVertical();
        if (_logoImage == null)
        {
            _logoImage = Resources.Load("VLights/Logo") as Texture;
        }
        GUI.DrawTexture(new Rect(-60, -64, 256, 256), _logoImage);
        GUILayout.Space(50);
        GUILayout.FlexibleSpace();
        GUILayout.Label("Version 1.1.14");
        GUILayout.FlexibleSpace();
        GUILayout.Label("For help and more information.");
        if (GUILayout.Button("http://vlights-system.blogspot.com/"))
        {
            Application.OpenURL("http://vlights-system.blogspot.com/");
        }

        GUILayout.FlexibleSpace();
        GUILayout.Label("(c) 2011-2014 Brian Su");

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }
}