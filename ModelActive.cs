using Microsoft.Win32;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelActive : MonoBehaviour
{
    public GameObject model;
    // Start is called before the first frame update
    void Start()
    {
        model.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        


    }
    public void launchTab()
    {
        model.SetActive(true);
    }
    public void settingTab()
    {
        model.SetActive(false);
    }
    public void producer()
    {
        Application.OpenURL("https://space.bilibili.com/3493124221438600?spm_id_from=333.1007.0.0");
    }
}
