using UnityEngine;

public class SudokuManager : MonoBehaviour
{
    [Header("Técnicas Conectadas")]
    public SudokuEvolution evolutionEngine;
    public SudokuAnnealing annealingEngine;
    public SudokuBackward backwardEngine;

    public SudokuBoard GenerateUltimateLevel(int cellsToRemove)
    {
        Debug.Log("Iniciando Pipeline de Generación...");
        SudokuBoard board = new SudokuBoard();

        // FASE 1: La Evolución intenta diseñar la solución perfecta
        if (evolutionEngine != null)
        {
            board = evolutionEngine.Solve(board);
        }

        // FASE 2: Si la evolución no pudo llegar a 0, el Annealing intenta pulir los errores
        if (annealingEngine != null && board.CountConflicts() > 0)
        {
            Debug.Log("La Evolución dejó errores. Pasando a Simulated Annealing...");
            board = annealingEngine.Solve(board);
        }

        // FASE 3 (GARANTÍA DE SOLUBILIDAD): Si la IA falló por completo, usamos la matemática
        if (board.CountConflicts() > 0)
        {
            Debug.LogWarning("La IA se atascó. Aplicando Fórmula Matemática para garantizar que el juego sea resoluble.");
            backwardEngine.CreateMathematicalBase(board);
        }

        // FASE 4: Borramos las celdas al tablero final (sea de la IA o el Matemático)
        if (backwardEngine != null)
        {
            backwardEngine.RemoveCells(board, cellsToRemove);
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