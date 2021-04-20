using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WSceneManager
{
    public static List<Action> OnChangedScene = new List<Action>();

    public static void SwitchScene (string sceneName)
    {
        SceneManager.LoadScene(sceneName);
        var _ocs = OnChangedScene;
        OnChangedScene = new List<Action>();
        foreach (Action a in _ocs) a.Invoke();
        OnChangedScene.Clear();
    }
}
