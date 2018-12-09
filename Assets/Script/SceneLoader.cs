using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
public class SceneLoader : MonoBehaviour
{
    private int scenceName;

    public void Changemenuscene(string sceneName)
    {
        Application.LoadLevel(2);
    }
}