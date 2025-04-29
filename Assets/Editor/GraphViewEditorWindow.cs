using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class GraphViewEditorWindow : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    [MenuItem("Window/GraphViewEditorWindow")]
    public static void ShowExample()
    {
        GraphViewEditorWindow wnd = GetWindow<GraphViewEditorWindow>();
        wnd.titleContent = new GUIContent("GraphViewEditorWindow");
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;
        // Instantiate UXML
        var FromUXML = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/GraphViewEditorWindow.uxml");
        FromUXML.CloneTree(root);

        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/GraphViewEditorWindow.uss");
        root.styleSheets.Add(styleSheet);

        
    }
}
