using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UICircularButton : MonoBehaviour
{
    #region Public Fields

    [Header("UI Circular Button")]
    public string Text;
    public Image Icon;
    [HideInInspector]
    public Animator Animator;
    public string AnimatorPressStateName;
    public UnityEvent Actions;

    #endregion

    #region Private Methods

    private void Awake()
    {
        Animator = GetComponent<Animator>();
    }

    #endregion
}