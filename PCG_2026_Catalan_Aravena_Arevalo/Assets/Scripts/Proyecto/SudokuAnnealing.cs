using System.Collections.Generic;
using UnityEngine;

public class SudokuAnnealing : MonoBehaviour
{
    [Header("Parámetros SA")]
    public float initialTemperature = 1000f;
    public float coolingRate = 0.9999f;
    public int maxIterations = 200000;

    public SudokuBoard Solve(SudokuBoard board)
    {
        InitializeForSimulatedAnnealing(board);
        float currentTemp = initialTemperature;
        int currentConflicts = board.CountConflicts();

        for (int i = 0; i < maxIterations; i++)
        {
            if (currentConflicts == 0)
            {
                Debug.Log($"¡SA encontró la solución perfecta en {i} iteraciones!");
                break;
            }

            int boxRow = Random.Range(0, 3);
            int boxCol = Random.Range(0, 3);

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

            if (movableCells.Count < 2) continue;

            int idx1 = Random.Range(0, movableCells.Count);
            int idx2 = Random.Range(0, movableCells.Count);
            while (idx1 == idx2) idx2 = Random.Range(0, movableCells.Count);

            Vector2Int cell1 = movableCells[idx1];
            Vector2Int cell2 = movableCells[idx2];

            int tempVal = board.GetCell(cell1.x, cell1.y);
            board.SetCell(cell1.x, cell1.y, board.GetCell(cell2.x, cell2.y));
            board.SetCell(cell2.x, cell2.y, tempVal);

            int newConflicts = board.CountConflicts();
            int delta = newConflicts - currentConflicts;

            if (delta < 0)
            {
                currentConflicts = newConflicts;
            }
            else
            {
                float acceptanceProbability = Mathf.Exp(-delta / currentTemp);

                if (Random.value < acceptanceProbability)
                {
                    currentConflicts = newConflicts;
                }
                else
                {
                    tempVal = board.GetCell(cell1.x, cell1.y);
                    board.SetCell(cell1.x, cell1.y, board.GetCell(cell2.x, cell2.y));
                    board.SetCell(cell2.x, cell2.y, tempVal);
                }
            }
            currentTemp *= coolingRate;
        }

        if (currentConflicts > 0)
            Debug.Log($"SA se detuvo. Quedaron {currentConflicts} conflictos.");

        return board;
    }

    private void InitializeForSimulatedAnnealing(SudokuBoard board)
    {
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                board.fixedCells[r, c] = (board.GetCell(r, c) != 0);

        for (int boxRow = 0; boxRow < 3; boxRow++)
            for (int boxCol = 0; boxCol < 3; boxCol++)
                FillBoxWithMissingNumbers(board, boxRow, boxCol);
    }

    private void FillBoxWithMissingNumbers(SudokuBoard board, int boxRow, int boxCol)
    {
        List<int> available = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
            {
                int val = board.GetCell(boxRow * 3 + r, boxCol * 3 + c);
                if (val != 0) available.Remove(val);
            }

        for (int r = 0; r < 3; r++)
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