// PURPOSE: The existing MeshDataTool contains many duplicates of vertices, edges, and faces, so raycasting and other display features cause issues
// This class eliminates duplicates

using Godot;
using System;
using System.Collections.Generic;

public class SimplifiedMesh
{
    // Tolerance constant for comparing floats
    public const float TOLERANCE = 0.001f;

    // List for storing unique vertices
    public List<Vector3> points = new List<Vector3>();

    // List of lists for storing the MeshDataTool indices that correspond to a unique vertex
    // Maps from local vertex (outside index) to MDT vertices (values of inside list)
    public List<List<int>> localToMdtV = new List<List<int>>();

    // List of ints for storing which local vertex an MDT vertex corresponds to
    // Maps from MDT vertex (outside index) to local vertex (stored int values)
    public List<int> mdtToLocalV = new List<int>();

    // Int for current index of local vertex list
    // Used for populating mdtToLocalV
    public int currentLocalVertex = 0;

    // Pairs (tuples) of (indices) of vertices that form an edge
    public List<(int, int)> edgePairs = new List<(int, int)>();

    // List of lists for storing the MeshDataTool indices that correspond to a unique edge
    // Maps from local edge (outside index) to MDT edges (values of inside list)
    public List<List<int>> localToMdtE = new List<List<int>>();

    // List of ints for storing which local edge an MDT vertex corresponds to
    // Maps from MDT edge (outside index) to local edge (stored int values)
    public List<int> mdtToLocalE = new List<int>();

    // Int for current index of local edge list
    // Used for populating mdtToLocalE
    public int currentLocalEdge = 0;

    // List of edges that are not to be displayed as they connect parallel faces
    public List<int> edgesNotShown = new List<int>();

    // List of edges to display
    public List<(int, int)> displayedEdges = new List<(int, int)>();

    // Constructor taking the reference MeshDataTool as a parameter
    public SimplifiedMesh(MeshDataTool mdt)
    {
        simplifyPoints(mdt);
        simplifyLines(mdt);
    }

    // Eliminating duplicate vertices
    public void simplifyPoints(MeshDataTool mdt)
    {
        int index = 0;
        for (int vertex = 0; vertex < mdt.GetVertexCount(); vertex++)
        {
            index = points.IndexOf(mdt.GetVertex(vertex));

            if (index != -1)
            {
                localToMdtV[index].Add(vertex);
                mdtToLocalV.Add(index);
            }
            else
            {
                points.Add(mdt.GetVertex(vertex));
                localToMdtV.Add(new List<int>{vertex});
                mdtToLocalV.Add(currentLocalVertex);
                currentLocalVertex++;
            }
        }
    }

    public void simplifyLines(MeshDataTool mdt)
    {
        for (int edge = 0; edge < mdt.GetEdgeCount(); edge++)
        {
            // Finds the local (unique) indices of points that form an edge (so duplicates can be compared)
            int pointAIndex = mdtToLocalV[mdt.GetEdgeVertex(edge, 0)];
            int pointBIndex = mdtToLocalV[mdt.GetEdgeVertex(edge, 1)];

            // A pair of tuples for comparing either order of vertices
            (int, int) pairA = (pointAIndex, pointBIndex);
            (int, int) pairB = (pointBIndex, pointAIndex);

            // Edgepairs list indices of each pair
            int pairAIndex = edgePairs.IndexOf(pairA);
            int pairBIndex = edgePairs.IndexOf(pairB);

            // Index to add to mdtToLocalE and localToMdtE lists
            int indexToAdd = -1;

            // Compare if either pair is in the list, and if not, add pairA to list
            // Also only add line if both faces the line's local equivalent is a part of are not parallel
            if ((pairAIndex == -1) && (pairBIndex == -1))
            {
                edgePairs.Add(pairA);
                localToMdtE.Add(new List<int>{edge});
                mdtToLocalE.Add(currentLocalEdge);
                currentLocalEdge++;
            }
            else
            {
                // Find which index to add the local mapping lists
                if (pairAIndex != -1)
                {
                    indexToAdd = pairAIndex;
                }
                else if (pairBIndex != -1)
                {
                    indexToAdd = pairBIndex;
                }

                localToMdtE[indexToAdd].Add(edge);
                mdtToLocalE.Add(indexToAdd);
            }
        }

        findDisplayedEdges(mdt);
    }

    // Function that find the local indices of edges that are to be displayed (eliminate fictional edges that connect two parallel faces)
    public void findDisplayedEdges(MeshDataTool mdt)
    {
        // For loop for running through all unique pairs of faces
        for (int faceA = 0; faceA < mdt.GetFaceCount(); faceA++)
        {
            for (int faceB = (faceA + 1); faceB < mdt.GetFaceCount(); faceB++)
            {
                Vector3 faceANomral = mdt.GetFaceNormal(faceA);
                Vector3 faceBNomral = mdt.GetFaceNormal(faceB);

                // Check if the planes are parallel (have the same normal)
                // Due to small variations, a tolerance is used to compare normals
                if (isSame(faceANomral.x, faceBNomral.x) && isSame(faceANomral.y, faceBNomral.y) && isSame(faceANomral.z, faceBNomral.z))
                {
                    // Check if any of the edges are the same by mapping them from mdt indices to local (unique) indices
                    for (int edgeA = 0; edgeA < 3; edgeA++)
                    {
                        for (int edgeB = 0; edgeB < 3; edgeB++)
                        {
                            // Convert mdt index to local index
                            int edgeAIndex = mdtToLocalE[mdt.GetFaceEdge(faceA, edgeA)];
                            int edgeBIndex = mdtToLocalE[mdt.GetFaceEdge(faceB, edgeB)];

                            // Add local index to edgesNotShown list if any of the edges are the same
                            if (edgeAIndex == edgeBIndex)
                            {
                                edgesNotShown.Add(edgeAIndex);
                            }
                        }
                    }
                }
            }
        }

        for (int index = 0; index < edgePairs.Count; index++)
        {
            if (!edgesNotShown.Contains(index))
            {
                displayedEdges.Add(edgePairs[index]);
            }
        }
    }

    public bool isSame(float numA, float numB)
    {
        return (Math.Abs(numA - numB) < TOLERANCE);
    }
}
