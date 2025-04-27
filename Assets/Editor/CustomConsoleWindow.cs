using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;

public class CustomConsoleWindow : EditorWindow
{
    private Vector2 _scrollPos;
    private List<LogEntry> _logs = new List<LogEntry>();

    // GUIStyle khởi tạo lazy
    private GUIStyle _logStyle;
    private GUIStyle _selectedStyle;

    private static readonly string[] _channels = { "Gameplay", "Network", "UI", "AI" };
    private Dictionary<string, bool> _channelFilters = new Dictionary<string, bool>();
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
        foreach (var ch in _channels)
            _channelFilters[ch] = true;
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
        string[] parts = logString.Split(new[]{"-"}, StringSplitOptions.None);
        timer = parts[0].Trim('(', ')');
        channel = parts[1].Trim('[').Trim(']');
        message = parts[2].Trim(':');
        _logs.Add(new LogEntry {
            Channel    = channel,
            Message    = message,
            Timer      = timer,
            StackTrace = stackTrace,
            Type       = type,
            Count      = 1
        });

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
        GUILayout.FlexibleSpace();

        foreach (var ch in _channels)
            _channelFilters[ch] = GUILayout.Toggle(
                _channelFilters[ch],
                ch,
                EditorStyles.toolbarButton
            );
        EditorGUILayout.EndHorizontal();
    }

    private void DrawLogs()
    {
        var collapsed = new Dictionary<string, LogEntry>();
        int displayIndex = 0;

        for (int i = 0; i < _logs.Count; i++)
        {
            var log = _logs[i];
            if (!string.IsNullOrEmpty(log.Channel)
             && (!_channelFilters.TryGetValue(log.Channel, out bool vis) || !vis))
                continue;

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
            _selectedLogIndex = index;
            Repaint();
        }

        // Highlight hover/selection
        if (isSel)
            EditorGUI.DrawRect(r, new Color(0.24f, 0.48f, 0.90f, 1f));
        else if (r.Contains(Event.current.mousePosition))
            EditorGUI.DrawRect(r, new Color(0.3f, 0.3f, 0.3f, 1f));

        // Icon
        Rect iconR = new Rect(r.x, r.y, 32, r.height);
        GUI.Label(iconR, GetIcon(log.Type));
        
        // Text
        Rect txtR = new Rect(iconR.x + iconR.width + 5, r.y, r.width - 60, r.height);
        GUI.Label(txtR, $"({log.Timer})-[{log.Channel}]-:{log.Message}", _logStyle);

        // Count nếu collapse
        if (log.Count > 1)
        {
            Rect cntR = new Rect(r.xMax - 30, r.y, 30, r.height);
            GUI.Label(cntR, $"x{log.Count}");
        }
    }

    private void DrawSelectedStackTrace()
    {
        if (_selectedLogIndex < 0 || _selectedLogIndex >= _logs.Count)
            return;

        var log = _logs[_selectedLogIndex];
        if (!string.IsNullOrEmpty(log.StackTrace))
        {
            GUILayout.Label("Stack Trace:", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(log.Message+'\n'+log.StackTrace, GUILayout.ExpandHeight(true));
        }
    }

    private GUIContent GetIcon(LogType type)
    {
        switch (type)
        {
            case LogType.Warning:   return EditorGUIUtility.IconContent("console.warnicon");
            case LogType.Error:
            case LogType.Exception: return EditorGUIUtility.IconContent("console.erroricon");
            default:                return EditorGUIUtility.IconContent("console.infoicon");
        }
    }

    private struct LogEntry
    {
        public string Channel, Message, Timer, StackTrace;
        public LogType Type;
        public int Count;
    }
}
