using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour {

    Animator anim;

    public string newGameSceneName;
    public int quickSaveSlotID;

    [Header("Options Panel")]
    public GameObject MainOptionsPanel;
    public GameObject StartGameOptionsPanel3D;
    public GameObject StartGameOptionsPanelVR;
    public GameObject StartGameOptionsPanelBus;
    public GameObject ReconfigPanel;
    public GameObject GamePanel;
    public GameObject ControlsPanel;
    public GameObject GfxPanel;
    public GameObject LoadGamePanel;


    private void Awake()
    {
        if (!EasyAudioUtility.instance)
            Instantiate(Resources.Load("Prefabs/EasyAudioUtility"));
    
    }

    // Use this for initialization
    void Start () {
        anim = GetComponent<Animator>();

        //new key
    #if !EMM_ES2
        PlayerPrefs.SetInt("quickSaveSlot", quickSaveSlotID);
    #else
        ES2.Save(quickSaveSlotID, "quickSaveSlot");
    #endif
    }

    #region Open Different panels

    public void openOptions()
    {
        //enable respective panel
        MainOptionsPanel.SetActive(true);
        StartGameOptionsPanel3D.SetActive(false);
        StartGameOptionsPanelVR.SetActive(false);
        StartGameOptionsPanelBus.SetActive(false);
        ReconfigPanel.SetActive(false);

        //play anim for opening main options panel
        anim.Play("buttonTweenAnims_on");

        //play click sfx
        playClickSound();

        //enable BLUR
        //Camera.main.GetComponent<Animator>().Play("BlurOn");
       
    }

    public void openStartGameOptions3D()
    {
        //enable respective panel
        MainOptionsPanel.SetActive(false);
        StartGameOptionsPanel3D.SetActive(true);
        StartGameOptionsPanelVR.SetActive(false);
        StartGameOptionsPanelBus.SetActive(false);
        ReconfigPanel.SetActive(false);

        //play anim for opening main options panel
        anim.Play("buttonTweenAnims_on");

        //play click sfx
        playClickSound();

        //enable BLUR
        //Camera.main.GetComponent<Animator>().Play("BlurOn");
        
    }
    public void openStartGameOptionsVR()
    {
        //enable respective panel
        MainOptionsPanel.SetActive(false);
        StartGameOptionsPanelVR.SetActive(true);
        StartGameOptionsPanel3D.SetActive(false);
        StartGameOptionsPanelBus.SetActive(false);
        ReconfigPanel.SetActive(false);

        //play anim for opening main options panel
        anim.Play("buttonTweenAnims_on");

        //play click sfx
        playClickSound();

        //enable BLUR
        //Camera.main.GetComponent<Animator>().Play("BlurOn");
        
    }
    public void openStartGameOptionsBus()
    {
        //enable respective panel
        MainOptionsPanel.SetActive(false);
        StartGameOptionsPanelBus.SetActive(true);
        StartGameOptionsPanel3D.SetActive(false);
        StartGameOptionsPanelVR.SetActive(false);
        ReconfigPanel.SetActive(false);

        //play anim for opening main options panel
        anim.Play("buttonTweenAnims_on");

        //play click sfx
        playClickSound();

        //enable BLUR
        //Camera.main.GetComponent<Animator>().Play("BlurOn");
        
    }

    public void openReconfig()
    {
        //enable respective panel
        MainOptionsPanel.SetActive(false);
        StartGameOptionsPanel3D.SetActive(false);
        StartGameOptionsPanelVR.SetActive(false);
        StartGameOptionsPanelBus.SetActive(false);
        ReconfigPanel.SetActive(true);

        //play anim for opening main options panel
        anim.Play("OptTweenAnim_off");

        //play click sfx
        playClickSound();

    }

    public void openOptions_Game()
    {
        //enable respective panel
        GamePanel.SetActive(true);
        ControlsPanel.SetActive(false);
        GfxPanel.SetActive(false);
        LoadGamePanel.SetActive(false);

        //play anim for opening game options panel
        anim.Play("OptTweenAnim_on");

        //play click sfx
        playClickSound();

    }
    public void openOptions_Controls()
    {
        //enable respective panel
        GamePanel.SetActive(false);
        ControlsPanel.SetActive(true);
        GfxPanel.SetActive(false);
        LoadGamePanel.SetActive(false);

        //play anim for opening game options panel
        anim.Play("OptTweenAnim_on");

        //play click sfx
        playClickSound();

    }
    public void openOptions_Gfx()
    {
        //enable respective panel
        GamePanel.SetActive(false);
        ControlsPanel.SetActive(false);
        GfxPanel.SetActive(true);
        LoadGamePanel.SetActive(false);

        //play anim for opening game options panel
        anim.Play("OptTweenAnim_on");

        //play click sfx
        playClickSound();

    }

    public void openContinue_Load()
    {
        //enable respective panel
        GamePanel.SetActive(false);
        ControlsPanel.SetActive(false);
        GfxPanel.SetActive(false);
        LoadGamePanel.SetActive(true);

        //play anim for opening game options panel
        anim.Play("OptTweenAnim_on");

        //play click sfx
        playClickSound();

    }

    public void newGame()
    {
        //if we don't have this component
        if (!GetComponent<LevelSelectManager>())
        {
            //loads a specific scene
            #if !EMM_ES2
            PlayerPrefs.SetString("sceneToLoad", newGameSceneName);
            #else
            ES2.Save(newGameSceneName, "sceneToLoad");
            #endif
        
            //load level via fader
            Fader fader = FindObjectOfType<Fader>();
            fader.FadeIntoLevel("LoadingScreen");

        }
        //open the level select screen
        else
        {
            GetComponent<LevelSelectManager>().openLevelSelect();
        }

        //delete slot id
        #if !EMM_ES2
        PlayerPrefs.DeleteKey("slotLoaded_");
        #else
        ES2.Delete("slotLoaded_");
        #endif

    }
    #endregion

    #region Back Buttons

    public void back_options()
    {
        //simply play anim for CLOSING main options panel
        anim.Play("buttonTweenAnims_off");

        //disable BLUR
       // Camera.main.GetComponent<Animator>().Play("BlurOff");

        //play click sfx
        playClickSound();
    }

    public void back_options_panels()
    {
        //simply play anim for CLOSING main options panel
        anim.Play("OptTweenAnim_off");
        
        //play click sfx
        playClickSound();

    }

    public void Quit()
    {
        Application.Quit();
    }
#endregion

    #region Sounds
    public void playHoverClip()
    {
        EasyAudioUtility.instance.Play("Hover");
    }

    void playClickSound() {
        EasyAudioUtility.instance.Play("Click");
    }


    #endregion
}
