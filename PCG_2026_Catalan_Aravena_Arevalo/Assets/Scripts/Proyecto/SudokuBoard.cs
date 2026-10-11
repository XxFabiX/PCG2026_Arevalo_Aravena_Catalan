using System.Collections.Generic;
using UnityEngine;

public class SudokuBoard
{
    // matriz bidimensional del tablero de 9x9 el 0 indica las vacias
    private int[,] grid;

    // inicializamos el tablero
    public SudokuBoard()
    {
        grid = new int[9, 9];
    }

    //clonamos un tablero copia en caso de tener que usar estados antiguos del tablero
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

    // metodos para leer y modificar las ceeldas
    public int GetCell(int row, int col) => grid[row, col];
    public void SetCell(int row, int col, int value) => grid[row, col] = value;
    public int[,] GetRawGrid() => grid;

    // funcion euristica o el fitness
    //cuenta los numeros de repetidos el que tiene 0 conflictos es el mejor
    public int CountConflicts()
    {
        int conflicts = 0;

        
        for (int i = 0; i < 9; i++)
        {
            // arreglo para contar las veces que salen los numeros del 1 al 9 en cada fila y columna
            int[] rowCounts = new int[10];
            int[] colCounts = new int[10];

            for (int j = 0; j < 9; j++)
            {
                int rowVal = grid[i, j];
                int colVal = grid[j, i]; 

                // si la celda no esta vacia sumamos 1
                if (rowVal != 0) rowCounts[rowVal]++;
                if (colVal != 0) colCounts[colVal]++;
            }

            //revisamos los contadores y si hay un numero repetido lo sumamos a los conflictos
            for (int val = 1; val <= 9; val++)
            {
                if (rowCounts[val] > 1) conflicts += (rowCounts[val] - 1);
                if (colCounts[val] > 1) conflicts += (colCounts[val] - 1);
            }
        }

        // se subdivide en grillas de 3x3
        // revisamos los bloques de 3x3 en caso de tener repetidos.
        for (int boxRow = 0; boxRow < 3; boxRow++)
        {
            for (int boxCol = 0; boxCol < 3; boxCol++)
            {
                int[] boxCounts = new int[10];
                for (int r = 0; r < 3; r++)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        // calcula las coordenadas a partir del bloque
                        int val = grid[boxRow * 3 + r, boxCol * 3 + c];
                        if (val != 0) boxCounts[val]++;
                    }
                }

                //sumamos conflictos dentro de bloques de 3x3
                for (int val = 1; val <= 9; val++)
                {
                    if (boxCounts[val] > 1) conflicts += (boxCounts[val] - 1);
                }
            }
        }

        return conflicts;
    }

    // aplicamos backward from goal state.
    // genera un tablero resuelto matematicamente y quita las pistas
    public void GenerateBackward(int cellsToRemove = 40)
    {
        // genera un patron base
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                grid[r, c] = ((r * 3 + r / 3 + c) % 9) + 1;
            }
        }

        // mezcla para no tener siempre la misma base
        ShuffleBoard();

        //oculta celdas mientras retrocede
        int removed = 0;
        while (removed < cellsToRemove)
        {
            int randRow = Random.Range(0, 9);
            int randCol = Random.Range(0, 9);

            if (grid[randRow, randCol] != 0)
            {
                grid[randRow, randCol] = 0; //aqui las ocultamos poniendolas en 0
                removed++;
            }
        }
    }

    // mezcla sin romper la validez matematica
    // limitamos los intercambios solo a bandas horizontales de los 3x3
    private void ShuffleBoard()
    {
        int band = Random.Range(0, 3); // elige una banda 
        int r1 = band * 3 + Random.Range(0, 3); 
        int r2 = band * 3 + Random.Range(0, 3); 

        for (int c = 0; c < 9; c++)
        {
            int temp = grid[r1, c];
            grid[r1, c] = grid[r2, c];
            grid[r2, c] = temp;
        }
    }

    // verifica si el tablero esta full
    public bool IsBoardFull()
    {
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                // si hay un 0 no esta completo
                if (grid[r, c] == 0) return false;
            }
        }
        return true;
    }

    // matriz para recordar al algoritmo que celdas son modificables
    public bool[,] fixedCells = new bool[9, 9];

    //*******************Simulated Annealing ***********************

    // Primero preparamos el tablero rellando celdas vacias evitando romper las grillas de 3x3
    // Esto reduce drásticamente el espacio de búsqueda para la IA, ya que los bloques siempre serán válidos.
    public void InitializeForSimulatedAnnealing()
    {
        // se identifican las pistas fijas del tablero
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                fixedCells[r, c] = (grid[r, c] != 0);
            }
        }

        // se rellenan las celdas vacias asegurandose de que cada subgrilla de 3x3 tenga los numeros del 1 al 9
        for (int boxRow = 0; boxRow < 3; boxRow++)
        {
            for (int boxCol = 0; boxCol < 3; boxCol++)
            {
                FillBoxWithMissingNumbers(boxRow, boxCol);
            }
        }
    }

    // Encuentra los numeros faltan en un bloque 3x3 y los distribuye aleatoriamente en las celdas vacias.
    private void FillBoxWithMissingNumbers(int boxRow, int boxCol)
    {
        List<int> available = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        // se descartan los numeros que ya son pistas en los bloques de 3x3
        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                int val = grid[boxRow * 3 + r, boxCol * 3 + c];
                if (val != 0) available.Remove(val);
            }
        }

        // se asignan los numeros faltantes en celdas vacias de manera aleatoria
        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                if (grid[boxRow * 3 + r, boxCol * 3 + c] == 0)
                {
                    int randIdx = Random.Range(0, available.Count);
                    grid[boxRow * 3 + r, boxCol * 3 + c] = available[randIdx];
                    available.RemoveAt(randIdx);
                }
            }
        }
    }


    //aca se establece el motor con los parametros establecidos en el informe que serian temperatura, etc
    public void SolveWithSimulatedAnnealing(float initialTemperature = 100f, float coolingRate = 0.999f, int maxIterations = 50000)
    {
        InitializeForSimulatedAnnealing();

        float currentTemp = initialTemperature;
        int currentConflicts = CountConflicts(); // se reutiliza la euristica que ya estaba

        for (int i = 0; i < maxIterations; i++)
        {
            // condicion de la victoria
            if (currentConflicts == 0)
            {
                Debug.Log($"¡Sudoku resuelto por IA en {i} iteraciones!");
                break;
            }

            //escogemos un bloque de 3x3 al azar
            int boxRow = Random.Range(0, 3);
            int boxCol = Random.Range(0, 3);

            // buscamos celdas modificables que no sean de las fijas en ese bloque
            List<Vector2Int> movableCells = new List<Vector2Int>();
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    int globalR = boxRow * 3 + r;
                    int globalC = boxCol * 3 + c;
                    if (!fixedCells[globalR, globalC])
                    {
                        movableCells.Add(new Vector2Int(globalR, globalC));
                    }
                }
            }

            // regla si hay 2 celdas modificables en el bloque no hacemos mas cambios
            // Necesitamos al menos 2 celdas libres para poder intercambiar (swap) sus valores.
            if (movableCells.Count < 2) continue;

            // regla de los vecinos intercambiamos 2 celdas aleatorias del mismo bloque
            // Esto asegura que generamos un "vecino" nuevo sin destruir la regla matematica del bloque 3x3.
            int idx1 = Random.Range(0, movableCells.Count);
            int idx2 = Random.Range(0, movableCells.Count);
            while (idx1 == idx2) idx2 = Random.Range(0, movableCells.Count);

            Vector2Int cell1 = movableCells[idx1];
            Vector2Int cell2 = movableCells[idx2];

            //hacemos el swap
            int tempVal = grid[cell1.x, cell1.y];
            grid[cell1.x, cell1.y] = grid[cell2.x, cell2.y];
            grid[cell2.x, cell2.y] = tempVal;

            //se aplica la funcion de evaluacion
            int newConflicts = CountConflicts();
            int delta = newConflicts - currentConflicts;

            // aplicamos el cirterio de aceptacion segun la temperatura
            if (delta < 0)
            {
                // si es un vecino MEJOR o que tiene menos conflictos, se acepta de una
                currentConflicts = newConflicts;
            }
            else
            {
                //si es un vecino PEOR (tiene muchos conflictos), evaluamos la probabilidad igual
                // Esto permite realizar movimientos "hacia abajo" para escapar de maximos/minimos locales[cite: 16].
                float acceptanceProbability = Mathf.Exp(-delta / currentTemp);
                if (Random.value < acceptanceProbability)
                {
                    currentConflicts = newConflicts; // aca se acepta a pesar de ser peor
                }
                else
                {
                    // aca nos aseguramos de deshacer el intercambio si no es viable
                    tempVal = grid[cell1.x, cell1.y];
                    grid[cell1.x, cell1.y] = grid[cell2.x, cell2.y];
                    grid[cell2.x, cell2.y] = tempVal;
                }
            }

            // reduce la temperatura lentamente
            // A menor temperatura menor es la probabilidad de aceptar vecinos peores enfocando la busqueda en el optimo global.
            currentTemp *= coolingRate;
        }

        if (currentConflicts > 0)
            Debug.Log($"SA termino. Quedaron {currentConflicts} conflictos.");
    }
}