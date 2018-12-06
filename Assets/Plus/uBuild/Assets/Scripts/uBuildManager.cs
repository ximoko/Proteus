using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

//piece variables
[System.Serializable]
public class piece{
	public GameObject prefab;
	public string name;
	public Sprite image;
	public bool floor;
	public bool disableYSnapping;
	public bool furniture;
	public string description;
	public bool requiresResources;
	public string resource;
	public float resourceAmount;
	
	//button UI of this piece
	[HideInInspector]
	public Button button;
}

//face, to detect floor snapping
public enum face{
	none, up, down, east, west, north, south
}

public class uBuildManager : MonoBehaviour {
	
	//variables visible in the inspector (under settings)
	public Color green;
	public Color red;
	public Color selected;
	public Color buttonHighlight;
	public Button button;
	public GameObject piecesList;
	public Vector3 fpCamPosition;
	public Behaviour[] characterScripts;
	public GameObject character;
	public string doorKey;
	
	//not visibible
	[Space(20), HideInInspector]
	public List<piece> pieces;
	
	//variables visible in the inspector (under settings)
	[Space(20)]
	public string buildModeKey;
	public string placeKey;
	public string switchFurnitureModeKey;
	
	[TextArea]
	public string helpTextDefault;
	[TextArea]
	public string helpTextPlacing;
	[TextArea]
	public string helpTextSelected;
	
	//not visibible
	private GameObject currentObject;
	private bool isPlacing;
	private int selectedPiece = 0;
	public static GameObject pieceSelected = null;
	private GameObject helpText;
	private face direction;
	private GameObject doorLabel;
	private GameObject furnitureLabel;
	private GameObject resourcesWarningLabel;
	
	public static bool buildMode;
	public static bool furnitureMode;

	public Camera thirdCamera;
	public Camera mainCamera;
	
	void Start(){
		//Find some objects and show the buttons to select pieces with
		mainCamera = Camera.main;
		helpText = GameObject.Find("help text");
		doorLabel = GameObject.Find("Door open/close text");
		furnitureLabel = GameObject.Find("Furniture label");
		resourcesWarningLabel = GameObject.Find("Resources warning");
		resourcesWarningLabel.SetActive(false);
		addDefaultButtons();
	}
	
	void Update(){
		
		if(!character){
			Debug.LogWarning("Please add a character to the uBuildManager script [uBuildManager object -> settings panel]");
			return;
		}
		
		//check if build mode key is pressed, mouse is not over UI and the remove layer warning is not active
		if(Input.GetKeyDown(buildModeKey) && !EventSystem.current.IsPointerOverGameObject() && !Layers.removeLayerWarning.activeSelf){
			//change buildmode
			if(!buildMode){
				buildModeTrue();
			}
			else{
				buildModeFalse();
			}
		}
		
		//check if door label should be displayed
		checkDoorText();
		
		//check for buildmode
		if(buildMode){
		//change help text
		updateHelpText();
		
		//third person camera mode is false and build camera mode is true
		//changeCamera(false);
		
		//if place key is pressed, instantiate new piece (selected one)
		if(Input.GetKeyDown(placeKey) && !isPlacing && !EventSystem.current.IsPointerOverGameObject() && (!pieces[selectedPiece].requiresResources 
		|| (pieces[selectedPiece].requiresResources && PlayerPrefs.GetFloat(pieces[selectedPiece].resource) >= pieces[selectedPiece].resourceAmount))){
			
		currentObject = Instantiate(pieces[selectedPiece].prefab, Vector3.zero, Quaternion.identity) as GameObject;
		
		PlayerPrefs.SetFloat(pieces[selectedPiece].resource, PlayerPrefs.GetFloat(pieces[selectedPiece].resource) - pieces[selectedPiece].resourceAmount);
		
		//disable collider (temporarily) and set type and layer
		currentObject.GetComponentInChildren<Collider>().enabled = false;
		currentObject.GetComponent<PieceTrigger>().type = selectedPiece;
		currentObject.GetComponent<PieceTrigger>().layer = PlayerPrefs.GetInt("defaultLayer");
			
			//add piece to the correct layer
			if(PlayerPrefs.GetInt("defaultLayer") != 0){
			GetComponent<Layers>().layers[PlayerPrefs.GetInt("defaultLayer") - 1].layerPieces.Add(currentObject);
			}
			//if object is door, disable door collider
			if(currentObject.transform.Find("Hinge") != null){
			currentObject.transform.Find("Hinge").gameObject.transform.Find("Door object").gameObject.GetComponent<Collider>().enabled = false;
			}
			
		//start placing
		isPlacing = true;
		pieceSelected = null;
		}
		else if(Input.GetKeyDown(placeKey) && !isPlacing && !EventSystem.current.IsPointerOverGameObject() && pieces[selectedPiece].requiresResources 
		&& PlayerPrefs.GetFloat(pieces[selectedPiece].resource) < pieces[selectedPiece].resourceAmount){
			StartCoroutine(resourcesWarning(selectedPiece));
		}
		
		
		RaycastHit hit;
		Ray ray = thirdCamera.ScreenPointToRay(Input.mousePosition);
		//ray from mouse position
        if(Physics.Raycast(ray, out hit))
			
		//check if mouse is over object
			if(hit.collider != null){
				//check if we're placing a piece
				if(isPlacing){
				Vector3 pos = Vector3.zero;	
				Vector3 objectScale = currentObject.GetComponent<PieceTrigger>().scale;
				
				//if piece is floor...
				if(pieces[selectedPiece].floor){
					//get face
					direction = GetHitFace(hit);
					
					//if there's a piece above this floor or underneath it, just move it normally
					if(direction == face.up || direction == face.down){
						pos = new Vector3(hit.point.x, hit.point.y + objectScale.y/2, hit.point.z);
					}
					else{
						//if object is not rotated, check where the other pieces are to move this floor accordingly
						if(currentObject.transform.rotation.y == 0 || currentObject.transform.rotation.y == -180 || currentObject.transform.rotation.y == 180){
							if(direction == face.north)
								pos = new Vector3(hit.point.x, hit.point.y + objectScale.y/2, hit.point.z) + new Vector3(0, 0, objectScale.z/2);
						
							if(direction == face.south)
								pos = new Vector3(hit.point.x, hit.point.y + objectScale.y/2, hit.point.z) + new Vector3(0, 0, -objectScale.z/2);
							
							if(direction == face.east)
								pos = new Vector3(hit.point.x, hit.point.y + objectScale.y/2, hit.point.z) + new Vector3(objectScale.x/2, 0, 0);
						
							if(direction == face.west)
								pos = new Vector3(hit.point.x, hit.point.y + objectScale.y/2, hit.point.z) + new Vector3(-objectScale.x/2, 0, 0);
						}
						else{
							//if object is rotated, still check where the other pieces are to move this floor accordingly
							if(direction == face.north)
								pos = new Vector3(hit.point.x, hit.point.y + objectScale.y/2, hit.point.z) + new Vector3(0, 0, objectScale.x/2);
						
							if(direction == face.south)
								pos = new Vector3(hit.point.x, hit.point.y + objectScale.y/2, hit.point.z) + new Vector3(0, 0, -objectScale.x/2);
							
							if(direction == face.east)
								pos = new Vector3(hit.point.x, hit.point.y + objectScale.y/2, hit.point.z) + new Vector3(objectScale.z/2, 0, 0);
						
							if(direction == face.west)
								pos = new Vector3(hit.point.x, hit.point.y + objectScale.y/2, hit.point.z) + new Vector3(-objectScale.z/2, 0, 0);
						}
					}
				}
				else{
					//if the current piece is not a floor, move it normally
					pos = new Vector3(hit.point.x, hit.point.y + objectScale.y/2, hit.point.z);
				}
				
				//move piece with snapping
				pos -= Vector3.one;
				pos /= 0.5f;
				if(!pieces[currentObject.GetComponent<PieceTrigger>().type].disableYSnapping && !pieces[currentObject.GetComponent<PieceTrigger>().type].furniture){
					//normally, use snapping for all directions
					pos = new Vector3(Mathf.Round(pos.x), Mathf.Round(pos.y), Mathf.Round(pos.z));
				}
				else if(pieces[currentObject.GetComponent<PieceTrigger>().type].disableYSnapping && !pieces[currentObject.GetComponent<PieceTrigger>().type].furniture){
					//disable y snapping
					pos = new Vector3(Mathf.Round(pos.x), pos.y, Mathf.Round(pos.z));
				}
				pos *= 0.5f;
				pos += Vector3.one;
				
				//apply position to current piece
				currentObject.transform.position = pos;
				
				//if currentobject is not triggered by another object, make it green and placeable
				if(!currentObject.GetComponent<PieceTrigger>().triggered){
					currentObject.GetComponentInChildren<Renderer>().material.color = green;
					//place piece on mouse click
					if(Input.GetMouseButtonDown(0)){
						StartCoroutine(place());
					}
				}
				else{
					//make it red when it's not placeable
					currentObject.GetComponentInChildren<Renderer>().material.color = red;
				}
				
				//cancel placing when key is pressed
				if(Input.GetKeyDown("delete")){
					cancel();
				}
				
				//update piece rotation
				updateRotation();
				}
				else{
				//if not placing a piece, use mouse button to select pieces
				if(Input.GetMouseButtonDown(0) && hit.collider.gameObject.CompareTag("Piece") && !EventSystem.current.IsPointerOverGameObject()){
					pieceSelected = hit.collider.gameObject;
					//set piece color to selected color
					hit.collider.gameObject.GetComponentInChildren<Renderer>().material.color = selected;
				}
				
				//if there is a selected piece
				if(pieceSelected != null){
					
					int type = pieceSelected.GetComponent<PieceTrigger>().type;
					
					//check if we want to duplicate the piece
					if(Input.GetKeyDown(KeyCode.LeftControl) && (!pieces[type].requiresResources 
					|| (pieces[type].requiresResources && PlayerPrefs.GetFloat(pieces[type].resource) >= pieces[type].resourceAmount))){
						
						PlayerPrefs.SetFloat(pieces[type].resource, PlayerPrefs.GetFloat(pieces[type].resource) - pieces[type].resourceAmount);
						
						//instantiate selected piece to duplicate it
						currentObject = Instantiate(pieceSelected, pieceSelected.transform.position, pieceSelected.transform.rotation) as GameObject;
						//disable collider temporarily
						currentObject.GetComponentInChildren<Collider>().enabled = false;
						//make piece untagged
						currentObject.tag = "Untagged";
						if(currentObject.GetComponent<PieceTrigger>().layer != 0){
						//add the duplicate to it's layer
						GetComponent<Layers>().layers[currentObject.GetComponent<PieceTrigger>().layer - 1].layerPieces.Add(currentObject);
						}
						//if piece is door, disable door collider
						if(currentObject.transform.Find("Hinge") != null){
						currentObject.transform.Find("Hinge").gameObject.transform.Find("Door object").gameObject.GetComponent<Collider>().enabled = false;
						}
						//start placing duplicate
						isPlacing = true;
						pieceSelected = null;
					}
					else if(Input.GetKeyDown(KeyCode.LeftControl) && pieces[type].requiresResources 
					&& PlayerPrefs.GetFloat(pieces[type].resource) < pieces[type].resourceAmount){
						StartCoroutine(resourcesWarning(type));
					}
		
					//check if we want to move the piece
					if(Input.GetKeyDown("m")){
						//set piece to selected piece to move it
						currentObject = pieceSelected;
						//disable collider temporarily
						currentObject.GetComponentInChildren<Collider>().enabled = false;
						//if piece is door, disable door collider
						if(currentObject.transform.Find("Hinge") != null){
						currentObject.transform.Find("Hinge").gameObject.transform.Find("Door object").gameObject.GetComponent<Collider>().enabled = false;
						}
						//make piece untagged
						currentObject.tag = "Untagged";
						//start moving piece
						isPlacing = true;
						pieceSelected = null;
					}
					//check if we want to remove piece
					if(Input.GetKeyDown("delete")){
						
						PlayerPrefs.SetFloat(pieces[type].resource, PlayerPrefs.GetFloat(pieces[type].resource) + pieces[type].resourceAmount);
						
						//set piece to selected piece
						currentObject = pieceSelected;
						//remove piece from it's layer
						if(currentObject.GetComponent<PieceTrigger>().layer != 0){
						GetComponent<Layers>().layers[currentObject.GetComponent<PieceTrigger>().layer - 1].layerPieces.Remove(currentObject);
						}
						//destroy current piece and set selected piece to null
						Destroy(currentObject);
						pieceSelected = null;	
					}
					//deselect piece with right mouse button
					if(Input.GetMouseButtonDown(1)){
						pieceSelected = null;	
					}
				}
				}
			}
			//switch furniture mode on key down
			if(Input.GetKeyDown(switchFurnitureModeKey) && !EventSystem.current.IsPointerOverGameObject() && !Layers.removeLayerWarning.activeSelf){
				furnitureMode = !furnitureMode;
				switchFurnitureMode(furnitureMode);
			}
		}
		//else{
			//if we're not in build mode, set camera to third person
		//	changeCamera(true);
		//}
		
		if(currentObject != null){
			//give current piece alpha shader to make it transparent
			currentObject.GetComponent<Renderer>().material.shader = Shader.Find("Unlit/UnlitAlphaWithFade");
		}
		
		if(!isPlacing){
		//if we're not placing a piece, check each piece
		foreach(GameObject piece in GameObject.FindGameObjectsWithTag("Piece")){
			if(piece == pieceSelected){
				//if this is the selected piece, give it the selected color and a transparent shader
				piece.GetComponentInChildren<Renderer>().material.color = selected;
				piece.GetComponent<Renderer>().material.shader = Shader.Find("Unlit/UnlitAlphaWithFade");
			}
			else{
				//if this is not the selected piece, give it a white color and a diffuse shader
				piece.GetComponentInChildren<Renderer>().material.color = Color.white;
				piece.GetComponent<Renderer>().material.shader = Shader.Find("Diffuse");
			}
		}
		}
	}
	
	void updateRotation(){
		//if right mouse button gets pressed, rotate the piece 90 degrees
		//please do not change the rotation angle, this will disturb floor placement
		if(Input.GetMouseButtonDown(1)){
			currentObject.transform.Rotate(Vector3.up, 90, Space.World);
		}
	}
	
	public void buildModeTrue(){
		//set build mode true
		buildMode = true;
		
		//show build mode UI
		GameObject.Find("Pieces list").GetComponent<Animator>().SetTrigger("Slide in");
		GameObject.Find("Help").GetComponent<Animator>().SetTrigger("Slide in");
		
		GameObject.Find("Layers panel").GetComponent<Animator>().SetTrigger("Slide in");
		GameObject.Find("Layers buttons").GetComponent<Animator>().SetTrigger("Slide in");
		
		//for each layer...
		for(int i = 0; i < GetComponent<Layers>().layers.Count; i++){
			//show pieces of this layer if layer is active
			if(PlayerPrefs.GetInt("layer" + i) == 0){
				foreach(GameObject piece in GetComponent<Layers>().layers[i].layerPieces){
				piece.SetActive(true);
				}
			}
			//do not show pieces of this layer when layer isn't active
			else{
				foreach(GameObject piece in GetComponent<Layers>().layers[i].layerPieces){
				piece.SetActive(false);
				}
			}
		}
	}
	
	public void buildModeFalse(){
		//set build mode false
		buildMode = false;
		
		//don't show build mode UI
		GameObject.Find("Pieces list").GetComponent<Animator>().SetTrigger("Slide out");
		GameObject.Find("Help").GetComponent<Animator>().SetTrigger("Slide out");
		
		GameObject.Find("Layers panel").GetComponent<Animator>().SetTrigger("Slide out");
		GameObject.Find("Layers buttons").GetComponent<Animator>().SetTrigger("Slide out");
				
		if(GameObject.Find("Layers panel").GetComponent<RectTransform>().anchoredPosition.x < 0){
		GameObject.Find("Layers panel").GetComponent<Animator>().SetTrigger("Slide out");
		GameObject.Find("Layers buttons").GetComponent<Animator>().SetTrigger("Slide out");
		}
		
		//set all pieces of all layers active
		for(int i = 0; i < GetComponent<Layers>().layers.Count; i++){
			foreach(GameObject piece in GetComponent<Layers>().layers[i].layerPieces){
			piece.SetActive(true);
			}
		}
	}
	
	IEnumerator place(){
		//set piece color to white
		currentObject.GetComponentInChildren<Renderer>().material.color = Color.white;
		//enable piece collider
		currentObject.GetComponentInChildren<Collider>().enabled = true;
			//if piece is door, set door collider true
			if(currentObject.transform.Find("Hinge") != null){
			currentObject.transform.Find("Hinge").gameObject.transform.Find("Door object").gameObject.GetComponent<Collider>().enabled = true;
			}
		
		//we're not placing anything anymore
		isPlacing = false;
		//wait a very small time
		yield return new WaitForSeconds(0.05f);
		//set piece tag to piece (also important to save it)
		currentObject.tag = "Piece";
		currentObject = null;
	}
	
	void cancel(){
		//remove piece from it's layer
		if(currentObject.GetComponent<PieceTrigger>().layer != 0){
			GetComponent<Layers>().layers[currentObject.GetComponent<PieceTrigger>().layer - 1].layerPieces.Remove(currentObject);
		}
		//destroy piece
		Destroy(currentObject);
		//stop placing
		isPlacing = false;
		
		PlayerPrefs.SetFloat(pieces[selectedPiece].resource, PlayerPrefs.GetFloat(pieces[selectedPiece].resource) + pieces[selectedPiece].resourceAmount);
	}
	
	//void changeCamera(bool fp){
		//if first person camera...
	//	if(fp){
			//parent the camera to character, give it proper position & rotation and disable movement script
	//		Camera.main.gameObject.transform.parent = character.transform;
	//		Camera.main.gameObject.transform.localEulerAngles = Vector3.zero;
	//		Camera.main.gameObject.transform.localPosition = fpCamPosition;
	//		Camera.main.GetComponent<CameraMovement>().enabled = false;
			
			//set character scripts true so it can move around
	//		foreach(Behaviour script in characterScripts){
	//			script.enabled = true;
	//		}
	//	}
	//	else{
			//unparent the camera and enable cam movement script
	//		Camera.main.gameObject.transform.parent = null;
	//		Camera.main.GetComponent<CameraMovement>().enabled = true;
			
			//disable character script to make sure it won't move while building
	//		foreach(Behaviour script in characterScripts){
	//			script.enabled = false;
	//		}
	//	}
	//}
	
	void addDefaultButtons(){
		//for all piece types...
		for(int i = 0; i < pieces.Count; i++){
			//add a button to the list of buttons
			Button newButton = Instantiate(button);
			RectTransform rectTransform = newButton.GetComponent<RectTransform>();
			rectTransform.SetParent(piecesList.transform, false);
			
			//set button outline
			newButton.GetComponent<Outline>().effectColor = buttonHighlight;
			
			//set the correct button sprite
			newButton.gameObject.GetComponent<Image>().sprite = pieces[i].image;
			
			//only enable outline for the first button
			if(i == 0){
			newButton.GetComponent<Outline>().enabled = true;
			}
			else{
			newButton.GetComponent<Outline>().enabled = false;	
			}
			
			//set piece description
			if(!pieces[i].requiresResources){
				newButton.transform.Find("Description").gameObject.GetComponentInChildren<Text>().text = pieces[i].description;
			}
			else{
				newButton.transform.Find("Description").gameObject.GetComponentInChildren<Text>().text = pieces[i].description + "\n\n<b>Requires " + pieces[i].resourceAmount + " " + pieces[i].resource + "</b>";
			}
			
			//set button name to its position in the list(important for the button to work later on)
			newButton.transform.name = "" + i;
			
			//add a onclick function to the button with the name to select the proper piece
			newButton.GetComponent<Button>().onClick.AddListener(
			() => { 
			selectPiece(int.Parse(newButton.transform.name)); 
			}
			);
			
			//show piece name
			newButton.GetComponentInChildren<Text>().text = "" + pieces[i].name;
			
			//this is the new button ui
			pieces[i].button = newButton;
		}
		
		//set furnituremode not active
		switchFurnitureMode(furnitureMode);
	}
	
	//update the help text
	void updateHelpText(){
		//show normal help text
		if(pieceSelected == null && !isPlacing){
			helpText.GetComponent<Text>().text = "" + helpTextDefault;
		}
		//show help text for placing a piece
		if(isPlacing){
			helpText.GetComponent<Text>().text = "" + helpTextPlacing;
		}
		//show help text for selected piece
		if(pieceSelected != null){
			helpText.GetComponent<Text>().text = "" + helpTextSelected;
		}
	}
	
	//return faces that were hit to place floors
	static face GetHitFace(RaycastHit hit){
		Vector3 incoming = hit.normal - Vector3.up;
		//south
		if(incoming == new Vector3(0, -1, -1))
			return face.south;
		//north
		if(incoming == new Vector3(0, -1, 1))
			return face.north;
		//up
		if(incoming == new Vector3(0, 0, 0))
			return face.up;
		//down
		if(incoming == new Vector3(1, 1, 1))
			return face.down;
		//west
		if(incoming == new Vector3(-1, -1, 0))
			return face.west;
		//east
		if(incoming == new Vector3(1, -1, 0))
			return face.east;
		//no face
		return face.none;
	}	
	
	void checkDoorText(){
		//display door label? (only when not in build mode)
		bool doorTextTrue = false;
		//check all doors
		foreach(Door door in GameObject.FindObjectsOfType(typeof(Door)) as Door[]){
			//if door is open/closeable set door label true
			if(door.possible){
				doorTextTrue = true;
			}
		}
		
		if(!buildMode && doorTextTrue){
			//set label active
			doorLabel.SetActive(true);
			//show proper text
			doorLabel.GetComponentInChildren<Text>().text = "Press " + doorKey + " to open/close door";
		}
		else{
			//if buildmode or doors are out of reach, don't show door label
			doorLabel.SetActive(false);
		}
	}
	
	//switch between normal build mode & furniture mode
	void switchFurnitureMode(bool FurnitureMode){
		if(FurnitureMode){
			//in furniture mode, only enable furniture buttons
			for(int i = 0; i < pieces.Count; i++){
				if(pieces[i].furniture){
					pieces[i].button.gameObject.SetActive(true);
				}
				else{
					pieces[i].button.gameObject.SetActive(false);
				}
			}
			
			//select first button after switching
			selectFirstPiece();
			//show the furniture label
			furnitureLabel.SetActive(true);
		}
		else{
			//don't enable furniture buttons
			for(int i = 0; i < pieces.Count; i++){
				if(!pieces[i].furniture){
					GameObject.Find("pieces").transform.Find(i.ToString()).gameObject.SetActive(true);
				}
				else{
					GameObject.Find("pieces").transform.Find(i.ToString()).gameObject.SetActive(false);
				}
			}
			
			//select first button after switching
			selectFirstPiece();
			//do not show furniture label
			furnitureLabel.SetActive(false);
		}
	}
	
	void selectFirstPiece(){
		//first button that is currently active
		int firstActiveButton = 0;
		for(int i = 0; i < pieces.Count; i++){
			if(pieces[i].button.gameObject.activeSelf){
				//if this is the active button, set it to be the active one
				firstActiveButton = i;
				//break the loop since we've already got the active button
				break;
			}
		}
		//disable button outline...
		for(int i = 0; i < pieces.Count; i++){
			pieces[i].button.GetComponent<Outline>().enabled = false;	
		}
		//... and enable it for the first button
		pieces[firstActiveButton].button.gameObject.GetComponent<Outline>().enabled = true;
		//set the first active button to be the selected button
		selectedPiece = firstActiveButton;
	}
	
	//select a new piece
	public void selectPiece(int piece){
		//disable all button outlines...
		for(int i = 0; i < pieces.Count; i++){
		pieces[i].button.GetComponent<Outline>().enabled = false;	
		}
		//... and enable it for the selected button
		EventSystem.current.currentSelectedGameObject.GetComponent<Outline>().enabled = true;
		//set selected piece to piece
		selectedPiece = piece;
	}
	
	IEnumerator resourcesWarning(int i){
		resourcesWarningLabel.GetComponent<Text>().text = "Requires " + pieces[i].resourceAmount + " " + pieces[i].resource + "!";
		resourcesWarningLabel.SetActive(true);
		yield return new WaitForSeconds(1);
		resourcesWarningLabel.SetActive(false);
	}
}
