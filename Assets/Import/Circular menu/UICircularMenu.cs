
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UICircularMenu : MonoBehaviour
{
    #region Public Fields

    [System.Serializable]
    public class UICustomCategory
    {
        public string Name;

        public GameObject Content;

        public Text NavigationText;

        [HideInInspector]
        public List<UICircularButton> Buttons = new List<UICircularButton>();
    }

    public enum ControllerType
    {
        Keyboard,
        Gamepad
    }

    [Header("UI Settings")]

    public ControllerType Controller = ControllerType.Keyboard;

    public KeyCode OpenCircularKey = KeyCode.Tab;

    public float ButtonSpacing = 160f;

    public GameObject[] ElementsAtDisabled;

    public int DefaultCategoryIndex;

    public UICustomCategory[] Categories;

    public Image Selection;

    public Color ButtonNormalColor;
    public Color ButtonHoverColor;
    public Color ButtonPressedColor;

    [Header("Gamepad Inputs Settings")]

    public string GamepadInputOpenName = "Open_Circular";
    public string GamepadInputAxisX = "Mouse X";
    public string GamepadInputAxisY = "Mouse Y";

#if WINTERBYTE
    [HideInInspector]
    public CustomBuildingMenu BuildingWheel;
#else
    [Header("Animator Settings")]
    public Animator Animator;

    public string ShowStateName;

    public string HideStateName;
#endif

    [HideInInspector]
    public GameObject SelectedSegment;

    [HideInInspector]
    public UICustomCategory CurrentCategory;

    #endregion

    #region Private Fields

    private bool IsActive = false;

    private List<float> ButtonsRotation = new List<float>();

    private Vector2 MousePosition;
    private Vector2 CircleMousePosition;

    private int CategoryIndex;

    private int Elements;
    private float Fill;
    private int Index;
    private int OldIndex;

#if WINTERBYTE
    private UltimateSurvival.GUISystem.ItemContainer Container;
#endif

    #endregion

    #region Private Methods

    private void Awake()
    {
        #if WINTERBYTE

        BuildingWheel = FindObjectOfType<CustomBuildingMenu>();

        #endif
    }

    private void Start()
    {
        for (int i = 0; i < Categories.Length; i++)
            Categories[i].Buttons = Categories[i].Content.GetComponentsInChildren<UICircularButton>().ToList();

        ChangeIndex(Categories[0].Name);

#if WINTERBYTE
        Container = UltimateSurvival.GUISystem.GUIController.Instance.GetContainer("Inventory");
#endif
    }

    private void Update()
    {
        if (!Application.isPlaying)
            return;

#if WINTERBYTE
        if (BaseBuilder.Instance.CurrentMode == BuildMode.Placement)
        {
            if (BaseBuilder.Instance.CurrentPreview == null)
                return;

            if (BaseBuilder.Instance.CurrentPreview.GetComponent<CustomGenericPart>() == null)
                return;

            if (!BaseBuilder.Instance.CurrentPreview.GetComponent<CustomGenericPart>().HasRequiredPlacementItems())
                BaseBuilder.Instance.ChangeMode(BuildMode.None);
        }

        if (!BuildingWheel.Window.IsOpen)
            return;
#else

        if (Controller == ControllerType.Keyboard ? Input.GetKeyDown(OpenCircularKey) : Input.GetButtonDown(GamepadInputOpenName))
        {
            for (int i = 0; i < ElementsAtDisabled.Length; i++)
                ElementsAtDisabled[i].SetActive(false);

            Animator.CrossFade(ShowStateName, 0.1f);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            IsActive = true;
        }
        else if (Controller == ControllerType.Keyboard ? Input.GetKeyUp(OpenCircularKey) : Input.GetButtonUp(GamepadInputOpenName))
        {
            for (int i = 0; i < ElementsAtDisabled.Length; i++)
                ElementsAtDisabled[i].SetActive(true);

            Animator.CrossFade(HideStateName, 0.1f);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            IsActive = false;
        }

        if (!IsActive)
            return;
#endif

        Selection.fillAmount = Mathf.Lerp(Selection.fillAmount, Fill, .2f);

        Vector3 BoundsScreen = new Vector3((float)Screen.width / 2f, (float)Screen.height / 2f, 0f);

        Vector3 RelativeBounds = Input.mousePosition - BoundsScreen;

        float CurrentRotation = ((Controller == ControllerType.Keyboard) ? Mathf.Atan2(RelativeBounds.x, RelativeBounds.y) : Mathf.Atan2(Input.GetAxis(GamepadInputAxisX), Input.GetAxis(GamepadInputAxisY))) * 57.29578f;

        if (CurrentRotation < 0f)
            CurrentRotation += 360f;

        float CursorRot = -(CurrentRotation - Selection.fillAmount * 360f / 2f);

        float Average = 9999;

        GameObject Nearest = null;

        for (int i = 0; i < Elements; i++)
        {
            GameObject Temp = CurrentCategory.Buttons[i].gameObject;

            Temp.transform.localScale = Vector3.one;

            float Rotation = System.Convert.ToSingle(Temp.name);

            if (Mathf.Abs(Rotation - CurrentRotation) < Average)
            {
                Nearest = Temp;

                Average = Mathf.Abs(Rotation - CurrentRotation);
            }
        }

        SelectedSegment = Nearest;

        CursorRot = -(System.Convert.ToSingle(SelectedSegment.name) - Selection.fillAmount * 360f / 2f);

        Selection.transform.localRotation = Quaternion.Slerp(Selection.transform.localRotation, Quaternion.Euler(0, 0, CursorRot), 15f * Time.deltaTime);

        for (int i = 0; i < Elements; i++)
        {
            UICircularButton Button = CurrentCategory.Buttons[i].GetComponent<UICircularButton>();

            if (Button.gameObject != SelectedSegment)
                Button.Icon.color = Color.Lerp(Button.Icon.color, ButtonNormalColor, 15f * Time.deltaTime);
            else
                Button.Icon.color = Color.Lerp(Button.Icon.color, ButtonHoverColor, 15f * Time.deltaTime);
        }
                        
        CurrentCategory.NavigationText.text = SelectedSegment.GetComponent<UICircularButton>().Text;

        if (Input.GetMouseButtonUp(0))
        {
            SelectedSegment.GetComponent<UICircularButton>().Animator.Play(SelectedSegment.GetComponent<UICircularButton>().AnimatorPressStateName);

            SelectedSegment.GetComponent<UICircularButton>().Actions.Invoke();
        }
    }

    private void RefreshButtons()
    {
        Elements = CurrentCategory.Buttons.Count;

        if (Elements > 0)
        {
            Fill = 1f / (float)Elements;

            float FillRadius = Fill * 360f;

            float LastRotation = 0;

            for (int i = 0; i < Elements; i++)
            {
                GameObject Temp = CurrentCategory.Buttons[i].gameObject;

                float Rotate = LastRotation + FillRadius / 2;

                LastRotation = Rotate + FillRadius / 2;

                Temp.transform.localPosition = new Vector2(ButtonSpacing * Mathf.Cos((Rotate - 90) * Mathf.Deg2Rad), -ButtonSpacing * Mathf.Sin((Rotate - 90) * Mathf.Deg2Rad));

                Temp.transform.localScale = Vector3.one;

                if (Rotate > 360)
                    Rotate -= 360;

                Temp.name = Rotate.ToString();

                ButtonsRotation.Add(Rotate);
            }
        }
    }

#endregion

    #region Public Methods

    public void ChangeIndex(string name)
    {
        DefaultCategoryIndex = Categories.ToList().FindIndex(entry=> entry.Name == name);

        if (DefaultCategoryIndex == -1)
            return;

        CurrentCategory = Categories[DefaultCategoryIndex];

        for (int i = 0; i < Categories.Length; i++)
        {
            if (i != DefaultCategoryIndex)
                Categories[i].Content.SetActive(false);
            else
                Categories[i].Content.SetActive(true);
        }

        RefreshButtons();
    }

#endregion
}
