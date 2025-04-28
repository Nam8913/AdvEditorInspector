using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Reflection;

public class CustomConsoleWindow : EditorWindow
{
    private Vector2 _scrollPos;
    private List<LogEntry> _logs = new List<LogEntry>();

    // GUIStyle khởi tạo lazy
    private GUIStyle _logStyle;
    private GUIStyle _linkStyle;
    private GUIStyle _selectedStyle;

    private static readonly string[] _channels = { "Gameplay", "Network", "UI", "AI" };
    private Dictionary<string, bool> _channelFilters = new Dictionary<string, bool>();
    private int _channelMask;

    // Type filters
    private bool _showLog = true;
    private bool _showWarning = true;
    private bool _showError = true;

    // Pause on Error
    private bool _pauseOnError = false;

    // Search
    private string _searchString = string.Empty;
    
    private bool _collapse = false;
    private int _selectedLogIndex = -1;

    [MenuItem("Window/Custom Console")]
    public static void ShowWindow()
    {
        GetWindow<CustomConsoleWindow>("Custom Console");
    }

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;

        // Khởi tạo filter channel
        _channelFilters.Clear();
        for (int i = 0; i < _channels.Length; i++)
        {
            _channelFilters[_channels[i]] = true;
        }
        _channelMask = (1 << _channels.Length) - 1;

        _showLog = _showWarning = _showError = true;
        _pauseOnError = false;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        string timer = null;
        string channel = null;
        string message = logString;

        // if (logString.StartsWith("[") && logString.Contains("]"))
        // {
        //     int end = logString.IndexOf(']');
        //     channel = logString.Substring(1, end - 1);
        //     message = logString.Substring(end + 1).Trim();
        // }
        try
        {
            string[] parts = logString.Split(new[]{"-"}, StringSplitOptions.None);
            timer = parts[0].Trim('(', ')');
            channel = parts[1].Trim('[').Trim(']');
            message = parts[2].Trim(':');
            var log = new LogEntry
            {
                Channel    = channel,
                Message    = message,
                Timer      = timer,
                StackTrace = stackTrace,
                Type       = type,
                Context    = null,
                Count      = 1
            };
            

            // Pause editor on error if enabled
            if (_pauseOnError && (type == LogType.Error || type == LogType.Exception))
            {
                EditorApplication.isPaused = true;
            }
            _logs.Add(log);
        }
        catch (Exception e)
        {
            return;
        }
        

        Repaint();
    }

    private void OnGUI()
    {
        // Lazy-init styles ở đây, GUI system đã sẵn sàng
        if (_logStyle == null)
        {
            _logStyle      = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                normal   = { textColor = Color.white },
                padding  = new RectOffset(5, 5, 5, 5),
                alignment = TextAnchor.MiddleLeft,
                richText = true
            };
            _selectedStyle = new GUIStyle(EditorStyles.helpBox);
        }
        if (_linkStyle == null)
        {
            _linkStyle = new GUIStyle(EditorStyles.label);
            _linkStyle.normal.textColor = Color.cyan;       // màu link chưa hover
            _linkStyle.hover.textColor  = new Color(0f, 0.5f, 1f); // màu khi hover
            _linkStyle.stretchWidth     = true;
        }

        DrawToolbar();
        GUILayout.Space(5);

        // Log list (60% chiều cao)
        _scrollPos = EditorGUILayout.BeginScrollView(
            _scrollPos,
            GUILayout.Height(position.height * 0.6f)
        );
        DrawLogs();
        EditorGUILayout.EndScrollView();

        GUILayout.Space(10);

        // Hiển thị stack trace khi đã chọn
        DrawSelectedStackTrace();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button("Clear", EditorStyles.toolbarButton))
        {
            _logs.Clear();
            _selectedLogIndex = -1;
        }

        _collapse = GUILayout.Toggle(_collapse, "Collapse", EditorStyles.toolbarButton);
        
        // Pause on Error button
       // var pauseContent = new GUIContent(EditorGUIUtility.IconContent("PauseButton")) { tooltip = "Pause on Error" };
        var pauseContent = new GUIContent("Pause on Error") { tooltip = "Pause on Error" };
        _pauseOnError = GUILayout.Toggle(_pauseOnError, pauseContent, EditorStyles.toolbarButton);
        GUILayout.FlexibleSpace();
        
        // Search field
        _searchString = GUILayout.TextField(_searchString, EditorStyles.toolbarSearchField, GUILayout.Width(200), GUILayout.MinWidth(100));
        if (GUILayout.Button(GUIContent.none, GUIStyle.none, GUILayout.Width(18)))
        {
            _searchString = string.Empty;
            GUI.FocusControl(null);
        }

        GUILayout.Space(10);

        // Channel mask selector
        _channelMask = EditorGUILayout.MaskField(_channelMask, _channels, EditorStyles.toolbarPopup, GUILayout.Width(120));
        // Update filters from mask bits
        for (int i = 0; i < _channels.Length; i++)
        {
            bool on = (_channelMask & (1 << i)) != 0;
            _channelFilters[_channels[i]] = on;
        }
        int errCount = _logs.Count(l => l.Type == LogType.Error || l.Type == LogType.Exception);
        int warnCount = _logs.Count(l => l.Type == LogType.Warning);
        int infoCount = _logs.Count(l => l.Type == LogType.Log);

        GUIContent errContent = EditorGUIUtility.IconContent("console.erroricon");
        errContent.text = errCount.ToString();
        var warnContent = EditorGUIUtility.IconContent("console.warnicon");
        warnContent.text = warnCount.ToString();
        var infoContent = EditorGUIUtility.IconContent("console.infoicon");
        infoContent.text = infoCount.ToString();

        _showError   = GUILayout.Toggle(_showError, errContent,   EditorStyles.toolbarButton);
        _showWarning = GUILayout.Toggle(_showWarning, warnContent, EditorStyles.toolbarButton);
        _showLog     = GUILayout.Toggle(_showLog, infoContent, EditorStyles.toolbarButton);

        EditorGUILayout.EndHorizontal();
    }

    private void DrawLogs()
    {
        var collapsed = new Dictionary<string, LogEntry>();
        int displayIndex = 0;

        for (int i = 0; i < _logs.Count; i++)
        {
            var log = _logs[i];

            // Type filter
            if (log.Type == LogType.Error || log.Type == LogType.Exception) { if (!_showError) continue; }
            else if (log.Type == LogType.Warning)                        { if (!_showWarning) continue; }
            else                                                          { if (!_showLog) continue; }

             // Filter by channel
            if (!string.IsNullOrEmpty(log.Channel)
             && (!_channelFilters.TryGetValue(log.Channel, out bool vis) || !vis))
                continue;

            // Filter by search text
            // if (!string.IsNullOrEmpty(_searchString)
            //     && !log.Message.IndexOf(_searchString, StringComparison.InvariantCultureIgnoreCase).Equals(-1))
            // {
            //     // match in message; do nothing
            // }
            // else if (!string.IsNullOrEmpty(_searchString))
            // {
            //     // also allow search in channel name
            //     if (string.IsNullOrEmpty(log.Channel)
            //         || !log.Channel.IndexOf(_searchString, StringComparison.InvariantCultureIgnoreCase).Equals(-1))
            //     {
            //         continue;
            //     }
            //     continue;
            // }
            if (!string.IsNullOrEmpty(_searchString) && !Regex.IsMatch(log.Message + log.Channel, Regex.Escape(_searchString), RegexOptions.IgnoreCase)) continue;

            string key = $"{log.Channel}|{log.Message}|{log.Type}";
            if (_collapse)
            {
                if (collapsed.ContainsKey(key))
                {
                    var e = collapsed[key];
                    e.Count++;
                    collapsed[key] = e;
                    continue;
                }
                collapsed[key] = log;
            }
            else
            {
                DrawLogEntry(displayIndex++, log);
            }
        }

        if (_collapse)
        {
            int idx = 0;
            foreach (var kv in collapsed)
                DrawLogEntry(idx++, kv.Value);
        }
    }

    private void DrawLogEntry(int index, LogEntry log)
    {
        Rect r = GUILayoutUtility.GetRect(10, 30, GUILayout.ExpandWidth(true));
        bool isSel = index == _selectedLogIndex;

        if (r.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown)
        {
            // if (Event.current.clickCount == 2)
            //     OpenStackTrace(log.StackTrace);
            // else
            if(Event.current.clickCount == 2)
            {
                OpenStackTrace(log.StackTrace);
            }
            _selectedLogIndex = index;
            if (log.Context != null)
            {
                Selection.activeObject = log.Context;
                EditorGUIUtility.PingObject(log.Context);
            }
            Repaint();
        }

        // switch(log.Type)
        // {
        //     case LogType.Warning:
        //         EditorGUI.DrawRect(r, new Color(1f, 0.8f, 0.2f, 1f));
        //         break;
        //     case LogType.Error:
        //     case LogType.Exception:
        //         EditorGUI.DrawRect(r, new Color(1f, 0.2f, 0.2f, 1f));
        //         break;
        //     default:
        //         EditorGUI.DrawRect(r, new Color(1f, 0.8f, 1f, 1f));
        //         break;
        // }
        
        if(index % 2 == 0)
            EditorGUI.DrawRect(r, new Color(0.3f, 0.3f, 0.3f, 1f));
        
        // Highlight hover/selection
        if (isSel)
            EditorGUI.DrawRect(r, new Color(0.24f, 0.48f, 0.90f, 1f));
        //else if (r.Contains(Event.current.mousePosition))
        

        // Icon
        Rect iconR = new Rect(r.x, r.y, 32, r.height);
        GUI.Label(iconR, GetIcon(log.Type));
        string fullText = $"({log.Timer})-[{log.Channel}]-:{log.Message}";
        // Text
        Rect txtR = new Rect(iconR.x + iconR.width, r.y, r.width - 60, r.height);
        if (!string.IsNullOrEmpty(_searchString))
        {
            // Escape để an toàn
            string pattern = Regex.Escape(_searchString);
            fullText = Regex.Replace(
                fullText,
                pattern,
                match => $"<color=yellow>{match.Value}</color>",
                RegexOptions.IgnoreCase
            );
        }

        // Vẽ label với richText enabled
        GUI.Label(
            txtR,
            fullText,
            _logStyle
        );

        // Count nếu collapse
        if (log.Count > 1)
        {
            Rect cntR = new Rect(r.xMax - 30, r.y, 30, r.height);
            GUI.Label(cntR, $"x{log.Count}");
        }
    }

    // private void DrawSelectedStackTrace()
    // {
    //     if (_selectedLogIndex < 0 || _selectedLogIndex >= _logs.Count)
    //         return;

    //     var log = _logs[_selectedLogIndex];
    //     if (!string.IsNullOrEmpty(log.StackTrace))
    //     {
    //         GUILayout.Label("Stack Trace:", EditorStyles.boldLabel);
    //         EditorGUILayout.SelectableLabel(log.Message + '\n' + log.StackTrace, EditorStyles.textArea, GUILayout.ExpandHeight(true));
    //     }
    // }
    private void DrawSelectedStackTrace()
    {
        if (_selectedLogIndex < 0 || _selectedLogIndex >= _logs.Count) return;
        var log = _logs[_selectedLogIndex];
        if (string.IsNullOrEmpty(log.StackTrace)) return;

        GUILayout.Label("Stack Trace:", EditorStyles.boldLabel);
        var lines = log.StackTrace.Split(new[] {'\r','\n'}, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var match = Regex.Match(line, @"\(at (.+):(\d+)\)");
            if (match.Success)
            {
                string path = match.Groups[1].Value;
                int lineNum = int.Parse(match.Groups[2].Value);

                Rect rect = GUILayoutUtility.GetRect(new GUIContent(line), _linkStyle);
                EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
                GUI.Label(rect, line, _linkStyle);
    
                if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                {
                    // Convert absolute to asset path if needed
                    if (System.IO.Path.IsPathRooted(path))
                    {
                        string dataPath = Application.dataPath.Replace("/", System.IO.Path.DirectorySeparatorChar.ToString());
                        if (path.StartsWith(dataPath))
                            path = "Assets" + path.Substring(dataPath.Length);
                    }
                    var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (asset != null) AssetDatabase.OpenAsset(asset, lineNum);
                    Event.current.Use();
                }
            }
            else
            {
                GUILayout.Label(line, EditorStyles.label);
            }
        }
    }
    private void OpenStackTrace(string stackTrace)
    {
        if (string.IsNullOrEmpty(stackTrace)) return;
        var lines = stackTrace.Split(new[] {'\r','\n'}, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0) return;
        
        var match = Regex.Match(lines.Last(), @"at\s+(.*):(\d+)");
        if (match.Success)
        {
            string path = match.Groups[1].Value;
            int line = int.Parse(match.Groups[2].Value);
            if (System.IO.Path.IsPathRooted(path))
            {
                string dataPath = Application.dataPath.Replace("/", System.IO.Path.DirectorySeparatorChar.ToString());
                if (path.StartsWith(dataPath))
                {
                    path = "Assets" + path.Substring(dataPath.Length);
                }
            }
            var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (asset != null)
            {
                
                AssetDatabase.OpenAsset(asset, line);
            }
        }
    }

    private GUIContent GetIcon(LogType type)
    {
        GUIContent icon;
        switch (type)
        {
            case LogType.Warning:   
                icon = EditorGUIUtility.IconContent("console.warnicon");
                break;
            case LogType.Error:
            case LogType.Exception: 
                icon = EditorGUIUtility.IconContent("console.erroricon");
                break;
            default:                
                icon = EditorGUIUtility.IconContent("console.infoicon");
                break;
        }
        icon.text = string.Empty; // Xóa text để chỉ hiển thị icon
        return icon;
    }

    

    private struct LogEntry
    {
        public string Channel, Message, Timer, StackTrace;
        public UnityEngine.Object Context;
        public string SceneName;
        public LogType Type;
        public int Count;
    }
}
