using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SudokuUI : MonoBehaviour
{
    private SudokuBoard board;
    private Image[,] cellBackgrounds = new Image[9, 9];
    private Text[,] cellTexts = new Text[9, 9];
    private InputField difficultyInput;
    private Text victoryText;
    private int selectedRow = 0;
    private int selectedCol = 0;

    // campos para los parametros del simulated
    private InputField tempInput;
    private InputField coolingInput;
    private InputField iterInput;

    void Start()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        board = new SudokuBoard();
        board.GenerateBackward(40);

        CreateSudokuUI();
        UpdateUIFromBoard();
        HighlightSelectedCell();
    }

    void Update()
    {
        HandleKeyboardInput();
    }

    //********************** UI******************************
    void CreateSudokuUI()
    {
        // configuracion del canvas
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        //fondo y contenedor del tablero
        GameObject mainPanel = new GameObject("SudokuMainPanel");
        mainPanel.transform.SetParent(canvas.transform, false);
        RectTransform mainRect = mainPanel.AddComponent<RectTransform>();
        mainRect.sizeDelta = new Vector2(490, 490);
        mainRect.anchoredPosition = new Vector2(0, -20);

        Image mainBg = mainPanel.AddComponent<Image>();
        mainBg.color = new Color(0.15f, 0.15f, 0.15f);

        GridLayoutGroup mainGrid = mainPanel.AddComponent<GridLayoutGroup>();
        mainGrid.cellSize = new Vector2(158, 158);
        mainGrid.spacing = new Vector2(5, 5);
        mainGrid.padding = new RectOffset(5, 5, 5, 5);
        mainGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        mainGrid.constraintCount = 3;

        // creamos los bloques de 3x3
        for (int br = 0; br < 3; br++)
        {
            for (int bc = 0; bc < 3; bc++)
            {
                GameObject blockObj = new GameObject($"Block_{br}_{bc}");
                blockObj.transform.SetParent(mainPanel.transform, false);

                Image blockBg = blockObj.AddComponent<Image>();
                blockBg.color = new Color(0.6f, 0.6f, 0.6f);

                GridLayoutGroup blockGrid = blockObj.AddComponent<GridLayoutGroup>();
                blockGrid.cellSize = new Vector2(50, 50);
                blockGrid.spacing = new Vector2(2, 2);
                blockGrid.padding = new RectOffset(2, 2, 2, 2);
                blockGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                blockGrid.constraintCount = 3;

                for (int r = 0; r < 3; r++)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        int globalRow = br * 3 + r;
                        int globalCol = bc * 3 + c;

                        GameObject cellObj = new GameObject($"Cell_{globalRow}_{globalCol}");
                        cellObj.transform.SetParent(blockObj.transform, false);

                        Image cellBg = cellObj.AddComponent<Image>();
                        cellBg.color = Color.white;
                        cellBackgrounds[globalRow, globalCol] = cellBg;

                        GameObject textObj = new GameObject("Text");
                        textObj.transform.SetParent(cellObj.transform, false);
                        Text text = textObj.AddComponent<Text>();
                        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                        text.color = Color.black;
                        text.alignment = TextAnchor.MiddleCenter;
                        text.fontSize = 28;
                        cellTexts[globalRow, globalCol] = text;
                    }
                }
            }
        }

        //****************funcion para las etiquetas de los textos*****************
        void CreateLabel(string title, Vector2 position)
        {
            GameObject labelObj = new GameObject(title + "_Label");
            labelObj.transform.SetParent(canvas.transform, false);
            RectTransform rect = labelObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.5f); rect.anchorMax = new Vector2(0, 0.5f); rect.pivot = new Vector2(0, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(100, 30);
            Text txt = labelObj.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.color = Color.white;
            txt.text = title;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontSize = 14;
        }

        //**************** Paneles de los botones e Inputs *******************

        CreateLabel("Temp. Inicial", new Vector2(350, 180)); 
        GameObject tempObj = new GameObject("TempInput");
        tempObj.transform.SetParent(canvas.transform, false);
        RectTransform tempRect = tempObj.AddComponent<RectTransform>();
        tempRect.anchorMin = new Vector2(0, 0.5f); tempRect.anchorMax = new Vector2(0, 0.5f); tempRect.pivot = new Vector2(0, 0.5f);
        tempRect.anchoredPosition = new Vector2(350, 150);
        tempRect.sizeDelta = new Vector2(80, 40);
        Image tempImage = tempObj.AddComponent<Image>(); tempImage.color = new Color(0.9f, 0.9f, 0.9f);
        tempInput = tempObj.AddComponent<InputField>();
        tempInput.characterValidation = InputField.CharacterValidation.Decimal;
        tempInput.text = "5"; //mejora valor definido para sudoku
        GameObject tempTextObj = new GameObject("Text"); tempTextObj.transform.SetParent(tempObj.transform, false);
        RectTransform ttRect = tempTextObj.AddComponent<RectTransform>(); ttRect.anchorMin = Vector2.zero; ttRect.anchorMax = Vector2.one;
        Text tText = tempTextObj.AddComponent<Text>(); tText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tText.color = Color.black; tText.alignment = TextAnchor.MiddleCenter; tText.fontSize = 16;
        tempInput.textComponent = tText;

        CreateLabel("Enfriamiento", new Vector2(440, 180));
        GameObject coolObj = new GameObject("CoolingInput");
        coolObj.transform.SetParent(canvas.transform, false);
        RectTransform coolRect = coolObj.AddComponent<RectTransform>();
        coolRect.anchorMin = new Vector2(0, 0.5f); coolRect.anchorMax = new Vector2(0, 0.5f); coolRect.pivot = new Vector2(0, 0.5f);
        coolRect.anchoredPosition = new Vector2(440, 150);
        coolRect.sizeDelta = new Vector2(80, 40);
        Image coolImage = coolObj.AddComponent<Image>(); coolImage.color = new Color(0.9f, 0.9f, 0.9f);
        coolingInput = coolObj.AddComponent<InputField>();
        coolingInput.characterValidation = InputField.CharacterValidation.Decimal;
        coolingInput.text = "0.99995";// mejor valor definido para sudoku
        GameObject coolTextObj = new GameObject("Text"); coolTextObj.transform.SetParent(coolObj.transform, false);
        RectTransform ctRect = coolTextObj.AddComponent<RectTransform>(); ctRect.anchorMin = Vector2.zero; ctRect.anchorMax = Vector2.one;
        Text cText = coolTextObj.AddComponent<Text>(); cText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        cText.color = Color.black; cText.alignment = TextAnchor.MiddleCenter; cText.fontSize = 16;
        coolingInput.textComponent = cText;

        CreateLabel("Iteraciones", new Vector2(530, 180));
        GameObject iterObj = new GameObject("IterInput");
        iterObj.transform.SetParent(canvas.transform, false);
        RectTransform iterRect = iterObj.AddComponent<RectTransform>();
        iterRect.anchorMin = new Vector2(0, 0.5f); iterRect.anchorMax = new Vector2(0, 0.5f); iterRect.pivot = new Vector2(0, 0.5f);
        iterRect.anchoredPosition = new Vector2(530, 150);
        iterRect.sizeDelta = new Vector2(80, 40);
        Image iterImage = iterObj.AddComponent<Image>(); iterImage.color = new Color(0.9f, 0.9f, 0.9f);
        iterInput = iterObj.AddComponent<InputField>();
        iterInput.characterValidation = InputField.CharacterValidation.Integer;
        iterInput.text = "500000";// mejor valor definido para sudoku
        GameObject iterTextObj = new GameObject("Text"); iterTextObj.transform.SetParent(iterObj.transform, false);
        RectTransform itRect = iterTextObj.AddComponent<RectTransform>(); itRect.anchorMin = Vector2.zero; itRect.anchorMax = Vector2.one;
        Text iText = iterTextObj.AddComponent<Text>(); iText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        iText.color = Color.black; iText.alignment = TextAnchor.MiddleCenter; iText.fontSize = 16;
        iterInput.textComponent = iText;


        //boton del simulated annealing
        GameObject saButtonObj = new GameObject("SolveSAButton");
        saButtonObj.transform.SetParent(canvas.transform, false);
        RectTransform saBtnRect = saButtonObj.AddComponent<RectTransform>();
        saBtnRect.anchorMin = new Vector2(0, 0.5f);
        saBtnRect.anchorMax = new Vector2(0, 0.5f);
        saBtnRect.pivot = new Vector2(0, 0.5f);
        saBtnRect.anchoredPosition = new Vector2(350, 80);
        saBtnRect.sizeDelta = new Vector2(260, 60);

        Image saBtnImage = saButtonObj.AddComponent<Image>();
        saBtnImage.color = new Color(0.85f, 0.45f, 0.15f);
        Button saBtn = saButtonObj.AddComponent<Button>();

        GameObject saBtnTextObj = new GameObject("SABtnText");
        saBtnTextObj.transform.SetParent(saButtonObj.transform, false);
        Text saBtnText = saBtnTextObj.AddComponent<Text>();
        saBtnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        saBtnText.color = Color.white;
        saBtnText.text = "Resolver con IA\n(Simulated Annealing)";
        saBtnText.alignment = TextAnchor.MiddleCenter;
        saBtnText.fontSize = 14;
        saBtn.onClick.AddListener(TriggerSimulatedAnnealing);

        // boton de regenerar el tablero
        GameObject buttonObj = new GameObject("RegenerateButton");
        buttonObj.transform.SetParent(canvas.transform, false);
        RectTransform btnRect = buttonObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0, 0.5f);
        btnRect.anchorMax = new Vector2(0, 0.5f);
        btnRect.pivot = new Vector2(0, 0.5f);
        btnRect.anchoredPosition = new Vector2(350, 0);
        btnRect.sizeDelta = new Vector2(260, 45);

        Image btnImage = buttonObj.AddComponent<Image>();
        btnImage.color = new Color(0.2f, 0.65f, 0.35f);
        Button btn = buttonObj.AddComponent<Button>();

        GameObject btnTextObj = new GameObject("BtnText");
        btnTextObj.transform.SetParent(buttonObj.transform, false);
        Text btnText = btnTextObj.AddComponent<Text>();
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnText.color = Color.white;
        btnText.text = "Generar Nuevo Tablero";
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.fontSize = 14;
        btn.onClick.AddListener(RegenerateBoard);

        //campo de texto de dificultad
        CreateLabel("Valor en celda", new Vector2(430, -30));
        GameObject inputObj = new GameObject("DifficultyInput");
        inputObj.transform.SetParent(canvas.transform, false);
        RectTransform inputRect = inputObj.AddComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0, 0.5f);
        inputRect.anchorMax = new Vector2(0, 0.5f);
        inputRect.pivot = new Vector2(0, 0.5f);
        inputRect.anchoredPosition = new Vector2(430, -60);
        inputRect.sizeDelta = new Vector2(100, 40);

        Image inputImage = inputObj.AddComponent<Image>();
        inputImage.color = new Color(0.9f, 0.9f, 0.9f);

        difficultyInput = inputObj.AddComponent<InputField>();
        difficultyInput.characterValidation = InputField.CharacterValidation.Integer;
        difficultyInput.text = "40";

        GameObject inputTextObj = new GameObject("InputText");
        inputTextObj.transform.SetParent(inputObj.transform, false);
        RectTransform textRect = inputTextObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        Text inputText = inputTextObj.AddComponent<Text>();
        inputText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        inputText.color = Color.black;
        inputText.alignment = TextAnchor.MiddleCenter;
        inputText.fontSize = 20;
        difficultyInput.textComponent = inputText;

        // texto de win
        GameObject victoryObj = new GameObject("VictoryText");
        victoryObj.transform.SetParent(canvas.transform, false);
        RectTransform vicRect = victoryObj.AddComponent<RectTransform>();
        vicRect.anchoredPosition = new Vector2(0, 330);
        vicRect.sizeDelta = new Vector2(600, 80);

        victoryText = victoryObj.AddComponent<Text>();
        victoryText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        victoryText.color = new Color(0.1f, 0.7f, 0.1f);
        victoryText.text = "¡FELICIDADES, HAS RESUELTO EL SUDOKU!";
        victoryText.alignment = TextAnchor.MiddleCenter;
        victoryText.fontSize = 32;
        victoryText.fontStyle = FontStyle.Bold;
        victoryObj.SetActive(false);
    }

    //***********************Logica visual y los controles****************************
    void UpdateUIFromBoard()
    {
        int[,] raw = board.GetRawGrid();

        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                int val = raw[r, c];
                if (val != 0)
                {
                    cellTexts[r, c].text = val.ToString();
                    cellTexts[r, c].color = new Color(0.1f, 0.2f, 0.6f);
                }
                else
                {
                    cellTexts[r, c].text = "";
                    cellTexts[r, c].color = Color.black;
                }
            }
        }
    }

    void HandleKeyboardInput()
    {
        bool moved = false;

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) { selectedRow = Mathf.Max(0, selectedRow - 1); moved = true; }
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) { selectedRow = Mathf.Min(8, selectedRow + 1); moved = true; }
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) { selectedCol = Mathf.Max(0, selectedCol - 1); moved = true; }
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) { selectedCol = Mathf.Min(8, selectedCol + 1); moved = true; }

        if (moved) HighlightSelectedCell();

        int enteredNum = -1;
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) enteredNum = 1;
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) enteredNum = 2;
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) enteredNum = 3;
        if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) enteredNum = 4;
        if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) enteredNum = 5;
        if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6)) enteredNum = 6;
        if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7)) enteredNum = 7;
        if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8)) enteredNum = 8;
        if (Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9)) enteredNum = 9;

        if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0))
        {
            enteredNum = 0;
        }

        if (enteredNum != -1)
        {
            TrySetCell(selectedRow, selectedCol, enteredNum);
        }
    }

    void TrySetCell(int r, int c, int val)
    {
        if (cellTexts[r, c].color == new Color(0.1f, 0.2f, 0.6f)) return;

        board.SetCell(r, c, val);
        cellTexts[r, c].text = val == 0 ? "" : val.ToString();
        cellTexts[r, c].color = Color.black;

        int conflicts = board.CountConflicts();
        if (conflicts == 0)
        {
            if (board.IsBoardFull())
            {
                Debug.Log("¡JUEGO COMPLETADO!");
                if (victoryText != null) victoryText.gameObject.SetActive(true);
            }
        }
    }

    void HighlightSelectedCell()
    {
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                bool isFixed = (cellTexts[r, c].color == new Color(0.1f, 0.2f, 0.6f));

                if (r == selectedRow && c == selectedCol)
                {
                    cellBackgrounds[r, c].color = new Color(1f, 0.95f, 0.4f);
                }
                else
                {
                    cellBackgrounds[r, c].color = isFixed ? new Color(0.9f, 0.92f, 0.95f) : Color.white;
                }
            }
        }
    }

    //**************Metodos para los botones***********************
    void RegenerateBoard()
    {
        if (victoryText != null) victoryText.gameObject.SetActive(false);

        int cellsToRemove = 40;

        if (int.TryParse(difficultyInput.text, out int parsedValue))
        {
            cellsToRemove = Mathf.Clamp(parsedValue, 0, 81);
        }

        difficultyInput.text = cellsToRemove.ToString();

        board = new SudokuBoard();
        board.GenerateBackward(cellsToRemove);

        selectedRow = 0;
        selectedCol = 0;

        UpdateUIFromBoard();
        HighlightSelectedCell();
    }

    void TriggerSimulatedAnnealing()
    {
        Debug.Log("Iniciando Simulated Annealing...");

        // extrae valores ingresados default en la UI
        float temp = float.TryParse(tempInput.text, out float t) ? t : 1000f;
        float cooling = float.TryParse(coolingInput.text, out float c) ? c : 0.9999f;
        int iter = int.TryParse(iterInput.text, out int i) ? i : 200000;

        // se limitan valoers para no romper el algoritmo
        temp = Mathf.Max(1f, temp);
        cooling = Mathf.Clamp(cooling, 0.1f, 0.99999f);
        iter = Mathf.Max(100, iter);

        // pasamos los datos al tablero
        board.SolveWithSimulatedAnnealing(temp, cooling, iter);

        UpdateUIFromBoard();

        int conflicts = board.CountConflicts();
        if (conflicts == 0)
        {
            if (victoryText != null) victoryText.gameObject.SetActive(true);
        }
        else
        {
            Debug.Log($"La IA se estancó. Quedaron {conflicts} errores.");
        }
    }
}