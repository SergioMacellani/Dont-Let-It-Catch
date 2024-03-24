using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindowManager : MonoBehaviour
{
    public static WindowManager instance;
    
    public int currentWindow;
    public int startWindow;
    public CanvasGroup[] windows;

    private void Awake()
    {
        if (instance == null) instance = this;
        else if (instance != this) Destroy(gameObject);
    }

    private void Start()
    {
        SetWindow(startWindow);
    }

    public void SetWindow(int i)
    {
        HideWindow(currentWindow);
        ShowWindow(i);
        currentWindow = i;
    }
    
    public void HideWindow(int i)
    {
        windows[i].alpha = 0;
        windows[i].interactable = false;
        windows[i].blocksRaycasts = false;
    }
    
    public void ShowWindow(int i)
    {
        windows[i].alpha = 1;
        windows[i].interactable = true;
        windows[i].blocksRaycasts = true;
    }
    
    public void CloseGame()
    {
        Application.Quit();
    }
    
    public void RestartGame()
    {
        Application.LoadLevel(Application.loadedLevel);
    }

    private void OnValidate()
    {
        currentWindow = Mathf.Clamp(currentWindow, 0, windows.Length - 1);
        startWindow = Mathf.Clamp(startWindow, 0, windows.Length - 1);
        for (int i = 0; i < windows.Length; i++)
        {
            HideWindow(i);
        }
        ShowWindow(currentWindow);
    }
}
