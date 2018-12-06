using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[System.Serializable]
//layer variables
public class Layer{
	public string name;
	public List<GameObject> layerPieces = new List<GameObject>();
	public GameObject layerUI;
}

public class Layers : MonoBehaviour {
	
	//not visible in the inspector
	[HideInInspector]
	public List<Layer> layers;
	
	//visible in the inspector
	public GameObject layerUI;
	public float defaultLayerResetWarningDuration;
	
	//not visible in the inspector
	GameObject layerCountText;
	GameObject defaultLayerText;
	GameObject defaultLayerResetText;
	
	public static GameObject removeLayerWarning;
	
	int layerToRemove;
	
	void Start(){
		//find some objects and labels
		defaultLayerResetText = GameObject.Find("default layer resetted warning");
		defaultLayerResetText.SetActive(false);
		
		layerCountText = GameObject.Find("layer count");		
		removeLayerWarning = GameObject.Find("Remove layer warning");
		removeLayerWarning.SetActive(false);
		
		defaultLayerText = GameObject.Find("default layer");	
		defaultLayerText.GetComponent<Text>().text = PlayerPrefs.GetInt("defaultLayer").ToString();
	}
	
	void Update(){
		//update the layer UI
		updateLayerUI();
		//show correct layer count on button
		layerCountText.GetComponent<Text>().text = "+ Layer (" + layers.Count + ")";
		
		//add a scaling effect when the layerreset text is active
		if(defaultLayerResetText.activeSelf){
			defaultLayerResetText.GetComponent<RectTransform>().localScale = 
			new Vector2(defaultLayerResetText.GetComponent<RectTransform>().localScale.x + (0.1f * Time.deltaTime), 
			defaultLayerResetText.GetComponent<RectTransform>().localScale.y + (0.1f * Time.deltaTime));
		}
		else{
			//reset scale
			defaultLayerResetText.GetComponent<RectTransform>().localScale = Vector2.one;
		}
	}
	
	//add layer ui (names, buttons, toggles etc.)
	public void addLayers(){
		//for all layers... (uses playerprefs number to see how much to add)
		for(int i = 0; i < PlayerPrefs.GetInt("layerCount"); i++){
			//instantiate new layer and add it to the scrollpanel
			GameObject newLayer = Instantiate(layerUI);
			RectTransform rectTransform = newLayer.GetComponent<RectTransform>();
			rectTransform.SetParent(GameObject.Find("Scroll panel").transform, false);
			
			//add an actual layer to the layerlist and apply the layer ui
			layers.Add(new Layer{layerUI = newLayer});
			//get layer name and show it
			newLayer.GetComponentInChildren<InputField>().text = "" + PlayerPrefs.GetString("L" + i + "N");
			
			//add function to the remove button
			newLayer.transform.Find("Remove button").gameObject.GetComponent<Button>().onClick.AddListener(
			() => { 
			removeLayer(); 
			}
			);
			//add function to the 'add to layer' button
			newLayer.transform.Find("Add to layer button").gameObject.GetComponent<Button>().onClick.AddListener(
			() => { 
			addToLayer(); 
			}
			);
			//add function to the 'remove from layer' button
			newLayer.transform.Find("Remove from layer button").gameObject.GetComponent<Button>().onClick.AddListener(
			() => { 
			removeFromLayer(); 
			}
			);
			
			//add togglelayer function to the toggle's onValueChanged
			newLayer.transform.Find("State").gameObject.GetComponent<Toggle>().onValueChanged.AddListener(
			delegate {toggleLayer ();}
			);
		}
	}
	
	//update the layer ui
	void updateLayerUI(){
		//for all layers...
		for(int i = 0; i < layers.Count; i++){
			//check if the layer has ui, assign a name to the layer ui (not visible in game, just in the hierarchy) and show the layer index
			if(layers[i].layerUI != null){
			layers[i].layerUI.transform.name = "" + i;
			layers[i].name = layers[i].layerUI.GetComponentInChildren<InputField>().text; 
			layers[i].layerUI.transform.Find("Index").gameObject.GetComponent<Text>().text = (i + 1).ToString();
			}
			//if a piece is selected, show the 'add to layer' buttons, to allow players to give the piece a layer
			if(uBuildManager.pieceSelected != null && uBuildManager.pieceSelected.GetComponent<PieceTrigger>().layer == 0){
				layers[i].layerUI.transform.Find("Add to layer button").gameObject.SetActive(true);
				layers[i].layerUI.transform.Find("Remove from layer button").gameObject.SetActive(false);
			}
			//if the selected piece already has a layer...
			else if(uBuildManager.pieceSelected != null){
				//don't show 'add to layer' buttons
				layers[i].layerUI.transform.Find("Add to layer button").gameObject.SetActive(false);
				
				//only show the remove button on the layer which contains the currently selected piece
				if(i == uBuildManager.pieceSelected.GetComponent<PieceTrigger>().layer - 1){
					layers[i].layerUI.transform.Find("Remove from layer button").gameObject.SetActive(true);
				}
				else{
					layers[i].layerUI.transform.Find("Remove from layer button").gameObject.SetActive(false);
				}
			}
			//if no piece is selected, hide both buttons for each layer
			if(uBuildManager.pieceSelected == null){
				layers[i].layerUI.transform.Find("Add to layer button").gameObject.SetActive(false);
				layers[i].layerUI.transform.Find("Remove from layer button").gameObject.SetActive(false);
			}
		}
	}
	
	//add a new layer
	public void addLayer(){
		//instantiate layer ui and add it to the scroll list
		GameObject newLayer = Instantiate(layerUI);
		RectTransform rectTransform = newLayer.GetComponent<RectTransform>();
		rectTransform.SetParent(GameObject.Find("Scroll panel").transform, false);
		
		//add an actual layer to the layerlist and apply the layer ui
		layers.Add(new Layer{layerUI = newLayer});
		
		//add function to the remove button
		newLayer.transform.Find("Remove button").gameObject.GetComponent<Button>().onClick.AddListener(
			() => { 
			removeLayer(); 
			}
		);
		//add function to the 'add to layer' button
		newLayer.transform.Find("Add to layer button").gameObject.GetComponent<Button>().onClick.AddListener(
			() => { 
			addToLayer(); 
			}
		);
		//add function to the 'remove from layer' button
		newLayer.transform.Find("Remove from layer button").gameObject.GetComponent<Button>().onClick.AddListener(
			() => { 
			removeFromLayer(); 
			}
		);
		
		//add togglelayer function to the toggle's onValueChanged
		newLayer.transform.Find("State").gameObject.GetComponent<Toggle>().onValueChanged.AddListener(
			delegate {toggleLayer ();}
		);
		
		//save layer state immidiately
		PlayerPrefs.SetInt("layer" + layers.Count, 0);
	}
	
	////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////
	//REMOVE LAYER OPTIONS
	
	public void removeLayer(){
		//check if we're really pressing the remove button
		if(EventSystem.current.currentSelectedGameObject.name == "Remove button"){
			//chech which layer to remove by getting the objects name (= index)
			layerToRemove = int.Parse(EventSystem.current.currentSelectedGameObject.transform.parent.gameObject.name);
			
			//if the layer contains pieces show warning
			if(layers[layerToRemove].layerPieces.Count != 0){	
			Time.timeScale = 0;
			removeLayerWarning.SetActive(true);
			}
			else{
				//if it does not contain any pieces...
				//change default layer
				if(layerToRemove + 1 == PlayerPrefs.GetInt("defaultLayer")){
					PlayerPrefs.SetInt("defaultLayer", 0);
					defaultLayerText.GetComponent<Text>().text = "0";
					StartCoroutine(defaultLayerResettedWarning());
				}
				else if(layerToRemove + 1 < PlayerPrefs.GetInt("defaultLayer")){
					PlayerPrefs.SetInt("defaultLayer", PlayerPrefs.GetInt("defaultLayer") - 1);
					defaultLayerText.GetComponent<Text>().text = PlayerPrefs.GetInt("defaultLayer").ToString();
				}
				//destroy layer ui
				Destroy(layers[layerToRemove].layerUI);
				//remove layer
				layers.RemoveAt(layerToRemove);
				
				//change the layers of the other pieces (-1)
				changePieceLayers();
			}
		}
	}
	
	//cancel removing the layer
	public void removeLayerCancel(){
		removeLayerWarning.SetActive(false);
		Time.timeScale = 1;
	}
	
	//remove layer and reset pieces layers
	public void removeLayerReset(){
		//reset default layer if this is default layer
		if(layerToRemove + 1 == PlayerPrefs.GetInt("defaultLayer")){
			PlayerPrefs.SetInt("defaultLayer", 0);
			defaultLayerText.GetComponent<Text>().text = "0";
			StartCoroutine(defaultLayerResettedWarning());
		}
		//change default layer
		else if(layerToRemove + 1 < PlayerPrefs.GetInt("defaultLayer")){
			PlayerPrefs.SetInt("defaultLayer", PlayerPrefs.GetInt("defaultLayer") - 1);
			defaultLayerText.GetComponent<Text>().text = PlayerPrefs.GetInt("defaultLayer").ToString();
		}
		//reset piece layers
		foreach(GameObject piece in layers[layerToRemove].layerPieces){
			piece.GetComponent<PieceTrigger>().layer = 0;
		}
		//destroy ui of the layer
		Destroy(layers[layerToRemove].layerUI);
		//remove the layer
		layers.RemoveAt(layerToRemove);
		
		//change the layers of the other pieces (-1)
		changePieceLayers();
		
		//close warning
		removeLayerWarning.SetActive(false);
		Time.timeScale = 1;
	}
	
	//remove layer and put pieces in default layer
	public void removeLayerDefault(){
		//reset default layer if this is default layer
		if(layerToRemove + 1 == PlayerPrefs.GetInt("defaultLayer")){
			PlayerPrefs.SetInt("defaultLayer", 0);
			defaultLayerText.GetComponent<Text>().text = "0";
			StartCoroutine(defaultLayerResettedWarning());
		}
		//change default layer
		else if(layerToRemove + 1 < PlayerPrefs.GetInt("defaultLayer")){
			PlayerPrefs.SetInt("defaultLayer", PlayerPrefs.GetInt("defaultLayer") - 1);
			defaultLayerText.GetComponent<Text>().text = PlayerPrefs.GetInt("defaultLayer").ToString();
		}
		
		//temporary list to hold pieces that will get default layer
		List<GameObject> addPieces = new List<GameObject>();
		
		//add pieces to the temporary list
		foreach(GameObject piece in layers[layerToRemove].layerPieces){
			piece.GetComponent<PieceTrigger>().layer = PlayerPrefs.GetInt("defaultLayer");
			if(PlayerPrefs.GetInt("defaultLayer") != 0){
				addPieces.Add(piece);
			}
		}
		//remove layer ui
		Destroy(layers[layerToRemove].layerUI);
		//remove layer
		layers.RemoveAt(layerToRemove);
		
		//add the pieces from the temporary list to the default layer
		foreach(GameObject piece in addPieces){
			layers[piece.GetComponent<PieceTrigger>().layer - 1].layerPieces.Add(piece);
		}
		//change the layers of the other pieces (-1)
		changePieceLayers();
		
		//close warning
		removeLayerWarning.SetActive(false);
		Time.timeScale = 1;
	}
	
	//remove layer and remove pieces as well
	public void removeLayerRemove(){
		//reset default layer if this is default layer
		if(layerToRemove + 1 == PlayerPrefs.GetInt("defaultLayer")){
			PlayerPrefs.SetInt("defaultLayer", 0);
			defaultLayerText.GetComponent<Text>().text = "0";
			StartCoroutine(defaultLayerResettedWarning());
		}
		//change default layer
		else if(layerToRemove + 1 < PlayerPrefs.GetInt("defaultLayer")){
			PlayerPrefs.SetInt("defaultLayer", PlayerPrefs.GetInt("defaultLayer") - 1);
			defaultLayerText.GetComponent<Text>().text = PlayerPrefs.GetInt("defaultLayer").ToString();
		}
		
		//destroy all pieces in the layer
		foreach(GameObject piece in layers[layerToRemove].layerPieces){
			Destroy(piece);
		}
		//remove layer ui
		Destroy(layers[layerToRemove].layerUI);
		//remove layer
		layers.RemoveAt(layerToRemove);
		
		//change the layers of the other pieces (-1)
		changePieceLayers();
		
		//close warning
		removeLayerWarning.SetActive(false);
		Time.timeScale = 1;
	}
	
	//END OF THE REMOVE LAYER OPTIONS
	////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////
	
	public void addToLayer(){
		//add piece to layer
		//check if the add to layer button was really pressed
		if(EventSystem.current.currentSelectedGameObject.name == "Add to layer button"){
		//get layer by checking object name (= index)
		int layer = int.Parse(EventSystem.current.currentSelectedGameObject.transform.parent.gameObject.name);
		
		//add piece to the layer list
		layers[layer].layerPieces.Add(uBuildManager.pieceSelected);
		//set piece layer (to save it correctly)
		uBuildManager.pieceSelected.GetComponent<PieceTrigger>().layer = layer + 1;
		}
	}
	
	//remove piece from layer
	public void removeFromLayer(){
		//check if the remove button was really pressed
		if(EventSystem.current.currentSelectedGameObject.name == "Remove from layer button"){
		//get layer
		int layer = int.Parse(EventSystem.current.currentSelectedGameObject.transform.parent.gameObject.name);
		//remove piece from layer
		layers[layer].layerPieces.Remove(uBuildManager.pieceSelected);
		//reset piece layer
		uBuildManager.pieceSelected.GetComponent<PieceTrigger>().layer = 0;
		}
	}
	
	//move default layer up
	public void defaultLayerUp(){
		if(PlayerPrefs.GetInt("defaultLayer") < layers.Count){
			PlayerPrefs.SetInt("defaultLayer", PlayerPrefs.GetInt("defaultLayer") + 1);
			defaultLayerText.GetComponent<Text>().text = PlayerPrefs.GetInt("defaultLayer").ToString();
		}
	}
	//move default layer down
	public void defaultLayerDown(){
		if(PlayerPrefs.GetInt("defaultLayer") > 0){
			PlayerPrefs.SetInt("defaultLayer", PlayerPrefs.GetInt("defaultLayer") - 1);
			defaultLayerText.GetComponent<Text>().text = PlayerPrefs.GetInt("defaultLayer").ToString();
		}
	}
	
	//toggle layer on/off
	public void toggleLayer(){
		//check if a toggle was pressed
		if(EventSystem.current.currentSelectedGameObject != null){
		//get toggle object
		GameObject toggleObject = EventSystem.current.currentSelectedGameObject;
			//check if its a layer toggle
			if(toggleObject.name == "State"){
			//get layer
			int toggledLayer = int.Parse(EventSystem.current.currentSelectedGameObject.transform.parent.gameObject.name);
				//if layer is now on
				if(toggleObject.GetComponent<Toggle>().isOn){
					//set pieces active
					foreach(GameObject piece in layers[toggledLayer].layerPieces){
						piece.SetActive(true);
					}
					//save state
					PlayerPrefs.SetInt("layer" + toggledLayer, 0);
				}
				else{
					//set piece not active
					foreach(GameObject piece in layers[toggledLayer].layerPieces){
						piece.SetActive(false);
					}
					//save state
					PlayerPrefs.SetInt("layer" + toggledLayer, 1);
				}
			}
		}
	}
	
	//change piece layers
	void changePieceLayers(){
		//for all layers...
		for(int i = 0; i < layers.Count; i++){
			//get pieces, check layer position and move the layer down
			foreach(GameObject piece in layers[i].layerPieces){
				if(piece.GetComponent<PieceTrigger>().layer > layerToRemove + 1){
					piece.GetComponent<PieceTrigger>().layer--;
				}
			}
		}
	}
	
	//warning when default layer is resetted to 0
	IEnumerator defaultLayerResettedWarning(){
		//enable label, wait, disable label
		defaultLayerResetText.SetActive(true);
		yield return new WaitForSeconds(defaultLayerResetWarningDuration);
		defaultLayerResetText.SetActive(false);
	}
}
