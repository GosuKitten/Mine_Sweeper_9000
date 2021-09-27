using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColorGetter : MonoBehaviour
{
    enum GraphicType { Sprite, Text, Image, Button, TextButton, InputField, Toggle, Camera };
    [SerializeField]
    GraphicType type;

    [SerializeField]
    bool useSecondaryColor = false;
    [SerializeField]
    bool useFlagColor = false;

    PaletteHolder holder;

    // Start is called before the first frame update
    void Start()
    {
        holder = PaletteHolder.instance;
        holder.OnUpdateColors += Holder_OnUpdateColors;

        Holder_OnUpdateColors();
    }

    private void Holder_OnUpdateColors()
    {
        switch (type)
        {
            case GraphicType.Sprite:
                SetSpriteColors();
                break;
            case GraphicType.Text:
                SetTextColors();
                break;
            case GraphicType.Image:
                SetImageColors();
                break;
            case GraphicType.Button:
                SetButtonColors();
                break;
            case GraphicType.TextButton:
                SetTextButtonColors();
                break;
            case GraphicType.InputField:
                SetInputFieldColors();
                break;
            case GraphicType.Toggle:
                SetToggleColors();
                break;
            case GraphicType.Camera:
                SetCameraColors();
                break;
            default:
                break;
        }
    }

    void SetSpriteColors()
    {
        Color color = holder.GetPrimaryColor();
        if (useSecondaryColor) color = holder.GetSecondaryColor();
        else if (useFlagColor) color = holder.GetFlagColor();

        GetComponent<SpriteRenderer>().color = color;

    }

    void SetTextColors()
    {
        Color color = holder.GetPrimaryColor();
        if (useSecondaryColor) color = holder.GetSecondaryColor();
        else if (useFlagColor) color = holder.GetFlagColor();

        GetComponent<Text>().color = color;
    }

    void SetImageColors()
    {
        Color color = holder.GetPrimaryColor();
        if (useSecondaryColor) color = holder.GetSecondaryColor();
        else if (useFlagColor) color = holder.GetFlagColor();

        GetComponent<Image>().color = color;
    }

    void SetButtonColors()
    {
        Button button = GetComponent<Button>();
        ColorBlock buttonColors = button.colors;
        buttonColors.normalColor = holder.GetPrimaryColor();
        button.colors = buttonColors;
    }

    void SetTextButtonColors()
    {
        GetComponentInChildren<Text>().color = useSecondaryColor ? holder.GetSecondaryColor() : holder.GetPrimaryColor();
    }

    void SetInputFieldColors()
    {
        InputField field = GetComponent<InputField>();
        ColorBlock fieldColors = field.colors;
        fieldColors.normalColor = holder.GetPrimaryColor();
        field.colors = fieldColors;
    }

    void SetToggleColors()
    {
        Toggle toggle = GetComponent<Toggle>();
        ColorBlock toggleColors = toggle.colors;
        toggleColors.normalColor = holder.GetPrimaryColor();
        toggleColors.pressedColor = holder.GetPrimaryColor();
        toggleColors.selectedColor = holder.GetPrimaryColor();
        toggle.colors = toggleColors;

        toggle.GetComponentInChildren<Text>().color = useSecondaryColor ? holder.GetSecondaryColor() : holder.GetPrimaryColor();
    }

    void SetCameraColors()
    {
        GetComponent<Camera>().backgroundColor = holder.GetBackgroundColor();
    }
}
