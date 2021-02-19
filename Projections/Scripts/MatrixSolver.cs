using Godot;
using System;

public class MatrixSolver : Node
{

    private const float TOLERANCE = 0.001f;
    private static int numOfIterations;
    public override void _Ready()
    {
        numOfIterations = 0;
        //float[,] matrix = new float[,] {{1, 2, 1}, {1, 2, 2}, {2, 4, 3}, {3, 6, 4}};
        //solveMatrix(matrix.GetLength(0), matrix.GetLength(1), matrix);
    }

    private static void swapRows(int columns, float[,] matrix, int rowA, int rowB)
    {
        float temp = 0;
        for (int col = 0; col < columns; col++)
        {
            temp = matrix[rowA, col];
            matrix[rowA, col] = matrix[rowB, col];
            matrix[rowB, col] = temp;
        }
    }

    private static void divideRow(int columns, float[,] matrix, int row, float divisor)
    {
        for (int col = 0; col < columns; col++)
        {
            matrix[row, col] /= divisor;
        }
    }

    // Adds coefficient * rowB to rowA
    private static void addRowMultiple(int columns, float[,] matrix, int rowA, int rowB, float coefficient)
    {
        for (int col = 0; col < columns; col++)
        {
            matrix[rowA, col] += coefficient * matrix[rowB, col];
        }
    }

    // Takes an augmented matrix to (almost) RREF (with leading entries on the main diagonal) and returns whether it has a unique solution
    public static bool solveMatrix(int rows, int columns, float[,] matrix, bool augmented)
    {
        bool isUnique = true;

        // Boolean to check if first entry of row is nonzero (for division)
        bool divisible = false;

        // Find minimum of rows and columns
        if (rows < columns)
        {
            numOfIterations = rows;
        }
        else if (augmented)
        {
            numOfIterations = columns - 1;
        }


        // Iterate over the minimum to reduce to RREF
        for (int iteration = 0; iteration < numOfIterations; iteration++)
        {
            // Swap the iteration row to be one with a leading entry (if possible)
            // Look only at incomplete rows (with index higher than iteration number)
            for (int row = iteration; row < rows; row++)
            {
                if (matrix[row, iteration] != 0)
                {
                    swapRows(columns, matrix, iteration, row);
                    divisible = true;
                    break;
                }
                divisible = false;
            }

            // Divide row by first entry (if nonzero) and subtract from all rows other than iteration row (with leading 1)
            if (divisible)
            {
                divideRow(columns, matrix, iteration, matrix[iteration, iteration]);
                for (int row = 0; row < rows; row++)
                {
                    if (row != iteration)
                    {
                        addRowMultiple(columns, matrix, row, iteration, -matrix[row, iteration]);
                    }
                }
            }
        }

        if (augmented)
        {
            for (int entry = 0; entry < numOfIterations; entry++)
            {
                if (Math.Abs(matrix[entry, entry]) < TOLERANCE)
                {
                    isUnique = false;
                    break;
                }
            }
        }

        //printMatrix(rows, columns, matrix);
        return isUnique;
    }

    public static void convertTo3x3(Vector3 column1, Vector3 column2, Vector3 column3, float[,] matrix)
    {
        matrix[0, 0] = column1.x;
        matrix[1, 0] = column1.y;
        matrix[2, 0] = column1.z;
        matrix[0, 1] = column2.x;
        matrix[1, 1] = column2.y;
        matrix[2, 1] = column2.z;
        matrix[0, 2] = column3.x;
        matrix[1, 2] = column3.y;
        matrix[2, 2] = column3.z;
    }

    public static void printMatrix(int rows, int columns, float[,] matrix)
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                GD.PrintRaw(matrix[row, col] + "\t");
            }
            GD.PrintRaw("\n");
        }
        GD.PrintRaw("\n");
    }
}