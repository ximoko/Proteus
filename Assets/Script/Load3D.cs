using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Load3D : MonoBehaviour {

	// Use this for initialization
    void Start()
    {
        // Only specifying the sceneName or sceneBuildIndex will load the Scene with the Single mode
        SceneManager.LoadScene("2", LoadSceneMode.Additive);
    }
}
