using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class VGraphView : GraphView
{
    public VGraphView()
    {
       // this.StretchToParentSize();
        var grid = new GridBackground();
        this.Insert(0, grid);
        //grid.StretchToParentSize();

        // float majorStep = 80f;      // khoảng cách giữa các đường chính
        // float minorStep = 20f;      // khoảng cách giữa các đường phụ
        // Color gridColor  = new Color(0.2f, 0.5f, 0.8f, 0.3f);

        // grid.style.width = majorStep;
        // grid.style.height = minorStep;
        // grid.style.backgroundColor = gridColor;
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new ContentZoomer());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());
    
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/GraphViewEditorWindow.uss");
        styleSheets.Add(styleSheet);
    }
}