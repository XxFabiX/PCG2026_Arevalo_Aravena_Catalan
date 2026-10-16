using UnityEngine;

// Este módulo representa la FASE FINAL de la tubería de generación procedural (Pipeline).
// Se encarga de aplicar la técnica "Backward from Goal State".
public class SudokuBackward : MonoBehaviour
{
    [Header("Parámetros Backward")]
    // Cantidad de casillas por defecto a borrar si no se especifica otra.
    // A mayor número, menos pistas tendrá el jugador y más difícil será el Sudoku.
    public int defaultCellsToRemove = 40;

    // Método principal que recibe un tablero (el "Goal State" perfecto generado por la IA)
    // y lo transforma en el puzle incompleto que jugará el usuario.
    public SudokuBoard Apply(SudokuBoard board, int cellsToRemove)
    {
        int removed = 0;

        // Entramos en un ciclo que se repetirá hasta alcanzar la cuota exacta de celdas a borrar
        while (removed < cellsToRemove)
        {
            // 1. Seleccionamos coordenadas completamente al azar (filas y columnas del 0 al 8)
            int randRow = Random.Range(0, 9);
            int randCol = Random.Range(0, 9);

            // 2. Verificamos que la celda seleccionada tenga un número visible (que no sea 0)
            // Esto evita que intentemos borrar una celda que ya está en blanco.
            if (board.GetCell(randRow, randCol) != 0)
            {
                // 3. Borramos la pista estableciendo su valor en 0 (nuestra representación de celda vacía)
                board.SetCell(randRow, randCol, 0);

                // 4. Confirmamos el borrado exitoso sumando 1 al contador
                removed++;
            }
        }

        // Retornamos el tablero modificado. Ahora es un desafío incompleto listo para la interfaz.
        return board;
    }
}