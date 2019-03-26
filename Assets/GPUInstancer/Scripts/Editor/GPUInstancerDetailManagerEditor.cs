using UnityEditor;
using UnityEngine;

namespace GPUInstancer
{
    [CustomEditor(typeof(GPUInstancerDetailManager))]
    [CanEditMultipleObjects]
    public class GPUInstancerDetailManagerEditor : GPUInstancerManagerEditor
    {
        private GPUInstancerDetailManager _detailManager;
        private GPUInstancerSimulator _simulator;

        protected override void OnEnable()
        {
            base.OnEnable();

            _detailManager = (target as GPUInstancerDetailManager);
            if(!Application.isPlaying)
                _simulator = new GPUInstancerSimulator(_detailManager, this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if(_simulator != null && _simulator.simulateAtEditor)
                _simulator.StopSimulation();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            base.OnInspectorGUI();

            if (_detailManager.terrain == null)
            {
                if (!Application.isPlaying && Event.current.type == EventType.ExecuteCommand && _detailManager.pickerControlID > 0 && Event.current.commandName == "ObjectSelectorClosed")
                {
                    if (EditorGUIUtility.GetObjectPickerControlID() == _detailManager.pickerControlID)
                        _detailManager.AddTerrainPickerObject(EditorGUIUtility.GetObjectPickerObject());
                    _detailManager.pickerControlID = -1;
                }

                EditorGUI.BeginDisabledGroup(Application.isPlaying);
                DrawDetailTerrainAddButton();
                EditorGUI.EndDisabledGroup();
                return;
            }
            else if (_detailManager.terrainSettings == null)
                _detailManager.SetupManagerWithTerrain(_detailManager.terrain);
            
            DrawSceneSettingsBox();

            if (_detailManager.terrainSettings != null)
            {
                DrawDebugBox(_simulator);

                DrawDetailGlobalInfoBox();

                DrawGPUInstancerManagerGUILayout();
            }

            HandlePickerObjectSelection();

            serializedObject.ApplyModifiedProperties();
        }

        public override void DrawSettingContents()
        {
            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            //EditorGUILayout.PropertyField(prop_settings);

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(GPUInstancerEditorConstants.TEXT_terrain, _detailManager.terrain, typeof(Terrain), true);
            EditorGUI.EndDisabledGroup();


            EditorGUILayout.BeginHorizontal();
            GPUInstancerEditorConstants.DrawColoredButton(GPUInstancerEditorConstants.Contents.paintOnTerrain, GPUInstancerEditorConstants.Colors.green, Color.white, FontStyle.Bold, Rect.zero,
                () =>
                {
                    if (_detailManager.terrain != null)
                    {
                        if (_detailManager.terrain.GetComponent<GPUInstancerTerrainProxy>() != null && _detailManager.terrain.GetComponent<GPUInstancerTerrainProxy>().detailManager != _detailManager)
                            _detailManager.terrain.GetComponent<GPUInstancerTerrainProxy>().detailManager = _detailManager;
                        Selection.activeGameObject = _detailManager.terrain.gameObject;
                    }
                });
            GPUInstancerEditorConstants.DrawColoredButton(GPUInstancerEditorConstants.Contents.removeTerrain, Color.red, Color.white, FontStyle.Bold, Rect.zero,
            () =>
            {
                if (EditorUtility.DisplayDialog(GPUInstancerEditorConstants.TEXT_removeTerrainConfirmation, GPUInstancerEditorConstants.TEXT_removeTerrainAreYouSure, GPUInstancerEditorConstants.TEXT_unset, GPUInstancerEditorConstants.TEXT_cancel))
                {
                    _detailManager.SetupManagerWithTerrain(null);
                }
            });
            EditorGUILayout.EndHorizontal();
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_terrain);

            DrawCameraDataFields();

            EditorGUI.EndDisabledGroup();
        }

        public void DrawDetailTerrainAddButton()
        {
            GUILayout.Space(10);
            Rect buttonRect = GUILayoutUtility.GetRect(100, 40, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));

            GPUInstancerEditorConstants.DrawColoredButton(GPUInstancerEditorConstants.Contents.setTerrain, GPUInstancerEditorConstants.Colors.lightBlue, Color.black, FontStyle.Bold, buttonRect,
                () =>
                {
                    _detailManager.pickerControlID = EditorGUIUtility.GetControlID(FocusType.Passive) + 100;
                    _detailManager.ShowTerrainPicker();
                },
                true, true,
                (o) =>
                {
                    _detailManager.AddTerrainPickerObject(o);
                });
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_setTerrain, true);
            GUILayout.Space(10);
        }

        public void DrawDetailGlobalInfoBox()
        {
            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            EditorGUILayout.BeginVertical(GPUInstancerEditorConstants.Styles.box);
            GPUInstancerEditorConstants.DrawCustomLabel(GPUInstancerEditorConstants.TEXT_detailGlobal, GPUInstancerEditorConstants.Styles.boldLabel);
            EditorGUI.BeginChangeCheck();

            float newMaxDetailDistance = EditorGUILayout.Slider(GPUInstancerEditorConstants.TEXT_maxDetailDistance, _detailManager.terrainSettings.maxDetailDistance, 0, 500);
            if(_detailManager.terrainSettings.maxDetailDistance != newMaxDetailDistance)
            {
                foreach(GPUInstancerDetailPrototype p in _detailManager.prototypeList)
                {
                    if(p.maxDistance == _detailManager.terrainSettings.maxDetailDistance || p.maxDistance > newMaxDetailDistance)
                    {
                        p.maxDistance = newMaxDetailDistance;
                        EditorUtility.SetDirty(p);
                    }
                }
                _detailManager.terrainSettings.maxDetailDistance = newMaxDetailDistance;
            }
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_maxDetailDistance);
            EditorGUILayout.Space();
            _detailManager.terrainSettings.windVector = EditorGUILayout.Vector2Field(GPUInstancerEditorConstants.TEXT_windVector, _detailManager.terrainSettings.windVector);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_windVector);
            EditorGUILayout.Space();
            
            _detailManager.terrainSettings.healthyDryNoiseTexture = (Texture2D)EditorGUILayout.ObjectField(GPUInstancerEditorConstants.TEXT_healthyDryNoiseTexture, _detailManager.terrainSettings.healthyDryNoiseTexture, typeof(Texture2D), false);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_healthyDryNoiseTexture);
            if (_detailManager.terrainSettings.healthyDryNoiseTexture == null)
                _detailManager.terrainSettings.healthyDryNoiseTexture = Resources.Load<Texture2D>(GPUInstancerConstants.NOISE_TEXTURES_PATH + GPUInstancerConstants.DEFAULT_HEALTHY_DRY_NOISE);

            _detailManager.terrainSettings.windWaveNormalTexture = (Texture2D)EditorGUILayout.ObjectField(GPUInstancerEditorConstants.TEXT_windWaveNormalTexture, _detailManager.terrainSettings.windWaveNormalTexture, typeof(Texture2D), false);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_windWaveNormalTexture);
            if (_detailManager.terrainSettings.windWaveNormalTexture == null)
                _detailManager.terrainSettings.windWaveNormalTexture = Resources.Load<Texture2D>(GPUInstancerConstants.NOISE_TEXTURES_PATH + GPUInstancerConstants.DEFAULT_WIND_WAVE_NOISE);

            _detailManager.terrainSettings.autoSPCellSize = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_autoSPCellSize, _detailManager.terrainSettings.autoSPCellSize);
            if (!_detailManager.terrainSettings.autoSPCellSize)
                _detailManager.terrainSettings.preferedSPCellSize = EditorGUILayout.IntSlider(GPUInstancerEditorConstants.TEXT_preferedSPCellSize, _detailManager.terrainSettings.preferedSPCellSize, 25, 500);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_spatialPartitioningCellSize);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_detailManager, "Editor data changed.");
                _detailManager.OnEditorDataChanged();
                EditorUtility.SetDirty(_detailManager.terrainSettings);
            }

            EditorGUILayout.EndVertical();
            EditorGUI.EndDisabledGroup();
        }

        public void DrawGPUInstancerManagerGUILayout()
        {

            int prototypeRowCount = Mathf.FloorToInt((EditorGUIUtility.currentViewWidth - 30f) / PROTOTYPE_RECT_SIZE);

            EditorGUILayout.BeginVertical(GPUInstancerEditorConstants.Styles.box);
            GPUInstancerEditorConstants.DrawCustomLabel(GPUInstancerEditorConstants.TEXT_prototypes, GPUInstancerEditorConstants.Styles.boldLabel);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_prototypes);

            if (!Application.isPlaying)
            {
                GPUInstancerEditorConstants.DrawColoredButton(GPUInstancerEditorConstants.Contents.generatePrototypes, GPUInstancerEditorConstants.Colors.darkBlue, Color.white, FontStyle.Bold, Rect.zero,
                () =>
                {
                    if (EditorUtility.DisplayDialog(GPUInstancerEditorConstants.TEXT_generatePrototypesConfirmation, GPUInstancerEditorConstants.TEXT_generatePrototypeAreYouSure, GPUInstancerEditorConstants.TEXT_generatePrototypes, GPUInstancerEditorConstants.TEXT_cancel))
                    {
                        _detailManager.GeneratePrototypes(true);
                    }
                });
                DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_generatePrototypes);
            }                

            int i = 0;
            EditorGUILayout.BeginHorizontal();
            foreach (GPUInstancerPrototype prototype in _detailManager.prototypeList)
            {
                if (prototype == null)
                    continue;
                if (i != 0 && i % prototypeRowCount == 0)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }

                DrawGPUInstancerPrototypeButton(prototype, prototypeContents[i]);
                i++;
            }

            if (i != 0 && i % prototypeRowCount == 0)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }
            if (!Application.isPlaying)
                DrawGPUInstancerPrototypeAddButton();

            EditorGUILayout.EndHorizontal();
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_addprototypedetail);

            DrawGPUInstancerPrototypeBox();

            EditorGUILayout.EndVertical();
        }

        public override void DrawGPUInstancerPrototypeInfo()
        {
            GPUInstancerDetailPrototype prototype = (GPUInstancerDetailPrototype)_detailManager.selectedPrototype;

            EditorGUI.BeginChangeCheck();
            prototype.detailScale = EditorGUILayout.Vector4Field(GPUInstancerEditorConstants.TEXT_detailScale, prototype.detailScale);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_detailScale);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this._detailManager, "Editor data changed.");
                _detailManager.OnEditorDataChanged();
                EditorUtility.SetDirty(prototype);
            }

            EditorGUI.BeginChangeCheck();
            if (!prototype.usePrototypeMesh)
            {
                prototype.useCustomMaterialForTextureDetail = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_useCustomMaterialForTextureDetail, prototype.useCustomMaterialForTextureDetail);
                DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_useCustomMaterialForTextureDetail);
                if (prototype.useCustomMaterialForTextureDetail)
                {
                    prototype.textureDetailCustomMaterial = (Material)EditorGUILayout.ObjectField(GPUInstancerEditorConstants.TEXT_textureDetailCustomMaterial, prototype.textureDetailCustomMaterial, typeof(Material), false);
                    DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_textureDetailCustomMaterial);
                    prototype.isBillboard = false;
                }
                else
                {
                    prototype.textureDetailCustomMaterial = null;
                }

                if (!prototype.isBillboard)
                {
                    prototype.useCrossQuads = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_crossQuads, prototype.useCrossQuads);
                    DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_crossQuads);
                }
                else
                {
                    prototype.useCrossQuads = false;
                }

                if (prototype.useCrossQuads)
                {
                    prototype.quadCount = EditorGUILayout.IntSlider(GPUInstancerEditorConstants.TEXT_quadCount, prototype.quadCount, 2, 4);
                    DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_quadCount);

                    if (!prototype.useCustomMaterialForTextureDetail)
                    { 
                        prototype.billboardDistance = EditorGUILayout.Slider(GPUInstancerEditorConstants.TEXT_billboardDistance, prototype.billboardDistance, 0.5f, 1f);
                        DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_billboardDistance);
                        prototype.billboardDistanceDebug = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_billboardDistanceDebug, prototype.billboardDistanceDebug);
                        DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_billboardDistanceDebug);
                        if (prototype.billboardDistanceDebug)
                        {
                            prototype.billboardDistanceDebugColor = EditorGUILayout.ColorField(GPUInstancerEditorConstants.TEXT_billboardDistanceDebugColor, prototype.billboardDistanceDebugColor);
                            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_billboardDistanceDebugColor);
                        }
                    }

                }
                else
                {
                    prototype.quadCount = 1;
                }

                
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                if (!prototype.usePrototypeMesh && prototype.useCustomMaterialForTextureDetail && prototype.textureDetailCustomMaterial != null)
                {
                    if (!_detailManager.shaderBindings.IsShadersInstancedVersionExists(prototype.textureDetailCustomMaterial.shader.name))
                    {
                        Shader instancedShader;
                        if (GPUInstancerUtility.IsMaterialInstanced(prototype.textureDetailCustomMaterial))
                            instancedShader = prototype.textureDetailCustomMaterial.shader;
                        else
                            instancedShader = GPUInstancerUtility.CreateInstancedShader(prototype.textureDetailCustomMaterial.shader, _detailManager.shaderBindings);

                        if (instancedShader != null)
                                _detailManager.shaderBindings.AddShaderInstance(prototype.textureDetailCustomMaterial.shader.name, instancedShader);
                        else
                            Debug.LogWarning("Can not create instanced version for shader: " + prototype.textureDetailCustomMaterial.shader.name + ". Standard Shader will be used instead.");
                    }
                }
                EditorUtility.SetDirty(prototype);
            }

            if (!prototype.useCustomMaterialForTextureDetail)
            {
                EditorGUI.BeginChangeCheck();
                if (!prototype.usePrototypeMesh)
                { 
                    prototype.detailHealthyColor = EditorGUILayout.ColorField(GPUInstancerEditorConstants.TEXT_detailHealthyColor, prototype.detailHealthyColor);
                    DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_detailHealthyColor);
                    prototype.detailDryColor = EditorGUILayout.ColorField(GPUInstancerEditorConstants.TEXT_detailDryColor, prototype.detailDryColor);
                    DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_detailDryColor);
                }
                
                prototype.noiseSpread = EditorGUILayout.FloatField(GPUInstancerEditorConstants.TEXT_noiseSpread, prototype.noiseSpread);
                DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_noiseSpread);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(this._detailManager, "Editor data changed.");
                    _detailManager.OnEditorDataChanged();
                    if (_simulator != null && _simulator.simulateAtEditor && !_simulator.initializingInstances)
                    {
                        GPUInstancerUtility.UpdateDetailInstanceRuntimeDataList(_simulator.GetRuntimeDataList(), _detailManager.terrainSettings);
                    }
                    EditorUtility.SetDirty(prototype);
                }
            }

            if (!prototype.usePrototypeMesh && !prototype.useCustomMaterialForTextureDetail)
            {
                EditorGUI.BeginChangeCheck();
                prototype.isBillboard = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_isBillboard, prototype.isBillboard);
                DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_isBillboard);

                prototype.ambientOcclusion = EditorGUILayout.Slider(GPUInstancerEditorConstants.TEXT_ambientOcclusion, prototype.ambientOcclusion, 0f, 1f);
                DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_ambientOcclusion);
                prototype.gradientPower = EditorGUILayout.Slider(GPUInstancerEditorConstants.TEXT_gradientPower, prototype.gradientPower, 0f, 1f);
                DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_gradientPower);
                prototype.windIdleSway = EditorGUILayout.Slider(GPUInstancerEditorConstants.TEXT_windIdleSway, prototype.windIdleSway, 0f, 1f);
                DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_windIdleSway);
                prototype.windWavesOn = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_windWavesOn, prototype.windWavesOn);
                DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_windWavesOn);
                if (prototype.windWavesOn)
                {
                    prototype.windWaveTintColor = EditorGUILayout.ColorField(GPUInstancerEditorConstants.TEXT_windWaveTintColor, prototype.windWaveTintColor);
                    DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_windWaveTintColor);
                    prototype.windWaveSize = EditorGUILayout.Slider(GPUInstancerEditorConstants.TEXT_windWaveSize, prototype.windWaveSize, 0f, 1f);
                    DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_windWaveSize);
                    prototype.windWaveTint = EditorGUILayout.Slider(GPUInstancerEditorConstants.TEXT_windWaveTint, prototype.windWaveTint, 0f, 1f);
                    DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_windWaveTint);
                    prototype.windWaveSway = EditorGUILayout.Slider(GPUInstancerEditorConstants.TEXT_windWaveSway, prototype.windWaveSway, 0f, 1f);
                    DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_windWaveSway);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(prototype);
                    if(_simulator != null && _simulator.simulateAtEditor && !_simulator.initializingInstances)
                    {
                        GPUInstancerUtility.UpdateDetailInstanceRuntimeDataList(_simulator.GetRuntimeDataList(), _detailManager.terrainSettings);
                    }
                }
            }
        }

        public override void DrawGPUInstancerPrototypeActions()
        {
            if (!_detailManager.editorDataChanged)
                EditorGUI.BeginDisabledGroup(true);
            GPUInstancerEditorConstants.DrawColoredButton(GPUInstancerEditorConstants.Contents.applyChangesToTerrain, GPUInstancerEditorConstants.Colors.green, Color.white, FontStyle.Bold, Rect.zero,
                () =>
                {
                    _detailManager.ApplyEditorDataChanges();
                });
            if (!_detailManager.editorDataChanged)
                EditorGUI.EndDisabledGroup();
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_applyChangesToTerrain);

            GPUInstancerEditorConstants.DrawColoredButton(GPUInstancerEditorConstants.Contents.delete, Color.red, Color.white, FontStyle.Bold, Rect.zero,
                () =>
                {
                    if (EditorUtility.DisplayDialog(GPUInstancerEditorConstants.TEXT_deleteConfirmation, GPUInstancerEditorConstants.TEXT_deleteAreYouSure + "\n\"" + _detailManager.selectedPrototype.ToString() + "\"", GPUInstancerEditorConstants.TEXT_delete, GPUInstancerEditorConstants.TEXT_cancel))
                    {
                        _detailManager.DeletePrototype(_detailManager.selectedPrototype);
                        _detailManager.selectedPrototype = null;
                    }
                });
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_delete);
        }

        public override float GetMaxDistance()
        {
            return _detailManager.terrainSettings != null ? _detailManager.terrainSettings.maxDetailDistance : 500F;
        }
    }
}