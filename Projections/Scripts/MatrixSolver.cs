using Godot;
using System;

public class MatrixSolver : Node
{

    private const double TOLERANCE = 0.001;
    private static int numOfIterations;
    public override void _Ready()
    {
        numOfIterations = 0;
        //float[,] matrix = new float[,] {{1, 2, 1}, {1, 2, 2}, {2, 4, 3}, {3, 6, 4}};
        //solveMatrix(matrix.GetLength(0), matrix.GetLength(1), matrix);
    }

    private static void swapRows(int columns, double[,] matrix, int rowA, int rowB)
    {
        double temp = 0;
        for (int col = 0; col < columns; col++)
        {
            temp = matrix[rowA, col];
            matrix[rowA, col] = matrix[rowB, col];
            matrix[rowB, col] = temp;
        }
    }

    private static void divideRow(int columns, double[,] matrix, int row, double divisor)
    {
        for (int col = 0; col < columns; col++)
        {
            matrix[row, col] /= divisor;
        }
    }

    // Adds coefficient * rowB to rowA
    private static void addRowMultiple(int columns, double[,] matrix, int rowA, int rowB, double coefficient)
    {
        for (int col = 0; col < columns; col++)
        {
            matrix[rowA, col] += coefficient * matrix[rowB, col];
        }
    }

    // Takes an augmented matrix to (almost) RREF (with leading entries on the main diagonal) and returns whether it has a unique solution
    public static bool solveMatrix(int rows, int columns, double[,] matrix, bool augmented)
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
            // Check nonzero entries on the main diagonal
            for (int coefficientEntry = 0; coefficientEntry < numOfIterations; coefficientEntry++)
            {
                if ((compareDoubles(matrix[coefficientEntry, coefficientEntry], 0, TOLERANCE)) && (compareDoubles(matrix[coefficientEntry, columns - 1], 0, TOLERANCE)))
                {
                    isUnique = false;
                    break;
                }
            }

            // Check for leading entries in constant matrix
            for (int constantEntry = numOfIterations; constantEntry < rows; constantEntry++)
            {
                if (!compareDoubles(matrix[constantEntry, columns - 1], 0, TOLERANCE))
                {
                    isUnique = false;
                    break;
                }
            }
        }

        //printMatrix(rows, columns, matrix);
        return isUnique;
    }

    // Compare float with tolerance
    public static bool compareDoubles(double numA, double numB, double tolerace)
    {
        return (Math.Abs(numA - numB) < tolerace);
    }

    public static void convertTo3x3(Vector3 column1, Vector3 column2, Vector3 column3, double[,] matrix)
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

    public static void addVector3Col(int columnNum, Vector3 column, double[,] matrix)
    {
        matrix[0, columnNum] = column.x;
        matrix[1, columnNum] = column.y;
        matrix[2, columnNum] = column.z;
    }

    public static void printMatrix(int rows, int columns, double[,] matrix)
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