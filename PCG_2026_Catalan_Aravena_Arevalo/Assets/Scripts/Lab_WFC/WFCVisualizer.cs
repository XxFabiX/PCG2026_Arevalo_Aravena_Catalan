using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Laboratorios.Lab7.WaveFunctionCollapse
{
    [RequireComponent(typeof(WFCGenerator))]
    public class WFCVisualizer : MonoBehaviour
    {
        private WFCGenerator generator;

        private GameObject[,] cellObjects;
        private SpriteRenderer[,] mainRenderers;
        private SpriteRenderer[,] borderRenderers;
        private TextMesh[,] domainTexts;

        private int pooledWidth = 0;
        private int pooledHeight = 0;
        private Sprite defaultSquareSprite;

        // Controles de UI e iteración automática
        private string inputWidth = "10";
        private string inputHeight = "8";
        private bool autoRun = false;
        [SerializeField, Range(0.01f, 1f)] private float autoStepDelay = 0.1f;
        private float nextStepTime = 0f;

        private void Awake()
        {
            generator = GetComponent<WFCGenerator>();
            defaultSquareSprite = CreateUnitSquareSprite();
            inputWidth = generator.Width.ToString();
            inputHeight = generator.Height.ToString();
        }

        private void OnEnable()
        {
            generator.OnStateUpdated += RefreshVisuals;
        }

        private void OnDisable()
        {
            generator.OnStateUpdated -= RefreshVisuals;
        }

        private void Update()
        {
            if (autoRun && Time.time >= nextStepTime)
            {
                if (generator.Phase == WFCPhase.ReadyToObserve || generator.Phase == WFCPhase.Propagating)
                {
                    generator.Step();
                    nextStepTime = Time.time + autoStepDelay;
                }
                else
                {
                    autoRun = false;
                }
            }
        }

        private void EnsureGridPool()
        {
            if (cellObjects != null && pooledWidth == generator.Width && pooledHeight == generator.Height)
                return;

            // Limpiar pool anterior sólo si cambiaron las dimensiones de la matriz
            if (cellObjects != null)
            {
                for (int x = 0; x < pooledWidth; x++)
                    for (int y = 0; y < pooledHeight; y++)
                        if (cellObjects[x, y] != null)
                            Destroy(cellObjects[x, y]);
            }

            pooledWidth = generator.Width;
            pooledHeight = generator.Height;

            cellObjects = new GameObject[pooledWidth, pooledHeight];
            mainRenderers = new SpriteRenderer[pooledWidth, pooledHeight];
            borderRenderers = new SpriteRenderer[pooledWidth, pooledHeight];
            domainTexts = new TextMesh[pooledWidth, pooledHeight];

            for (int x = 0; x < pooledWidth; x++)
            {
                for (int y = 0; y < pooledHeight; y++)
                {
                    GameObject cellObj = new GameObject($"Cell_{x}_{y}");
                    cellObj.transform.SetParent(transform);

                    // Fondo / Marco para resaltar Observe y Propagate
                    GameObject borderObj = new GameObject("Border");
                    borderObj.transform.SetParent(cellObj.transform, false);
                    SpriteRenderer borderSr = borderObj.AddComponent<SpriteRenderer>();
                    borderSr.sprite = defaultSquareSprite;
                    borderSr.sortingOrder = 0;
                    borderRenderers[x, y] = borderSr;

                    // Sprite principal del patrón
                    GameObject spriteObj = new GameObject("TileSprite");
                    spriteObj.transform.SetParent(cellObj.transform, false);
                    SpriteRenderer mainSr = spriteObj.AddComponent<SpriteRenderer>();
                    mainSr.sortingOrder = 1;
                    mainRenderers[x, y] = mainSr;

                    // Texto para mostrar el tamaño del dominio
                    GameObject textObj = new GameObject("DomainText");
                    textObj.transform.SetParent(cellObj.transform, false);
                    textObj.transform.localPosition = new Vector3(0, 0, -0.1f);
                    TextMesh tm = textObj.AddComponent<TextMesh>();
                    tm.alignment = TextAlignment.Center;
                    tm.anchor = TextAnchor.MiddleCenter;
                    tm.characterSize = 0.12f;
                    tm.fontSize = 36;
                    tm.color = Color.white;
                    domainTexts[x, y] = tm;

                    cellObjects[x, y] = cellObj;
                }
            }
        }

        public void RefreshVisuals()
        {
            EnsureGridPool();

            var tileSet = generator.ActiveTileSet;
            float size = generator.CellSize;

            for (int x = 0; x < pooledWidth; x++)
            {
                for (int y = 0; y < pooledHeight; y++)
                {
                    GameObject cellObj = cellObjects[x, y];
                    cellObj.transform.position = generator.GetCellWorldPosition(x, y);

                    SpriteRenderer borderSr = borderRenderers[x, y];
                    SpriteRenderer mainSr = mainRenderers[x, y];
                    TextMesh tm = domainTexts[x, y];

                    borderSr.transform.localScale = Vector3.one * (size * 0.96f);

                    if (!generator.IsInitialized)
                    {
                        borderSr.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
                        mainSr.sprite = null;
                        tm.text = "-";
                        continue;
                    }

                    IReadOnlyList<int> domain = generator.GetDomain(x, y);
                    int count = domain != null ? domain.Count : 0;
                    Vector2Int coords = new Vector2Int(x, y);

                    // Colorear marco según estado (Observado, Propagado, Contradicción o Normal)
                    if (count == 0)
                        borderSr.color = new Color(0.9f, 0.15f, 0.15f, 1f); // Rojo: Contradicción
                    else if (generator.LastObservedCell == coords)
                        borderSr.color = new Color(1f, 0.85f, 0.1f, 1f);    // Amarillo: Recién observado
                    else if (generator.LastPropagatedCells.Contains(coords))
                        borderSr.color = new Color(0.2f, 0.8f, 1f, 1f);     // Celeste: Recién propagado
                    else if (count == 1)
                        borderSr.color = new Color(0.15f, 0.35f, 0.15f, 0.6f); // Verde oscuro: Resuelto
                    else
                        borderSr.color = new Color(0.22f, 0.22f, 0.26f, 0.9f); // Gris: Sin resolver

                    // Mostrar patrón si está resuelto o cantidad de posibilidades si no lo está
                    if (count == 1 && tileSet != null)
                    {
                        WFCTile tile = tileSet.Tiles[domain[0]];
                        mainSr.sprite = tile.sprite != null ? tile.sprite : defaultSquareSprite;
                        mainSr.color = Color.white;

                        // Ajustar escala del sprite al cellSize
                        if (mainSr.sprite != null)
                        {
                            Vector2 spriteSize = mainSr.sprite.bounds.size;
                            mainSr.transform.localScale = new Vector3(
                                (size * 0.88f) / spriteSize.x,
                                (size * 0.88f) / spriteSize.y,
                                1f);
                        }
                        tm.text = "";
                    }
                    else if (count > 1)
                    {
                        mainSr.sprite = defaultSquareSprite;
                        mainSr.color = new Color(0.12f, 0.12f, 0.15f, 0.95f);
                        mainSr.transform.localScale = Vector3.one * (size * 0.82f);
                        tm.text = count.ToString();
                        tm.color = generator.LastPropagatedCells.Contains(coords) ? Color.cyan : Color.white;
                    }
                    else
                    {
                        mainSr.sprite = null;
                        tm.text = "X";
                        tm.color = Color.yellow;
                    }
                }
            }
        }

        // Dibuja en Scene y Game (si Gizmos están activos) las conexiones de las piezas resueltas
        private void OnDrawGizmos()
        {
            if (generator == null || !generator.IsInitialized || generator.ActiveTileSet == null)
                return;

            var tiles = generator.ActiveTileSet.Tiles;
            float r = generator.CellSize * 0.42f;

            for (int x = 0; x < generator.Width; x++)
            {
                for (int y = 0; y < generator.Height; y++)
                {
                    var domain = generator.GetDomain(x, y);
                    if (domain == null || domain.Count != 1) continue;

                    WFCTile tile = tiles[domain[0]];
                    Vector3 center = generator.GetCellWorldPosition(x, y);

                    Gizmos.color = Color.green;
                    if (tile.north) Gizmos.DrawLine(center, center + Vector3.up * r);
                    if (tile.east) Gizmos.DrawLine(center, center + Vector3.right * r);
                    if (tile.south) Gizmos.DrawLine(center, center + Vector3.down * r);
                    if (tile.west) Gizmos.DrawLine(center, center + Vector3.left * r);
                }
            }
        }

        // Panel de control en pantalla para cumplir todos los requisitos de interacción
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(15, 15, 340, 420), GUI.skin.box);
            GUILayout.Label("<b>CONTROL WAVE FUNCTION COLLAPSE</b>");
            GUILayout.Space(5);

            GUILayout.Label($"<b>Fase:</b> {generator.Phase}");
            GUILayout.Label($"<b>Estado:</b> {generator.Status}");
            GUILayout.Space(8);

            // 1. Configurar dimensiones
            GUILayout.BeginHorizontal();
            GUILayout.Label("Ancho:", GUILayout.Width(50));
            inputWidth = GUILayout.TextField(inputWidth, GUILayout.Width(45));
            GUILayout.Label("Alto:", GUILayout.Width(40));
            inputHeight = GUILayout.TextField(inputHeight, GUILayout.Width(45));
            if (GUILayout.Button("Aplicar Tamano"))
            {
                autoRun = false;
                if (int.TryParse(inputWidth, out int w) && int.TryParse(inputHeight, out int h))
                {
                    generator.SetDimensions(w, h);
                    generator.Initialize();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            // 2. Seleccionar entre los dos tilesets
            GUILayout.Label("<b>Seleccionar TileSet:</b>");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < generator.TileSetCount; i++)
            {
                bool isSelected = (i == generator.ActiveTileSetIndex);
                string btnLabel = isSelected ? $"[ TileSet {i + 1} ]" : $"TileSet {i + 1}";
                if (GUILayout.Button(btnLabel))
                {
                    autoRun = false;
                    generator.SelectTileSet(i);
                    generator.Initialize();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            // 3. Inicializar / Reiniciar
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Inicializar / Reiniciar", GUILayout.Height(28)))
            {
                autoRun = false;
                generator.Initialize();
            }
            if (GUILayout.Button("Limpiar", GUILayout.Height(28)))
            {
                autoRun = false;
                generator.Clear();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            // 4. Ejecución incremental (Observe y Propagate diferenciados)
            GUI.enabled = generator.Phase == WFCPhase.ReadyToObserve || generator.Phase == WFCPhase.Propagating;
            string stepText = generator.Phase == WFCPhase.ReadyToObserve
                ? "Paso Siguiente: OBSERVE"
                : "Paso Siguiente: PROPAGATE";

            if (GUILayout.Button(stepText, GUILayout.Height(32)))
            {
                autoRun = false;
                generator.Step();
            }

            string autoText = autoRun ? "Pausar Auto-Generacion" : "Ejecutar Automatico";
            if (GUILayout.Button(autoText, GUILayout.Height(28)))
            {
                autoRun = !autoRun;
            }
            GUI.enabled = true;

            GUILayout.Space(6);
            GUILayout.Label("<b>Leyenda de Colores:</b>\n" +
                            "• Amarillo: Celda recien observada\n" +
                            "• Celeste: Dominios reducidos en Propagate\n" +
                            "• Numero: Cantidad de patrones posibles\n" +
                            "• Rojo (X): Contradiccion detectada");
            GUILayout.EndArea();
        }

        private Sprite CreateUnitSquareSprite()
        {
            Texture2D tex = new Texture2D(2, 2);
            tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
        }
    }
}