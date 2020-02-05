﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Compilation;
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
    public TestingDebugDrawOptions DrawOptions;

    public static NativeArray<float3> Hexagon;
    private JobHandle _jobHandle;
    private bool _isBurstJob;
    private SceneView _sceneView;

    void Update()
    {
        if (!Application.isPlaying && !DrawInEditMode)
            return;

        if (Start == null || End == null)
            return;

        var text = new NativeString512("MyText");

        RefreshSceneViewOnJobTypeChanged();

        if (UseBurstJob)
        {
            _jobHandle.Complete();
            //_jobHandle = new DrawTestingJob
            //{
            //    Start = Start.transform.position,
            //    End = End.transform.position,
            //    Text = text,
            //    Methods = DrawOptions,
            //    Polyhedron = Hexagon
            //}.Schedule(_jobHandle);

            _jobHandle = new DrawTestingParallelJob
            {
                Start = Start.transform.position,
                End = End.transform.position,
                Text = text,
                Methods = DrawOptions,
                Polyhedron = Hexagon

            }.Schedule(10, 1, _jobHandle);
        }
        else
        {
            ManagedDraw(Start.transform.position, End.transform.position, text, DrawOptions);
        }
    }

    private void RefreshSceneViewOnJobTypeChanged()
    {
        var useBurst = UseBurstJob;
        if (useBurst != _isBurstJob)
        {
            SceneView.RepaintAll();
            _isBurstJob = useBurst;
        }
    }

    private static void ManagedDraw(Vector3 start, Vector3 end, NativeString512 text, TestingDebugDrawOptions methods)
    {
        //for (int i = 0; i < 5; i++)
        //{
        //    Task.Run(() =>
        //    {
        //        DrawTests(Thread.CurrentThread.ManagedThreadId, start, end, text, methods, Hexagon);
        //    });
        //}

        DrawTests(Thread.CurrentThread.ManagedThreadId, start, end, text, methods, Hexagon);
    }

    [BurstCompile]
    public struct DrawTestingJob : IJob
    {
        public Vector3 Start;
        public Vector3 End;
        public NativeString512 Text;
        public TestingDebugDrawOptions Methods;
        public NativeArray<float3> Polyhedron;

        [NativeSetThreadIndex] public int ThreadIndex;

        public void Execute()
        {
            DrawTests(ThreadIndex, Start, End, Text, Methods, Polyhedron);
        }
    }

    [BurstCompile]
    public struct DrawTestingParallelJob : IJobParallelFor
    {
        public Vector3 Start;
        public Vector3 End;
        public NativeString512 Text;
        public TestingDebugDrawOptions Methods;
        public NativeArray<float3> Polyhedron;

        public void Execute(int index)
        {
            DrawTests(index, Start, End, Text, Methods, Polyhedron);
        }
    }

    private void OnEnable()
    {
        Hexagon = DebugShapes.GenerateHexagon();
    }

    private void OnDestroy()
    {
        Hexagon.Dispose(_jobHandle);
    }

    private static NativeArray<float3> GenerateHexagon(float radius = 0.5f)
    {
        var arr = new NativeArray<float3>(6, Allocator.Persistent);
        var a = radius * 0.5f;
        arr[0] = new float3(radius, 0, 0);
        arr[1] = new float3(a, 0, radius);
        arr[2] = new float3(-a, 0, radius);
        arr[3] = new float3(-radius, 0, 0);
        arr[4] = new float3(-a, 0, -radius);
        arr[5] = new float3(a, 0, -radius);
        return arr;
    }

    private static unsafe void DrawTests(int threadIndex, float3 start, float3 end, NativeString512 text, TestingDebugDrawOptions methods, NativeArray<float3> polygon, int index = -1)
    {
        float3 offset = Vector3.up * 0.05f + Vector3.left * 0.05f;
        float3 center = (start + (end - start) / 2);

        if (methods.Sphere)
            DebugDrawer.DrawSphere(start, 0.75f, UnityColors.GhostDodgerBlue);

        if (methods.RectangleWithOutline)
        {
            var size = 0.25f;
            var points = stackalloc[]
            {
                center + offset + new float3(0, 0, 0),
                center + offset + new float3(0, size, 0),
                center + offset + new float3(0, size, size),
                center + offset + new float3(0, 0, size)
            };

            DebugDrawer.DrawSolidRectangleWithOutline(points, UnityColors.LightYellow, UnityColors.Yellow);
        }

        if (methods.Polygon)
        {
            DebugDrawer.UnsafeDrawAAConvexPolygon(polygon, center + (float3)Vector3.down * 0.25f, UnityColors.GhostDodgerBlue);
        }

        if (methods.Line)
            DebugDrawer.DrawLine(start + offset, end + offset);

        if (methods.Ray)
            DebugDrawer.DrawRay(center, Vector3.up, UnityColors.MediumBlue);

        if (methods.Cone)
            DebugDrawer.DrawCone(center + (float3)Vector3.up * 0.5f, Vector3.up, UnityColors.DarkKhaki, 22.5f);

        if (methods.Circle)
            DebugDrawer.DrawCircle(center, Vector3.up, 0.25f, UnityColors.AliceBlue);

        if (methods.DottedLine)
            DebugDrawer.DrawDottedLine(start, end, Color.yellow);

        if (methods.WireCube)
            DebugDrawer.DrawWireCube(end, Vector3.one / 2, Color.yellow);

        if (methods.DottedWireCube)
            DebugDrawer.DrawDottedWireCube(end, Vector3.one, Color.black);

        if (methods.Label)
            DebugDrawer.DrawLabel(center + (float3)Vector3.down * 0.25f, text);

        if (methods.Arrow)
            DebugDrawer.DrawArrow(start + (float3)Vector3.up * 0.5f, Vector3.up, Color.blue);

        if (methods.Log)
            DebugDrawer.Log(threadIndex, text);

        if (methods.LogWarning)
            DebugDrawer.LogWarning(text);

        if (methods.LogError)
            DebugDrawer.LogError(text);

        if (methods.Point)
            DebugDrawer.DrawPoint(center + (float3)Vector3.forward, UnityColors.Lavender, 0.25f);
    }
}

public static class DebugShapes
{
    public static NativeArray<float3> GenerateHexagon(float radius = 0.5f)
    {
        var arr = new NativeArray<float3>(6, Allocator.Persistent);
        var a = radius * 0.5f;
        arr[0] = new float3(radius, 0, 0);
        arr[1] = new float3(a, 0, radius);
        arr[2] = new float3(-a, 0, radius);
        arr[3] = new float3(-radius, 0, 0);
        arr[4] = new float3(-a, 0, -radius);
        arr[5] = new float3(a, 0, -radius);
        return arr;
    }
}

[Serializable]
public struct TestingDebugDrawOptions : ISerializationCallbackReceiver
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
    public bool Point;

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

    public static TestingDebugDrawOptions Defaults = new TestingDebugDrawOptions
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
        Point = true,
    };
}