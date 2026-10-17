using System;

public class SudokuBoard
{
    private int[,] grid;
    public bool[,] fixedCells = new bool[9, 9];

    public SudokuBoard()
    {
        grid = new int[9, 9];
    }

    public SudokuBoard(int[,] sourceGrid)
    {
        grid = new int[9, 9];
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
                grid[row, col] = sourceGrid[row, col];
    }

    public int GetCell(int row, int col) => grid[row, col];
    public void SetCell(int row, int col, int value) => grid[row, col] = value;
    public int[,] GetRawGrid() => grid;

    //funcion heuristica que evalua el fitness
    public int CountConflicts()
    {
        int conflicts = 0;
        int[] rowCounts = new int[10];
        int[] colCounts = new int[10];

        for (int i = 0; i < 9; i++)
        {
            Array.Clear(rowCounts, 0, 10);
            Array.Clear(colCounts, 0, 10);

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

    public bool IsBoardFull()
    {
        for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                if (grid[r, c] == 0) return false;
        return true;
    }

    public SudokuBoard Clone()
    {
        SudokuBoard copy = new SudokuBoard(this.grid);
        Array.Copy(this.fixedCells, copy.fixedCells, this.fixedCells.Length);
        return copy;
    }
}