using Godot;
using System;
using System.Collections.Generic;

public class ProjectionRoot : Node2D
{
    private const int SCREEN_WIDTH = 681;
    private const int SCREEN_HEIGHT = 705;
    private const float POINT_RADIUS = 1.5f;
    private const float HIDDEN_POINT_RADIUS = 1f;
    private const float LINE_WIDTH = 3f;
    private const float HIDDEN_LINE_WIDTH = 2f;
    private Color RED = new Color(1, 0, 0);
    private Color WHITE = new Color(1, 1, 1);
    private Color GRAY = new Color((float)77/255, (float)77/255, (float)77/255);
    private Rect2 Background = new Rect2(0, 0, new Vector2(SCREEN_WIDTH, SCREEN_HEIGHT));
    private List<(Vector2, bool)> displayedVertices = new List<(Vector2, bool)>();
    private List<(Vector2, Vector2, bool)> displayedEdges = new List<(Vector2, Vector2, bool)>();
    private float zoomFactor = 1;

    public void AddPoint(Vector2 vertex, bool isShown)
    {
        displayedVertices.Add((vertex, isShown));
        Update();
    }

    private void DrawPoints()
    {
        foreach ((Vector2, bool) vertex in displayedVertices)
        {
            if (!vertex.Item2)
            {
                DrawCircle(vertex.Item1, HIDDEN_POINT_RADIUS * zoomFactor, RED);
            }
        }

        foreach ((Vector2, bool) vertex in displayedVertices)
        {
            if (vertex.Item2)
            {
                DrawCircle(vertex.Item1, POINT_RADIUS * zoomFactor, WHITE);
            }
        }
    }

    public void AddLine(Vector2 edgePointA, Vector2 edgePointB, bool isShown)
    {
        displayedEdges.Add((edgePointA, edgePointB, isShown));
        Update();
    }

    private void DrawLines()
    {
        foreach ((Vector2, Vector2, bool) edge in displayedEdges)
        {
            if (!edge.Item3)
            {
                DrawLine(edge.Item1, edge.Item2, RED, HIDDEN_LINE_WIDTH * zoomFactor);
            }
        }

        foreach ((Vector2, Vector2, bool) edge in displayedEdges)
        {
            if (edge.Item3)
            {
                DrawLine(edge.Item1, edge.Item2, WHITE, LINE_WIDTH * zoomFactor);
            }
        }
    }

    public void SetZoomFactor(float zoomFactor)
    {
        this.zoomFactor = zoomFactor;
        Background = new Rect2(SCREEN_WIDTH*(1-zoomFactor)/2, SCREEN_HEIGHT*(1-zoomFactor)/2, new Vector2(SCREEN_WIDTH * zoomFactor, SCREEN_HEIGHT * zoomFactor));
    }

    public void Reset()
    {
        displayedVertices.Clear();
        displayedEdges.Clear();
    }

    public override void _Draw()
    {
        GetChild<Camera2D>(0).Zoom = new Vector2(zoomFactor, zoomFactor);
        DrawRect(Background, GRAY);
        DrawPoints();
        DrawLines();
    }
}
