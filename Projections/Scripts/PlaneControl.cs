using Godot;
using System;
using System.Collections.Generic;

public class PlaneControl : Spatial
{
    private const float TOLERANCE = 0.01f;

    // Constants for 2D projection display
    private const float PLANE_X_SCALE = 681;
    private const float PLANE_Y_SCALE = 705;
    private const float MAX_FOCUS = 4;

    // Integers for specifying mode of projection
    public const int PERSPECTIVE = 0;
    public const int ORTHOGRAPHIC = 1;
    public static int projectionMode = 0;

    // Meshes and data required for projection calculations
    private MeshInstance eye;
    private MeshInstance planeMesh;
    private ArrayMesh planeArrayMesh = new ArrayMesh();
    private MeshDataTool planeData = new MeshDataTool();
    public static MeshInstance objectMesh;
    public static SimplifiedMesh basicObjectMesh;

    // Constants for accessing plane data and using matrices
    private const int OBJ_DIMENSIONS = 3;
    private const int PROJ_DIMENSIONS = 2;
    private const int CORNER_INDEX = 0;
    private const int X_CORNER = 2;
    private const int Y_CORNER = 1;

    // Plane data vectors
    private Vector3 planeCorner;
    private Vector3 planeNormal;
    private Vector3 xDirection;
    private Vector3 yDirection;

    // No solution vector for if the ray does not intersect plane
    private static Vector2 noSolutionVector = new Vector2(-100, -100);

    // Plane distance information (for zoom of display in orthographic projection)
    public static double zoomSliderValue = 1;
    public static float zoomFactor = 1;

    // Display root node (to display 2D projection)
    private Node2D projectionRoot;

    // List of (projected) Vector2s that are to be displayed onto the screen (projection root node)
    private List<Vector2> displayedVertices = new List<Vector2>();

    // List of SimplifiedMesh indices that correspond to each displayedVertices vertex
    private List<int> displayedVertexIndices = new List<int>();

    // List of tuples for storing edge pairs with local (displayedVertices) indices
    private List<(int, int)> edgePairs = new List<(int, int)>();

    private List<Vector2> intersectionPoints = new List<Vector2>();
    private List<(int, int)> edgeSections = new List<(int, int)>();
    private List<Vector3> rayPoints = new List<Vector3>();
    private List<Vector2> vertices = new List<Vector2>();
    private List<bool> isVertexShown = new List<bool>();
    private List<bool> isEdgeShown = new List<bool>();
    private bool allIntersectionsFound = false;
    public static RayCast ray;
    //public static Area pointCollision;

    public override void _Ready()
    {
        // Assignment of required nodes
        eye = this.GetChild<MeshInstance>(0);
        planeMesh = eye.GetChild<MeshInstance>(0);

        // Creating plane data from mesh
        planeArrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, planeMesh.Mesh.SurfaceGetArrays(0));
        planeData.CreateFromSurface(planeArrayMesh, 0);
    }

    public void DisplayPlane()
    {
        Reset();
        UpdateVectors();
        DrawVertices();
        DrawLines();
    }

    public void UpdateVectors()
    {
        planeMesh.Translation = new Vector3(-((float)zoomSliderValue), planeMesh.Translation.y, planeMesh.Translation.z);

        planeCorner = planeMesh.GlobalTransform.Xform(planeData.GetVertex(CORNER_INDEX));

        // Order of subtration is reversed to flip normal and have same sign as matrix[0, 3] with perspective projection to check if solution is valid
        planeNormal = planeMesh.GlobalTransform.origin - planeMesh.GlobalTransform.Xform(planeData.GetFaceNormal(0));

        xDirection = planeMesh.GlobalTransform.Xform(planeData.GetVertex(X_CORNER)) - planeCorner;
        yDirection = planeMesh.GlobalTransform.Xform(planeData.GetVertex(Y_CORNER)) - planeCorner;

        if (projectionMode == ORTHOGRAPHIC)
        {
            zoomFactor = MAX_FOCUS/((float)zoomSliderValue);
        }
        else
        {
            zoomFactor = 1;
        }

        projectionRoot.Call("SetZoomFactor", zoomFactor);
    }

    public void DrawVertices()
    {
        for (int vertex = 0; vertex < basicObjectMesh.points.Count; vertex++)
        {
            // Vector of object vertex in global coordinates
            Vector3 globalVertexVector = objectMesh.GlobalTransform.Xform(basicObjectMesh.points[vertex]);

            // Direction of RAY cast from eye/eye plane
            Vector3 rayDirection = eye.GlobalTransform.origin - globalVertexVector;

            // Gets 2D screen coordinates of projection (if solution exists)
            Vector2 coordinates = CalculatePlaneCoordinates(globalVertexVector, rayDirection);

            // If the coordinates represent a valid solution, add to lists and display onto screen
            if (coordinates != noSolutionVector)
            {
                displayedVertices.Add(coordinates);
                displayedVertexIndices.Add(vertex);
                rayPoints.Add(globalVertexVector);
            }
        }

        findHiddenPoints();

        for (int vertex = 0; vertex < rayPoints.Count; vertex++)
        {
            projectionRoot.Call("AddPoint", displayedVertices[vertex], isVertexShown[vertex]);
        }
    }

    // Projects vertex onto plane and calculates the 2D display coordinates (if they exist)
    public Vector2 CalculatePlaneCoordinates(Vector3 objectVertex, Vector3 rayDirectionVector)
    {
        double[,] matrix = new double[OBJ_DIMENSIONS, OBJ_DIMENSIONS + 1];

        // Different matrix depending on projection type
        // Express point as linear combination of rayDirection/planeNormal, xDirection, and yDirection (thus projecting the point onto the plane)
        if (projectionMode == PERSPECTIVE)
        {
            MatrixSolver.ConvertTo3x3(rayDirectionVector, xDirection, yDirection, matrix);
        }
        else if (projectionMode == ORTHOGRAPHIC)
        {
            MatrixSolver.ConvertTo3x3(planeNormal, xDirection, yDirection, matrix);
        }

        // Perspective: LinePoint - PlanePoint = -r*LineDir + s*PlaneDir1 + t*PlaneDir2
        // Orthographic: Point = r*planeNormalVector + s*PlaneDir1 + t*PlaneDir2
        Vector3 constantMatrix = objectVertex - planeCorner;

        // Add constant matrix to 4th (index 3) row
        MatrixSolver.AddVector3Col(3, constantMatrix, matrix);

        // Reduce (solve) matrix and check if solution on plane exists
        bool pointOnPlane = MatrixSolver.SolveMatrix(OBJ_DIMENSIONS, OBJ_DIMENSIONS + 1, matrix, true);

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

    public void DrawLines()
    {
        for (int edge = 0; edge < basicObjectMesh.displayedEdges.Count; edge++)
        {
            int pointA = basicObjectMesh.displayedEdges[edge].Item1;
            int pointB = basicObjectMesh.displayedEdges[edge].Item2;

            int indexA = displayedVertexIndices.IndexOf(pointA);
            int indexB = displayedVertexIndices.IndexOf(pointB);

            if ((indexA != -1) && (indexB != -1))
            {
                edgePairs.Add((indexA, indexB));
                //projectionRoot.Call("AddLine", displayedVertices[indexA], displayedVertices[indexB], true);
            }
        }

        findIntersections();
        findHiddenEdges();

        for (int edge = 0; edge < edgeSections.Count; edge++)
        {
            int pointA = edgeSections[edge].Item1;
            int pointB = edgeSections[edge].Item2;

            //bool isEdgeShown = (isVertexShown[pointA] && isVertexShown[pointB]);

            //projectionRoot.Call("AddLine", vertices[pointA], vertices[pointB], isEdgeShown);
            projectionRoot.Call("AddLine", vertices[pointA], vertices[pointB], isEdgeShown[edge]);
        }
    }

    public void findIntersections()
    {
        edgeSections.AddRange(edgePairs);
        vertices.AddRange(displayedVertices);
        int count = edgeSections.Count;
        int currentVertex = vertices.Count - 1;

        allIntersectionsFound = false;

        while (!allIntersectionsFound)
        {
            allIntersectionsFound = true;
            for (int edgeA = 0; edgeA < count; edgeA++)
            {
                for (int edgeB = (edgeA + 1); edgeB < count; edgeB++)
                {
                    double[,] matrix = new double[PROJ_DIMENSIONS, PROJ_DIMENSIONS + 1];

                    // Line1Point - Line2Point = t*Line2Dir - s*Line1Dir
                    // Subtraction for Line1Dir is flipped to account for negative sign in equation
                    // LineDir = Point2 (or Item2) - Point1 (or Item1)
                    int edgeAPointA = edgeSections[edgeA].Item1;
                    int edgeAPointB = edgeSections[edgeA].Item2;
                    int edgeBPointA = edgeSections[edgeB].Item1;
                    int edgeBPointB = edgeSections[edgeB].Item2;

                    // Check if the edges do not share a vertex, and only find intersection if they do not
                    if ((vertices[edgeAPointA] != vertices[edgeBPointA]) && (vertices[edgeAPointA] != vertices[edgeBPointB]) && (vertices[edgeAPointB] != vertices[edgeBPointA]) && (vertices[edgeAPointB] != vertices[edgeBPointB]))
                    {
                        Vector2 edgeADirection = vertices[edgeAPointA] - vertices[edgeAPointB];
                        Vector2 edgeBDirection = vertices[edgeBPointB] - vertices[edgeBPointA];
                        Vector2 constantMatrix = vertices[edgeAPointA] - vertices[edgeBPointA];

                        MatrixSolver.ConvertTo2x2(edgeADirection, edgeBDirection, matrix);
                        MatrixSolver.AddVector2Col(2, constantMatrix, matrix);
                        bool isMatrixSolvable = MatrixSolver.SolveMatrix(PROJ_DIMENSIONS, PROJ_DIMENSIONS + 1, matrix, true);

                        // Check if lines actually intersect (coefficients must be positive and < 1) and matrix has a solution
                        if ((matrix[0, 2] > 0) && (matrix[0, 2] < 1) && (matrix[1, 2] > 0) && (matrix[1, 2] < 1) && isMatrixSolvable)
                        {
                            //GD.Print(matrix[0, 2], "    ", matrix[1, 2], "    ", isMatrixSolvable);
                            // Intersection = Line2Point + t*Line2Dir (t is matrix[1, 2])
                            Vector2 pointOfIntersection = vertices[edgeBPointA] + ((float)matrix[1, 2] * edgeBDirection);
                            intersectionPoints.Add(pointOfIntersection);

                            Vector3 objectEdgeADirection = rayPoints[edgeAPointB] - rayPoints[edgeAPointA];
                            rayPoints.Add(rayPoints[edgeAPointA] + ((float)matrix[0, 2] * objectEdgeADirection));

                            Vector3 objectEdgeBDirection = rayPoints[edgeBPointB] - rayPoints[edgeBPointA];
                            rayPoints.Add(rayPoints[edgeBPointA] + ((float)matrix[1, 2] * objectEdgeBDirection));

                            vertices.Add(pointOfIntersection);
                            currentVertex++;
                            edgeSections.Add((edgeAPointA, currentVertex));
                            edgeSections.Add((edgeAPointB, currentVertex));
                            edgeSections.RemoveAt(edgeA);

                            vertices.Add(pointOfIntersection);
                            currentVertex++;
                            edgeSections.Add((edgeBPointA, currentVertex));
                            edgeSections.Add((edgeBPointB, currentVertex));
                            edgeSections.RemoveAt(edgeB - 1);

                            count += 2;

                            allIntersectionsFound = false;
                            break;
                        }
                    }
                }

                if (!allIntersectionsFound)
                {
                    break;
                }
            }
        }
    }

    private void findHiddenPoints()
    {
        ray.Translation = eye.GlobalTransform.origin;
        ray.ForceUpdateTransform();
        
        for (int point = 0; point < rayPoints.Count; point++)
        {
            //pointCollision.Translation = rayPoints[point];
            //pointCollision.ForceUpdateTransform();
            
            if (projectionMode == ORTHOGRAPHIC)
            {
                ray.CastTo = planeNormal;
            }
            else if (projectionMode == PERSPECTIVE)
            {
                ray.CastTo = rayPoints[point] - ray.GlobalTransform.origin;
            }

            ray.ForceRaycastUpdate();

            if (ray.IsColliding())
            {
                if (MatrixSolver.CompareVector3s(ray.GetCollisionPoint(), rayPoints[point], TOLERANCE))
                {
                    isVertexShown.Add(true);
                }
                else
                {
                    isVertexShown.Add(false);
                }
            }
            else
            {
                isVertexShown.Add(true);
            }
        }
    }

    /*private void findHiddenPoints()
    {

        for (int point = 0; point < rayPoints.Count; point++)
        {
            ray.Translation = rayPoints[point];
            //ray.ForceUpdateTransform();

            if (projectionMode == ORTHOGRAPHIC)
            {
                ray.CastTo = planeNormal;
            }
            else if (projectionMode == PERSPECTIVE)
            {
                ray.CastTo = eye.GlobalTransform.origin - ray.GlobalTransform.origin;
            }

            ray.ForceRaycastUpdate();

            //GD.Print(ray.GlobalTransform.origin);

            if (ray.IsColliding())
            {
                GD.Print(ray.GetCollisionPoint());
                isVertexShown.Add(false);
            }
            else
            {
                isVertexShown.Add(true);
            }
        }
        //GD.Print();
    }*/

    /*private void findHiddenPoints()
    {
        ray.Translation = eye.GlobalTransform.origin;
        ray.ForceUpdateTransform();
        for (int point = 0; point < rayPoints.Count; point++)
        {
            pointCollision.Translation = rayPoints[point];
            pointCollision.ForceUpdateTransform();
            
            if (projectionMode == ORTHOGRAPHIC)
            {
                ray.CastTo = planeNormal;
            }
            else if (projectionMode == PERSPECTIVE)
            {
                ray.CastTo = rayPoints[point] - ray.GlobalTransform.origin;
            }

            ray.ForceRaycastUpdate();

            //GD.Print(ray.GlobalTransform.origin);

            if (ray.IsColliding())
            {
                //GD.Print(ray.GetCollisionPoint());
                if (MatrixSolver.CompareVector3s(ray.GetCollisionPoint(), rayPoints[point], 0.055))
                {
                    isVertexShown.Add(true);
                }
                else
                {
                    isVertexShown.Add(false);
                }
            }
            else
            {
                isVertexShown.Add(true);
            }
        }
        //GD.Print();
    }*/

    private void findHiddenEdges()
    {
        ray.Translation = eye.GlobalTransform.origin;
        ray.ForceUpdateTransform();
        
        for (int edge = 0; edge < edgeSections.Count; edge++)
        {
            //pointCollision.Translation = (rayPoints[edgeSections[edge].Item1] + rayPoints[edgeSections[edge].Item2])/2;
            //pointCollision.ForceUpdateTransform();

            Vector3 midPoint = (rayPoints[edgeSections[edge].Item1] + rayPoints[edgeSections[edge].Item2])/2;
            
            if (projectionMode == ORTHOGRAPHIC)
            {
                ray.CastTo = planeNormal;
            }
            else if (projectionMode == PERSPECTIVE)
            {
                ray.CastTo = midPoint - ray.GlobalTransform.origin;
            }

            ray.ForceRaycastUpdate();

            //GD.Print(ray.GlobalTransform.origin);

            if (ray.IsColliding())
            {
                //GD.Print(ray.GetCollisionPoint());
                if (MatrixSolver.CompareVector3s(ray.GetCollisionPoint(), midPoint, TOLERANCE))
                {
                    isEdgeShown.Add(true);
                }
                else
                {
                    isEdgeShown.Add(false);
                }
            }
            else
            {
                isEdgeShown.Add(true);
            }
        }
        //GD.Print();
    }

    public void Reset()
    {
        displayedVertices.Clear();
        displayedVertexIndices.Clear();
        edgePairs.Clear();
        intersectionPoints.Clear();
        rayPoints.Clear();
        edgeSections.Clear();
        vertices.Clear();
        isVertexShown.Clear();
        isEdgeShown.Clear();
        projectionRoot.Call("Reset");
    }

    public void SetDisplayNode(NodePath projectionRootPath)
    {
        projectionRoot = GetNode<Node2D>(projectionRootPath);
    }
}
