using System;
using UnityEngine;

[Serializable]
public class CustomColor
{
    [Range(0f,1f)]public float r,g,b,a;
    
    public CustomColor(Color c)
    {
        this.r = c.r; this.g = c.g; this.b = c.b; this.a = c.a;
    }
    public CustomColor(float r,float g,float b,float a)
    {
        this.r = r; this.g = g; this.b = b; this.a = a;
    }
    public static implicit operator Color(CustomColor customColor)
    {
        return new Color(customColor.r, customColor.g, customColor.b, customColor.a);
    }
}
