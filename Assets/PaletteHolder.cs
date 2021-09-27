using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaletteHolder : MonoBehaviour
{
    public static PaletteHolder instance;

    public ColorPalette[] palettes;
    int colorPaletteIndex;

    public delegate void UpdateColors();
    public event UpdateColors OnUpdateColors;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(this);
    }

    private void Start()
    {
        SetColorPalette(0);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SetColorPalette(1);
        }
    }

    public void SetColorPalette(int index)
    {
        colorPaletteIndex = index;
        OnUpdateColors?.Invoke();
    }

    public Color GetPrimaryColor()
    {
        return palettes[colorPaletteIndex].primary;
    }

    public Color GetSecondaryColor()
    {
        return palettes[colorPaletteIndex].secondary;
    }

    public Color GetFlagColor()
    {
        return palettes[colorPaletteIndex].flag;
    }

    public Color GetBackgroundColor()
    {
        return palettes[colorPaletteIndex].background;
    }

    [System.Serializable]
    public struct ColorPalette
    {
        public Color primary;
        public Color secondary;
        public Color flag;
        public Color background;
    }
}
