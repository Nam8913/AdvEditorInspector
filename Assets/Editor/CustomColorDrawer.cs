using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(CustomColor))]
public class CustomColorDrawer : PropertyDrawer
{
    // Height của một dòng bình thường là ~18
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Bắt đầu vẽ property
        EditorGUI.BeginProperty(position, label, property);
        // Lấy 4 prop con r,g,b,a
        var pr = property.FindPropertyRelative("r");
        var pg = property.FindPropertyRelative("g");
        var pb = property.FindPropertyRelative("b");
        var pa = property.FindPropertyRelative("a");

        Rect r = EditorGUI.PrefixLabel(position, label);
        r.width = 300; r.height = EditorGUIUtility.singleLineHeight;
        
        Color current = new Color(pr.floatValue, pg.floatValue, pb.floatValue, pa.floatValue);
        EditorGUI.DrawRect(r, current);

        // Bắt click để mở picker popup
        if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
        {
            IMGUIColorPickerPopup win = new IMGUIColorPickerPopup(property);
            PopupWindow.Show(r, win);
            Event.current.Use();
        }
        EditorGUI.EndProperty();
    }
}
public class IMGUIColorPickerPopup : PopupWindowContent
{
    // Lưu reference đến SerializedProperty để apply thay đổi
    SerializedProperty prop;
    Color old;
    // Màu đang chọn (h,s,v,a)
    float hue, sat, val, alpha;
    Texture2D hueTex, svTex;
    Texture2D texR, texG, texB, texA_bg, texA_fg;
    const int sliderH = 20;
    const int pickerSize = 260;

    // Swatches
    static readonly Color[] basicColors = new Color[] {
        Color.white, Color.black, Color.red, Color.green, Color.blue, Color.yellow, Color.cyan, Color.magenta, Color.gray 
    };
    static List<Color> customColors = new List<Color>();
    const int swatchColumns = 8;
    const float swatchSize = 16f;
    const float swatchPadding = 4f;
    const float swatchBrightness = 1.2f;

    Color GetColor(SerializedProperty prop)
    {
        var pr = prop.FindPropertyRelative("r");
        var pg = prop.FindPropertyRelative("g");
        var pb = prop.FindPropertyRelative("b");
        var pa = prop.FindPropertyRelative("a");
        return new Color(pr.floatValue, pg.floatValue, pb.floatValue, pa.floatValue);
    }
    float r
    {
        get {return prop.FindPropertyRelative("r").floatValue;}
    }
    float g
    {
        get {return prop.FindPropertyRelative("g").floatValue;}
    }
    float b
    {
        get {return prop.FindPropertyRelative("b").floatValue;}
    }
    float a
    {
        get {return prop.FindPropertyRelative("a").floatValue;}
    }
    Color SetColor(SerializedProperty prop,Color color)
    {
        var pr = prop.FindPropertyRelative("r");
        var pg = prop.FindPropertyRelative("g");
        var pb = prop.FindPropertyRelative("b");
        var pa = prop.FindPropertyRelative("a");

        if(pr != null)
        {
            pr.floatValue = color.r;
        }
        if(pg != null)
        {
            pg.floatValue = color.g;
        }
        if(pb != null)
        {
            pb.floatValue = color.b;
        }
        if(pa != null)
        {
            pa.floatValue = color.a;
        }

        return color;
    }

    public IMGUIColorPickerPopup(SerializedProperty property)
    {
        prop = property;
        // Khởi tạo từ giá trị ban đầu
        Color c = GetColor(prop);
        old = c;
        Color.RGBToHSV(c, out hue, out sat, out val);
        alpha = c.a;
    }

    public override Vector2 GetWindowSize()
    {
        // width = pickerSize, height = pickerSize + các slider + padding
        return new Vector2(pickerSize, pickerSize + sliderH * 2 + 400);
    }

    public override void OnGUI(Rect rect)
    {
        // 1. VẼ ô SV
        var svRect = new Rect(0, 0, pickerSize, pickerSize);
        if (svTex == null || GetColor(prop) != old) GenerateSVTexture();
        GUI.DrawTexture(svRect, svTex);

        // 2. BẮT input cho SV
        var e = Event.current;
        if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
        {
            if (svRect.Contains(e.mousePosition))
            {
                sat = Mathf.Clamp01(e.mousePosition.x / pickerSize);
                val = Mathf.Clamp01(e.mousePosition.y / pickerSize);
                e.Use();
                ApplyToProperty();
            }
        }

        // 3. VẼ thanh Hue bên dưới
        var hueRect = new Rect(0, pickerSize + 5, pickerSize, sliderH);
        if (hueTex == null) GenerateHueTexture();
        GUI.DrawTexture(hueRect, hueTex);

        // 4. Bắt input cho Hue
        if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
        {
            if (hueRect.Contains(e.mousePosition))
            {
                hue = Mathf.Clamp01(e.mousePosition.x / pickerSize);
                GenerateSVTexture(); // cập nhật lại ô SV cho hue mới
                e.Use();
                ApplyToProperty();
            }
        }

        // 5. Slider alpha
        // var alphaRect = new Rect(0, pickerSize + 5 + sliderH + 5, pickerSize, sliderH);
        // EditorGUI.BeginChangeCheck();
        // alpha = GUI.HorizontalSlider(alphaRect, alpha, 0f, 1f);
        // if (EditorGUI.EndChangeCheck())
        //     ApplyToProperty();

        // 6. Preview và label
        Color preview = Color.HSVToRGB(hue, sat, val);
        preview.a = alpha;
        EditorGUI.DrawRect(new Rect(0, pickerSize + 10 + sliderH , 24, 24), preview);
        EditorGUI.DrawRect(new Rect(32, pickerSize + 10 + sliderH , 24, 24), old);
        // EditorGUI.LabelField(new Rect(64, pickerSize + 10 + sliderH , 150, 24),
        //     $"RGBA: {(int)(preview.r*255)},{(int)(preview.g*255)},{(int)(preview.b*255)},{(int)(preview.a*255)}");
    
         float y0 = pickerSize + sliderH * 2 + 10;

        // 1) Vẽ nhãn "Shades"
        Rect labSh = new Rect(0, y0, rect.width, EditorGUIUtility.singleLineHeight);
        GUI.Label(labSh, "Shades");

        // 2) Vẽ hàng shade (mix với đen)
        float rowH = 20;
        float rowY = y0 + EditorGUIUtility.singleLineHeight + 2;
        DrawVariationRow(rect, preview, Color.black, rowY, rowH);

        // 3) Vẽ nhãn "Tints"
        float y1 = rowY + rowH + 4;
        Rect labTi = new Rect(0, y1, rect.width, EditorGUIUtility.singleLineHeight);
        GUI.Label(labTi, "Tints");

        // 4) Vẽ hàng tint (mix với trắng)
        float rowY2 = y1 + EditorGUIUtility.singleLineHeight + 2;
        DrawVariationRow(rect, preview, Color.white, rowY2, rowH);

        InitChannelTextures();

        //Rect chanelR = new Rect(0, rowY2 + EditorGUIUtility.singleLineHeight + 4, 200, EditorGUIUtility.singleLineHeight);
        float cy = y1 + EditorGUIUtility.singleLineHeight + rowH + 6 + EditorGUIUtility.singleLineHeight + 4;
        //Rect channelRect = new Rect(0, cy, pickerSize, sliderH);
        

        // --- Channel sliders --- RGBA sliders sequentially:
        Color cur = GetColor(prop);
        float cr = cur.r, cg = cur.g, cb = cur.b, ca = cur.a;
        
        Rect r = new Rect(0, cy, pickerSize, sliderH);
        DrawChannel(ref r, "R", ref cr, texR);
        DrawChannel(ref r, "G", ref cg, texG);
        DrawChannel(ref r, "B", ref cb, texB);
        DrawChannel(ref r, "A", ref ca, texA_bg, texA_fg);
        // apply channel changes
        if (cr!=cur.r || cg!=cur.g || cb!=cur.b || ca!=cur.a)
        {
            Color newC = new Color(cr, cg, cb, ca);
            SetColor(prop, newC);
            Color.RGBToHSV(newC, out hue, out sat, out val);
            alpha = ca;
            prop.serializedObject.ApplyModifiedProperties();
        }

        // --- Swatch Palettes ---
        float sy = cy + (sliderH+4)*4 + 10;
        GUILayout.BeginArea(new Rect(0, sy, pickerSize, rect.height - sy));
        GUILayout.Label("Basic Colors", EditorStyles.boldLabel);
        DrawSwatchGrid(basicColors, false);
        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Custom Colors", EditorStyles.boldLabel);
        if (GUILayout.Button("+", GUILayout.Width(swatchSize), GUILayout.Height(swatchSize)))
        {
            if (!customColors.Contains(GetColor(prop)))
                customColors.Add(GetColor(prop));
        }
        GUILayout.EndHorizontal();
        DrawSwatchGrid(customColors.ToArray(), true);
        GUILayout.EndArea();
    }

    void ApplyToProperty()
    {
        SetColor(prop, Color.HSVToRGB(hue, sat, val) * new Color(1,1,1,1));
        SetColor(prop, GetColor(prop));
        prop.serializedObject.ApplyModifiedProperties();
    }

    void GenerateHueTexture()
    {
        hueTex = new Texture2D(pickerSize, sliderH);
        for (int x = 0; x < pickerSize; x++)
        {
            Color c = Color.HSVToRGB((float)x / pickerSize, 1f, 1f);
            for (int y = 0; y < sliderH; y++)
                hueTex.SetPixel(x, y, c);
        }
        hueTex.Apply();
    }

    void GenerateSVTexture()
    {
        svTex = new Texture2D(pickerSize, pickerSize);
        for (int x = 0; x < pickerSize; x++)
            for (int y = 0; y < pickerSize; y++)
            {
                float s = (float)x / (pickerSize - 1);
                float v = 1f - (float)y / (pickerSize - 1);
                svTex.SetPixel(x, y, Color.HSVToRGB(hue, s, v));
            }
        svTex.Apply();
    }
    Texture2D GenerateGradientTex(Color from, Color to)
    {
        var tex = new Texture2D(256, 1);
        for (int i = 0; i < 256; i++)
        {
            float t = i / 255f;
            tex.SetPixel(i, 0, Color.Lerp(from, to, t));
        }
        tex.Apply();
        return tex;
    }
    void DrawVariationRow(Rect totalRect, Color baseColor, Color mixColor, float y, float height)
    {
        int steps = 10;
        float cellW = totalRect.width / (steps + 1);
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            Color c = Color.Lerp(baseColor, mixColor, t);
            Rect cell = new Rect(i * cellW, y, cellW, height);
            EditorGUI.DrawRect(cell, c);

            // bắt click vào ô thì chuyển hue/sat/val/alpha và apply
            if (Event.current.type == EventType.MouseDown && cell.Contains(Event.current.mousePosition))
            {
                Color.RGBToHSV(c, out hue, out sat, out val);
                alpha = c.a;
                ApplyToProperty();
                Event.current.Use();
            }
        }
    }
    void InitChannelTextures()
    {
        if (texR != null) return;
        // gradients
        texR = GenerateGradientTex(Color.black, Color.red);
        texG = GenerateGradientTex(Color.black, Color.green);
        texB = GenerateGradientTex(Color.black, Color.blue);
        // alpha checker
        texA_bg = new Texture2D(16,16);
        Color c1 = Color.white * .7f, c2 = Color.white * .3f;
        for(int x=0;x<16;x++) for(int y=0;y<16;y++) texA_bg.SetPixel(x,y, ((x+y)%2==0)?c1:c2);
        texA_bg.Apply();
        // alpha gradient fg
        texA_fg = new Texture2D(256,1);
        for(int i=0;i<256;i++) texA_fg.SetPixel(i,0,new Color(0,0,0,1 - i/255f));
        texA_fg.Apply();
    }
    void DrawChannel(ref Rect r, string label, ref float ch, Texture2D bg, Texture2D fg = null)
    {
        // label
        var lr = new Rect(r.x, r.y, 20, r.height);
        EditorGUI.LabelField(lr, label);
        // slider background
        float x0 = r.x + 24;
        float w = r.width - 24 - 40;
        var bgRect = new Rect(x0, r.y, w, r.height);
        if (fg != null)
        {
            EditorGUI.DrawTextureTransparent(bgRect, bg);
            EditorGUI.DrawTextureTransparent(bgRect, fg);
        }
        else EditorGUI.DrawTextureTransparent(bgRect, bg);
        // slider
        int valI = Mathf.RoundToInt(ch * 255f);
        valI = EditorGUI.IntSlider(bgRect, GUIContent.none, valI, 0, 255);
        ch = valI / 255f;
        // value label
        var vr = new Rect(x0 + w + 4, r.y, 36, r.height);
        EditorGUI.LabelField(vr, valI.ToString());
        // next
        r.y += r.height + 4;
    }
    GenericMenu menu;
    void DrawSwatchGrid(Color[] colors, bool isCustom)
    {
        for (int i = 0; i < colors.Length; i++)
        {
            if (i % swatchColumns == 0) GUILayout.BeginHorizontal();

            
            GUI.backgroundColor = colors[i];

            if (GUILayout.Button(GUIContent.none, GUILayout.Width(swatchSize), GUILayout.Height(swatchSize)))
            {
                Color c = colors[i];
                Color.RGBToHSV(c, out hue, out sat, out val);
                alpha = c.a;
                ApplyToProperty();
            }
            Rect cellRect = GUILayoutUtility.GetLastRect();
            if (isCustom && Event.current.type == EventType.ContextClick && cellRect.Contains(Event.current.mousePosition))
            {
                int idx = i;
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Replace"), false, () => { customColors[idx] = GetColor(prop); });
                menu.AddItem(new GUIContent("Delete"), false, () => { customColors.RemoveAt(idx); });
                menu.AddItem(new GUIContent("Move To First"), false, () => {
                    Color c = customColors[idx];
                    customColors.RemoveAt(idx);
                    customColors.Insert(0, c);
                });
                menu.ShowAsContext();
                Event.current.Use();
            }
            GUI.backgroundColor = Color.white;

            if (i % swatchColumns == swatchColumns - 1) GUILayout.EndHorizontal();
        }
        if (colors.Length % swatchColumns != 0) GUILayout.EndHorizontal();
    }
}

