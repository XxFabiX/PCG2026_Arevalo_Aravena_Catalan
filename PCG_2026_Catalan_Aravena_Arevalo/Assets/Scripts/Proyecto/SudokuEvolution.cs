using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

// Este módulo representa la FASE DE EXPLORACIÓN GLOBAL del Pipeline.
// Implementa la Estrategia Evolutiva (mu + lambda) para diseñar un Sudoku válido.
public class SudokuEvolution : MonoBehaviour
{
    [Header("Configuración Evolutiva")]
    // mu: Cantidad de padres que sobreviven en cada generación (Los más aptos)
    public int mu = 15;

    // lambda: Cantidad de hijos generados por mutación en cada generación
    public int lambda = 45;

    // generations: Límite de ciclos evolutivos antes de detenerse
    public int generations = 100;

    // mutationRate: Probabilidad (0.0 a 1.0) de que un bloque de 3x3 sufra una mutación (intercambio)
    [Range(0f, 1f)] public float mutationRate = 0.20f;

    // Método principal: Recibe un tablero inicial (vacío) y devuelve el mejor individuo evolucionado
    public SudokuBoard Solve(SudokuBoard initialBoard)
    {
        List<SudokuBoard> parents = new List<SudokuBoard>();

        // 1. POBLACIÓN INICIAL: Crear "mu" padres aleatorios
        for (int i = 0; i < mu; i++)
        {
            SudokuBoard parent = initialBoard.Clone();
            // Preparamos el genotipo base: Rellenamos bloques de 3x3 garantizando que sean válidos
            // Esto reduce masivamente el espacio de búsqueda.
            parent.InitializeForSimulatedAnnealing();

            // Mutamos fuertemente a los padres iniciales para asegurar diversidad genética en la población
            for (int m = 0; m < 5; m++) MutateBoard(parent, 1.0f);
            parents.Add(parent);
        }

        // 2. CICLO EVOLUTIVO: Evolucionar por N generaciones
        for (int gen = 0; gen < generations; gen++)
        {
            List<SudokuBoard> offspring = new List<SudokuBoard>();

            // 2a. REPRODUCCIÓN ASECXUAL (Generar lambda hijos)
            // A diferencia de los Algoritmos Genéticos (GA), la Estrategia Evolutiva suele omitir 
            // el Crossover y basar la variación enteramente en la mutación direccional.
            for (int i = 0; i < lambda; i++)
            {
                // Selección Aleatoria: Elegimos un padre cualquiera para clonarlo
                SudokuBoard parent = parents[Random.Range(0, parents.Count)];
                SudokuBoard child = parent.Clone();

                // Mutación: Aplicamos la variación genética al hijo
                MutateBoard(child, mutationRate);
                offspring.Add(child);
            }

            // 2b. COMPETENCIA Y EVALUACIÓN (mu + lambda)
            // Unimos a padres e hijos en la misma piscina de supervivencia para que compitan juntos.
            List<SudokuBoard> combined = new List<SudokuBoard>(parents);
            combined.AddRange(offspring);

            // EVALUACIÓN (Fitness): Ordenamos la población usando la heurística CountConflicts.
            // En Sudoku, el individuo más "apto" es el que tiene MENOS conflictos.
            combined.Sort((a, b) => a.CountConflicts().CompareTo(b.CountConflicts()));

            // 2c. CONDICIÓN DE PARADA TEMPRANA
            // Si el mejor individuo (índice 0) tiene 0 conflictos, encontramos la solución global perfecta.
            if (combined[0].CountConflicts() == 0)
            {
                Debug.Log($"¡Evolución exitosa! Solución encontrada en la generación {gen}.");
                return combined[0];
            }

            // 2d. SUPERVIVENCIA (Elitismo)
            // Descartamos a la población débil y conservamos solo a los "mu" individuos más aptos 
            // para que sean los padres de la próxima generación.
            parents.Clear();
            for (int i = 0; i < mu; i++) parents.Add(combined[i]);
        }

        // Si se acaban las generaciones, devolvemos el mejor individuo encontrado,
        // el cual será refinado posteriormente por el Simulated Annealing.
        Debug.Log($"La evolución terminó. El mejor individuo quedó con {parents[0].CountConflicts()} errores.");
        return parents[0];
    }

    // --- OPERADOR DE MUTACIÓN ---
    // En el contexto del Sudoku, la mutación consiste en intercambiar (swap) dos celdas móviles
    // dentro de un mismo bloque de 3x3. Esto preserva la integridad matemática del bloque.
    private void MutateBoard(SudokuBoard candidate, float currentMutationRate)
    {
        bool mutated = false;

        // Recorremos cada uno de los 9 bloques de 3x3 del tablero
        for (int boxRow = 0; boxRow < 3; boxRow++)
        {
            for (int boxCol = 0; boxCol < 3; boxCol++)
            {
                // Evaluamos probabilísticamente si este bloque sufrirá una mutación
                if (Random.value < currentMutationRate)
                {
                    List<Vector2Int> movableCells = new List<Vector2Int>();

                    // Recopilamos todas las celdas de este bloque que NO sean pistas fijas
                    for (int r = 0; r < 3; r++)
                    {
                        for (int c = 0; c < 3; c++)
                        {
                            int globalR = boxRow * 3 + r;
                            int globalC = boxCol * 3 + c;
                            if (!candidate.fixedCells[globalR, globalC])
                            {
                                movableCells.Add(new Vector2Int(globalR, globalC));
                            }
                        }
                    }

                    // Se requieren al menos 2 celdas libres para efectuar un intercambio
                    if (movableCells.Count >= 2)
                    {
                        // Selección aleatoria de 2 índices distintos
                        int idx1 = Random.Range(0, movableCells.Count);
                        int idx2 = Random.Range(0, movableCells.Count);
                        while (idx1 == idx2) idx2 = Random.Range(0, movableCells.Count);

                        Vector2Int cell1 = movableCells[idx1];
                        Vector2Int cell2 = movableCells[idx2];

                        // Ejecución de la Mutación Genética (Swap de valores)
                        int temp = candidate.GetCell(cell1.x, cell1.y);
                        candidate.SetCell(cell1.x, cell1.y, candidate.GetCell(cell2.x, cell2.y));
                        candidate.SetCell(cell2.x, cell2.y, temp);

                        mutated = true;
                    }
                }
            }
        }

        // --- GARANTÍA DE VARIACIÓN ---
        // Si por azar estadístico ninguna mutación ocurrió en todo el tablero,
        // forzamos recursivamente una mutación con 100% de probabilidad.
        // Esto asegura que cada "hijo" sea genuinamente distinto a su "padre", evitando estancamiento.
        if (!mutated) MutateBoard(candidate, 1.0f);
    }
}