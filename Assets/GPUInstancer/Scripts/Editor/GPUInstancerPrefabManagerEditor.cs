using UnityEditor;
using UnityEngine;

namespace GPUInstancer
{
    [CustomEditor(typeof(GPUInstancerPrefabManager))]
    [CanEditMultipleObjects]
    public class GPUInstancerPrefabManagerEditor : GPUInstancerManagerEditor
    {
        private GPUInstancerPrefabManager _prefabManager;
        
        protected override void OnEnable()
        {
            base.OnEnable();
            _prefabManager = (target as GPUInstancerPrefabManager);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            base.OnInspectorGUI();

            DrawSceneSettingsBox();

            DrawGPUInstancerManagerGUILayout();

            HandlePickerObjectSelection();

            serializedObject.ApplyModifiedProperties();
        }

        public override void DrawSettingContents()
        {
            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            //EditorGUILayout.PropertyField(prop_settings);
            DrawCameraDataFields();
            EditorGUI.EndDisabledGroup();
        }

        public void DrawGPUInstancerManagerGUILayout()
        {
            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            DrawRegisterPrefabsBox();
            EditorGUI.EndDisabledGroup();

            int prototypeRowCount = Mathf.FloorToInt((EditorGUIUtility.currentViewWidth - 30f) / PROTOTYPE_RECT_SIZE);

            EditorGUILayout.BeginVertical(GPUInstancerEditorConstants.Styles.box);
            GPUInstancerEditorConstants.DrawCustomLabel(GPUInstancerEditorConstants.TEXT_prototypes, GPUInstancerEditorConstants.Styles.boldLabel);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_prototypes);

            int i = 0;
            EditorGUILayout.BeginHorizontal();
            foreach (GPUInstancerPrototype prototype in _prefabManager.prototypeList)
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
            
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_addprototypeprefab);

            DrawGPUInstancerPrototypeBox();

            EditorGUILayout.EndVertical();
        }

        public void DrawRegisterPrefabsBox()
        {
            EditorGUILayout.BeginVertical(GPUInstancerEditorConstants.Styles.box);
            GPUInstancerEditorConstants.DrawCustomLabel(GPUInstancerEditorConstants.TEXT_registeredPrefabs, GPUInstancerEditorConstants.Styles.boldLabel);
            GPUInstancerEditorConstants.DrawColoredButton(GPUInstancerEditorConstants.Contents.registerPrefabsInScene, GPUInstancerEditorConstants.Colors.darkBlue, Color.white, FontStyle.Bold, Rect.zero,
                () =>
                {
                    Undo.RecordObject(_prefabManager, "Register prefabs in scene");
                    _prefabManager.RegisterPrefabsInScene();
                });
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_registerPrefabsInScene);

            if (!Application.isPlaying && _prefabManager.registeredPrefabs.Count > 0)
            {
                foreach (RegisteredPrefabsData rpd in _prefabManager.registeredPrefabs)
                {
                    GPUInstancerEditorConstants.DrawCustomLabel(rpd.prefabPrototype.ToString() + " Instance Count: " +
                        rpd.registeredPrefabs.Count,
                        GPUInstancerEditorConstants.Styles.label, false);
                }
            }
            else if(Application.isPlaying && _prefabManager.GetRegisteredPrefabsRuntimeData().Count > 0)
            {
                foreach (GPUInstancerPrototype p in _prefabManager.GetRegisteredPrefabsRuntimeData().Keys)
                {
                    GPUInstancerEditorConstants.DrawCustomLabel(p.ToString() + " Instance Count: " +
                        _prefabManager.GetRegisteredPrefabsRuntimeData()[p].Count,
                        GPUInstancerEditorConstants.Styles.label, false);
                }
            }
            else
                GPUInstancerEditorConstants.DrawCustomLabel("No registered prefabs.", GPUInstancerEditorConstants.Styles.label, false);

            EditorGUILayout.EndVertical();
        }
        
        public override void DrawGPUInstancerPrototypeInfo()
        {
            GPUInstancerPrefabPrototype prototype = (GPUInstancerPrefabPrototype)_prefabManager.selectedPrototype;
            prototype.enableRuntimeModifications = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_enableRuntimeModifications, prototype.enableRuntimeModifications);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_enableRuntimeModifications);
            if (prototype.enableRuntimeModifications)
            {
                if(prototype.prefabObject.GetComponent<Rigidbody>() != null)
                {
                    prototype.startWithRigidBody = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_startWithRigidBody, prototype.startWithRigidBody);
                    DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_startWithRigidBody);
                }

                prototype.addRemoveInstancesAtRuntime = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_addRemoveInstancesAtRuntime, prototype.addRemoveInstancesAtRuntime);
                DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_addRemoveInstancesAtRuntime);
                if (prototype.addRemoveInstancesAtRuntime)
                {
                    prototype.extraBufferSize = EditorGUILayout.IntSlider(GPUInstancerEditorConstants.TEXT_extraBufferSize, prototype.extraBufferSize, 0, 1024);
                    DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_extraBufferSize);
                }
            }
        }

        public override void DrawGPUInstancerPrototypeActions()
        {
            GPUInstancerEditorConstants.DrawColoredButton(GPUInstancerEditorConstants.Contents.delete, Color.red, Color.white, FontStyle.Bold, Rect.zero,
                () =>
                {
                    if (EditorUtility.DisplayDialog(GPUInstancerEditorConstants.TEXT_deleteConfirmation, GPUInstancerEditorConstants.TEXT_deleteAreYouSure + "\n\"" + _prefabManager.selectedPrototype.ToString() + "\"", GPUInstancerEditorConstants.TEXT_delete, GPUInstancerEditorConstants.TEXT_cancel))
                    {
                        _prefabManager.DeletePrototype(_prefabManager.selectedPrototype);
                        _prefabManager.selectedPrototype = null;
                    }
                });
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_delete);
        }

        public override float GetMaxDistance()
        {
            return 2500f;
        }
    }
}