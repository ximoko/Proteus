﻿using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
public class SceneLoader : MonoBehaviour {
  public void SceneSwitcher ()
    {
        Application.LoadLevel (2);
    }
}