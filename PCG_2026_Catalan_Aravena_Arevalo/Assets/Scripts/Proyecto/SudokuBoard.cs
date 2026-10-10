using UnityEngine;

public class SudokuBoard
{
    private int[,] grid;

    public SudokuBoard()
    {
        grid = new int[9, 9];
    }

    public SudokuBoard(int[,] sourceGrid)
    {
        grid = new int[9, 9];
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                grid[row, col] = sourceGrid[row, col];
            }
        }
    }

    public int GetCell(int row, int col) => grid[row, col];
    public void SetCell(int row, int col, int value) => grid[row, col] = value;
    public int[,] GetRawGrid() => grid;

    public int CountConflicts()
    {
        int conflicts = 0;

        // 1. Filas y columnas
        for (int i = 0; i < 9; i++)
        {
            int[] rowCounts = new int[10];
            int[] colCounts = new int[10];

            for (int j = 0; j < 9; j++)
            {
                int rowVal = grid[i, j];
                int colVal = grid[j, i];

                if (rowVal != 0) rowCounts[rowVal]++;
                if (colVal != 0) colCounts[colVal]++;
            }

            for (int val = 1; val <= 9; val++)
            {
                if (rowCounts[val] > 1) conflicts += (rowCounts[val] - 1);
                if (colCounts[val] > 1) conflicts += (colCounts[val] - 1);
            }
        }

        // 2. Subgrillas de 3x3
        for (int boxRow = 0; boxRow < 3; boxRow++)
        {
            for (int boxCol = 0; boxCol < 3; boxCol++)
            {
                int[] boxCounts = new int[10];
                for (int r = 0; r < 3; r++)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        int val = grid[boxRow * 3 + r, boxCol * 3 + c];
                        if (val != 0) boxCounts[val]++;
                    }
                }

                for (int val = 1; val <= 9; val++)
                {
                    if (boxCounts[val] > 1) conflicts += (boxCounts[val] - 1);
                }
            }
        }

        return conflicts;
    }

    public void GenerateBackward(int cellsToRemove = 40)
    {
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                grid[r, c] = ((r * 3 + r / 3 + c) % 9) + 1;
            }
        }

        ShuffleBoard();

        int removed = 0;
        while (removed < cellsToRemove)
        {
            int randRow = Random.Range(0, 9);
            int randCol = Random.Range(0, 9);

            if (grid[randRow, randCol] != 0)
            {
                grid[randRow, randCol] = 0;
                removed++;
            }
        }
    }

    private void ShuffleBoard()
    {
        int band = Random.Range(0, 3);
        int r1 = band * 3 + Random.Range(0, 3);
        int r2 = band * 3 + Random.Range(0, 3);

        for (int c = 0; c < 9; c++)
        {
            int temp = grid[r1, c];
            grid[r1, c] = grid[r2, c];
            grid[r2, c] = temp;
        }
    }
    public bool IsBoardFull()
    {
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                // Si encuentra al menos un 0, el tablero no está lleno
                if (grid[r, c] == 0) return false;
            }
        }
        return true;
    }
}