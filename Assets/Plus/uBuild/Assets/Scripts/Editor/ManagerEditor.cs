using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.AnimatedValues;

//change inspector look/functionality
[CustomEditor(typeof(uBuildManager))]
public class ManagerEditor : Editor{
	
	//variables not visible in the inspector
	uBuildManager managerScript;
	ReorderableList list;
	AnimBool showPieces;
	AnimBool showSettings;
	
	void OnEnable(){
	//get target manager script
	managerScript = (target as uBuildManager).gameObject.GetComponent<uBuildManager>();	
	
	//add some new animbools (fold pieces and settings in/out)
	showPieces = new AnimBool(false);
	showPieces.valueChanged.AddListener(Repaint);
	showSettings = new AnimBool(false);
	showSettings.valueChanged.AddListener(Repaint);
	
	//create the reorderable list
	list = new ReorderableList(serializedObject, 
    serializedObject.FindProperty("pieces"), 
    true, true, true, true);
	
	//draw list element
	list.drawElementCallback =  
    (Rect rect, int index, bool isActive, bool isFocused) => {
    var element = list.serializedProperty.GetArrayElementAtIndex(index);
	
	//width of text label
	int labelWidth = 60;
	
	//check if element is active and draw element background
	if(!isActive){
	GUI.color = new Color(0.9f, 0.9f, 0.9f, 1);
	EditorGUI.DrawRect(new Rect(16, rect.y + EditorGUIUtility.singleLineHeight + 5, rect.width + 22, EditorGUIUtility.singleLineHeight * 11.5f), new Color(0.9f, 0.9f, 0.9f, 1));	
	}
	else{
	GUI.color = new Color(0.8f, 0.85f, 0.9f, 1);
	EditorGUI.DrawRect(new Rect(16, rect.y + EditorGUIUtility.singleLineHeight + 5, rect.width + 22, EditorGUIUtility.singleLineHeight * 11.5f), new Color(0.8f, 0.85f, 0.9f, 1));	
	}
	
	//draw element labels
	GUI.color = Color.white;
    EditorGUI.LabelField(new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight + 5, labelWidth - 10, EditorGUIUtility.singleLineHeight), "Prefab");
    EditorGUI.LabelField(new Rect(rect.x, rect.y + (EditorGUIUtility.singleLineHeight * 2) + 10, labelWidth - 10, EditorGUIUtility.singleLineHeight), "Image");
	EditorGUI.LabelField(new Rect(rect.x, rect.y + (EditorGUIUtility.singleLineHeight * 3) + 15, (labelWidth * 2.5f) - 10, EditorGUIUtility.singleLineHeight), "Floor");
	EditorGUI.LabelField(new Rect(rect.x, rect.y + (EditorGUIUtility.singleLineHeight * 4) + 20, (labelWidth * 2.5f) - 10, EditorGUIUtility.singleLineHeight), "Disable y snapping");
	EditorGUI.LabelField(new Rect(rect.x, rect.y + (EditorGUIUtility.singleLineHeight * 5) + 25, (labelWidth * 2.5f) - 10, EditorGUIUtility.singleLineHeight), "Furniture");
	EditorGUI.LabelField(new Rect(rect.x, rect.y + (EditorGUIUtility.singleLineHeight * 9) + 35, (labelWidth * 2), EditorGUIUtility.singleLineHeight), "Requires resources");
	
	//set color and draw element title
	GUI.color = new Color(0.7f, 0.7f, 0.7f, 1);
	EditorGUI.PropertyField(
        new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
        element.FindPropertyRelative("name"), GUIContent.none);
	
	//draw all element fields
	GUI.color = new Color(1f, 1f, 1f, 1);
    EditorGUI.PropertyField(
        new Rect(rect.x + labelWidth, rect.y + EditorGUIUtility.singleLineHeight + 5, rect.width - labelWidth, EditorGUIUtility.singleLineHeight),
        element.FindPropertyRelative("prefab"), GUIContent.none);
		
    EditorGUI.PropertyField(
        new Rect(rect.x + labelWidth, rect.y + (EditorGUIUtility.singleLineHeight * 2) + 10, rect.width - labelWidth, EditorGUIUtility.singleLineHeight),
        element.FindPropertyRelative("image"), GUIContent.none);
	
	EditorGUI.PropertyField(
        new Rect(rect.x + (labelWidth * 2.5f), rect.y + (EditorGUIUtility.singleLineHeight * 3) + 15, rect.width - (labelWidth * 2.5f), EditorGUIUtility.singleLineHeight),
        element.FindPropertyRelative("floor"), GUIContent.none);
	
	EditorGUI.PropertyField(
		new Rect(rect.x + (labelWidth * 2.5f), rect.y + (EditorGUIUtility.singleLineHeight * 4) + 20, rect.width - (labelWidth * 2.5f), EditorGUIUtility.singleLineHeight),
        element.FindPropertyRelative("disableYSnapping"), GUIContent.none);  
	
	EditorGUI.PropertyField(
		new Rect(rect.x + (labelWidth * 2.5f), rect.y + (EditorGUIUtility.singleLineHeight * 5) + 25, rect.width - (labelWidth * 2.5f), EditorGUIUtility.singleLineHeight),
        element.FindPropertyRelative("furniture"), GUIContent.none); 	
	
    element.FindPropertyRelative("description").stringValue = EditorGUI.TextArea(
		new Rect(rect.x, rect.y + (EditorGUIUtility.singleLineHeight * 6) + 30, rect.width, EditorGUIUtility.singleLineHeight * 3),
		element.FindPropertyRelative("description").stringValue);
		
	EditorGUI.PropertyField(
        new Rect(rect.x + (labelWidth * 2), rect.y + (EditorGUIUtility.singleLineHeight * 9) + 35, 15, EditorGUIUtility.singleLineHeight),
        element.FindPropertyRelative("requiresResources"), GUIContent.none);
		
	if(managerScript.pieces[index].requiresResources){
		EditorGUI.PropertyField(
			new Rect(rect.x + (labelWidth * 2) + 20, rect.y + (EditorGUIUtility.singleLineHeight * 9) + 35, labelWidth - 5, EditorGUIUtility.singleLineHeight),
			element.FindPropertyRelative("resourceAmount"), GUIContent.none);
			
		EditorGUI.PropertyField(
			new Rect(rect.x + (labelWidth * 3) + 20, rect.y + (EditorGUIUtility.singleLineHeight * 9) + 35, rect.width - ((labelWidth * 3) + 20), EditorGUIUtility.singleLineHeight),
			element.FindPropertyRelative("resource"), GUIContent.none);
	}
	
	//if there's no prefab, draw a warning
	if(managerScript.pieces[index].prefab == null){
		EditorGUI.HelpBox(new Rect(rect.x + (labelWidth * 2.5f) + 20, rect.y + (EditorGUIUtility.singleLineHeight * 3) + 17, rect.width - ((labelWidth * 2.5f) + 20), EditorGUIUtility.singleLineHeight * 2.5f), "Please add a prefab", MessageType.Warning);
	}
	};
	
	//set height of one element
	list.elementHeightCallback = (index) => { 
		return EditorGUIUtility.singleLineHeight * 13f;
	};
	
	//clear added element
	list.onAddCallback = (ReorderableList l) => {  
    var index = l.serializedProperty.arraySize;
    l.serializedProperty.arraySize++;
    l.index = index;
    var element = l.serializedProperty.GetArrayElementAtIndex(index);
    element.FindPropertyRelative("prefab").objectReferenceValue = null;
	element.FindPropertyRelative("name").stringValue = "No name";
	element.FindPropertyRelative("image").objectReferenceValue = null;
	element.FindPropertyRelative("floor").boolValue = false;
	element.FindPropertyRelative("disableYSnapping").boolValue = false;
	element.FindPropertyRelative("furniture").boolValue = false;
	element.FindPropertyRelative("description").stringValue = "Description";
	};
	
	//show a warning when element is going to be removed
	list.onRemoveCallback = (ReorderableList l) => {  
    if (EditorUtility.DisplayDialog("Remove piece", 
        "Are you sure you want to remove piece?", "Yes", "No")) {
        ReorderableList.defaultBehaviours.DoRemoveButton(l);
    }
	};
	
	//draw list title/header
	list.drawHeaderCallback = (Rect rect) => {  
    EditorGUI.LabelField(rect, "Pieces (" + managerScript.pieces.Count + ")");
	};
	}
	
	public override void OnInspectorGUI(){	
	//set space + color
	GUILayout.Space(10);
	GUI.color = new Color(0.9f, 0.9f, 0.9f, 1);
	
	//create a button to show pieces
	if(GUILayout.Button("Edit pieces", GUILayout.Height(25))){
	showPieces.target = !showPieces.target;
	}
	
	//set differend color and begin group
	GUI.color = new Color(0.9f, 0.9f, 0.9f, 1);
	if(EditorGUILayout.BeginFadeGroup(showPieces.faded)){
	
	//show a clear all button that clears all pieces
	if(GUILayout.Button("Clear all", GUILayout.Height(20)) && 
	EditorUtility.DisplayDialog("Clear all pieces",
	"Are you sure you want to clear all pieces?", "Yes", "No")){
	managerScript.pieces.Clear();
	}	
	
	//draw reorderable list
	serializedObject.Update();
    list.DoLayoutList();
    serializedObject.ApplyModifiedProperties();
	}
	
	//end fade group
	EditorGUILayout.EndFadeGroup();
	
	//show a settings button and start a settings fade group
	if(GUILayout.Button("Settings", GUILayout.Height(25))){
	showSettings.target = !showSettings.target;
	}
	
	GUI.color = new Color(1, 1, 1, 1);
	
	if(EditorGUILayout.BeginFadeGroup(showSettings.faded)){
	GUILayout.Space(10);
	
	//draw normal inspector
	DrawDefaultInspector();
	
	//set new color and delete playerprefs button
	GUI.color = new Color(0.6f, 0.6f, 0.6f, 1);
	if(GUILayout.Button("Delete Data", GUILayout.Height(20)) && 
	EditorUtility.DisplayDialog("Delete Data",
	"Are you sure you want to delete PlayerPrefs to reset game?", "Delete", "Don't delete")){
	//delete player prefs
	PlayerPrefs.DeleteAll();
	}
	}
	//end group
	EditorGUILayout.EndFadeGroup();
	
	//apply modifications
	serializedObject.ApplyModifiedProperties(); 
	//undo funtionality
	Undo.RecordObject(managerScript, "change in uBuild manager");
	}
}
