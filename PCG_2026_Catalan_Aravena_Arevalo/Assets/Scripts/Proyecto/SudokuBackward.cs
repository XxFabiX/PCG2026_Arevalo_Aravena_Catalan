using UnityEngine;

public class SudokuBackward : MonoBehaviour
{
    [Header("Parámetros Backward")]
    public int defaultCellsToRemove = 40;

    public void CreateMathematicalBase(SudokuBoard board)
    {
        //formula
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                board.SetCell(r, c, ((r * 3 + r / 3 + c) % 9) + 1);
            }
        }

        //Shuffle
        int band = Random.Range(0, 3);
        int r1 = band * 3 + Random.Range(0, 3);
        int r2 = band * 3 + Random.Range(0, 3);

        for (int c = 0; c < 9; c++)
        {
            int temp = board.GetCell(r1, c);
            board.SetCell(r1, c, board.GetCell(r2, c));
            board.SetCell(r2, c, temp);
        }
    }

    //borra las celdas
    public void RemoveCells(SudokuBoard board, int cellsToRemove)
    {
        int removed = 0;
        while (removed < cellsToRemove)
        {
            int randRow = Random.Range(0, 9);
            int randCol = Random.Range(0, 9);

            if (board.GetCell(randRow, randCol) != 0)
            {
                board.SetCell(randRow, randCol, 0);
                removed++;
            }
        }
    }
}