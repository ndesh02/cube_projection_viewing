using Godot;
using System;
using System.Collections.Generic;

public class Node : Godot.Node
{
    // Constants for specifying mode of projection
    private const int PERSPECTIVE = 0;
    private const int ORTHOGRAPHIC = 1;
    private static int projectionMode = 0;

    private static Node2D projectionRoot;
    private static Spatial projectionControl;
    private static MeshInstance eye;
    private static Camera2D projectionCamera;
    private static MeshInstance objectMesh;
    private static ArrayMesh objectArrayMesh = new ArrayMesh();
    private static MeshDataTool objectData = new MeshDataTool();
    private static SimplifiedMesh basicObjectMesh;
    private static MeshInstance planeMesh;
    private static ArrayMesh planeArrayMesh = new ArrayMesh();
    private static MeshDataTool planeData = new MeshDataTool();

    private const int OBJ_DIMENSIONS = 3;
    private const int CORNER_INDEX = 0;
    private const int X_CORNER = 2;
    private const int Y_CORNER = 1;

    private static Vector3 planeCorner;
    private static Vector3 planeNormal;
    private static Vector3 xDirection;
    private static Vector3 yDirection;
    private static Vector3 eyePosition;
    
    private const float PLANE_X_SCALE = 681;
    private const float PLANE_Y_SCALE = 705;
    private const float MAX_FOCUS = 4;
    private static Vector2 noSolutionVector = new Vector2(-100, -100);

    private static Control ControlsNode;
    private static HSlider FocusZoomSlider;
    private static HSlider GlobalXDegSlider;
    private static HSlider GlobalYDegSlider;
    private static HSlider GlobalZDegSlider;

    private static HSlider ObjectXDegSlider;
    private static HSlider ObjectYDegSlider;
    private static HSlider ObjectZDegSlider;

    public override void _Ready()
    {
        projectionRoot = GetNode<Node2D>("HBoxContainer/ProjectionContainer/ProjectionViewport/ProjectionRoot");
        projectionControl = GetNode<Spatial>("HBoxContainer/ObjectViewContainter/ObjectViewport/ObjectRoot/ProjectionControl");
        eye = GetNode<MeshInstance>("HBoxContainer/ObjectViewContainter/ObjectViewport/ObjectRoot/ProjectionControl/Eye");
        projectionCamera = GetNode<Camera2D>("HBoxContainer/ProjectionContainer/ProjectionViewport/ProjectionRoot/ProjectionCamera");
        objectMesh = GetNode<MeshInstance>("HBoxContainer/ObjectViewContainter/ObjectViewport/ObjectRoot/Object3/ObjectMesh");
        planeMesh = GetNode<MeshInstance>("HBoxContainer/ObjectViewContainter/ObjectViewport/ObjectRoot/ProjectionControl/Eye/Plane");
        ControlsNode = GetNode<Control>("HBoxContainer/ObjectViewContainter/ObjectViewport/Controls");
        FocusZoomSlider = GetNode<HSlider>("HBoxContainer/ObjectViewContainter/ObjectViewport/Controls/PlaneControl/FocusZoomSlider");
        GlobalXDegSlider = GetNode<HSlider>("HBoxContainer/ObjectViewContainter/ObjectViewport/Controls/PlaneControl/GlobalXDegSlider");
        GlobalYDegSlider = GetNode<HSlider>("HBoxContainer/ObjectViewContainter/ObjectViewport/Controls/PlaneControl/GlobalYDegSlider");
        GlobalZDegSlider = GetNode<HSlider>("HBoxContainer/ObjectViewContainter/ObjectViewport/Controls/PlaneControl/GlobalZDegSlider");
        ObjectXDegSlider = GetNode<HSlider>("HBoxContainer/ObjectViewContainter/ObjectViewport/Controls/ObjectControl/ObjectXDegSlider");
        ObjectYDegSlider = GetNode<HSlider>("HBoxContainer/ObjectViewContainter/ObjectViewport/Controls/ObjectControl/ObjectYDegSlider");
        ObjectZDegSlider = GetNode<HSlider>("HBoxContainer/ObjectViewContainter/ObjectViewport/Controls/ObjectControl/ObjectZDegSlider");

        planeArrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, planeMesh.Mesh.SurfaceGetArrays(0));
        planeData.CreateFromSurface(planeArrayMesh, 0);
        
        updateVectors();

        objectArrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, objectMesh.Mesh.SurfaceGetArrays(0));
        objectData.CreateFromSurface(objectArrayMesh, 0);
        basicObjectMesh = new SimplifiedMesh(objectData);

        drawVertices(basicObjectMesh, objectMesh, planeCorner, planeNormal, xDirection, yDirection, eyePosition, projectionMode);
        drawLines(basicObjectMesh);
    }

    public override void _PhysicsProcess(float delta)
    {
        planeMesh.Translation = new Vector3(-((float)FocusZoomSlider.Value), planeMesh.Translation.y, planeMesh.Translation.z);
        projectionControl.RotationDegrees = new Vector3((float)GlobalXDegSlider.Value, (float)GlobalYDegSlider.Value, (float)GlobalZDegSlider.Value);

        objectMesh.RotationDegrees = new Vector3((float)ObjectXDegSlider.Value, (float)ObjectYDegSlider.Value, (float)ObjectZDegSlider.Value);

        updateVectors();
        drawVertices(basicObjectMesh, objectMesh, planeCorner, planeNormal, xDirection, yDirection, eyePosition, projectionMode);
        drawLines(basicObjectMesh);
    }

    public void updateVectors()
    {
        projectionRoot.Call("Reset");
        planeCorner = planeMesh.GlobalTransform.Xform(planeData.GetVertex(CORNER_INDEX));
        planeNormal = planeMesh.GlobalTransform.origin - planeMesh.GlobalTransform.Xform(planeData.GetFaceNormal(0));
        xDirection = planeMesh.GlobalTransform.Xform(planeData.GetVertex(X_CORNER)) - planeCorner;
        yDirection = planeMesh.GlobalTransform.Xform(planeData.GetVertex(Y_CORNER)) - planeCorner;
        eyePosition = eye.GlobalTransform.origin;

        if (projectionMode == ORTHOGRAPHIC)
        {
            float zoomValue = MAX_FOCUS/((float)FocusZoomSlider.Value);
            projectionCamera.Zoom = new Vector2(zoomValue, zoomValue);
        }
        else
        {
            projectionCamera.Zoom = new Vector2(1, 1);
        }
    }

    public void drawVertices(SimplifiedMesh basicMesh, MeshInstance mesh, Vector3 cornerVector, Vector3 planeNormalVector, Vector3 xVector, Vector3 yVector, Vector3 eyePos, int mode)
    {
        for (int vertex = 0; vertex < basicMesh.points.Count; vertex++)
        {
            Vector3 globalVertexVector = mesh.GlobalTransform.Xform(basicMesh.points[vertex]);
            Vector3 rayDirection = eyePos - globalVertexVector;
            Vector2 coordinates = calculatePlaneCoordinates(globalVertexVector, cornerVector, planeNormalVector, rayDirection, xVector, yVector, mode);

            if (coordinates != noSolutionVector)
            {
                projectionRoot.Call("addPoint", coordinates, vertex);
            }
        }
    }

    public Vector2 calculatePlaneCoordinates(Vector3 objectVertex, Vector3 cornerVector, Vector3 planeNormalVector, Vector3 lineDirection, Vector3 xVector, Vector3 yVector, int mode)
    {
        double[,] matrix = new double[OBJ_DIMENSIONS, OBJ_DIMENSIONS + 1];

        if (mode == PERSPECTIVE)
        {
            MatrixSolver.convertTo3x3(lineDirection, xVector, yVector, matrix);
        }
        else if (mode == ORTHOGRAPHIC)
        {
            MatrixSolver.convertTo3x3(planeNormalVector, xVector, yVector, matrix);
        }

        // Perspective: LinePoint - PlanePoint = -r*LineDir + s*PlaneDir1 + t*PlaneDir2
        // Orthographic: Point = r*planeNormalVector + s*PlaneDir1 + t*PlaneDir2
        Vector3 constantMatrix = objectVertex - cornerVector;
        MatrixSolver.addVector3Col(3, constantMatrix, matrix);

        bool pointOnPlane = MatrixSolver.solveMatrix(OBJ_DIMENSIONS, OBJ_DIMENSIONS + 1, matrix, true);

        // Make sure point and eye are on opposite sides of the plane (-r < 0) or there will be no projection
        if (pointOnPlane && (matrix[0, 3] < 0))
        {
            return new Vector2((float)(PLANE_X_SCALE * matrix[1, 3]), (float)(PLANE_Y_SCALE * matrix[2, 3]));
        }
        else
        {
            return noSolutionVector;
        }
    }

    public void drawLines(SimplifiedMesh basicMesh)
    {
        for (int edge = 0; edge < basicMesh.displayedEdges.Count; edge++)
        {
            int pointA = basicMesh.displayedEdges[edge].Item1;
            int pointB = basicMesh.displayedEdges[edge].Item2;
            projectionRoot.Call("addEdge", pointA, pointB);
        }
    }

    public void _on_PerspectiveCheckBox_toggled(bool buttonPressed)
    {
        if (buttonPressed)
        {
            projectionMode = PERSPECTIVE;
        }
        else
        {
            projectionMode = ORTHOGRAPHIC;
        }
    }

    public void _on_ShowControlsButton_toggled(bool buttonPressed)
    {
        if (buttonPressed)
        {
            ControlsNode.Visible = true;
        }
        else
        {
            ControlsNode.Visible = false;
        }
    }
}
