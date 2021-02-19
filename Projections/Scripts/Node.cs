using Godot;
using System;
using System.Collections.Generic;

public class Node : Godot.Node
{
    public Node2D projectionRoot;
    public RayCast collisionRay;
    public MeshInstance objectMesh;
    public ArrayMesh objectArrayMesh = new ArrayMesh();
    public MeshDataTool objectData = new MeshDataTool();
    public SimplifiedMesh basicObjectMesh;
    public MeshInstance planeMesh;
    public ArrayMesh planeArrayMesh = new ArrayMesh();
    public MeshDataTool planeData = new MeshDataTool();

    public const int OBJ_DIMENSIONS = 3;
    public const int CORNER_INDEX = 0;
    public const int X_CORNER = 2;
    public const int Y_CORNER = 1;

    public Vector3 planeCorner;
    public Vector3 xDirection;
    public Vector3 yDirection;
    
    public const float PLANE_X_SCALE = 510;
    public const float PLANE_Y_SCALE = 600;
    public Vector2 noSolutionVector = new Vector2(-100, -100);

    public override void _Ready()
    {
        projectionRoot = GetNode<Node2D>("HBoxContainer/ProjectionContainer/ProjectionViewport/ProjectionRoot");
        collisionRay = GetNode<RayCast>("HBoxContainer/ObjectViewContainter/ObjectViewport/ObjectRoot/Eye/RayCast");
        objectMesh = GetNode<MeshInstance>("HBoxContainer/ObjectViewContainter/ObjectViewport/ObjectRoot/Object/ObjectMesh");
        planeMesh = GetNode<MeshInstance>("HBoxContainer/ObjectViewContainter/ObjectViewport/ObjectRoot/Plane/PlaneFront");
        
        planeArrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, planeMesh.Mesh.SurfaceGetArrays(0));
        planeData.CreateFromSurface(planeArrayMesh, 0);
        planeCorner = planeMesh.GlobalTransform.Xform(planeData.GetVertex(CORNER_INDEX));
        xDirection = planeMesh.GlobalTransform.Xform(planeData.GetVertex(X_CORNER)) - planeCorner;
        yDirection = planeMesh.GlobalTransform.Xform(planeData.GetVertex(Y_CORNER)) - planeCorner;

        objectArrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, objectMesh.Mesh.SurfaceGetArrays(0));
        objectData.CreateFromSurface(objectArrayMesh, 0);
        basicObjectMesh = new SimplifiedMesh(objectData);

        drawVertices(basicObjectMesh, objectMesh, planeCorner, xDirection, yDirection, collisionRay);
        drawLines(basicObjectMesh);
    }

    public void drawVertices(SimplifiedMesh basicMesh, MeshInstance mesh, Vector3 cornerVector, Vector3 xVector, Vector3 yVector, RayCast ray)
    {
        for (int vertex = 0; vertex < basicMesh.points.Count; vertex++)
        {
            Vector3 collisionPoint = getRayCollision(ray, mesh.GlobalTransform.Xform(basicMesh.points[vertex]));
            Vector2 coordinates = calculatePlaneCoordinates(xVector, yVector, collisionPoint - cornerVector);

            if (coordinates != noSolutionVector)
            {
                projectionRoot.Call("addPoint", coordinates, vertex);
            }
        }
    }

    public Vector3 getRayCollision(RayCast ray, Vector3 point)
    {
        ray.CastTo = point - ray.GlobalTransform.origin;
        ray.ForceRaycastUpdate();
        return ray.GetCollisionPoint();
    }

    public Vector2 calculatePlaneCoordinates(Vector3 xVector, Vector3 yVector, Vector3 cornerToPoint)
    {
        float[,] matrix = new float[OBJ_DIMENSIONS, OBJ_DIMENSIONS];
        MatrixSolver.convertTo3x3(xVector, yVector, cornerToPoint, matrix);
        bool pointOnPlane = MatrixSolver.solveMatrix(OBJ_DIMENSIONS, OBJ_DIMENSIONS, matrix, true);

        if (pointOnPlane)
        {
            return new Vector2(PLANE_X_SCALE * matrix[0, 2], PLANE_Y_SCALE * matrix[1, 2]);
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
}
