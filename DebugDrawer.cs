using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// A structure capable of being drawn within <see cref="DebugDrawer"/>
/// </summary>
public interface IDebugDrawing
{
    /// <summary>
    /// Draws debug information; is called by <see cref="DebugDrawer"/> from within SceneGUI context.
    /// </summary>
    void Draw();
}

/// <summary>
/// Utility to allow debug drawing of 'Handles' and GUI content (such as labels)
/// without being restricted to Monobehavior/Editor OnGUI methods.
/// </summary>
public static class DebugDrawer
{

#if UNITY_EDITOR

    private static List<IDebugDrawing> Drawings = new List<IDebugDrawing>();

    static DebugDrawer()
    {
        SceneView.duringSceneGui += SceneViewOnDuringSceneGui;
    }

    private static void SceneViewOnDuringSceneGui(SceneView obj)
    {
        using (var scope = new Handles.DrawingScope())
        {
            foreach (var drawing in Drawings)
            {
                drawing.Draw();
            }
        }
    }

    private static int _lastFrame;
    private static void CheckForFrameChange()
    {
        // SceneGui and Monobehavior update ticks are out of sync
        // So redraw elements between monobehavior ticks.

        var t = Time.frameCount;
        if (_lastFrame != t)
        {
            Drawings.Clear();
            _lastFrame = t;
        }
    }

#endif

    /// <summary>
    /// Draw something custom in the scene view.
    /// </summary>
    /// <param name="drawing">instance of your IDebugDrawing implementation</param>
    [Conditional("UNITY_EDITOR")]
    public static void Draw(IDebugDrawing drawing)
    {
        CheckForFrameChange();
        Drawings.Add(drawing);
    }

    /// <summary>
    /// Draw a text label in 3D space.
    /// </summary>
    /// <param name="position">Where to draw the label in world coordinates</param>
    /// <param name="text">What the label should say</param>
    /// <param name="style">Style controlling how the label should look</param>
    [Conditional("UNITY_EDITOR")]
    public static void DrawLabel(Vector3 position, string text, GUIStyle style = null)
    {
        Draw(new LabelDrawing
        {
            Position = position,
            Text = text,
            Style = style,
        });
    }

    /// <summary>
    /// Draw a solid outlined rectangle in 3D space.
    /// </summary>
    /// <param name="verts">The screen coodinates rectangle.</param>
    /// <param name="faceColor">The color of the rectangle's face.</param>
    /// <param name="outlineColor">The outline color of the rectangle.</param>
    [Conditional("UNITY_EDITOR")]
    public static void DrawSolidRectangleWithOutline(Rect rectangle, Color faceColor, Color outlineColor)
    {
        Vector3[] verts = new Vector3[]
        {
            new Vector3(rectangle.xMin, rectangle.yMin, 0f),
            new Vector3(rectangle.xMax, rectangle.yMin, 0f),
            new Vector3(rectangle.xMax, rectangle.yMax, 0f),
            new Vector3(rectangle.xMin, rectangle.yMax, 0f)
        };

        Draw(new RectangleWithOutlineDrawing
        {
            FaceColor = faceColor,
            OutlineColor = outlineColor,
            Verts = verts,
        });
    }

    /// <summary>
    /// Draw a solid outlined rectangle in 3D space.
    /// </summary>
    /// <param name="verts">The 4 vertices of the rectangle in world coordinates.</param>
    /// <param name="faceColor">The color of the rectangle's face.</param>
    /// <param name="outlineColor">The outline color of the rectangle.</param>
    [Conditional("UNITY_EDITOR")]
    public static void DrawSolidRectangleWithOutline(Vector3[] verts, Color faceColor, Color outlineColor)
    {
        Draw(new RectangleWithOutlineDrawing
        {
            FaceColor = faceColor,
            OutlineColor = outlineColor,
            Verts = verts,
        });
    }

    /// <summary>
    /// Draw anti-aliased convex polygon specified with point array.
    /// </summary>
    /// <param name="verts">List of points describing the convex polygon</param>
    /// <param name="faceColor"></param>
    [Conditional("UNITY_EDITOR")]
    public static void DrawAAConvexPolygon(Vector3[] verts, Color color)
    {
        Draw(new PolygonDrawing
        {
            Color = color,
            Verts = verts,
        });
    }


}

public struct RectangleWithOutlineDrawing : IDebugDrawing
{
    public Color FaceColor;
    public Color OutlineColor;
    public Vector3[] Verts;

    public void Draw()
    {
        Handles.DrawSolidRectangleWithOutline(Verts, FaceColor, OutlineColor);      
    }
}

public struct PolygonDrawing : IDebugDrawing
{
    public Color Color;
    public Vector3[] Verts;

    public void Draw()
    { 
        Handles.color = Color;
        Handles.DrawAAConvexPolygon(Verts);       
    }
}

public struct LabelDrawing : IDebugDrawing
{
    public Vector3 Position;
    public string Text;
    public GUIStyle Style;

    public void Draw()
    {
        CenteredLabel(Position, Text, Style ?? SceneBoldLabelWithBackground.Value);
    }

    private static void CenteredLabel(Vector3 position, string text, GUIStyle style)
    {
        try
        {
            GUIContent gUIContent = TempGuiContent(text, null, null);
            if (HandleUtility.WorldToGUIPointWithDepth(position).z < 0.0)
                return;

            var size = style.CalcSize(gUIContent) / 2;
            Handles.BeginGUI();
            var screenPos = HandleUtility.WorldPointToSizedRect(position, gUIContent, style);
            screenPos.x -= size.x;
            screenPos.y -= size.y;
            GUI.Label(screenPos, gUIContent, style);
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
        finally
        {
            Handles.EndGUI();
        }
    }

    public static Lazy<GUIStyle> SceneBoldLabelWithBackground { get; } = new Lazy<GUIStyle>(() =>
    {
        #if UNITY_EDITOR
            GUIStyle style = new GUIStyle(EditorStyles.helpBox);
        #else
            GUIStyle style = new GUIStyle();
        #endif
        style.contentOffset = new Vector2(2, 2);
        style.padding = new RectOffset(2, 2, 2, 2);
        style.normal.textColor = Color.black;
        return style;
    });

    public static Lazy<GUIStyle> SceneBoldLabel { get; } = new Lazy<GUIStyle>(() =>
    {
        #if UNITY_EDITOR
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
        #else
            GUIStyle style = new GUIStyle();
        #endif
        style.contentOffset = new Vector2(-1, 2);
        style.padding = new RectOffset(2, 2, 2, 2);
        style.normal.textColor = Color.black;
        return style;
    });

    private static GUIContent _guiContent = null;
    private static GUIContent TempGuiContent(string label, string tooltip = null, Texture2D icon = null)
    {
        if (_guiContent == null)
        {
            _guiContent = new GUIContent();
        }
        _guiContent.text = label;
        _guiContent.tooltip = tooltip;
        _guiContent.image = icon;
        return _guiContent;
    }
}
