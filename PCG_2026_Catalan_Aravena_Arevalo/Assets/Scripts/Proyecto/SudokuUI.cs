using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SudokuUI : MonoBehaviour
{
    private SudokuBoard board;
    private Image[,] cellBackgrounds = new Image[9, 9];
    private Text[,] cellTexts = new Text[9, 9];

    // Conexión con nuestro director de orquesta
    private SudokuManager manager;

    // Campos de UI
    private InputField difficultyInput;
    private InputField tempInput;
    private InputField coolingInput;
    private InputField iterInput;
    private Text victoryText;

    private int selectedRow = 0;
    private int selectedCol = 0;
    private int[,] initialGridBackup;

    void Start()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        // Buscamos el Manager en la escena al iniciar
        manager = FindAnyObjectByType<SudokuManager>();

        CreateSudokuUI();

        if (manager != null)
        {
            // Generamos el primer nivel automáticamente usando el Pipeline
            board = manager.GenerateUltimateLevel(40);
            initialGridBackup = (int[,])board.GetRawGrid().Clone();
        }
        else
        {
            Debug.LogError("¡Falta el script SudokuManager en la escena!");
            board = new SudokuBoard();
        }

        UpdateUIFromBoard();
        HighlightSelectedCell();
    }

    void Update()
    {
        HandleKeyboardInput();
    }

    //********************** CREACIÓN DE LA UI ******************************
    void CreateSudokuUI()
    {
        // 1. Configuración del Canvas (Fuerza la resolución a 1920x1080)
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 2. Panel Izquierdo (Fondo oscuro para el menú)
        GameObject leftPanelObj = new GameObject("LeftMenuPanel");
        leftPanelObj.transform.SetParent(canvas.transform, false);
        RectTransform leftBgRect = leftPanelObj.AddComponent<RectTransform>();
        leftBgRect.anchorMin = new Vector2(0, 0); // Anclado a la izquierda
        leftBgRect.anchorMax = new Vector2(0, 1); // Alto completo
        leftBgRect.pivot = new Vector2(0, 0.5f);
        leftBgRect.anchoredPosition = new Vector2(0, 0);
        leftBgRect.sizeDelta = new Vector2(380, 0); // 380 píxeles de ancho
        Image leftBg = leftPanelObj.AddComponent<Image>();
        leftBg.color = new Color(0.12f, 0.12f, 0.15f, 0.95f); // Gris oscuro

        // 3. Contenedor del Tablero (A la derecha del menú)
        GameObject mainPanel = new GameObject("SudokuMainPanel");
        mainPanel.transform.SetParent(canvas.transform, false);
        RectTransform mainRect = mainPanel.AddComponent<RectTransform>();
        mainRect.sizeDelta = new Vector2(600, 600);
        mainRect.anchoredPosition = new Vector2(250, 0); // Desplazado al centro-derecha

        Image mainBg = mainPanel.AddComponent<Image>();
        mainBg.color = new Color(0.15f, 0.15f, 0.15f);

        GridLayoutGroup mainGrid = mainPanel.AddComponent<GridLayoutGroup>();
        mainGrid.cellSize = new Vector2(194, 194);
        mainGrid.spacing = new Vector2(6, 6);
        mainGrid.padding = new RectOffset(6, 6, 6, 6);
        mainGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        mainGrid.constraintCount = 3;

        // Construir bloques y celdas
        for (int br = 0; br < 3; br++)
        {
            for (int bc = 0; bc < 3; bc++)
            {
                GameObject blockObj = new GameObject($"Block_{br}_{bc}");
                blockObj.transform.SetParent(mainPanel.transform, false);
                Image blockBg = blockObj.AddComponent<Image>();
                blockBg.color = new Color(0.6f, 0.6f, 0.6f);

                GridLayoutGroup blockGrid = blockObj.AddComponent<GridLayoutGroup>();
                blockGrid.cellSize = new Vector2(62, 62);
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
                        text.fontSize = 32;
                        cellTexts[globalRow, globalCol] = text;
                    }
                }
            }
        }

        // =========================================================
        // FUNCIONES AUXILIARES PARA CREAR LA UI LIMPIA
        // =========================================================
        Text CreateLabel(string title, Vector2 pos, Transform parent, int fontSize = 18, TextAnchor align = TextAnchor.MiddleRight)
        {
            GameObject labelObj = new GameObject(title + "_Label");
            labelObj.transform.SetParent(parent, false);
            RectTransform rect = labelObj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(180, 30);
            Text txt = labelObj.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.color = Color.white;
            txt.text = title;
            txt.alignment = align;
            txt.fontSize = fontSize;
            return txt;
        }

        InputField CreateInput(string defaultVal, Vector2 pos, Transform parent, InputField.CharacterValidation validation)
        {
            GameObject inputObj = new GameObject("Input");
            inputObj.transform.SetParent(parent, false);
            RectTransform inputRect = inputObj.AddComponent<RectTransform>();
            inputRect.anchoredPosition = pos;
            inputRect.sizeDelta = new Vector2(100, 35);
            Image inputImage = inputObj.AddComponent<Image>(); inputImage.color = new Color(0.9f, 0.9f, 0.9f);

            InputField inputField = inputObj.AddComponent<InputField>();
            inputField.characterValidation = validation;
            inputField.text = defaultVal;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(inputObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;

            Text txt = textObj.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.color = Color.black;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontSize = 18;
            inputField.textComponent = txt;
            return inputField;
        }

        // =========================================================
        // AGREGANDO LOS CONTROLES AL PANEL IZQUIERDO
        // =========================================================

        CreateLabel("PARÁMETROS PCG", new Vector2(0, 250), leftPanelObj.transform, 22, TextAnchor.MiddleCenter);

        // Inputs de la IA
        CreateLabel("Temperatura SA :", new Vector2(-60, 180), leftPanelObj.transform);
        tempInput = CreateInput("1000", new Vector2(90, 180), leftPanelObj.transform, InputField.CharacterValidation.Decimal);

        CreateLabel("Enfriamiento SA :", new Vector2(-60, 120), leftPanelObj.transform);
        coolingInput = CreateInput("0.9999", new Vector2(90, 120), leftPanelObj.transform, InputField.CharacterValidation.Decimal);

        CreateLabel("Iteraciones SA :", new Vector2(-60, 60), leftPanelObj.transform);
        iterInput = CreateInput("200000", new Vector2(90, 60), leftPanelObj.transform, InputField.CharacterValidation.Integer);

        // Input de Backward
        CreateLabel("Celdas a Borrar :", new Vector2(-60, 0), leftPanelObj.transform);
        difficultyInput = CreateInput("40", new Vector2(90, 0), leftPanelObj.transform, InputField.CharacterValidation.Integer);

        // Botón Maestro de Generación
        GameObject btnObj = new GameObject("GenerateButton");
        btnObj.transform.SetParent(leftPanelObj.transform, false);
        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchoredPosition = new Vector2(0, -100);
        btnRect.sizeDelta = new Vector2(300, 60);
        Image btnImage = btnObj.AddComponent<Image>(); btnImage.color = new Color(0.2f, 0.65f, 0.35f); // Verde
        Button genBtn = btnObj.AddComponent<Button>();
        Text btnTxt = CreateLabel("Generar Nivel Procedural\n(Evolución -> SA -> Backward)", Vector2.zero, btnObj.transform, 16, TextAnchor.MiddleCenter);
        btnTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 60);
        genBtn.onClick.AddListener(RegenerateUltimate);

        // Botón Restaurar Tablero
        GameObject resObj = new GameObject("RestoreButton");
        resObj.transform.SetParent(leftPanelObj.transform, false);
        RectTransform resRect = resObj.AddComponent<RectTransform>();
        resRect.anchoredPosition = new Vector2(0, -180);
        resRect.sizeDelta = new Vector2(300, 50);
        Image resImage = resObj.AddComponent<Image>(); resImage.color = new Color(0.2f, 0.4f, 0.8f); // Azul
        Button resBtn = resObj.AddComponent<Button>();
        Text resTxt = CreateLabel("Restaurar Tablero Inicial", Vector2.zero, resObj.transform, 16, TextAnchor.MiddleCenter);
        resTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 50);
        resBtn.onClick.AddListener(RestoreInitialBoard);

        // Texto de Victoria (Arriba del tablero)
        GameObject victoryObj = new GameObject("VictoryText");
        victoryObj.transform.SetParent(canvas.transform, false);
        RectTransform vicRect = victoryObj.AddComponent<RectTransform>();
        vicRect.anchoredPosition = new Vector2(250, 350); // Sobre el tablero
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

    //*********************** Lógica de Controles ****************************

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

        if (enteredNum != -1) TrySetCell(selectedRow, selectedCol, enteredNum);
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
                    cellBackgrounds[r, c].color = new Color(1f, 0.95f, 0.4f);
                else
                    cellBackgrounds[r, c].color = isFixed ? new Color(0.9f, 0.92f, 0.95f) : Color.white;
            }
        }
    }

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

    // =========================================================
    // MÉTODOS DE LOS BOTONES
    // =========================================================

    // Genera la cadena completa: Evolución -> SA -> Backward
    void RegenerateUltimate()
    {
        if (victoryText != null) victoryText.gameObject.SetActive(false);

        if (manager == null) manager = FindAnyObjectByType<SudokuManager>();

        if (manager != null)
        {
            // Extraer y aplicar las variables desde la Interfaz de Usuario
            int cellsToRemove = int.TryParse(difficultyInput.text, out int val) ? Mathf.Clamp(val, 0, 81) : 40;
            float temp = float.TryParse(tempInput.text, out float t) ? t : 1000f;
            float cooling = float.TryParse(coolingInput.text, out float c) ? c : 0.9999f;
            int iter = int.TryParse(iterInput.text, out int i) ? i : 200000;

            // Actualizar los parámetros de la IA directamente en el Script antes de ejecutarlo
            if (manager.annealingEngine != null)
            {
                manager.annealingEngine.initialTemperature = Mathf.Max(1f, temp);
                manager.annealingEngine.coolingRate = Mathf.Clamp(cooling, 0.1f, 0.99999f);
                manager.annealingEngine.maxIterations = Mathf.Max(100, iter);
            }

            // Llamamos al Orquestador para fabricar el nivel
            board = manager.GenerateUltimateLevel(cellsToRemove);

            selectedRow = 0; selectedCol = 0;
            UpdateUIFromBoard();
            HighlightSelectedCell();

            // Guardamos la copia de seguridad para poder restaurar
            initialGridBackup = (int[,])board.GetRawGrid().Clone();
        }
        else
        {
            Debug.LogError("Error: El objeto 'SudokuManager' no se encuentra conectado o en la escena.");
        }
    }

    void RestoreInitialBoard()
    {
        if (victoryText != null) victoryText.gameObject.SetActive(false);
        if (initialGridBackup != null)
        {
            board = new SudokuBoard(initialGridBackup);
            UpdateUIFromBoard();
            Debug.Log("Tablero inicial restaurado para probar configuraciones.");
        }
    }
}