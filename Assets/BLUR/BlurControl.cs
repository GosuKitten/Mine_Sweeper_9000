using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BlurControl : MonoBehaviour
{
    [SerializeField]
    float blurStrength;
    [SerializeField]
    float blurTotalTime;
    float blurCurrentTime;

    float startingBlur;
    float targetBlur;

    Material mat;
    Image image;

    bool mainMenuOpen;


    // Start is called before the first frame update
    void Start()
    {
        GameManager.OnMainMenuToggled += GameManager_OnMainMenuToggled;

        mat = GetComponent<Image>().material;
        image = GetComponent<Image>();
    }

    private void GameManager_OnMainMenuToggled(bool state)
    {
        mainMenuOpen = state;
        blurCurrentTime = blurTotalTime;

        startingBlur = mat.GetFloat("_Size");

        if (state) targetBlur = blurStrength;
        else targetBlur = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (blurCurrentTime > 0)
        {
            if (mainMenuOpen) image.enabled = true;
            blurCurrentTime = Mathf.Clamp(blurCurrentTime, 0, blurCurrentTime - Time.deltaTime);
            mat.SetFloat("_Size", Mathf.Lerp(startingBlur, targetBlur, (blurTotalTime - blurCurrentTime) / blurTotalTime));
        }
        else
        {
            if (!mainMenuOpen) image.enabled = false;
        }
    }
}
