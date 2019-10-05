using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Vella.Common;

[ExecuteInEditMode]
public class DrawTester : MonoBehaviour
{
    [Header("Targets")]
    public GameObject Start;
    public GameObject End;

    [Header("Options")]
    public bool DrawInEditMode = true;
    public bool UseBurstJob;

    [Header("Tests")]
    public DrawingMethods DrawMethods;

    void Update()
    {
        if (!Application.isPlaying && !DrawInEditMode)
            return;

        var text = new NativeString512("MyText");

        if (UseBurstJob)
        {
            new NativeDrawJob
            {
                Start = Start.transform.position,
                End = End.transform.position,
                Text = text,
                Methods = DrawMethods

            }.Run();
        }
        else
        {
            ManagedDraw(Start.transform.position, End.transform.position, text, DrawMethods);
        }
    }

    private static void ManagedDraw(Vector3 start, Vector3 end, NativeString512 text, DrawingMethods methods)
    {
        DrawTests(start, end, text, methods);
    }

    [BurstCompile]
    public struct NativeDrawJob : IJob
    {
        public Vector3 Start;
        public Vector3 End;
        public NativeString512 Text;
        public DrawingMethods Methods;

        public void Execute()
        {
            DrawTests(Start, End, Text, Methods);
        }
    }

    private static void DrawTests(Vector3 start, Vector3 end, NativeString512 text, DrawingMethods methods)
    {
        var offset = Vector3.up * 0.05f + Vector3.left * 0.05f;
        var center = start + (end - start) / 2;

        if (methods.Sphere)
            NativeDebug.DrawSphere(start, 1.5f, UnityColors.GhostDodgerBlue);

        if (methods.RectangleWithOutline)
            NativeDebug.DrawSolidRectangleWithOutline(new Rect(center, new Vector2(0.5f, 0.5f)), UnityColors.LightYellow, UnityColors.Yellow);

        if (methods.Polygon)
        {
            //NativeDebug.DrawAAConvexPolygon(new[]
            //{
            //    center + new Vector3(0,0.5f,0),
            //    center - new Vector3(0,0.5f,0),
            //    center + new Vector3(0,0.5f,0.5f),
            //    center - new Vector3(0,0.5f,0.5f),

            //}, UnityColors.LightYellow);
        }

        if (methods.Line)
            NativeDebug.DrawLine(start + offset, end + offset);

        if (methods.Ray)
            NativeDebug.DrawRay(center, Vector3.up, UnityColors.MediumBlue);

        if (methods.Cone)
            NativeDebug.DrawCone(center, Vector3.up, UnityColors.DarkKhaki, 22.5f);

        if (methods.Circle)
            NativeDebug.DrawCircle(center, Vector3.up, 0.25f, UnityColors.AliceBlue);

        if (methods.DottedLine)
            NativeDebug.DrawDottedLine(start, end, Color.yellow);

        if (methods.WireCube)
            NativeDebug.DrawWireCube(end, Vector3.one/2, Color.yellow);

        if (methods.DottedWireCube)
            NativeDebug.DrawDottedWireCube(end, Vector3.one, Color.black);

        if (methods.Label)
            NativeDebug.DrawLabel(center + -Vector3.up * 0.25f, text);

        if (methods.Arrow)
            NativeDebug.DrawArrow(start, Vector3.up, Color.blue);

        if (methods.Log)
            NativeDebug.Log(text);

        if (methods.LogWarning)
            NativeDebug.LogWarning(text);

        if (methods.LogError)
            NativeDebug.LogError(text);
    }
}

[Serializable]
public struct DrawingMethods : ISerializationCallbackReceiver
{
    public bool Sphere;
    public bool RectangleWithOutline;
    public bool Polygon;
    public bool Line;
    public bool Ray;
    public bool Cone;
    public bool Circle;
    public bool DottedLine;
    public bool WireCube;
    public bool DottedWireCube;
    public bool Label;
    public bool Log;
    public bool LogWarning;
    public bool LogError;
    public bool Arrow;

    [SerializeField, HideInInspector]
    private bool _saved;

    public void OnBeforeSerialize()
    {
        if (!_saved)
        {
            this = Defaults;
        }
        _saved = true;
    }

    public void OnAfterDeserialize()
    {

    }

    public static DrawingMethods Defaults = new DrawingMethods
    {
        Sphere = true,
        RectangleWithOutline = true,
        Polygon = true,
        Line = true,
        Ray = true,
        Cone = true,
        Circle = true,
        DottedLine = true,
        WireCube = true,
        DottedWireCube = true,
        Label = true,
        Log = false,
        LogWarning = false,
        LogError = false,
        Arrow = true,
    };
}