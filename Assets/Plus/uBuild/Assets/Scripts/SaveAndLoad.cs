using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public class SaveAndLoad : MonoBehaviour {
	
	//variables visible in the inspector
	public GameObject character;
	public int saveAfterSeconds;
	public bool saveOnQuit;
	
	//not visible in the inspector
	private uBuildManager managerScript;
	private GameObject doneSavingText;
	
	void Awake(){
		if(!character){
			Debug.LogWarning("Please add a character to the SaveAndLoad script [uBuildManager object]");
			return;
		}
		//find uBuildManager
		managerScript = GetComponent<uBuildManager>();
		//load all data (player position, buildings etc.)
		load();
	}
	
	void Start(){
		//save occasionally
		InvokeRepeating("save", saveAfterSeconds, saveAfterSeconds);
	}
	
	void OnApplicationQuit(){
		//save when application quits or when leaving editor playmode
		if(saveOnQuit){
			GameObject.Find("uBuildManager").GetComponent<SaveAndLoad>().save();
		}
	}
	
	public void save(){
		//check for buildmode
		if(uBuildManager.buildMode){
			//save buildmode state
			PlayerPrefs.SetInt("build mode", 1);
			
			//save camera position
			PlayerPrefs.SetFloat("cam p x", Camera.main.gameObject.transform.position.x);
			PlayerPrefs.SetFloat("cam p y", Camera.main.gameObject.transform.position.y);
			PlayerPrefs.SetFloat("cam p z", Camera.main.gameObject.transform.position.z);
			
			//save camera rotation
			PlayerPrefs.SetFloat("cam r x", Camera.main.gameObject.transform.localEulerAngles.x);
			PlayerPrefs.SetFloat("cam r y", Camera.main.gameObject.transform.localEulerAngles.y);
			PlayerPrefs.SetFloat("cam r z", Camera.main.gameObject.transform.localEulerAngles.z);
		}
		else{
			//if not in buildmode, there's no need to save camera so just save buildmode state
			PlayerPrefs.SetInt("build mode", 0);
		}
		
		//save furniture mode state
		if(uBuildManager.furnitureMode){
			PlayerPrefs.SetInt("furniture mode", 1);
		}
		else{
			PlayerPrefs.SetInt("furniture mode", 0);
		}
		
		//set all pieces active to find and save all of them
		for(int i = 0; i < GetComponent<Layers>().layers.Count; i++){
			foreach(GameObject piece in GetComponent<Layers>().layers[i].layerPieces){
			piece.SetActive(true);
			}
		}
		
		//find all pieces (which have just been activated)
		GameObject[] pieces = GameObject.FindGameObjectsWithTag("Piece");
		//get pieces count
		int piecesCount = pieces.Length;
		//save pieces count
		PlayerPrefs.SetInt("piecesCount", piecesCount);
		
		//for all pieces...
		for(int i = 0; i < piecesCount; i++){
			//save piece position
			PlayerPrefs.SetFloat(i + "p x", pieces[i].transform.position.x);
			PlayerPrefs.SetFloat(i + "p y", pieces[i].transform.position.y);
			PlayerPrefs.SetFloat(i + "p z", pieces[i].transform.position.z);
			
			//save piece rotation
			PlayerPrefs.SetFloat(i + "r x", pieces[i].transform.localEulerAngles.x);
			PlayerPrefs.SetFloat(i + "r y", pieces[i].transform.localEulerAngles.y);
			PlayerPrefs.SetFloat(i + "r z", pieces[i].transform.localEulerAngles.z);
			
			//save piece type (very important, index in the list)
			PlayerPrefs.SetInt(i + "type", pieces[i].GetComponent<PieceTrigger>().type);
			//save the layer this piece is on
			PlayerPrefs.SetInt(i + "layer", pieces[i].GetComponent<PieceTrigger>().layer);
			
			//if piece is a door...
			if(pieces[i].transform.Find("Hinge") != null){
				//save the state of this door (open/closed)
				if(pieces[i].transform.Find("Hinge").gameObject.GetComponent<Door>().open){
				PlayerPrefs.SetInt(i + "door open", 1);
				}
				else{
				PlayerPrefs.SetInt(i + "door open", 0);	
				}
			}
		}
		
		//check for buildmode
		if(uBuildManager.buildMode){
			//for all layers...
			for(int i = 0; i < GetComponent<Layers>().layers.Count; i++){
				//turn pieces active when the layer is toggled on
				if(PlayerPrefs.GetInt("layer" + i) == 0){
					foreach(GameObject piece in GetComponent<Layers>().layers[i].layerPieces){
					piece.SetActive(true);
					}
				}
				//turn pieces not active when the layer is toggled off
				else{
					foreach(GameObject piece in GetComponent<Layers>().layers[i].layerPieces){
					piece.SetActive(false);
					}
				}
			}
		}
		else{
			//if not in build mode, still loop through all layers
			for(int i = 0; i < GetComponent<Layers>().layers.Count; i++){
				//set all pieces active
				foreach(GameObject piece in GetComponent<Layers>().layers[i].layerPieces){
				piece.SetActive(true);
				}
			}
		}
		
		//check if there is a character
		if(character != null){
			
		//save character position
		PlayerPrefs.SetFloat("character p x", character.transform.position.x);
		PlayerPrefs.SetFloat("character p y", character.transform.position.y);
		PlayerPrefs.SetFloat("character p z", character.transform.position.z);
		
		//save character rotation
		PlayerPrefs.SetFloat("character r x", character.transform.localEulerAngles.x);
		PlayerPrefs.SetFloat("character r y", character.transform.localEulerAngles.y);
		PlayerPrefs.SetFloat("character r z", character.transform.localEulerAngles.z);
		}
		
		//save the amount of layers
		PlayerPrefs.SetInt("layerCount", GameObject.Find("uBuildManager").GetComponent<Layers>().layers.Count);
		//for all layers...
		for(int i = 0; i < GameObject.Find("uBuildManager").GetComponent<Layers>().layers.Count; i++){
			//get layer name
			string layerName = GameObject.Find("uBuildManager").GetComponent<Layers>().layers[i].name;
			//save layername
			PlayerPrefs.SetString("L" + i + "N", layerName);
		}
		
		//save playerprefs
		PlayerPrefs.Save();
	}
	
	public void load(){
		//check buildmode
		if(PlayerPrefs.GetInt("build mode") != 0){
			//set build mode to true
			GetComponent<uBuildManager>().buildModeTrue();
			
			//check furnituremode to see if it's active
			if(PlayerPrefs.GetInt("furniture mode") == 0){
				uBuildManager.furnitureMode = false;
			}
			else{
				uBuildManager.furnitureMode = true;
			}
		}
		
		//add layers
		GetComponent<Layers>().addLayers();
		
		//get the amount of pieces to instantiate
		int piecesCount = PlayerPrefs.GetInt("piecesCount");
		
		//check if there are saved pieces
		if(piecesCount > 0){
		//for each piece...
		for(int i = 0; i < piecesCount; i++){
			//get the piece type
			int pieceType = PlayerPrefs.GetInt(i + "type");
			//get piece layer
			int pieceLayer = PlayerPrefs.GetInt(i + "layer");
			//get piece rotation
			Quaternion pieceRotation = Quaternion.Euler(PlayerPrefs.GetFloat(i + "r x"), PlayerPrefs.GetFloat(i + "r y"), PlayerPrefs.GetFloat(i + "r z"));
			//get piece position
			Vector3 piecePosition = new Vector3(PlayerPrefs.GetFloat(i + "p x"), PlayerPrefs.GetFloat(i + "p y"), PlayerPrefs.GetFloat(i + "p z"));
			
			//actually add the piece based on its type
			GameObject loadedPiece = Instantiate(managerScript.pieces[pieceType].prefab, piecePosition, pieceRotation) as GameObject;
			//set piece type
			loadedPiece.GetComponent<PieceTrigger>().type = pieceType;
			//set piece layer
			loadedPiece.GetComponent<PieceTrigger>().layer = pieceLayer;
			//check if piece has a layer
			if(pieceLayer != 0){
			//add piece to the layer list
			GameObject.Find("uBuildManager").GetComponent<Layers>().layers[pieceLayer - 1].layerPieces.Add(loadedPiece);
			}
			
			//check if piece is a door
			if(loadedPiece.transform.Find("Hinge") != null){
				//check if door should be open and open/close it
				if(PlayerPrefs.GetInt(i + "door open") == 0){
				loadedPiece.transform.Find("Hinge").gameObject.GetComponent<Door>().open = false;
				}
				else{
				loadedPiece.transform.Find("Hinge").gameObject.GetComponent<Door>().open = true;
				}
			}
			//give piece the correct tag
			loadedPiece.tag = "Piece";
		}
		}
		
		//get character position and rotation and apply it to the character
		character.transform.position = new Vector3(PlayerPrefs.GetFloat("character p x"), PlayerPrefs.GetFloat("character p y"), PlayerPrefs.GetFloat("character p z"));
		character.transform.localEulerAngles = new Vector3(PlayerPrefs.GetFloat("character r x"), PlayerPrefs.GetFloat("character r y"), PlayerPrefs.GetFloat("character r z"));
		
		//for all layers...
		for(int i = 0; i < GetComponent<Layers>().layers.Count; i++){
			//get layer toggle
			Toggle toggle = GetComponent<Layers>().layers[i].layerUI.transform.Find("State").gameObject.GetComponent<Toggle>();
			//check if layer is on/off and apply it to the toggle
			if(PlayerPrefs.GetInt("layer" + i) == 0){
				toggle.isOn = true;
			}
			else{
				toggle.isOn = false;
			}
		}
		
		//check for build mode
		if(uBuildManager.buildMode){
			//for all layers...
			for(int i = 0; i < GetComponent<Layers>().layers.Count; i++){
				//check if layer is on/off and set pieces on/off
				if(PlayerPrefs.GetInt("layer" + i) == 0){
					foreach(GameObject piece in GetComponent<Layers>().layers[i].layerPieces){
					piece.SetActive(true);
					}
				}
				else{
					foreach(GameObject piece in GetComponent<Layers>().layers[i].layerPieces){
					piece.SetActive(false);
					}
				}
			}
			//get camera position and rotation
			Camera.main.gameObject.transform.position = new Vector3(PlayerPrefs.GetFloat("cam p x"), PlayerPrefs.GetFloat("cam p y"), PlayerPrefs.GetFloat("cam p z"));
			Camera.main.gameObject.transform.localEulerAngles = new Vector3(PlayerPrefs.GetFloat("cam r x"), PlayerPrefs.GetFloat("cam r y"), PlayerPrefs.GetFloat("cam r z"));
		}
		//if not in build mode
		else{
			//set all pieces active
			for(int i = 0; i < GetComponent<Layers>().layers.Count; i++){
				foreach(GameObject piece in GetComponent<Layers>().layers[i].layerPieces){
				piece.SetActive(true);
				}
			}
		}
	}
}
