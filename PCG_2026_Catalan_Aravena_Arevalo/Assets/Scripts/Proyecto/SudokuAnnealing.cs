using System.Collections.Generic;
using UnityEngine;

// Este módulo representa la FASE DE REFINAMIENTO (Búsqueda Local) del Pipeline.
// Se encarga de tomar un tablero y llevar sus conflictos a 0 usando Simulated Annealing.
public class SudokuAnnealing : MonoBehaviour
{
    [Header("Parámetros SA")]
    public float initialTemperature = 1000f; // Temperatura alta permite escapar de mínimos locales al inicio
    public float coolingRate = 0.9999f;      // Qué tan rápido se vuelve estricto el algoritmo
    public int maxIterations = 200000;       // Límite de seguridad para evitar ciclos infinitos

    public SudokuBoard Solve(SudokuBoard board)
    {
        // 1. PREPARACIÓN: Rellenamos el tablero inteligentemente para reducir el espacio de búsqueda.
        InitializeForSimulatedAnnealing(board);

        float currentTemp = initialTemperature;

        // Evaluamos el estado inicial usando la heurística compartida del tablero
        int currentConflicts = board.CountConflicts();

        // 2. CICLO DE BÚSQUEDA LOCAL
        for (int i = 0; i < maxIterations; i++)
        {
            // Condición de éxito: Si llegamos a 0 conflictos, el Sudoku es perfecto.
            if (currentConflicts == 0)
            {
                Debug.Log($"¡SA encontró la solución perfecta en {i} iteraciones!");
                break;
            }

            // --- DEFINICIÓN DEL VECINDARIO ---
            // Elegimos un bloque de 3x3 al azar
            int boxRow = Random.Range(0, 3);
            int boxCol = Random.Range(0, 3);

            // Buscamos qué celdas podemos modificar (que no sean pistas fijas) en este bloque
            List<Vector2Int> movableCells = new List<Vector2Int>();
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    int globalR = boxRow * 3 + r;
                    int globalC = boxCol * 3 + c;
                    if (!board.fixedCells[globalR, globalC])
                    {
                        movableCells.Add(new Vector2Int(globalR, globalC));
                    }
                }
            }

            // Necesitamos al menos 2 celdas libres para poder intercambiarlas
            if (movableCells.Count < 2) continue;

            // Seleccionamos 2 celdas distintas al azar
            int idx1 = Random.Range(0, movableCells.Count);
            int idx2 = Random.Range(0, movableCells.Count);
            while (idx1 == idx2) idx2 = Random.Range(0, movableCells.Count);

            Vector2Int cell1 = movableCells[idx1];
            Vector2Int cell2 = movableCells[idx2];

            // Generamos el "Vecino" haciendo un Swap (intercambio)
            // Como el swap es dentro del mismo bloque 3x3, el bloque sigue siendo matemáticamente válido.
            int tempVal = board.GetCell(cell1.x, cell1.y);
            board.SetCell(cell1.x, cell1.y, board.GetCell(cell2.x, cell2.y));
            board.SetCell(cell2.x, cell2.y, tempVal);

            // --- FUNCIÓN DE EVALUACIÓN ---
            int newConflicts = board.CountConflicts();
            int delta = newConflicts - currentConflicts;

            // --- CRITERIO DE ACEPTACIÓN TERMODINÁMICO ---
            if (delta < 0)
            {
                // Si el vecino es MEJOR (menos conflictos), lo aceptamos inmediatamente
                currentConflicts = newConflicts;
            }
            else
            {
                // Si el vecino es PEOR, calculamos la probabilidad de aceptarlo de todos modos
                // Esto nos permite escapar de mínimos locales en el espacio de búsqueda.
                float acceptanceProbability = Mathf.Exp(-delta / currentTemp);

                if (Random.value < acceptanceProbability)
                {
                    currentConflicts = newConflicts; // Aceptamos el error temporalmente
                }
                else
                {
                    // Rechazamos el vecino: Deshacemos el intercambio para volver al estado anterior
                    tempVal = board.GetCell(cell1.x, cell1.y);
                    board.SetCell(cell1.x, cell1.y, board.GetCell(cell2.x, cell2.y));
                    board.SetCell(cell2.x, cell2.y, tempVal);
                }
            }

            // Enfriamos el sistema: A menor temperatura, menor probabilidad de aceptar errores
            currentTemp *= coolingRate;
        }

        if (currentConflicts > 0)
            Debug.Log($"SA se detuvo. Quedaron {currentConflicts} conflictos.");

        return board; // Devolvemos el tablero pulido
    }

    // Método auxiliar para preparar el tablero antes de la búsqueda
    private void InitializeForSimulatedAnnealing(SudokuBoard board)
    {
        // Identificamos las pistas que no se pueden tocar
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                board.fixedCells[r, c] = (board.GetCell(r, c) != 0);
            }
        }

        // Rellenamos las celdas vacías asegurando que cada bloque de 3x3 tenga los números del 1 al 9
        for (int boxRow = 0; boxRow < 3; boxRow++)
        {
            for (int boxCol = 0; boxCol < 3; boxCol++)
            {
                FillBoxWithMissingNumbers(board, boxRow, boxCol);
            }
        }
    }

    private void FillBoxWithMissingNumbers(SudokuBoard board, int boxRow, int boxCol)
    {
        List<int> available = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        // Descartamos los números que ya existen en el bloque
        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                int val = board.GetCell(boxRow * 3 + r, boxCol * 3 + c);
                if (val != 0) available.Remove(val);
            }
        }

        // Repartimos los números faltantes en las celdas vacías al azar
        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                if (board.GetCell(boxRow * 3 + r, boxCol * 3 + c) == 0)
                {
                    int randIdx = Random.Range(0, available.Count);
                    board.SetCell(boxRow * 3 + r, boxCol * 3 + c, available[randIdx]);
                    available.RemoveAt(randIdx);
                }
            }
        }
    }
}