using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraManager : MonoBehaviour
{
    bool dragging = false;
    Vector3 mouseDownPos;

    [SerializeField]
    float cameraSizeMax = 25f;
    [SerializeField]
    float cameraSizeMin = 5f;
    [SerializeField]
    float cameraSize = 10f;
    [SerializeField]
    float scrollSpeed = 8f;
    [SerializeField]
    public bool invertZoom = false;

    Vector3 startingPos;

    [SerializeField]
    float shakeStrength = 1;
    [SerializeField]
    float shakeTimeTotal = 1;
    float shakeTime = 0;

    bool mainMenuOpen = false;
    Camera cam;

    [SerializeField]
    AnimationCurve shakeCurve;

    private void Awake()
    {
        GameObject.Find("InvertZoomDirToggle").GetComponent<Toggle>().isOn = invertZoom;

        cam = GetComponent<Camera>();
    }

    void Start()
    {
        GameManager.OnLostGame += GameManager_OnLostGame;
        GameManager.OnMainMenuToggled += GameManager_OnMainMenuToggled;
        GameManager.OnNewGame += GameManager_OnNewGame;
    }

    private void GameManager_OnNewGame(int w, int h)
    {
        transform.position = new Vector3((w / 2f) - .5f, h / 2f, -10f);

        float newCameraSize = (float)((h + (h * .20)) / 2);
        cameraSize = newCameraSize + .5f;
        cameraSizeMax = newCameraSize + 10;
    }

    // Update is called once per frame
    void Update()
    {
        Drag();
        Zoom();

        if (shakeTime != 0) Shake();
    }

    public void InvertCameraZoomToggle(bool state)
    {
        invertZoom = state;
    }

    void Drag()
    {
        if (!mainMenuOpen)
        {
            if (Input.GetMouseButtonDown(2))
            {
                dragging = true;
                mouseDownPos = GetComponent<Camera>().ScreenToWorldPoint(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(2))
            {
                dragging = false;
            }
            else if (dragging)
            {
                Vector3 mouseDragPos = GetComponent<Camera>().ScreenToWorldPoint(Input.mousePosition);
                transform.position = transform.position + (mouseDownPos - mouseDragPos);
            }
        }
        else dragging = false;
    }

    void Zoom()
    {
        if (!mainMenuOpen)
        {
            float invert = (invertZoom) ? 1 : -1;
            cameraSize += Input.GetAxis("Mouse ScrollWheel") * scrollSpeed * invert;
            cameraSize = Mathf.Clamp(cameraSize, cameraSizeMin, cameraSizeMax);
            GetComponent<Camera>().orthographicSize = cameraSize;
        }
    }

    void Shake()
    {
        float curve = shakeCurve.Evaluate(shakeTime / shakeTimeTotal);
        float x = startingPos.x + (Random.Range(-shakeStrength, shakeStrength) * curve);
        float y = startingPos.y + (Random.Range(-shakeStrength, shakeStrength) * curve);

        transform.position = new Vector3(x, y, startingPos.z);

        shakeTime -= Time.deltaTime;
        if (shakeTime <= 0)
        {
            shakeTime = 0;
            transform.position = startingPos;
        }
    }

     void GameManager_OnMainMenuToggled(bool state)
    {
        mainMenuOpen = state;
    }

    void GameManager_OnLostGame()
    {
        startingPos = transform.position;
        shakeTime = shakeTimeTotal;
    }
}
