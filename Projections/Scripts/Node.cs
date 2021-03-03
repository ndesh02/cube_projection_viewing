using Godot;
using System;
using System.Collections.Generic;

public class Node : Godot.Node
{
    // Integers for specifying mode of projection
    private const int PERSPECTIVE = 0;
    private const int ORTHOGRAPHIC = 1;
    private static int projectionMode = 0;

    // Nodes required in this script
    private static Spatial topViewPlane;
    private static Spatial frontViewPlane;
    private static Spatial sideViewPlane;
    private static Node2D mainDisplayRoot;
    private static Node2D topViewRoot;
    private static Node2D frontViewRoot;
    private static Node2D sideViewRoot;

    private static StaticBody objectBody;
    private static MeshInstance objectMesh;
    private static ArrayMesh objectArrayMesh = new ArrayMesh();
    private static MeshDataTool objectData = new MeshDataTool();
    private static SimplifiedMesh basicObjectMesh;

    // UI Nodes for controlling object, plane, and projection
    private static Control ControlsNode;
    private static HSlider FocusZoomSlider;
    private static HSlider ObjectXDegSlider;
    private static HSlider ObjectYDegSlider;
    private static HSlider ObjectZDegSlider;
    private static OptionButton viewList;
    private static int selectedView = 0;
    private static GridContainer viewGrid;

    public override void _Ready()
    {
        // Assignment of required nodes
        topViewPlane = GetNode<Spatial>("HorizontalViewContainer/ObjectViewContainter/ObjectViewport/ObjectRoot/TopViewPlane");
        frontViewPlane = GetNode<Spatial>("HorizontalViewContainer/ObjectViewContainter/ObjectViewport/ObjectRoot/FrontViewPlane");
        sideViewPlane = GetNode<Spatial>("HorizontalViewContainer/ObjectViewContainter/ObjectViewport/ObjectRoot/RightViewPlane");
        mainDisplayRoot = GetNode<Node2D>("HorizontalViewContainer/MainProjectionDisplayContainer/MainProjectionViewport/ProjectionRoot");
        topViewRoot = GetNode<Node2D>("HorizontalViewContainer/MainProjectionDisplayContainer/ViewGridContainer/TopViewportContainer/TopViewport/TopViewRoot");
        frontViewRoot = GetNode<Node2D>("HorizontalViewContainer/MainProjectionDisplayContainer/ViewGridContainer/FrontViewportContainer/FrontViewport/FrontViewRoot");
        sideViewRoot = GetNode<Node2D>("HorizontalViewContainer/MainProjectionDisplayContainer/ViewGridContainer/SideViewportContainer/SideViewport/SideViewRoot");
        //objectMesh = GetNode<MeshInstance>("HorizontalViewContainer/ObjectViewContainter/ObjectViewport/ObjectRoot/Spatial/ObjectMesh6");
        objectBody = GetNode<StaticBody>("HorizontalViewContainer/ObjectViewContainter/ObjectViewport/ObjectRoot/Object3");
        objectMesh = GetNode<MeshInstance>("HorizontalViewContainer/ObjectViewContainter/ObjectViewport/ObjectRoot/Object3/ObjectMesh");
        ControlsNode = GetNode<Control>("HorizontalViewContainer/ObjectViewContainter/ObjectViewport/Controls");
        FocusZoomSlider = GetNode<HSlider>("HorizontalViewContainer/ObjectViewContainter/ObjectViewport/Controls/ControlsPanel/ViewControl/FocusZoomSlider");
        ObjectXDegSlider = GetNode<HSlider>("HorizontalViewContainer/ObjectViewContainter/ObjectViewport/Controls/ControlsPanel/ObjectControl/HBoxContainer/SlidersVBoxContainer/ObjectXDegSlider");
        ObjectYDegSlider = GetNode<HSlider>("HorizontalViewContainer/ObjectViewContainter/ObjectViewport/Controls/ControlsPanel/ObjectControl/HBoxContainer/SlidersVBoxContainer/ObjectYDegSlider");
        ObjectZDegSlider = GetNode<HSlider>("HorizontalViewContainer/ObjectViewContainter/ObjectViewport/Controls/ControlsPanel/ObjectControl/HBoxContainer/SlidersVBoxContainer/ObjectZDegSlider");
        viewList = GetNode<OptionButton>("HorizontalViewContainer/ObjectViewContainter/ObjectViewport/Controls/ControlsPanel/ViewControl/ViewSelectList");
        viewGrid = GetNode<GridContainer>("HorizontalViewContainer/MainProjectionDisplayContainer/ViewGridContainer");

        // Creating SimplifiedMesh object data from mesh
        objectArrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, objectMesh.Mesh.SurfaceGetArrays(0));
        objectData.CreateFromSurface(objectArrayMesh, 0);
        basicObjectMesh = new SimplifiedMesh(objectData);

        PlaneControl.objectMesh = objectMesh;
        PlaneControl.basicObjectMesh = basicObjectMesh;
        PlaneControl.ray = GetNode<RayCast>("HorizontalViewContainer/ObjectViewContainter/ObjectViewport/ObjectRoot/HiddenLineRayCast");
        //PlaneControl.pointCollision = GetNode<Area>("HorizontalViewContainer/ObjectViewContainter/ObjectViewport/ObjectRoot/PointCollision");

        //GetTree().CallGroup("Slider", "Connect", "value_changed", this, nameof(ChangeDisplay));

        selectedView = viewList.GetSelectedId();
        SetDisplay();
        Display();
    }

    /*public override void _PhysicsProcess(float delta)
    {
        objectBody.RotationDegrees = new Vector3((float)ObjectXDegSlider.Value, (float)ObjectYDegSlider.Value, (float)ObjectZDegSlider.Value);
        PlaneControl.zoomSliderValue = FocusZoomSlider.Value;

        Display();
    }*/


    public static void ChangeDisplay(float newValue)
    {
        objectBody.RotationDegrees = new Vector3((float)ObjectXDegSlider.Value, (float)ObjectYDegSlider.Value, (float)ObjectZDegSlider.Value);
        objectBody.ForceUpdateTransform();
        PlaneControl.zoomSliderValue = FocusZoomSlider.Value;

        Display();
    }

    public static void SetDisplay()
    {
        viewGrid.Visible = false;
        mainDisplayRoot.Visible = true;

        topViewPlane.Visible = false;
        frontViewPlane.Visible = false;
        sideViewPlane.Visible = false;
        
        if (selectedView == 0)
        {
            viewGrid.Visible = true;
            mainDisplayRoot.Visible = false;
            topViewPlane.Call("SetDisplayNode", topViewRoot.GetPath());
            frontViewPlane.Call("SetDisplayNode", frontViewRoot.GetPath());
            sideViewPlane.Call("SetDisplayNode", sideViewRoot.GetPath());

            topViewPlane.Visible = true;
            frontViewPlane.Visible = true;
            sideViewPlane.Visible = true;
        }
        else if (selectedView == 1)
        {
            topViewPlane.Call("SetDisplayNode", mainDisplayRoot.GetPath());
            topViewPlane.Visible = true;
        }
        else if (selectedView == 2)
        {
            frontViewPlane.Call("SetDisplayNode", mainDisplayRoot.GetPath());
            frontViewPlane.Visible = true;
        }
        else if (selectedView == 3)
        {
            sideViewPlane.Call("SetDisplayNode", mainDisplayRoot.GetPath());
            sideViewPlane.Visible = true;
        }
    }
    
    public static void Display()
    {
        if (selectedView == 0)
        {
            topViewPlane.Call("DisplayPlane");
            frontViewPlane.Call("DisplayPlane");
            sideViewPlane.Call("DisplayPlane");
        }
        else if (selectedView == 1)
        {
            topViewPlane.Call("DisplayPlane");
        }
        else if (selectedView == 2)
        {
            frontViewPlane.Call("DisplayPlane");
        }
        else if (selectedView == 3)
        {
            sideViewPlane.Call("DisplayPlane");
        }
    }

    public void _on_PerspectiveCheckBox_toggled(bool buttonPressed)
    {
        if (buttonPressed)
        {
            PlaneControl.projectionMode = PlaneControl.PERSPECTIVE;
        }
        else
        {
            PlaneControl.projectionMode = PlaneControl.ORTHOGRAPHIC;
        }
        Display();
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

    public void _on_OptionButton_item_selected(int index)
    {
        selectedView = index;
        SetDisplay();
        Display();
    }

    public void _on_Button_button_down()
    {
        FocusZoomSlider.Value = 1;
        ObjectXDegSlider.Value = 0;
        ObjectYDegSlider.Value = 0;
        ObjectZDegSlider.Value = 0;
    }
}
