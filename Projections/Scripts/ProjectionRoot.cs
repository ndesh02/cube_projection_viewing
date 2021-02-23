using Godot;
using System;
using System.Collections.Generic;

public class ProjectionRoot : Node2D
{
    private const int SCREEN_WIDTH = 681;
    private const int SCREEN_HEIGHT = 705;
    private const int POINT_RADIUS = 2;
    private const int LINE_WIDTH = 4;
    private Color RED = new Color(1, 0, 0);
    private Color WHITE = new Color(1, 1, 1);
    private Color GRAY = new Color((float)77/255, (float)77/255, (float)77/255);
    private Rect2 BACKGROUND = new Rect2(0, 0, new Vector2(SCREEN_WIDTH, SCREEN_HEIGHT));
    private List<Vector2> vertices = new List<Vector2>();

    // Used to store the SimplifiedMesh vertex indices that correspond to local (2D) points
    // Maps from local index to SimplifiedMesh vertex
    private List<int> vertexIndices = new List<int>();

    // List of tuples for storing edge pair with local indices
    private List<(int, int)> edgePairs = new List<(int, int)>();

    public void addPoint(Vector2 pos, int index)
    {
        vertices.Add(pos);
        vertexIndices.Add(index);
        Update();
    }

    public void addEdge(int pointA, int pointB)
    {
        int indexA = vertexIndices.IndexOf(pointA);
        int indexB = vertexIndices.IndexOf(pointB);

        // Display edge if at least one of the points are on screen
        if ((indexA != -1) && (indexB != -1))
        {
            edgePairs.Add((indexA, indexB));
        }

        Update();
    }

    private void drawPoints()
    {
        foreach (Vector2 vertex in vertices)
        {
            DrawCircle(vertex, POINT_RADIUS, WHITE);
        }
    }

    private void drawLines()
    {
        foreach ((int, int) pair in edgePairs)
        {
            DrawLine(vertices[pair.Item1], vertices[pair.Item2], WHITE, LINE_WIDTH);
        }
    }

    public override void _Draw()
    {
        DrawRect(BACKGROUND, GRAY);
        drawLines();
        drawPoints();
    }

    public void Reset()
    {
        vertices.Clear();
        vertexIndices.Clear();
        edgePairs.Clear();
    }
}
