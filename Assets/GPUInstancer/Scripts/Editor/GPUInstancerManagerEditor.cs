using UnityEditor;
using UnityEngine;

namespace GPUInstancer
{
    public abstract class GPUInstancerManagerEditor : Editor
    {
        public static readonly float PROTOTYPE_RECT_SIZE = 80;
        public static readonly float PROTOTYPE_RECT_PADDING = 5;
        public static readonly Vector2 PROTOTYPE_RECT_PADDING_VECTOR = new Vector2(PROTOTYPE_RECT_PADDING, PROTOTYPE_RECT_PADDING);
        public static readonly Vector2 PROTOTYPE_RECT_SIZE_VECTOR = new Vector2(PROTOTYPE_RECT_SIZE - PROTOTYPE_RECT_PADDING * 2, PROTOTYPE_RECT_SIZE - PROTOTYPE_RECT_PADDING * 2);

        protected bool showPrototypeBox = true;
        protected bool showHelpText = false;
        protected Texture2D helpIcon;
        protected Texture2D helpIconActive;

        protected GUIContent[] prototypeContents = null;

        //protected SerializedProperty prop_settings;
        protected SerializedProperty prop_autoSelectCamera;
        protected SerializedProperty prop_mainCamera;

        private GPUInstancerManager _manager;

        private PreviewRenderUtility _previewRenderUtility;

        protected virtual void OnEnable()
        {
            prototypeContents = null;

            _manager = (target as GPUInstancerManager);
            //prop_settings = serializedObject.FindProperty("settings");
            prop_autoSelectCamera = serializedObject.FindProperty("autoSelectCamera");
            prop_mainCamera = serializedObject.FindProperty("cameraData").FindPropertyRelative("mainCamera");
            helpIcon = Resources.Load<Texture2D>(GPUInstancerConstants.EDITOR_TEXTURES_PATH + GPUInstancerConstants.HELP_ICON);
            helpIconActive = Resources.Load<Texture2D>(GPUInstancerConstants.EDITOR_TEXTURES_PATH + GPUInstancerConstants.HELP_ICON_ACTIVE);
            if (_previewRenderUtility == null)
                _previewRenderUtility = new PreviewRenderUtility();
        }

        protected virtual void OnDisable()
        {
            if (prototypeContents != null)
            {
                for (int i = 0; i < prototypeContents.Length; i++)
                {
                    if (prototypeContents[i].image != null)
                        DestroyImmediate(prototypeContents[i].image);
                }
            }
            prototypeContents = null;
            if(_previewRenderUtility != null)
                _previewRenderUtility.Cleanup();
        }

        public override void OnInspectorGUI()
        {
            if (prototypeContents == null || _manager.prototypeList.Count != prototypeContents.Length)
                GeneratePrototypeContents();
            GPUInstancerEditorConstants.Styles.foldout.fontStyle = FontStyle.Bold;

            EditorGUILayout.BeginHorizontal(GPUInstancerEditorConstants.Styles.box);
            EditorGUILayout.LabelField(GPUInstancerEditorConstants.GPUI_VERSION, GPUInstancerEditorConstants.Styles.boldLabel);
            GUILayout.FlexibleSpace();
            DrawHelpButton(GUILayoutUtility.GetRect(20, 20), showHelpText);
            EditorGUILayout.EndHorizontal();

            if (Application.isPlaying && _manager.cameraData != null && _manager.cameraData.mainCamera == null && _manager.runtimeDataList != null && _manager.runtimeDataList.Count != 0)
                EditorGUILayout.HelpBox(GPUInstancerEditorConstants.ERRORTEXT_cameraNotFound, MessageType.Error);
        }

        public void GeneratePrototypeContents()
        {
            if (_manager.prototypeList == null || _manager.prototypeList.Count == 0)
                return;
            prototypeContents = new GUIContent[_manager.prototypeList.Count];
            for (int i = 0; i < _manager.prototypeList.Count; i++)
            {
#if UNITY_2017_1_OR_NEWER
                prototypeContents[i] = new GUIContent(GetPreview(_manager.prototypeList[i]), _manager.prototypeList[i].ToString());
#else
                prototypeContents[i] = new GUIContent(GetPreviewTexture(_manager.prototypeList[i]), _manager.prototypeList[i].ToString());
#endif
            }
        }

#if !UNITY_2017_1_OR_NEWER
        public Texture GetPreviewTexture(GPUInstancerPrototype prototype)
        {
            if (prototype is GPUInstancerDetailPrototype && ((GPUInstancerDetailPrototype)prototype).prototypeTexture != null)
                return GetPreview(prototype);

            if (prototype.prefabObject == null)
                return null;

            Mesh mPreviewMesh = prototype.prefabObject.GetComponentInChildren<MeshFilter>().sharedMesh;
            Material mMat = prototype.prefabObject.GetComponentInChildren<Renderer>().sharedMaterial;

            _previewRenderUtility.m_Camera.transform.position = -Vector3.forward * (Mathf.Max(mPreviewMesh.bounds.extents.x, mPreviewMesh.bounds.extents.y) * 10 + 1);
            _previewRenderUtility.m_Camera.transform.rotation = Quaternion.identity;
            _previewRenderUtility.m_Camera.farClipPlane = -_previewRenderUtility.m_Camera.transform.position.z + mPreviewMesh.bounds.extents.z * 2;

            _previewRenderUtility.BeginPreview(new Rect(0, 0, PROTOTYPE_RECT_SIZE - 10, PROTOTYPE_RECT_SIZE - 10), GUIStyle.none);
            _previewRenderUtility.DrawMesh(mPreviewMesh, -mPreviewMesh.bounds.center - Vector3.forward, Quaternion.Euler(-30, 0, 0), mMat, 0);

            _previewRenderUtility.m_Camera.Render();

            Texture preview = _previewRenderUtility.EndPreview();
            if (preview != null)
            {
                // Copy preview texture so that if unity destroys it we have our own texture reference
                Texture2D newTx = new Texture2D(preview.width, preview.height);
                if(preview is Texture2D)
                    newTx.SetPixels(((Texture2D)preview).GetPixels());
                else if(preview is RenderTexture)
                {
                    RenderTexture.active = (RenderTexture)preview;
                    newTx.ReadPixels(new Rect(0, 0, preview.width, preview.height), 0, 0);
                }
                newTx.Apply();
                return newTx;
            }
            return null;
        }
#endif

        public static Texture2D GetPreview(GPUInstancerPrototype prototype)
        {
            Object previewObject = GetPreviewObject(prototype);

            if (previewObject == null)
                return null;

            Texture2D preview = AssetPreview.GetAssetPreview(previewObject);
            if (preview != null)
            {
                // Copy preview texture so that if unity destroys it we have our own texture reference
                Texture2D newTx = new Texture2D(preview.width, preview.height);
                newTx.SetPixels(preview.GetPixels());
                newTx.Apply();
                return newTx;
            }
            return null;
        }

        public static Object GetPreviewObject(GPUInstancerPrototype prototype)
        {
            Object previewObject = prototype.prefabObject;
            if (prototype is GPUInstancerDetailPrototype)
            {
                if (((GPUInstancerDetailPrototype)prototype).prototypeTexture != null)
                    previewObject = ((GPUInstancerDetailPrototype)prototype).prototypeTexture;
            }

            return previewObject;
        }

        public bool HandlePickerObjectSelection()
        {
            if (!Application.isPlaying && Event.current.type == EventType.ExecuteCommand && _manager.pickerControlID > 0 && Event.current.commandName == "ObjectSelectorClosed")
            {
                if (EditorGUIUtility.GetObjectPickerControlID() == _manager.pickerControlID)
                    _manager.AddPickerObject(EditorGUIUtility.GetObjectPickerObject());
                _manager.pickerControlID = -1;
                return true;
            }
            return false;
        }

        public void DrawSceneSettingsBox()
        {
            EditorGUILayout.BeginVertical(GPUInstancerEditorConstants.Styles.box);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(GPUInstancerEditorConstants.TEXT_sceneSettings, GPUInstancerEditorConstants.Styles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            DrawSettingContents();

            EditorGUILayout.EndVertical();
        }

        public abstract void DrawSettingContents();

        public void DrawCameraDataFields()
        {
            EditorGUILayout.PropertyField(prop_autoSelectCamera);
            if (!_manager.autoSelectCamera)
                EditorGUILayout.PropertyField(prop_mainCamera, GPUInstancerEditorConstants.Contents.useCamera);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_camera);
        }

        public void DrawDebugBox(GPUInstancerSimulator simulator = null)
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.BeginVertical(GPUInstancerEditorConstants.Styles.box);
                GPUInstancerEditorConstants.DrawCustomLabel(GPUInstancerEditorConstants.TEXT_debug, GPUInstancerEditorConstants.Styles.boldLabel);

                if (simulator != null)
                {
                    if (simulator.simulateAtEditor)
                    {
                        if (simulator.initializingInstances)
                        {
                            EditorGUI.BeginDisabledGroup(true);
                            GPUInstancerEditorConstants.DrawColoredButton(GPUInstancerEditorConstants.Contents.simulateAtEditorPrep, GPUInstancerEditorConstants.Colors.darkBlue, Color.white,
                                FontStyle.Bold, Rect.zero, null);
                            EditorGUI.EndDisabledGroup();
                        }
                        else
                        {
                            if (Event.current.type == EventType.Repaint)
                                simulator.UpdateSimulation();
                            GPUInstancerEditorConstants.DrawColoredButton(GPUInstancerEditorConstants.Contents.simulateAtEditorStop, Color.red, Color.white,
                                FontStyle.Bold, Rect.zero, () =>
                                {
                                    simulator.StopSimulation();
                                });
                        }
                    }
                    else
                    {
                        GPUInstancerEditorConstants.DrawColoredButton(GPUInstancerEditorConstants.Contents.simulateAtEditor, GPUInstancerEditorConstants.Colors.green, Color.white,
                            FontStyle.Bold, Rect.zero, () =>
                            {
                                simulator.StartSimulation();
                            });
                    }
                }
                DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_simulator);

                EditorGUILayout.EndVertical();
            }
        }

        public void DrawGPUInstancerPrototypeButton(GPUInstancerPrototype prototype, GUIContent prototypeContent, Editor prototypeEditor = null)
        {
            if (prototypeContent.image == null)
                prototypeContent.image = GetPreview(prototype);

            Rect prototypeRect = GUILayoutUtility.GetRect(PROTOTYPE_RECT_SIZE, PROTOTYPE_RECT_SIZE, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));

            Rect iconRect = new Rect(prototypeRect.position + new Vector2(PROTOTYPE_RECT_PADDING, PROTOTYPE_RECT_PADDING),
                new Vector2(PROTOTYPE_RECT_SIZE - PROTOTYPE_RECT_PADDING * 2, PROTOTYPE_RECT_SIZE - PROTOTYPE_RECT_PADDING * 2));

            GUI.SetNextControlName(prototypeContent.tooltip);
            if (_manager.selectedPrototype == prototype)
            {
                GPUInstancerEditorConstants.DrawColoredButton(prototypeContent, GPUInstancerEditorConstants.Colors.lightGreen, Color.black, FontStyle.Normal, iconRect, null);
            }
            else
            {
                GPUInstancerEditorConstants.DrawColoredButton(prototypeContent, GUI.backgroundColor, Color.black, FontStyle.Normal, iconRect,
                    () =>
                    {
                        _manager.selectedPrototype = prototype;
                        GUI.FocusControl(prototypeContent.tooltip);
                    });
            }
        }

        public void DrawGPUInstancerPrototypeAddButton()
        {
            Rect prototypeRect = GUILayoutUtility.GetRect(PROTOTYPE_RECT_SIZE, PROTOTYPE_RECT_SIZE, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));

            Rect iconRect = new Rect(prototypeRect.position + PROTOTYPE_RECT_PADDING_VECTOR, PROTOTYPE_RECT_SIZE_VECTOR);

            GPUInstancerEditorConstants.DrawColoredButton(GPUInstancerEditorConstants.Contents.add, GPUInstancerEditorConstants.Colors.lightBlue, Color.black, FontStyle.Bold, iconRect,
                () =>
                {
                    _manager.pickerControlID = EditorGUIUtility.GetControlID(FocusType.Passive) + 100;
                    _manager.ShowObjectPicker();
                },
                true, true,
                (o) =>
                {
                    _manager.AddPickerObject(o);
                });
        }

        public void DrawGPUInstancerPrototypeBox()
        {
            if (_manager.selectedPrototype == null)
                return;

            EditorGUILayout.BeginVertical(GPUInstancerEditorConstants.Styles.box);
            // title
            Rect foldoutRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
            foldoutRect.x += 12;
            showPrototypeBox = EditorGUI.Foldout(foldoutRect, showPrototypeBox, _manager.selectedPrototype.ToString(), true, GPUInstancerEditorConstants.Styles.foldout);

            if (!showPrototypeBox)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(GPUInstancerEditorConstants.TEXT_prefabObject, _manager.selectedPrototype.prefabObject, typeof(GameObject), false);
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginChangeCheck();

            _manager.selectedPrototype.isShadowCasting = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_isShadowCasting, _manager.selectedPrototype.isShadowCasting);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_isShadowCasting);
            _manager.selectedPrototype.isFrustumCulling = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_isFrustumCulling, _manager.selectedPrototype.isFrustumCulling);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_isFrustumCulling);
            _manager.selectedPrototype.frustumOffset = EditorGUILayout.Slider(GPUInstancerEditorConstants.TEXT_frustumOffset, _manager.selectedPrototype.frustumOffset, 0.0f, 0.5f);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_frustumOffset);
            _manager.selectedPrototype.maxDistance = EditorGUILayout.Slider(GPUInstancerEditorConstants.TEXT_maxDistance, _manager.selectedPrototype.maxDistance, 0.0f, GetMaxDistance());
            DrawHelpText(_manager is GPUInstancerDetailManager ? GPUInstancerEditorConstants.HELPTEXT_maxDistanceDetail : GPUInstancerEditorConstants.HELPTEXT_maxDistance);

            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            DrawGPUInstancerPrototypeInfo();

            GUILayout.Space(10);
            GPUInstancerEditorConstants.DrawCustomLabel(GPUInstancerEditorConstants.TEXT_actions, GPUInstancerEditorConstants.Styles.boldLabel, false);

            DrawGPUInstancerPrototypeActions();

            if (EditorGUI.EndChangeCheck() && _manager.selectedPrototype != null)
            {
                EditorUtility.SetDirty(_manager.selectedPrototype);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();
        }

        public void DrawHelpText(string text, bool forceShow = false)
        {
            if (showHelpText || forceShow)
            {
                EditorGUILayout.HelpBox(text, MessageType.Info);
            }
        }

        public void DrawHelpButton(Rect buttonRect, bool showingHelp)
        {
            if (GUI.Button(buttonRect, new GUIContent(showHelpText ? helpIconActive : helpIcon,
                showHelpText ? GPUInstancerEditorConstants.TEXT_hideHelpTooltip : GPUInstancerEditorConstants.TEXT_showHelpTooltip), showHelpText ? GPUInstancerEditorConstants.Styles.helpButtonSelected : GPUInstancerEditorConstants.Styles.helpButton))
            {
                showHelpText = !showHelpText;
            }
        }

        public abstract void DrawGPUInstancerPrototypeInfo();
        public abstract void DrawGPUInstancerPrototypeActions();
        public abstract float GetMaxDistance();
    }
}
