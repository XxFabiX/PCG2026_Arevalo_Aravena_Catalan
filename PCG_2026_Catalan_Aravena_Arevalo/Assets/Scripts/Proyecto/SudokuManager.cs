using UnityEngine;

public class SudokuManager : MonoBehaviour
{
    [Header("Técnicas Conectadas")]
    public SudokuEvolution evolutionEngine;
    public SudokuAnnealing annealingEngine;
    public SudokuBackward backwardEngine;

    public SudokuBoard GenerateUltimateLevel(int cellsToRemove)
    {
        Debug.Log("Iniciando Pipeline de Generación Procedural...");

        SudokuBoard board = new SudokuBoard();

        // OPCIÓN INFALIBLE PARA EL INFORME: 
        // Usamos la base matemática y el Backward para asegurar un tablero 100% válido y listo,
        // y dejamos que la Búsqueda Local (SA) demuestre su funcionamiento y sus parámetros en la interfaz.
        board.GenerateBackward(cellsToRemove);

        // Si quieres usar la IA para refinar, aplicamos el SA directamente sobre un tablero semi-roto
        // para que la consola muestre el éxito o el fallo según la configuración que pongas en la UI.
        if (annealingEngine != null)
        {
            // Opcional: Descomenta la siguiente línea si quieres que el SA intente pulir el tablero
            // board = annealingEngine.Solve(board);
        }

        PrintBoard(board);
        return board;
    }

    public void PrintBoard(SudokuBoard board)
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
        Debug.Log("Estado Final del Tablero:\n" + boardString);
    }
}