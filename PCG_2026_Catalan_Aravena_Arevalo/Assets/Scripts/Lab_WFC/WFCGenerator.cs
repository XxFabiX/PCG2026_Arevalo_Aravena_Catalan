using System;
using System.Collections.Generic;
using UnityEngine;

namespace Laboratorios.Lab7.WaveFunctionCollapse
{
    public enum WFCPhase
    {
        NotInitialized, ReadyToObserve, Propagating,
        Completed, Contradiction
    }

    public class WFCGenerator : MonoBehaviour
    {
        [Header("Tile Sets")]
        [SerializeField] private List<WFCTileSet> tileSets = new()
        {
            new WFCTileSet { id = "TileSet_01" },
            new WFCTileSet { id = "TileSet_02" }
        };
        [SerializeField, Min(0)] private int activeTileSetIndex = 0;

        [Header("Mapa")]
        [SerializeField, Min(1)] private int width = 16;
        [SerializeField, Min(1)] private int height = 12;
        [SerializeField, Min(0.1f)] private float cellSize = 1f;

        [Header("Generacion")]
        [SerializeField] private bool randomSeed = false;
        [SerializeField] private int seed = 12345;

        // --------------------------------------------------------
        // ESTRUCTURAS DEL ALGORITMO
        // --------------------------------------------------------

        /*
         * domains[x,y]:
         * Contiene los indices de los patrones que todavia pueden
         * ocupar la posicion (x,y).
         *
         * compatible[d,a,b]:
         * Indica si el patron B puede ubicarse en la direccion d
         * respecto del patron A.
         *
         * pendingArcs:
         * Relaciones entre celdas pendientes de comprobar
         * durante la propagacion.
         */

        [Header("Modo de propagacion")]
        [Tooltip("Si es true, Propagate() procesa toda la cola de una vez. Si es false, procesa un arco por llamada a Step() para ver la propagacion paso a paso.")]
        [SerializeField] private bool propagateFullStep = true;


        private List<int>[,] domains;
        private bool[,,] compatible;
        private Queue<WFCArc> pendingArcs = new();
        private System.Random random;
        private bool firstObservation;

        //datos auxiliares para que el visualizador distinga cambios recientes
        public Vector2Int LastObservedCell { get; private set; } = new Vector2Int(-1, -1);
        public HashSet<Vector2Int> LastPropagatedCells { get; private set; } = new();
        public event Action OnStateUpdated;

        private struct WFCArc
        {
            public int x, y, direction;

            public WFCArc(int x, int y, int direction)
            {
                this.x = x;
                this.y = y;
                this.direction = direction;
            }
        }

        private static readonly Vector2Int[] Offsets =
        {
            new Vector2Int(0, 1),   // North
            new Vector2Int(1, 0),   // East
            new Vector2Int(0, -1),  // South
            new Vector2Int(-1, 0)   // West
        };

        public WFCPhase Phase { get; private set; } = WFCPhase.NotInitialized;
        public string Status { get; private set; } = "Sin inicializar.";

        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;
        public int ActiveTileSetIndex => activeTileSetIndex;
        public int TileSetCount => tileSets != null ? tileSets.Count : 0;
        public bool IsInitialized => domains != null;

        public WFCTileSet ActiveTileSet =>
            tileSets != null && activeTileSetIndex >= 0 &&
            activeTileSetIndex < tileSets.Count
                ? tileSets[activeTileSetIndex] : null;

        // ========================================================
        // CONTROL GENERAL
        // ========================================================

        /*
         * El algoritmo WFC desarrolla cuatro tareas principales:
         *
         * 1. Preparar patrones.
         * 2. Construir restricciones.
         * 3. Generar mediante observacion y propagacion.
         * 4. Representar el resultado.
         *
         * Durante la generacion se trabaja con asignaciones
         * parciales. Los dominios disminuyen progresivamente
         * hasta que cada celda tenga un unico patron.
         *
         * Referencia: Karth y Smith (2017), Seccion 4.
         */

        //metodos extra para configurar dimensiones desde UI 
        public void SetDimensions(int newWidth, int newHeight)
        {
            width = Mathf.Max(1, newWidth);
            height = Mathf.Max(1, newHeight);
            Clear();
        }

        public void SelectTileSet(int index)
        {
            if (tileSets == null || index < 0 || index >= tileSets.Count)
                return;

            activeTileSetIndex = index;
            Clear();
        }

        public bool Initialize()
        {
            Clear();

            if (ActiveTileSet == null || ActiveTileSet.Tiles.Count == 0)
            {
                Status = "El TileSet seleccionado no contiene patrones.";
                Debug.LogWarning(Status, this);
                return false;
            }

            random = new System.Random(
                randomSeed ? Environment.TickCount : seed);

            firstObservation = true;

            // Preparar los dominios iniciales.
            if (!PatternsFromSample() || domains == null)
            {
                Status = "Error en PatternsFromSample(): No hay patrones habilitados.";
                return false;
            }

            // Construir las relaciones de compatibilidad.
            BuildPropagator();

            Phase = WFCPhase.ReadyToObserve;
            Status = "Inicializado. Listo para observar.";

            CheckCompletion();
            OutputObservations();
            return true;
        }

        public void Step()
        {
            switch (Phase)
            {
                case WFCPhase.ReadyToObserve:
                    Observe();
                    break;

                case WFCPhase.Propagating:
                    Propagate();
                    break;
            }

            OutputObservations();
        }

        public void Clear()
        {
            domains = null;
            compatible = null;
            pendingArcs.Clear();
            LastObservedCell = new Vector2Int(-1, -1);
            LastPropagatedCells.Clear();

            firstObservation = true;
            Phase = WFCPhase.NotInitialized;
            Status = "Sin inicializar.";
            OutputObservations();
        }

        // ========================================================
        // ETAPA 1: PATTERNS FROM SAMPLE
        // ========================================================

        /*
         * OBJETIVO:
         * Preparar los patrones disponibles y los dominios
         * iniciales de todas las posiciones de la matriz.
         *
         * En esta version utilizamos Simple Tiled WFC:
         * los patrones corresponden a piezas definidas en
         * el TileSet, no a subimagenes extraidas automaticamente.
         *
         * EJEMPLO:
         *
         * Si el TileSet contiene cuatro patrones:
         *
         *   P0, P1, P2, P3
         *
         * Pero P2 esta deshabilitado, el dominio inicial es:
         *
         *   D(x,y) = {P0, P1, P3}
         *
         * Todas las celdas comienzan con esos patrones
         * disponibles. Durante WFC se iran descartando opciones.
         */

        private bool PatternsFromSample()
        {
            /*
             * TODO 1:
             *
             * 1. Identificar los patrones habilitados del TileSet.
             * 2. Crear los dominios de la matriz.
             * 3. Inicializar cada dominio con los indices
             *    de los patrones disponibles.
             *
             * Considerar:
             * - Cada celda mantiene su propio dominio.
             * - No deben incluirse patrones deshabilitados.
             * - Los dominios pueden tener distinta cantidad
             *   de elementos durante la generacion.
             *
             * Retornar true cuando la inicializacion sea valida.
             */

            var tiles = ActiveTileSet.Tiles;
            List<int> enabledPatterns = new List<int>();

            for (int i = 0; i < tiles.Count; i++)
            {
                if (tiles[i] != null && tiles[i].available)
                {
                    enabledPatterns.Add(i);
                }
            }

            if (enabledPatterns.Count == 0)
                return false;

            domains = new List<int>[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    //cda celda recibe su propia copia de la lista de dominios
                    domains[x, y] = new List<int>(enabledPatterns);
                }
            }

            return true;
        }

        // ========================================================
        // ETAPA 2: BUILD PROPAGATOR
        // ========================================================

        /*
         * OBJETIVO:
         * Construir un indice con las relaciones de compatibilidad
         * entre todos los patrones disponibles.
         *
         * La compatibilidad depende de la direccion relativa.
         *
         * EJEMPLO:
         *
         * Un camino horizontal tiene:
         *
         *   North = 0, East = 1, South = 0, West = 1
         *
         * Si queremos colocar otra pieza al Este, esta debe
         * tener una conexion compatible hacia el Oeste.
         *
         * Esto permite descartar combinaciones incorrectas
         * sin evaluar nuevamente sus conexiones durante
         * cada propagacion.
         *
         * compatible[d,a,b] almacena esta relacion.
         */

        private void BuildPropagator()
        {
            var tiles = ActiveTileSet.Tiles;
            int count = tiles.Count;
            compatible = new bool[4, count, count];

            /*
             * TODO 2:
             *
             * Construir las relaciones entre pares de patrones
             * para las cuatro direcciones.
             *
             * Utilizar IsCompatible() definido en WFCTile.
             *
             * El arreglo "compatible" debe permitir consultar
             * posteriormente si un patron puede ser vecino
             * de otro en una direccion determinada.
             */
            for (int d = 0; d < 4; d++)
            {
                WFCDirection dir = (WFCDirection)d;
                for (int a = 0; a < count; a++)
                {
                    if (tiles[a] == null || !tiles[a].available) continue;

                    for (int b = 0; b < count; b++)
                    {
                        if (tiles[b] == null || !tiles[b].available) continue;

                        compatible[d, a, b] = tiles[a].IsCompatible(tiles[b], dir);
                    }
                }
            }
        }

        // ========================================================
        // ETAPA 3A: OBSERVE
        // ========================================================

        /*
         * OBJETIVO:
         * Seleccionar una celda que todavia no este resuelta
         * y asignarle uno de sus patrones posibles.
         *
         * EJEMPLO:
         *
         * Antes de observar:
         *
         *   D(2,3) = {P0, P2, P5, P7}
         *
         * Si se selecciona P5:
         *
         *   D(2,3) = {P5}
         *
         * La celda queda resuelta, pero sus vecinos pueden
         * mantener patrones incompatibles con esta decision.
         *
         * Por lo tanto, debe ejecutarse la propagacion.
         */

        public bool Observe()
        {
            if (Phase != WFCPhase.ReadyToObserve)
                return false;

            /*
             * TODO 3:
             *
             * 1. Seleccionar una celda no resuelta.
             * 2. Elegir uno de sus patrones disponibles.
             * 3. Mantener solamente el patron seleccionado.
             * 4. Preparar las relaciones que requieren
             *    propagacion.
             *
             * Considerar:
             * - Primera observacion: posicion aleatoria.
             * - Observaciones siguientes: menor entropia.
             * - La eleccion del patron considera su peso.
             *
             * Actualizar Phase y Status segun corresponda.
             */

            LastPropagatedCells.Clear();

            Vector2Int targetCell = firstObservation
                ? FindRandomUnresolvedCell()
                : FindLowestEntropy();

            if (targetCell.x < 0 || targetCell.y < 0)
            {
                CheckCompletion();
                return false;
            }

            firstObservation = false;
            List<int> cellDomain = domains[targetCell.x, targetCell.y];

            int chosenPattern = ChooseWeightedPattern(cellDomain);
            if (chosenPattern < 0)
            {
                ReportContradiction(targetCell.x, targetCell.y);
                return false;
            }

            //colapsar el dominio de la celda al unico patron elegido
            cellDomain.Clear();
            cellDomain.Add(chosenPattern);

            LastObservedCell = targetCell;
            EnqueueOutgoing(targetCell.x, targetCell.y);

            Phase = WFCPhase.Propagating;
            string tileId = ActiveTileSet.Tiles[chosenPattern].id;
            Status = $"Observado ({targetCell.x},{targetCell.y}) -> Patron '{tileId}' (#{chosenPattern}). Pendiente propagar.";

            return true;
        }

        // --------------------------------------------------------
        // SELECCION DE CELDA
        // --------------------------------------------------------

        private Vector2Int FindRandomUnresolvedCell()
        {
            /*
             * TODO:
             *
             * Seleccionar una posicion aleatoria entre aquellas
             * que todavia tengan mas de un patron disponible.
             *
             * No considerar posiciones ya resueltas.
             */

            List<Vector2Int> unresolved = new List<Vector2Int>();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (domains[x, y].Count > 1)
                        unresolved.Add(new Vector2Int(x, y));
                }
            }

            if (unresolved.Count == 0)
                return new Vector2Int(-1, -1);

            int randomIndex = random.Next(unresolved.Count);
            return unresolved[randomIndex];
        }

        private Vector2Int FindLowestEntropy()
        {
            /*
             * TODO:
             *
             * Seleccionar la posicion no resuelta con
             * menor entropia positiva.
             *
             * EJEMPLO SIMPLIFICADO (pesos iguales):
             *
             * Celda A: {P0, P1, P2, P3} -> 4 posibilidades
             * Celda B: {P0, P2}         -> 2 posibilidades
             *
             * B esta mas restringida y tiene prioridad.
             *
             * Cuando los pesos son diferentes, la entropia
             * debe considerar su distribucion.
             *
             * Se puede utilizar aleatoriedad para resolver
             * empates entre celdas igualmente restringidas.
             */

            var tiles = ActiveTileSet.Tiles;
            double minEntropy = double.MaxValue;
            Vector2Int selectedCell = new Vector2Int(-1, -1);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    List<int> domain = domains[x, y];
                    if (domain.Count <= 1)
                        continue;

                    double sumWeights = 0.0;
                    double sumWeightLogWeight = 0.0;

                    for (int i = 0; i < domain.Count; i++)
                    {
                        float w = Mathf.Max(0.0001f, tiles[domain[i]].weight);
                        sumWeights += w;
                        sumWeightLogWeight += w * Math.Log(w);
                    }

                    // Entropía de Shannon + pequeño ruido aleatorio para desempatar
                    double shannonEntropy = Math.Log(sumWeights) - (sumWeightLogWeight / sumWeights);
                    double noise = random.NextDouble() * 1e-5;
                    double totalEntropy = shannonEntropy + noise;

                    if (totalEntropy < minEntropy)
                    {
                        minEntropy = totalEntropy;
                        selectedCell = new Vector2Int(x, y);
                    }
                }
            }

            return selectedCell;
        }

        // --------------------------------------------------------
        // SELECCION DE PATRON
        // --------------------------------------------------------

        private int ChooseWeightedPattern(IReadOnlyList<int> domain)
        {
            /*
             * TODO:
             *
             * Seleccionar aleatoriamente un patron del dominio
             * considerando su peso.
             *
             * EJEMPLO:
             *
             * Patron A -> peso 3
             * Patron B -> peso 1
             *
             * A tiene mayor probabilidad de seleccion,
             * pero B sigue siendo una alternativa valida.
             *
             * La seleccion debe limitarse a los patrones
             * que permanecen en el dominio.
             */

            if (domain == null || domain.Count == 0)
                return -1;

            var tiles = ActiveTileSet.Tiles;
            float totalWeight = 0f;

            for (int i = 0; i < domain.Count; i++)
                totalWeight += Mathf.Max(0.01f, tiles[domain[i]].weight);

            double sample = random.NextDouble() * totalWeight;
            float cumulative = 0f;

            for (int i = 0; i < domain.Count; i++)
            {
                cumulative += Mathf.Max(0.01f, tiles[domain[i]].weight);
                if (sample <= cumulative)
                    return domain[i];
            }

            return domain[domain.Count - 1];
        }

        // ========================================================
        // ETAPA 3B: PROPAGATE
        // ========================================================

        /*
         * OBJETIVO:
         * Actualizar los dominios de las celdas relacionadas
         * despues de una observacion o de otra reduccion.
         *
         * EJEMPLO:
         *
         * Se observa un camino horizontal en la celda A.
         * Su conexion Este esta abierta.
         *
         * La celda B, ubicada al Este, tiene:
         *
         *   D(B) = {Horizontal, Vertical}
         *
         * Horizontal tiene West = 1.
         * Vertical tiene West = 0.
         *
         * Por lo tanto, Vertical deja de ser valido:
         *
         *   D(B) = {Horizontal}
         *
         * La reduccion de B puede afectar a sus propios
         * vecinos. Este proceso continua hasta que no
         * queden restricciones pendientes.
         */

        public bool Propagate()
        {
            if (Phase != WFCPhase.Propagating)
                return false;

            /*
             * TODO 4:
             *
             * Mientras existan relaciones pendientes:
             *
             * 1. Revisar los dominios de las celdas relacionadas.
             * 2. Identificar patrones sin una alternativa
             *    compatible en la celda vecina.
             * 3. Eliminar los patrones incompatibles.
             * 4. Continuar propagando cuando existan cambios.
             *
             * CONSIDERACIONES:
             *
             * - Un dominio solamente puede mantener o reducir
             *   su cantidad de patrones durante la generacion.
             *
             * - Si queda un unico patron, la celda esta resuelta.
             *
             * - Si el dominio queda vacio, existe una contradiccion.
             *
             * - Cuando no quedan relaciones pendientes, puede
             *   continuar la siguiente observacion.
             *
             * No se requiere implementar backtracking.
             */

            LastPropagatedCells.Clear();
            int reducedCellsCount = 0;

            while (pendingArcs.Count > 0)
            {
                WFCArc arc = pendingArcs.Dequeue();

                int nx = arc.x + Offsets[arc.direction].x;
                int ny = arc.y + Offsets[arc.direction].y;

                if (!IsInBounds(nx, ny))
                    continue;

                List<int> sourceDomain = domains[arc.x, arc.y];
                List<int> neighborDomain = domains[nx, ny];

                bool neighborModified = false;

                // Revisar de atrás hacia adelante para eliminar de forma segura
                for (int i = neighborDomain.Count - 1; i >= 0; i--)
                {
                    int patternB = neighborDomain[i];
                    bool hasSupport = false;

                    // Comprobar si existe al menos un patrón A en sourceDomain compatible con patternB
                    for (int j = 0; j < sourceDomain.Count; j++)
                    {
                        int patternA = sourceDomain[j];
                        if (compatible[arc.direction, patternA, patternB])
                        {
                            hasSupport = true;
                            break;
                        }
                    }

                    if (!hasSupport)
                    {
                        neighborDomain.RemoveAt(i);
                        neighborModified = true;
                    }
                }

                if (neighborModified)
                {
                    LastPropagatedCells.Add(new Vector2Int(nx, ny));
                    reducedCellsCount++;

                    if (neighborDomain.Count == 0)
                    {
                        pendingArcs.Clear();
                        ReportContradiction(nx, ny);
                        return false;
                    }

                    // Si el vecino redujo su dominio, encolar sus propios vecinos
                    EnqueueOutgoing(nx, ny);
                }

                // Si queremos ver la propagación arco por arco en lugar de toda la ola de una vez
                if (!propagateFullStep && neighborModified)
                {
                    Status = $"Propagando... Dominio reducido en ({nx},{ny}). Arcos pendientes: {pendingArcs.Count}";
                    return true;
                }
            }

            Phase = WFCPhase.ReadyToObserve;
            Status = $"Propagacion finalizada ({reducedCellsCount} reducciones). Listo para observar.";

            CheckCompletion();
            return true;
        }

        // --------------------------------------------------------
        // HERRAMIENTAS DE PROPAGACION
        // --------------------------------------------------------

        /*
         * WFCArc representa una relacion dirigida:
         *
         *   (x,y) ---- direccion ----> vecino
         *
         * EnqueueOutgoing agrega las relaciones entre una
         * celda y sus vecinos existentes.
         *
         * La cola pendingArcs permite revisar estas relaciones
         * durante la propagacion.
         *
         * Este metodo se entrega implementado.
         */

        private void EnqueueOutgoing(int x, int y)
        {
            for (int d = 0; d < 4; d++)
            {
                int nx = x + Offsets[d].x;
                int ny = y + Offsets[d].y;

                if (IsInBounds(nx, ny))
                    pendingArcs.Enqueue(new WFCArc(x, y, d));
            }
        }

        // Se utiliza cuando alguna celda pierde todas
        // sus posibilidades de asignacion.
        private void ReportContradiction(int x, int y)
        {
            Phase = WFCPhase.Contradiction;
            Status = $"Contradiccion en ({x},{y}): dominio vacio.";
            Debug.LogWarning(Status, this);
        }

        // Comprueba si todas las posiciones tienen un unico
        // patron y ya no existen relaciones pendientes.
        private void CheckCompletion()
        {
            if (domains == null || Phase == WFCPhase.Contradiction ||
                pendingArcs.Count > 0)
                return;

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (domains[x, y] == null || domains[x, y].Count != 1)
                        return;

            Phase = WFCPhase.Completed;
            Status = "Generacion completada.";
        }

        // ========================================================
        // ETAPA 4: OUTPUT OBSERVATIONS
        // ========================================================

        /*
         * OBJETIVO:
         * Representar el estado actual de la generacion.
         *
         * Durante WFC, una celda puede encontrarse:
         *
         * - Sin resolver: mantiene multiples patrones.
         * - Resuelta: tiene un unico patron.
         * - En contradiccion: no tiene patrones disponibles.
         *
         * EJEMPLO:
         *
         *   8  4  2
         *   4  1  2
         *   8  4  8
         *
         * Los numeros representan las posibilidades restantes
         * por celda. La posicion central ya esta resuelta.
         *
         * La visualizacion deberia permitir observar como
         * los dominios cambian durante la generacion.
         *
         * Puede utilizarse SpriteRenderer, Gizmos, Canvas
         * u otros elementos de Unity.
         */

        private void OutputObservations()
        {
            /*
             * TODO 5:
             *
             * Desarrollar una visualizacion propia.
             *
             * Representar los patrones asignados y las
             * posiciones que todavia no estan resueltas.
             *
             * Debe ser posible identificar los cambios
             * producidos por Observe() y Propagate().
             *
             * La representacion se puede implementar aqui
             * o delegar a componentes creados por el grupo.
             *
             * Evitar recrear innecesariamente todos los
             * objetos graficos en cada actualizacion.
             */
            OnStateUpdated?.Invoke();
        }

        // ========================================================
        // CONSULTAS PARA LA VISUALIZACION
        // ========================================================

        // Verifica si una posicion pertenece a la matriz.
        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        // Obtiene la cantidad de patrones posibles.
        // Devuelve -1 para una posicion invalida.
        public int GetDomainCount(int x, int y)
        {
            if (!IsInitialized || !IsInBounds(x, y))
                return -1;

            return domains[x, y] != null
                ? domains[x, y].Count : -1;
        }

        // Permite consultar los patrones restantes.
        // Los valores corresponden a indices del TileSet.
        public IReadOnlyList<int> GetDomain(int x, int y)
        {
            if (!IsInitialized || !IsInBounds(x, y))
                return null;

            return domains[x, y];
        }

        // Obtiene el centro de una celda en coordenadas
        // globales, considerando las dimensiones de la matriz.
        public Vector3 GetCellWorldPosition(int x, int y)
        {
            Vector3 position = new Vector3(
                (x - (width - 1) * 0.5f) * cellSize,
                (y - (height - 1) * 0.5f) * cellSize,
                0f);

            return transform.TransformPoint(position);
        }
    }
}
