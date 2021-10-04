using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GoToGameScene : MonoBehaviour
{
    void Update()
    {
        if (Input.anyKey)
            SceneManager.LoadScene("game_scene", LoadSceneMode.Single);
    }
}
