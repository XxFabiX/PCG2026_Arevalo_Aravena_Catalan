using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SudokuUI : MonoBehaviour
{
    private SudokuBoard board;
    private Image[,] cellBackgrounds = new Image[9, 9];
    private Text[,] cellTexts = new Text[9, 9];

    private int selectedRow = 0;
    private int selectedCol = 0;

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

    void CreateSudokuUI()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Panel Principal (Contenedor de los 9 bloques de 3x3) con fondo oscuro grueso para las divisiones principales
        GameObject mainPanel = new GameObject("SudokuMainPanel");
        mainPanel.transform.SetParent(canvas.transform, false);
        RectTransform mainRect = mainPanel.AddComponent<RectTransform>();
        mainRect.sizeDelta = new Vector2(480, 480);
        mainRect.anchoredPosition = Vector2.zero;

        Image mainBg = mainPanel.AddComponent<Image>();
        mainBg.color = new Color(0.1f, 0.1f, 0.1f); // Líneas divisorias gruesas entre los bloques 3x3

        GridLayoutGroup mainGrid = mainPanel.AddComponent<GridLayoutGroup>();
        mainGrid.cellSize = new Vector2(154, 154); // Tamaño de cada bloque 3x3
        mainGrid.spacing = new Vector2(4, 4);      // Grosor de la línea divisoria entre bloques 3x3
        mainGrid.padding = new RectOffset(4, 4, 4, 4);
        mainGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        mainGrid.constraintCount = 3;

        // Crear los 9 bloques de 3x3
        for (int br = 0; br < 3; br++)
        {
            for (int bc = 0; bc < 3; bc++)
            {
                GameObject blockObj = new GameObject($"Block_{br}_{bc}");
                blockObj.transform.SetParent(mainPanel.transform, false);

                Image blockBg = blockObj.AddComponent<Image>();
                blockBg.color = Color.white; // Fondo blanco para las celdas internas

                GridLayoutGroup blockGrid = blockObj.AddComponent<GridLayoutGroup>();
                blockGrid.cellSize = new Vector2(48, 48); // Tamaño de cada celda individual
                blockGrid.spacing = new Vector2(1, 1);    // Línea fina entre celdas internas
                blockGrid.padding = new RectOffset(1, 1, 1, 1);
                blockGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                blockGrid.constraintCount = 3;

                // Crear las 9 celdas dentro de cada bloque
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
                        text.fontSize = 22;
                        cellTexts[globalRow, globalCol] = text;
                    }
                }
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
                    cellTexts[r, c].color = new Color(0.1f, 0.2f, 0.6f); // Azul para pistas fijas
                }
                else
                {
                    cellTexts[r, c].text = "";
                    cellTexts[r, c].color = Color.black; // Negro para números del jugador
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

        int conflicts = board.CountConflicts();
        if (conflicts == 0)
        {
            Debug.Log("¡Tablero sin conflictos hasta el momento!");
        }
        else
        {
            Debug.Log($"Conflictos detectados en el tablero: {conflicts}");
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
                    cellBackgrounds[r, c].color = new Color(1f, 0.9f, 0.4f); // Amarillo para celda seleccionada
                }
                else
                {
                    cellBackgrounds[r, c].color = isFixed ? new Color(0.85f, 0.9f, 0.95f) : Color.white;
                }
            }
        }
    }
}