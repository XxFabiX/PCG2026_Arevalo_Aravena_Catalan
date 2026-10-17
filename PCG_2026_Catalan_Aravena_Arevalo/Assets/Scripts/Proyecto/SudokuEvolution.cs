using System.Collections.Generic;
using UnityEngine;

public class SudokuEvolution : MonoBehaviour
{
    [Header("Configuración Evolutiva")]
    public int mu = 30;
    public int lambda = 100;
    public int generations = 1000;
    [Range(0f, 1f)] public float mutationRate = 0.20f;

    public SudokuBoard Solve(SudokuBoard initialBoard)
    {
        List<SudokuBoard> parents = new List<SudokuBoard>();

        for (int i = 0; i < mu; i++)
        {
            SudokuBoard parent = initialBoard.Clone();
            InitializeBlocks(parent);
            for (int m = 0; m < 5; m++) MutateBoard(parent, 1.0f);
            parents.Add(parent);
        }

        for (int gen = 0; gen < generations; gen++)
        {
            List<SudokuBoard> offspring = new List<SudokuBoard>();

            for (int i = 0; i < lambda; i++)
            {
                SudokuBoard parent = parents[Random.Range(0, parents.Count)];
                SudokuBoard child = parent.Clone();
                MutateBoard(child, mutationRate);
                offspring.Add(child);
            }

            List<SudokuBoard> combined = new List<SudokuBoard>(parents);
            combined.AddRange(offspring);
            combined.Sort((a, b) => a.CountConflicts().CompareTo(b.CountConflicts()));

            if (combined[0].CountConflicts() == 0)
            {
                Debug.Log($"¡Evolución exitosa! Solución perfecta en generación {gen}.");
                return combined[0];
            }

            parents.Clear();
            for (int i = 0; i < mu; i++) parents.Add(combined[i]);
        }

        Debug.Log($"La evolución terminó. El mejor individuo quedó con {parents[0].CountConflicts()} errores.");
        return parents[0];
    }

    private void MutateBoard(SudokuBoard candidate, float currentMutationRate)
    {
        bool mutated = false;
        for (int boxRow = 0; boxRow < 3; boxRow++)
        {
            for (int boxCol = 0; boxCol < 3; boxCol++)
            {
                if (Random.value < currentMutationRate)
                {
                    List<Vector2Int> movableCells = new List<Vector2Int>();
                    for (int r = 0; r < 3; r++)
                    {
                        for (int c = 0; c < 3; c++)
                        {
                            int globalR = boxRow * 3 + r;
                            int globalC = boxCol * 3 + c;
                            if (!candidate.fixedCells[globalR, globalC])
                                movableCells.Add(new Vector2Int(globalR, globalC));
                        }
                    }

                    if (movableCells.Count >= 2)
                    {
                        int idx1 = Random.Range(0, movableCells.Count);
                        int idx2 = Random.Range(0, movableCells.Count);
                        while (idx1 == idx2) idx2 = Random.Range(0, movableCells.Count);

                        Vector2Int cell1 = movableCells[idx1];
                        Vector2Int cell2 = movableCells[idx2];

                        int temp = candidate.GetCell(cell1.x, cell1.y);
                        candidate.SetCell(cell1.x, cell1.y, candidate.GetCell(cell2.x, cell2.y));
                        candidate.SetCell(cell2.x, cell2.y, temp);
                        mutated = true;
                    }
                }
            }
        }
        if (!mutated) MutateBoard(candidate, 1.0f);
    }

    //inicializa el tablero para la IA (rellenando bloques 3x3)
    private void InitializeBlocks(SudokuBoard board)
    {
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                board.fixedCells[r, c] = (board.GetCell(r, c) != 0);

        for (int boxRow = 0; boxRow < 3; boxRow++)
        {
            for (int boxCol = 0; boxCol < 3; boxCol++)
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
    }
}