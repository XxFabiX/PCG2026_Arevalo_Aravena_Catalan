using UnityEngine;

public class SudokuManager : MonoBehaviour
{
    void Start()
    {
        SudokuBoard board = new SudokuBoard();
        board.GenerateBackward(45);

        Debug.Log("¡Tablero generado hacia atrás con éxito!");
        Debug.Log("Conflictos en el puzle generado: " + board.CountConflicts());

        PrintBoard(board);
    }

    void PrintBoard(SudokuBoard board)
    {
        string boardString = "";
        int[,] raw = board.GetRawGrid();
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                boardString += (raw[r, c] == 0 ? "." : raw[r, c].ToString()) + " ";
            }
            boardString += "\n";
        }
        Debug.Log("Estado del Tablero:\n" + boardString);
    }
}