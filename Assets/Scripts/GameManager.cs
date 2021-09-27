using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    bool canAreaUncover = true;
    bool canAreaFlag = false;

    public int width;
    public int height;
    public int mineTotal;
    int mineCount;

    int flaggedSpaces;

    bool playing = false;
    bool wonLastGame = false;
    bool startingClick = true;
    bool changesMade = false;

    bool playingTutorial = false;
    bool guidedPlay = false;

    Queue<Vector2> tutPosQueue;
    Vector2 nextTutPos;

    Queue<Actions> tutActionQueue;
    Actions nextTutAction;
    Transform tutPointer;

    [SerializeField]
    int rerolls = 10;
    int rerollCount = 0;

    bool[,] isMine;
    int[,] nums;
    bool[,] isCovered;
    bool[,] isFlagged; 
    bool[,] isAnimated;
    Vector2[,][] neighbors;
    GameObject[,] tileGO;
    GameObject[,] flagGO;
    HashSet<GameObject> mineBGs;

    [SerializeField]
    Transform levelHolder;
    [SerializeField]
    Object gridTile;
    [SerializeField]
    Object coverTile;

    [SerializeField]
    AnimationCurve tileUncoverCurve;
    [SerializeField]
    AnimationCurve areaUncoverCurve;
    [SerializeField]
    AnimationCurve tileFlagCurve;

    [SerializeField]
    Color[] colors = new Color[9];

    [SerializeField]
    Text timerText;
    Stopwatch gameTimer;
    string lastGameTime;

    Camera mainCam;

    GameObject mineTracker;
    GameObject beginningMessage;

    GameObject mainMenu;
    CanvasGroup mainMenuAlpha;
    bool mainMenuOpen = false;

    [SerializeField]
    float totalMenuAlphaTime;
    float currentMenuAlphaTime;

    float startAlpha;
    float endAlpha;

    GameObject mainMenuButtons;
    GameObject optionButtons;

    Text remainingMinesText;

    InputField widthIn;
    InputField heightIn;
    InputField minesIn;
    Text mTag;

    bool validMineDensity = true;
    bool hasBeenWarned = false;
    GameObject warningMessage;
    enum WarningType { MaximumBoardSize, MinimumBoardSize, MineDensity };

    GraphicRaycaster raycaster;
    PointerEventData raycastData;
    EventSystem eventSystem;

    enum Actions { Uncover, Flag, AreaUncover };

    public delegate void MainMenuToggled(bool state);
    public static event MainMenuToggled OnMainMenuToggled;

    public delegate void NewGame(int w, int h);
    public static event NewGame OnNewGame;

    public delegate void WonGame();
    public static event WonGame OnWonGame;

    public delegate void LostGame();
    public static event LostGame OnLostGame;

    private void Awake()
    {
        beginningMessage = GameObject.Find("BeginningMessage");
        mineTracker = GameObject.Find("MineTracker");
        remainingMinesText = GameObject.Find("RemainingMinesText").GetComponent<Text>();

        widthIn = GameObject.Find("WidthInput").GetComponent<InputField>();
        heightIn = GameObject.Find("HeightInput").GetComponent<InputField>();
        minesIn = GameObject.Find("MinesInput").GetComponent<InputField>();

        warningMessage = GameObject.Find("SizeWarning");
        mTag = GameObject.Find("MTag").GetComponent<Text>();

        mainMenuButtons = GameObject.Find("MainMenuButtons");
        optionButtons = GameObject.Find("OptionButtons");
        GameObject.Find("AreaUncoverToggle").GetComponent<Toggle>().isOn = canAreaUncover;
        GameObject.Find("AreaFlagToggle").GetComponent<Toggle>().isOn = canAreaFlag;

        mainMenu = GameObject.Find("MainMenu");
        mainMenuAlpha = mainMenu.GetComponent<CanvasGroup>();

        raycaster = GameObject.Find("Canvas").GetComponent<GraphicRaycaster>();
        eventSystem = GameObject.Find("EventSystem").GetComponent<EventSystem>();

        gameTimer = new Stopwatch();
        mainCam = Camera.main;

        tutPointer = GameObject.Find("TutorialPointer").GetComponent<Transform>();
        tutPointer.gameObject.SetActive(false);
    }

    private void Start()
    {
        widthIn.text = width.ToString();
        heightIn.text = height.ToString();
        minesIn.text = mineTotal.ToString();

        widthIn.onValueChanged.AddListener(delegate { CalculateMineDensity(); });
        heightIn.onValueChanged.AddListener(delegate { CalculateMineDensity(); });
        minesIn.onValueChanged.AddListener(delegate { CalculateMineDensity(); });

        mineTracker.SetActive(false);
        warningMessage.SetActive(false);
        optionButtons.SetActive(false);
        mainMenu.SetActive(false);

        CalculateMineDensity();
        InitializeNeighbors();
        NewLevel();
    }

    void CalculateMineDensity()
    {
        int inputWidth = int.Parse(widthIn.text);
        int inputHeight = int.Parse(heightIn.text);
        int inputMines = int.Parse(minesIn.text);

        float percentage = ((float)inputMines / ((float)inputWidth * (float)inputHeight)) * 100;
        string density = (percentage > 100) ? mTag.text = ">100%" : mTag.text = $"{percentage.ToString("F0")}%";

        validMineDensity = (percentage > 100) ? false : true;
        warningMessage.SetActive(false);
        hasBeenWarned = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (optionButtons.activeSelf) ToggleOptionsMenu();
            else ToggleMainMenu();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetLevel();
        }

        if (playing && !mainMenuOpen)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (InsidePlayArea())
                {
                    Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    print($"Mouse Position: {mousePos}");
                    if (ValidCell(mousePos))
                    {
                        ExecuteAction(mousePos, Actions.Uncover);
                    }
                }
            }

            if (Input.GetMouseButtonDown(1))
            {
                if (InsidePlayArea())
                {
                    Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    if (ValidCell(mousePos))
                    {
                        ExecuteAction(mousePos, Actions.Flag);
                    }
                }
            }
        }

        //Update remainingMines in UI
        if (!mainMenuOpen)
        {
            UpdateUIInfo();
            UpdateTimer();
        }

        if (changesMade)
        {
            changesMade = false;
            if (CheckIfWin()) FinishGame(true);
        }
    }

    public void ResetLevel()
    {
        if (playingTutorial)
        {
            RunTutorial();
        }
        else
        {
            NewLevel();
        }

        if (mainMenuOpen) ToggleMainMenu();
    }

    public void ToggleOptionsMenu()
    {
        mainMenuButtons.SetActive(!mainMenuButtons.activeSelf);
        optionButtons.SetActive(!optionButtons.activeSelf);
    }

    public void ToggleAreaUncover(bool canDo)
    {
        canAreaUncover = canDo;
    }

    public void ToggleAreaFlag(bool canDo)
    {
        canAreaFlag = canDo;
    }

    public void QuitGame()
    {
        ToggleMainMenu();
        Application.Quit();
    }

    public void StartWithNewDims()
    {
        int inputWidth = int.Parse(widthIn.text);
        int inputHeight = int.Parse(heightIn.text);

        if (inputWidth == 0 || inputHeight == 0)
        {
            DisplayWarningMessage(WarningType.MinimumBoardSize);
            return;
        }

        if (!validMineDensity)
        {
            DisplayWarningMessage(WarningType.MineDensity);
            return;
        }

        if (inputWidth * inputHeight > 10000 && !hasBeenWarned)
        {
            hasBeenWarned = true;
            DisplayWarningMessage(WarningType.MaximumBoardSize);
            return;
        }

        playingTutorial = false;

        width = inputWidth;
        height = inputHeight;
        mineTotal = int.Parse(minesIn.text);

        if (width <= 0 || height <= 0)
        {
            timerText.text = "Can't create levels with 0 width or height .-.";
        }
        else
        {
            timerText.text = "Good Luck <3";
        }

        NewLevel();

        hasBeenWarned = false;
        warningMessage.SetActive(false);

        ToggleMainMenu();
    }

    public void ToggleMainMenu()
    {
        mainMenuOpen = !mainMenuOpen;

        StopCoroutine(MainMenuAlpha());
        currentMenuAlphaTime = totalMenuAlphaTime;
        if (mainMenuOpen)
        {
            startAlpha = 0;
            endAlpha = 1;

            widthIn.text = width.ToString();
            heightIn.text = height.ToString();
            minesIn.text = mineCount.ToString();

            warningMessage.SetActive(false);
            hasBeenWarned = false;
        }
        else
        {
            startAlpha = 1;
            endAlpha = 0;
        }
        StartCoroutine(MainMenuAlpha());

        if (optionButtons.activeSelf) ToggleOptionsMenu();

        if (playing)
        {
            if (gameTimer.IsRunning) gameTimer.Stop();
            else gameTimer.Start();
        }

        OnMainMenuToggled?.Invoke(mainMenuOpen);
    }

    IEnumerator MainMenuAlpha()
    {
        while (currentMenuAlphaTime > 0)
        {
            if (currentMenuAlphaTime > 0)
            {
                if (mainMenuOpen) mainMenu.SetActive(true);
                currentMenuAlphaTime = Mathf.Clamp(currentMenuAlphaTime, 0, currentMenuAlphaTime - Time.deltaTime);
                mainMenuAlpha.alpha = Mathf.Lerp(startAlpha, endAlpha, (totalMenuAlphaTime - currentMenuAlphaTime) / totalMenuAlphaTime);
            }
            yield return new WaitForSeconds(.001f);
        }
        if (!mainMenuOpen) mainMenu.SetActive(false);
    }

    void DisplayWarningMessage(WarningType type)
    {
        switch (type)
        {
            case WarningType.MaximumBoardSize:
                warningMessage.GetComponentInChildren<Text>().text =
                    "Game sizes over 10,000 total tiles may cause a crash ;~; \nYou can click apply again though, IF YOU DARE!";
                break;
            case WarningType.MinimumBoardSize:
                warningMessage.GetComponentInChildren<Text>().text =
                    "I can't create board sizes that have 0 width or height \n...obviously... you dum dum ^//^; <3";
                break;
            case WarningType.MineDensity:
                warningMessage.GetComponentInChildren<Text>().text =
                    "I can't pack so many mines into this board ;//;!!";
                break;
            default:
                warningMessage.GetComponentInChildren<Text>().text =
                    "Looks like this warning type doesnt exist o.O;";
                break;
        }
        warningMessage.SetActive(true);
    }

    public void RunTutorial()
    {
        width = 8;
        height = 8;

        ResetValues();
        CreateTutorialBoard();

        FillTutorialQueues();
        NextTutorialStep();

        if (mainMenuOpen) ToggleMainMenu();

        playingTutorial = true;
        guidedPlay = true;
        canAreaFlag = true;
        canAreaUncover = true;

        tutPointer.gameObject.SetActive(true);
    }

    void CreateTutorialBoard()
    {
        mineCount = 10;

        isMine[0, 0] = false;
        isMine[1, 0] = true;
        isMine[2, 0] = true;
        isMine[3, 0] = false;
        isMine[4, 0] = false;
        isMine[5, 0] = false;
        isMine[6, 0] = true;
        isMine[7, 0] = false;

        isMine[0, 1] = true;
        isMine[1, 1] = false;
        isMine[2, 1] = false;
        isMine[3, 1] = false;
        isMine[4, 1] = false;
        isMine[5, 1] = false;
        isMine[6, 1] = true;
        isMine[7, 1] = false;

        isMine[0, 2] = true;
        isMine[1, 2] = false;
        isMine[2, 2] = false;
        isMine[3, 2] = false;
        isMine[4, 2] = false;
        isMine[5, 2] = false;
        isMine[6, 2] = false;
        isMine[7, 2] = false;

        isMine[0, 3] = false;
        isMine[1, 3] = false;
        isMine[2, 3] = false;
        isMine[3, 3] = false;
        isMine[4, 3] = false;
        isMine[5, 3] = false;
        isMine[6, 3] = false;
        isMine[7, 3] = false;

        isMine[0, 4] = false;
        isMine[1, 4] = false;
        isMine[2, 4] = false;
        isMine[3, 4] = false;
        isMine[4, 4] = false;
        isMine[5, 4] = false;
        isMine[6, 4] = false;
        isMine[7, 4] = true;

        isMine[0, 5] = false;
        isMine[1, 5] = true;
        isMine[2, 5] = false;
        isMine[3, 5] = false;
        isMine[4, 5] = false;
        isMine[5, 5] = false;
        isMine[6, 5] = false;
        isMine[7, 5] = false;

        isMine[0, 6] = false;
        isMine[1, 6] = false;
        isMine[2, 6] = false;
        isMine[3, 6] = false;
        isMine[4, 6] = false;
        isMine[5, 6] = false;
        isMine[6, 6] = false;
        isMine[7, 6] = true;

        isMine[0, 7] = false;
        isMine[1, 7] = false;
        isMine[2, 7] = false;
        isMine[3, 7] = false;
        isMine[4, 7] = true;
        isMine[5, 7] = true;
        isMine[6, 7] = false;
        isMine[7, 7] = false;

        GenerateNums();
        GenerateLevel();
    }

    void FillTutorialQueues()
    {
        // fill tutorial pow queue
        tutPosQueue = new Queue<Vector2>();
        tutPosQueue.Enqueue(new Vector2(3, 3));
        tutPosQueue.Enqueue(new Vector2(1, 5));
        tutPosQueue.Enqueue(new Vector2(1, 4));
        tutPosQueue.Enqueue(new Vector2(0, 5));
        tutPosQueue.Enqueue(new Vector2(1, 6));
        tutPosQueue.Enqueue(new Vector2(3, 6));
        tutPosQueue.Enqueue(new Vector2(4, 6));
        tutPosQueue.Enqueue(new Vector2(5, 6));


        // fill tutorial actions queue
        tutActionQueue = new Queue<Actions>();
        tutActionQueue.Enqueue(Actions.Uncover);
        tutActionQueue.Enqueue(Actions.Flag);
        tutActionQueue.Enqueue(Actions.Uncover);
        tutActionQueue.Enqueue(Actions.Uncover);
        tutActionQueue.Enqueue(Actions.Uncover);
        tutActionQueue.Enqueue(Actions.Flag);
        tutActionQueue.Enqueue(Actions.Flag);
        tutActionQueue.Enqueue(Actions.Uncover);


    }

    void NextTutorialStep()
    {
        if (tutPosQueue.Count > 0)
        {
            nextTutPos = tutPosQueue.Dequeue();
            nextTutAction = tutActionQueue.Dequeue();
            tutPointer.position = nextTutPos;
        }
        else
        {
            tutPointer.gameObject.SetActive(false);
            guidedPlay = false;
            // wait for complete
            FinishTutorial();
        }
    }

    void FinishTutorial()
    {
        // implement end to the 
    }

    bool InsidePlayArea()
    {
        PointerEventData pData = new PointerEventData(eventSystem);
        pData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(pData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject.name == "PlayArea") return true;
        }

        return false;
    }

    bool CheckIfWin()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int num = nums[x, y];
                if (num == 99) continue;
                if (isCovered[x, y]) return false;
            }
        }
        return true;
    }

    void UpdateUIInfo()
    {
        if (playing)
        {
            remainingMinesText.text = (mineTotal - flaggedSpaces).ToString();
        }
    }

    void UpdateTimer()
    {
        if (playing && !startingClick)
        {
            if (!gameTimer.IsRunning) gameTimer.Start();
            timerText.text = GetGameTime();
        }
        else
        {
            if (!playing && gameTimer.IsRunning)
            {
                gameTimer.Stop();
                if (wonLastGame)
                {
                    lastGameTime = $"Congrats!~ Your time was {GetGameTime()}";
                }
                else
                {
                    lastGameTime = "You failed ^_^;";
                }
            }
            timerText.text = lastGameTime;
        }
    }

    string GetGameTime()
    {
        string gameTime = gameTimer.Elapsed.Seconds.ToString();

        int seconds = gameTimer.Elapsed.Seconds;
        int hours = gameTimer.Elapsed.Hours;
        int minutes = gameTimer.Elapsed.Minutes;

        if (minutes > 0) 
        {
            if (seconds < 10) gameTime = $"0{gameTime}";
            gameTime = $"{minutes.ToString()}:{gameTime}";
        }

        if (hours > 0)
        {
            if (minutes < 10) gameTime = $"0{gameTime}";
            gameTime = $"{hours.ToString()}:{gameTime}";
        }

        if (wonLastGame) gameTime = $"{gameTime}.{gameTimer.Elapsed.Milliseconds}";

        return gameTime;
    }

    bool ValidCell(Vector3 pos)
    {
        if (pos.x < -0.5f || pos.y < -0.5f) return false;
        if (pos.x > width - 0.5f || pos.y > height - 0.5f) return false;
        return true;
    }

    void ExecuteAction(Vector3 pos, Actions action)
    {
        int x = (int)(pos.x + .5f);
        int y = (int)(pos.y + .5f);

        if (playingTutorial)
        {
            if (guidedPlay)
            {
                if (x != nextTutPos.x || y != nextTutPos.y || action != nextTutAction) return;
                else NextTutorialStep();
            }
        }

        switch (action)
        {
            case Actions.Uncover:
                if (startingClick) RerollCheck(x, y);
                else UncoverLogic(x, y, Actions.Uncover);
                break;
            case Actions.Flag:
                ToggleFlag(x, y);
                break;
            default:
                break;
        }
    }
    
    void RerollCheck(int x, int y)
    {
        startingClick = false;
        mineTracker.SetActive(true);
        beginningMessage.SetActive(false);

        if (!playingTutorial && nums[x, y] != 0)
        {
            RerollLevel(x, y);
        }
        gameTimer.Start();
        UncoverLogic(x, y, Actions.Uncover);
    }

    void UncoverLogic(int x, int y, Actions mode)
    {
        //TileInfo t = tiles[x, y];
        if (isFlagged[x, y]) return;

        if (!isCovered[x, y])
        {
            if (canAreaUncover)
            {
                if (nums[x, y] != 99 || nums[x, y] != 0)
                {
                    Vector2[] numsToUncover = SolveNeighbors(x, y, Actions.Uncover);
                    if (numsToUncover.Length != 0)
                    {
                        foreach (Vector2 pos in numsToUncover)
                        {
                            UncoverLogic((int)pos.x, (int)pos.y, Actions.Uncover);
                        }
                    }
                }
                return;
            }
        }
        else
        {
            isCovered[x, y] = false;
            changesMade = true;
        }

        if (nums[x, y] == 0)
        {
            mode = Actions.AreaUncover;
        }
        else if (nums[x,y] == 99)
        {
            FinishGame(false);
        }

        switch (mode)
        {
            case Actions.Uncover:
                StartCoroutine(TileAnimation(tileGO[x,y].transform, Actions.Uncover, 0, 0));
                break;
            case Actions.AreaUncover:
                StartCoroutine(TileAnimation(tileGO[x, y].transform, Actions.AreaUncover, 0, 0));
                isAnimated[x, y] = true;
                break;
            default:
                break;
        }
    }

    void ToggleFlag(int x, int y)
    {
        if (startingClick) return;

        if (!isCovered[x,y])
        {
            if (!canAreaFlag) return;
            if (nums[x, y] != 99 || nums[x, y] != 0)
            {
                Vector2[] CellsToFlag = SolveNeighbors(x, y, Actions.Flag);
                if(CellsToFlag.Length != 0)
                {
                    foreach (Vector2 pos in CellsToFlag)
                    {
                        int cellX = (int)pos.x;
                        int cellY = (int)pos.y;

                        flagGO[cellX, cellY].SetActive(true);
                        isFlagged[cellX, cellY] = true;
                        flaggedSpaces++;

                        StartCoroutine(TileAnimation(tileGO[cellX, cellY].transform, Actions.Flag, 0, 0));
                    }
                }
            }
            return;
        }
        else
        {
            bool toBeFlagged = (isFlagged[x, y]) ? false : true;

            flagGO[x, y].SetActive(toBeFlagged);
            isFlagged[x, y] = toBeFlagged;

            // adjust values for remaining mine andd flagged spaces
            if (toBeFlagged) flaggedSpaces++;
            else flaggedSpaces--;

            StartCoroutine(TileAnimation(tileGO[x, y].transform, Actions.Flag, 0, 0));
        }
    }

    Vector2[] SolveNeighbors(int x, int y, Actions mode)
    {
        int numOfMines = 0;
        int numOfFlags = 0;
        int numOfCoveredTiles = 0;
        int numOfCoveredMines = 0;
        List<Vector2> solved = new List<Vector2>();

        foreach (Vector2 pos in neighbors[x,y])
        {
            int neighborX = (int)pos.x;
            int neighborY = (int)pos.y;

            bool covered = isCovered[neighborX, neighborY];
            if (isMine[neighborX, neighborY])
            {
                numOfMines++;
                if (mode == Actions.Flag)
                {
                    if (!isFlagged[neighborX, neighborY]) solved.Add(new Vector2(neighborX, neighborY));
                    if (covered && !isFlagged[neighborX, neighborY]) numOfCoveredMines++;
                }
            }
            if (isFlagged[neighborX, neighborY]) numOfFlags++;
            if (mode == Actions.Uncover && !isFlagged[neighborX, neighborY] && covered) solved.Add(new Vector2(neighborX, neighborY));
            if (mode == Actions.Flag) if (covered) numOfCoveredTiles++;
        }

        if (mode == Actions.Flag)
        {
            if (numOfCoveredTiles - numOfFlags == numOfCoveredMines) return solved.ToArray();
            else return new Vector2[0];
        }
        else if (mode == Actions.Uncover)
        {
            if (numOfMines == numOfFlags) return solved.ToArray();
            else return new Vector2[0];
        }
        return null;
    }

    IEnumerator TileAnimation(Transform t, Actions mode, int order, float delay)
    {
        if (delay != 0) yield return new WaitForSeconds(delay);

        if (mode == Actions.AreaUncover)
        {
            int x = (int)t.transform.position.x;
            int y = (int)t.transform.position.y;

            isCovered[x, y] = false;
            changesMade = true;

            if (isFlagged[x, y])
            {
                isFlagged[x, y] = false;
                flagGO[x, y].SetActive(false);
            }

            if (order > 0) yield return new WaitForSeconds(0.025f);

            // uncover all blank spots
            if (nums[x, y] == 0)
            {
                foreach (Vector2 pos in neighbors[x,y])
                {
                    int neighborX = (int)pos.x;
                    int neighborY = (int)pos.y;

                    if (!isFlagged[neighborX, neighborY] && !isAnimated[neighborX, neighborY])
                    {
                        isAnimated[neighborX, neighborY] = true;
                        StartCoroutine(TileAnimation(tileGO[neighborX, neighborY].transform, Actions.AreaUncover, order + 1, 0));
                    }
                }
            }

            if (order > 0) yield return new WaitForSeconds(order * 0.025f);
        }

        AnimationCurve ac = new AnimationCurve();
        bool disappear = true;
        float totalTime = 0;

        switch (mode)
        {
            case Actions.Uncover:
                ac = tileUncoverCurve;
                totalTime = 0.4f;
                break;
            case Actions.Flag:
                ac = tileFlagCurve;
                disappear = false;
                totalTime = 0.25f;
                break;
            case Actions.AreaUncover:
                ac = areaUncoverCurve;
                totalTime = .7f;
                break;
            default:
                ac = tileUncoverCurve;
                break;
        }

        float currentTime = 0;
        while (currentTime <= totalTime)
        {
            t.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, ac.Evaluate(currentTime / totalTime));

            currentTime += Time.deltaTime;
            yield return null;
        }

        if (disappear) t.gameObject.SetActive(false);
    }

    void FinishGame(bool gameWon)
    {
        UpdateUIInfo();
        playing = false;

        if (gameWon)
        {
            wonLastGame = true;
            OnWonGame?.Invoke();

            if (playingTutorial)
            {
                UpdateTimer();
                ToggleMainMenu();
                ToggleOptionsMenu();
            }
        }
        else
        {
            wonLastGame = false;
            OnLostGame?.Invoke();

            // show red backgrounds for mines
            foreach (GameObject bg in mineBGs) bg.SetActive(true);


            List<Vector2> minesToReveal = new List<Vector2>();
            int mineCount = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (isMine[x, y])
                    {
                        minesToReveal.Add(new Vector2(x, y));
                        mineCount++;
                    }
                }
            }

            float delay = 0;
            float delayDelta = 0.75f / mineCount;

            for (int i = 0; i < mineCount; i++)
            {
                int x = (int)minesToReveal[i].x;
                int y = (int)minesToReveal[i].y;

                StartCoroutine(TileAnimation(tileGO[x, y].transform, Actions.Uncover, 0, delay));
                delay += delayDelta;
            }
        }
    }

    void ClearLevel()
    {
        for (int i = 0; i < levelHolder.childCount; i++)
        {
            Destroy(levelHolder.GetChild(i).gameObject);
        }
    }

    void InitializeNeighbors()
    {
        neighbors = new Vector2[width, height][];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                List<Vector2> neighborList = new List<Vector2>();
                for (int i = -1; i <= 1; i++)
                {
                    int neighborY = y + i;
                    if (neighborY < 0 || neighborY > height - 1) continue;

                    for (int j = -1; j <= 1; j++)
                    {
                        int neighborX = x + j;
                        if (neighborX < 0 || neighborX > width - 1 || i == 0 && j == 0) continue;

                        neighborList.Add(new Vector2(neighborX, neighborY));
                    }
                }
                neighbors[x, y] = neighborList.ToArray();
            }
        }
    }

    void ResetValues()
    {
        OnNewGame?.Invoke(width, height);

        playing = true;
        startingClick = true;

        mineTracker.SetActive(false);
        beginningMessage.SetActive(true);

        beginningMessage.GetComponent<Text>().text = "Click Anywhere to Start...";

        rerollCount = 0;
        lastGameTime = "Good Luck ♥";
        flaggedSpaces = 0;

        StopAllCoroutines();
        ClearLevel();
        ResetArrays();

        if (neighbors.GetLength(0) != width || neighbors.GetLength(1) != height) InitializeNeighbors();

        gameTimer.Restart();
        gameTimer.Stop();
    }

    void NewLevel()
    {
        ResetValues();
        GenerateMines();
        GenerateNums();
        GenerateLevel();
    }

    void RerollLevel(int x, int y)
    {
        print("Reroll");
        ResetArrays();
        GenerateMines();
        GenerateNums();
        if (nums[x, y] != 0)
        {
            if (rerollCount < rerolls)
            {
                rerollCount++;
                RerollLevel(x, y);
                return;
            }
        }
        ClearLevel();
        GenerateLevel();
    }

    void ResetArrays()
    {
        isCovered = new bool[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                isCovered[x, y] = true;
            }
        }

        isMine = new bool[width, height];
        nums = new int[width, height];
        isFlagged = new bool[width, height];

        isAnimated = new bool[width, height];
        tileGO = new GameObject[width, height];
        flagGO = new GameObject[width, height];

        // TODO: reset arrays
        mineBGs = new HashSet<GameObject>();
        mineCount = 0;
    }

    void GenerateMines()
    {
        float propability = ((float)mineTotal / (float)(width * height)) / 2f;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (Random.Range(0f, 1f) <= propability)
                {
                    if (!isMine[x, y])
                    {
                        isMine[x, y] = true;
                        mineCount++;
                    }
                }
                if (mineCount >= mineTotal) break;
            }
            if (mineCount >= mineTotal) break;
        }
        if (mineCount < mineTotal) GenerateMines();
    }

    void GenerateNums()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (isMine[x,y])
                {
                    nums[x, y] = 99;
                    continue;
                }
                nums[x, y] = NeighboringMines(x, y);
            }
        }
    }

    int NeighboringMines(int x, int y)
    {
        int neighborMines = 0;
        for (int i = -1; i <= 1; i++)
        {
            int neighborY = y + i;
            if (neighborY < 0 || neighborY > height - 1) continue;

            for (int j = -1; j <= 1; j++)
            {
                int neighborX = x + j;
                if (neighborX < 0 || neighborX > width - 1 || i == 0 && j == 0) continue;

                if (isMine[neighborX, neighborY]) neighborMines++;
            }
        }
        return neighborMines;
    }

    void GenerateLevel()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject cTile = Instantiate(coverTile, new Vector2(x, y), Quaternion.identity, levelHolder) as GameObject;
                tileGO[x, y] = cTile;
                flagGO[x, y] = cTile.transform.GetChild(0).gameObject;

                GameObject gTile = Instantiate(gridTile, new Vector2(x, y), Quaternion.identity, levelHolder) as GameObject;
                TextMesh text = gTile.GetComponentInChildren<TextMesh>();

                if (isMine[x,y])
                {
                    mineBGs.Add(gTile.transform.GetChild(0).gameObject);
                    text.text = "";
                    text.color = colors[0];
                }
                else
                {
                    if (nums[x,y] == 0)
                    {
                        text.text = "";
                    }
                    else
                    {
                        text.text = nums[x, y].ToString();
                        text.color = colors[nums[x, y]];
                    }
                }
            }
        }
    }
}
