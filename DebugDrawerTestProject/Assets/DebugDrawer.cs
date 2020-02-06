using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;
using System.Runtime.CompilerServices;

namespace Vella.Common
{
    public interface INativeDebuggable
    {
        void Execute();
    }

    public enum DebugDrawingType
    {
        None = 0,
        Sphere,
        RectangleWithOutline,
        Polygon,
        Line,
        Ray,
        Cone,
        Circle,
        DottedLine,
        DottedWireCube,
        Label,
        Log,
        WireCube
    }

    public static class DebugDrawer
    {
        public static Color DefaultColor => UnityColors.White;
        private static bool _isCreated;
        private static int _lastProcessedFrame;

        // An extra channel for drawings queued from main thread / unspecified index.
        private const int UnknownThreadIndex = JobsUtility.MaxJobThreadCount;

#if UNITY_EDITOR

        [InitializeOnLoadMethod]
        static void OnRuntimeMethodLoad()
        {
            NativeDebugSharedData.Stream.Allocate(JobsUtility.MaxJobThreadCount + 5, Allocator.Persistent);
            SceneView.duringSceneGui += SceneViewOnDuringSceneGui;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.assemblyCompilationFinished += OnCompilationFinished;
        }

        private static void OnCompilationStarted(object obj)
        {
            NativeDebugSharedData.State.IsTransitioning = true;
        }

        private static void OnCompilationFinished(string arg1, CompilerMessage[] arg2)
        {
            NativeDebugSharedData.State.IsTransitioning = false;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                case PlayModeStateChange.ExitingPlayMode:
                    NativeDebugSharedData.State.IsTransitioning = true;
                    //NativeDebugSharedData.Stream.Clear();
                    break;

                case PlayModeStateChange.EnteredEditMode:
                case PlayModeStateChange.EnteredPlayMode:
                    NativeDebugSharedData.State.IsTransitioning = false;
                    break;
            }
        }

        private unsafe static void SceneViewOnDuringSceneGui(SceneView obj)
        {
            if (NativeDebugSharedData.State.IsTransitioning)
                return;

            if (!NativeDebugSharedData.Stream.Current.Stream.IsCreated)
                return;

            var thisFrame = Time.frameCount;
            var lastQueuedFrame = NativeDebugSharedData.Time.LastQueuedFrame;

            // Reset after editor with script recompile.
            if (lastQueuedFrame > thisFrame || _lastProcessedFrame > thisFrame) 
            {
                _lastProcessedFrame = 0;
                NativeDebugSharedData.Time.LastQueuedFrame = thisFrame;
                Debug.Log("Transition Reset");
            }

            //Debug.Log($"Current Frame: {thisFrame} LastQueued: {lastQueuedFrame} Processed: {_lastProcessedFrame}");

            NativeDebugSharedData.Time.CurrentFrame = thisFrame;
            if (lastQueuedFrame < _lastProcessedFrame)
            {
                //Debug.Log("Skipped");

                if (Application.isEditor)
                {
                    //if (thisFrame - lastQueuedFrame > 0)
                    //{
                        // Clear the current stream to stop drawing.
                       // NativeDebugSharedData.Stream.Rotate();
                        //_lastStoppedFrame = lastFrame;
                        //Debug.Log("Framecount unchanged.");
                    //SceneView.RepaintAll();
                    //}
                    //EditorWindow.GetWindow<SceneView>().Repaint();

                    //using (var scope = new Handles.DrawingScope())
                    //{

                    //}
                }

                return;


            }



            //var lastFrame = NativeDebugSharedData.Time.LastFrame;
            //if (_lastStoppedFrame != lastFrame)
            //{


            //var startDepth = NativeDebugSharedData.Time.Depth;

            // Catch the case where incoming drawing commands have stopped and therefore FrameCount is not being incremented,
            // this could happen for example if an option was toggled to disable drawing in edit mode. 



            //NativeDebugSharedData.Time.LastQueuedFrame = Time.frameCount;



            //Debug.Log($"Processing: {NativeDebugSharedData.Stream.ProcessingStream.ComputeItemCount()} items, Id={NativeDebugSharedData.Stream.ProcessingStreamId} Ptr={((IntPtr)NativeDebugSharedData.Stream.ProcessingStream.GetUnsafePtr()):X}");





            //if (NativeDebugSharedData.Time.FrameCount - lastFrame > 1)
            //{
            //    // Clear the current stream to stop drawing.
            //    NativeDebugSharedData.Stream.UseNext();
            //    _lastStoppedFrame = lastFrame;
            //    Debug.Log("Framecount unchanged.");
            //    SceneView.RepaintAll();
            //}

            //
            //if (itemCount > 0)
            //{

            //NativeDebugSharedData.Stream.CloseWriters();



            NativeDebugSharedData.Stream.Rotate();

            var itemCount = NativeDebugSharedData.Stream.ProcessingStream.Stream.ComputeItemCount();

            Debug.Log($"Processing: {itemCount} items");

            using (var scope = new Handles.DrawingScope())
            {
                var reader = NativeDebugSharedData.Stream.ProcessingStream.Stream.AsReader();
                for (int i = 0; i < NativeDebugSharedData.Stream.ProcessingStream.Indices.Length; i++)
                {
                    var index = NativeDebugSharedData.Stream.ProcessingStream.Indices.Ptr[i];
                    reader.BeginForEachIndex(index);
                    Draw(ref reader);
                    reader.EndForEachIndex();
                }
                    
                //for (int i = 0; i < reader.ForEachCount; i++)
                //{
                //    reader.BeginForEachIndex(i);
                //    Draw(ref reader);
                //    reader.EndForEachIndex();
                //}
            }

            _lastProcessedFrame = thisFrame;
            //}

        }

        private static void Draw(ref UnsafeStream.Reader reader)
        {
            while (reader.RemainingItemCount > 0)
            {
                var type = reader.Peek<DebugDrawingType>();
                switch (type)
                {
                    case DebugDrawingType.Sphere:
                        reader.Read<Sphere>().Execute();
                        break;
                    case DebugDrawingType.RectangleWithOutline:
                        reader.Read<RectangleWithOutline>().Execute();
                        break;
                    case DebugDrawingType.Polygon:
                        reader.Read<Polygon>().Execute();
                        break;
                    case DebugDrawingType.Line:
                        reader.Read<Line>().Execute();
                        break;
                    case DebugDrawingType.Ray:
                        reader.Read<Ray>().Execute();
                        break;
                    case DebugDrawingType.Cone:
                        reader.Read<Cone>().Execute();
                        break;
                    case DebugDrawingType.Circle:
                        reader.Read<Circle>().Execute();
                        break;
                    case DebugDrawingType.DottedLine:
                        reader.Read<DottedLine>().Execute();
                        break;
                    case DebugDrawingType.DottedWireCube:
                        reader.Read<DottedWireCube>().Execute();
                        break;
                    case DebugDrawingType.WireCube:
                        reader.Read<WireCube>().Execute();
                        break;
                    case DebugDrawingType.Label:
                        reader.Read<Label>().Execute();
                        break;
                    case DebugDrawingType.Log:
                        reader.Read<Log>().Execute();
                        break;
                    default:
                        Debug.LogError($"The type {type} is not valid");
                        return;
                        //throw new ArgumentOutOfRangeException($"The type {type} is not valid");
                }
            }
        }
#endif


        /// <summary>
        /// Draw something custom in the scene view.
        /// </summary>
        /// <param name="drawing">instance of your IDebugDrawing implementation</param>
        [Conditional("UNITY_EDITOR"), MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void QueueDrawing<T>(T drawing) where T : struct, INativeDebuggable
        {
            QueueDrawing(UnknownThreadIndex, drawing);

            //if (NativeDebugSharedData.State.IsTransitioning)
            //    return;

            //if (!NativeDebugSharedData.Stream.Current.IsCreated)
            //    return;

            //CheckForFrameChange();

            //// new AsWriter() is injected with the threadId when its part of a job which isn't going to happen
            //// here so the whole per thread separation aspect of UnsafeStream thread safety is not working in this context.

            //// Usually in a job situation UnsafeStream can rely on knowing a range of indices to be written, which is not the case here.
            //// Could potentially remove the interlocked count by having separate draw loops in SceneViewOnDuringSceneGui for each thread;
            //// Using a stream per threadId + 1 for managed. Though the user would have to pass in the ThreadId manually.

            //var writer = NativeDebugSharedData.Stream.Current.AsWriter();
            //var count = NativeDebugSharedData.Stream.GetIndex();

            //// Just cap draw calls rather than trying to figure out if a scene view is visible
            //// (SceneViewOnDuringSceneGui won't run while scene views aren't visible, so the stream would fill up);
            //if (count >= writer.ForEachCount)
            //    return;

            //writer.BeginForEachIndex(count);
            //writer.Write(drawing);
            //writer.EndForEachIndex();
        }

        [Conditional("UNITY_EDITOR")]
        public static void QueueDrawing<T>(int threadIndex, T drawing) where T : struct, INativeDebuggable
        {
            if (NativeDebugSharedData.State.IsTransitioning)
                return;

            if (!NativeDebugSharedData.Stream.Current.Stream.IsCreated)
                return;

            //ref var writer = ref NativeDebugSharedData.Stream.Writer;
            NativeDebugSharedData.Stream.Current.Write(threadIndex, drawing);

            //writer.BeginForEachIndex(threadIndex);
            //writer.Write(drawing);
            //writer.EndForEachIndex();

            ref var time = ref NativeDebugSharedData.Time;
            if (time.CurrentFrame > time.LastQueuedFrame)
                time.LastQueuedFrame = time.CurrentFrame;
        }

        private static void CheckForFrameChange()
        {
            var currentFrame = NativeDebugSharedData.Time.CurrentFrame;
            if (NativeDebugSharedData.Time.LastQueuedFrame != currentFrame)
            {
                NativeDebugSharedData.Time.Depth++;
                NativeDebugSharedData.Stream.Rotate();
                NativeDebugSharedData.Time.LastQueuedFrame = currentFrame;
            }
        }

        /// <summary>
        /// Draw a text label in 3D space.
        /// </summary>
        /// <param name="position">Where to draw the label in world coordinates</param>
        /// <param name="text">What the label should say</param>
        /// <param name="style">Style controlling how the label should look</param>
        [Conditional("UNITY_EDITOR")]
        public static void DrawLabel(Vector3 position, string text, NativeLabelStyles style = NativeLabelStyles.Default)
        {
            QueueDrawing(new Label
            {
                Type = DebugDrawingType.Label,
                Position = position,
                Text = new NativeString512(text),
                Style = style,
            });
        }

        [Conditional("UNITY_EDITOR")]
        public static void DrawLabel(int threadIndex, Vector3 position, string text, NativeLabelStyles style = NativeLabelStyles.Default)
        {
            QueueDrawing(threadIndex, new Label
            {
                Type = DebugDrawingType.Label,
                Position = position,
                Text = new NativeString512(text),
                Style = style,
            });
        }

        /// <summary>
        /// Draw a text label in 3D space.
        /// </summary>
        /// <param name="position">Where to draw the label in world coordinates</param>
        /// <param name="text">What the label should say</param>
        /// <param name="style">Style controlling how the label should look</param>
        [Conditional("UNITY_EDITOR")]
        public static void DrawLabel(Vector3 position, NativeString512 text, NativeLabelStyles style = NativeLabelStyles.Default)
        {
            QueueDrawing(new Label
            {
                Type = DebugDrawingType.Label,
                Position = position,
                Text = text,
                Style = style,
            });
        }

        [Conditional("UNITY_EDITOR")]
        public static void DrawLabel(int threadIndex, Vector3 position, NativeString512 text, NativeLabelStyles style = NativeLabelStyles.Default)
        {
            QueueDrawing(threadIndex, new Label
            {
                Type = DebugDrawingType.Label,
                Position = position,
                Text = text,
                Style = style,
            });
        }

        /// <summary>
        /// Draw a text label in 3D space.
        /// </summary>
        /// <param name="position">Where to draw the label in world coordinates</param>
        /// <param name="text">What the label should say</param>
        /// <param name="style">Style controlling how the label should look</param>
        [Conditional("UNITY_EDITOR")]
        public static void DrawLine(Vector3 start, Vector3 end, Color color = default)
        {
            QueueDrawing(new Line
            {
                Type = DebugDrawingType.Line,
                Color = color == default ? DefaultColor : color,
                Start = start,
                End = end,
            });
        }

        public static void DrawLine(int threadIndex, Vector3 start, Vector3 end, Color color = default)
        {
            QueueDrawing(threadIndex, new Line
            {
                Type = DebugDrawingType.Line,
                Color = color == default ? DefaultColor : color,
                Start = start,
                End = end,
            });
        }

        /// <summary>
        /// Draw a debug dotted line.
        /// </summary>
        /// <param name="start">start position in world space</param>
        /// <param name="end">end position in world space</param>
        /// <param name="color">color of the line</param>
        /// <param name="GapSize">The space between dots in pixels</param>
        [Conditional("UNITY_EDITOR")]
        public static void DrawDottedLine(Vector3 start, Vector3 end, Color? color = null, float GapSize = default)
        {
            if (GapSize == default)
            {
                //GapSize = Vector3.Distance(Camera.main.transform.position, start) / 10f;
                GapSize = 1;
            }

            QueueDrawing(new DottedLine
            {
                Type = DebugDrawingType.DottedLine,
                Color = color ?? DefaultColor,
                Start = start,
                End = end,
                GapSize = GapSize,
            });
        }

        /// <summary>
        /// Draw a solid outlined rectangle in 3D space.
        /// </summary>
        /// <param name="verts">The 4 vertices of the rectangle in world coordinates.</param>
        /// <param name="faceColor">The color of the rectangle's face.</param>
        /// <param name="outlineColor">The outline color of the rectangle.</param>
        [Conditional("UNITY_EDITOR")]
        public static void DrawSolidRectangleWithOutline(Vector3[] verts, Color? faceColor = null, Color? outlineColor = null)
        {
            QueueDrawing(new RectangleWithOutline
            {
                Type = DebugDrawingType.RectangleWithOutline,
                FaceColor = faceColor ?? DefaultColor,
                OutlineColor = outlineColor ?? DefaultColor,
                VertA = verts[0],
                VertB = verts[1],
                VertC = verts[2],
                VertD = verts[3],
            });
        }

        [Conditional("UNITY_EDITOR")]
        public static unsafe void DrawSolidRectangleWithOutline(NativeArray<float3> points, Color? faceColor = null, Color? outlineColor = null)
        {
            DrawSolidRectangleWithOutline(points[0], points[1], points[2], points[3], faceColor, outlineColor);
        }

        [Conditional("UNITY_EDITOR")]
        public static unsafe void DrawSolidRectangleWithOutline(float3* points, Color? faceColor = null, Color? outlineColor = null)
        {
            DrawSolidRectangleWithOutline(points[0], points[1], points[2], points[3], faceColor, outlineColor);
        }

        [Conditional("UNITY_EDITOR")]
        public static void DrawSolidRectangleWithOutline(float3 a, float3 b, float3 c, float3 d, Color? faceColor = null, Color? outlineColor = null)
        {
            QueueDrawing(new RectangleWithOutline
            {
                Type = DebugDrawingType.RectangleWithOutline,
                FaceColor = faceColor ?? DefaultColor,
                OutlineColor = outlineColor ?? DefaultColor,
                VertA = a,
                VertB = b,
                VertC = c,
                VertD = d,
            });
        }

        ///// <summary>
        ///// Draw anti-aliased convex polygon specified with point array.
        ///// </summary>
        ///// <param name="verts">List of points describing the convex polygon</param>
        ///// <param name="faceColor"></param>
        //[Conditional("UNITY_EDITOR")]
        //public static void DrawAAConvexPolygon(Vector3[] verts, Color? color = null)
        //{
        //    QueueDrawing(new Polygon
        //    {
        //        Type = DebugDrawingType.Polygon,
        //        Color = color ?? DefaultColor,
        //        Verts = verts,
        //    });
        //}

        [Conditional("UNITY_EDITOR")]
        public static unsafe void UnsafeDrawAAConvexPolygon(NativeArray<float3> worldPoints, Color? color = null)
        {
            QueueDrawing(new Polygon
            {
                Type = DebugDrawingType.Polygon,
                Color = color ?? DefaultColor,
                Verts = (float3*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(worldPoints),
                Count = worldPoints.Length
            });
        }

        [Conditional("UNITY_EDITOR")]
        public static unsafe void UnsafeDrawAAConvexPolygon(NativeArray<float3> localPoints, float3 offset, Color? color = null)
        {
            QueueDrawing(new Polygon
            {
                Type = DebugDrawingType.Polygon,
                Color = color ?? DefaultColor,
                Verts = (float3*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(localPoints),
                Count = localPoints.Length,
                Offset = offset,
            });
        }

        [Conditional("UNITY_EDITOR")]
        public static unsafe void UnsafeDrawAAConvexPolygon(float3* points, int pointCount, Color? color = null)
        {
            QueueDrawing(new Polygon
            {
                Type = DebugDrawingType.Polygon,
                Color = color ?? DefaultColor,
                Verts = points,
                Count = pointCount,
            });
        }

        [Conditional("UNITY_EDITOR")]
        public static void DrawSphere(Vector3 center, float radius, Color? color = null)
        {
            QueueDrawing(new Sphere
            {
                Type = DebugDrawingType.Sphere,
                Color = color ?? DefaultColor,
                Center = center,
                Radius = radius,
            });
        }

        /// <summary>
        /// Draws an arrow
        /// </summary>
        /// <param name="position">The start position of the arrow.</param>
        /// <param name="direction">The direction the arrow will point in.</param>
        /// <param name="color">The color of the arrow.</param>
        /// <param name="duration">How long to draw the arrow.</param>
        /// <param name="depthTest">Whether or not the arrow should be faded when behind other objects. </param>
        [Conditional("UNITY_EDITOR")]
        public static void DrawArrow(Vector3 position, Vector3 direction, Color? color = null, float duration = 0, bool depthTest = true)
        {
            QueueDrawing(new Ray
            {
                Type = DebugDrawingType.Ray,
                Position = position,
                Direction = direction,
                Color = color ?? DefaultColor,
                Duration = duration,
                DepthTest = depthTest,
            });

            DrawCone(position + direction, -direction * 0.33f, color ?? DefaultColor, 15, 1f, duration, depthTest);
        }

        /// <summary>
        /// Draw a point as a cross/star shape made of lines.
        /// </summary>
        /// <param name="position">The point to debug.</param>
        /// <param name="color">The color of the point.</param>
        /// <param name="scale">The size of the point.</param>
        /// <param name="duration">How long to draw the point.</param>
        /// <param name="depthTest">depthTest</param>
        [Conditional("UNITY_EDITOR")]
        public static void DrawPoint(Vector3 position, Color? color = null, float scale = 1.0f, float duration = 0, bool depthTest = true)
        {
            // Debug Extension
            // By Arkham Interactive
            // Source: https://assetstore.unity.com/packages/tools/debug-drawing-extension-11396
            // 	- Static class that extends Unity's debugging functionallity.
            // 	- Attempts to mimic Unity's existing debugging behaviour for ease-of-use.
            // 	- Includes gizmo drawing methods for less memory-intensive debug visualization.

            //color = (color != default) ? color : DefaultColor;
            //Debug.DrawRay(position + (Vector3.up * (scale * 0.25f)), -Vector3.up * scale * 0.5f, color, duration, depthTest);
            //Debug.DrawRay(position + (Vector3.right * (scale * 0.25f)), -Vector3.right * scale * 0.5f, color, duration, depthTest);
            //Debug.DrawRay(position + (Vector3.forward * (scale * 0.25f)), -Vector3.forward * scale * 0.5f, color, duration, depthTest);

            QueueDrawing(new Ray
            {
                Type = DebugDrawingType.Ray,
                Position = position + (Vector3.up * (scale * 0.25f)),
                Direction = -Vector3.up * scale * 0.5f,
                Color = color ?? DefaultColor,
                Duration = duration,
                DepthTest = depthTest,
            });

            QueueDrawing(new Ray
            {
                Type = DebugDrawingType.Ray,
                Position = position + (Vector3.right * (scale * 0.25f)),
                Direction = -Vector3.right * scale * 0.5f,
                Color = color ?? DefaultColor,
                Duration = duration,
                DepthTest = depthTest,
            });

            QueueDrawing(new Ray
            {
                Type = DebugDrawingType.Ray,
                Position = position + (Vector3.forward * (scale * 0.25f)),
                Direction = -Vector3.forward * scale * 0.5f,
                Color = color ?? DefaultColor,
                Duration = duration,
                DepthTest = depthTest,
            });
        }

        /// <summary>
        /// Draws a line from a position and direction
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void DrawRay(Vector3 position, Vector3 direction, Color? color = null, float distance = 1f, float duration = 0, bool depthTest = true)
        {
            DrawRay(Thread.CurrentThread.ManagedThreadId, position, direction, color, distance, duration, depthTest);
        }

        /// <summary>
        /// Draws a line from a position and direction
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void DrawRay(int threadIndex, Vector3 position, Vector3 direction, Color? color = null, float distance = 1f, float duration = 0, bool depthTest = true)
        {
            QueueDrawing(threadIndex, new Ray
            {
                Type = DebugDrawingType.Ray,
                Position = position,
                Direction = direction * distance,
                Color = color ?? DefaultColor,
                Duration = duration,
                DepthTest = depthTest,
            });
        }

        /// <summary>
        /// Draws a circle
        /// </summary>
        /// <param name="position">Where the center of the circle will be positioned.</param>
        /// <param name="up">The direction perpendicular to the surface of the circle.</param>
        /// <param name="color">The color of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="duration">How long to draw the circle.</param>
        /// <param name="depthTest">Whether or not the circle should be faded when behind other objects.</param>
        [Conditional("UNITY_EDITOR")]
        public static void DrawCircle(Vector3 position, Vector3 up, float radius = 1.0f, Color? color = null, float duration = 0, bool depthTest = true)
        {
            QueueDrawing(new Circle
            {
                Type = DebugDrawingType.Circle,
                Position = position,
                Up = up,
                Color = color ?? DefaultColor,
                Radius = radius,
                Duration = duration,
                DepthTest = depthTest,
            });
        }

        /// <summary>
        /// Debugs a cone.
        /// </summary>
        /// <param name="position">The position for the tip of the cone.</param>
        /// <param name="direction">The direction for the cone gets wider in.</param>
        /// <param name="color">The angle of the cone.</param>
        /// <param name="angle">The color of the cone.</param>
        /// <param name="duration">How long to draw the cone.</param>
        /// <param name="depthTest">Whether or not the cone should be faded when behind other objects.</param>
        [Conditional("UNITY_EDITOR")]
        public static void DrawCone(Vector3 position, Vector3 direction, Color color = default, float angle = 45, float scale = 1f, float duration = 0, bool depthTest = true)
        {
            QueueDrawing(new Cone
            {
                Type = DebugDrawingType.Cone,
                Position = position,
                Direction = direction,
                Color = color != default ? color : DefaultColor,
                Angle = angle,
                Scale = scale,
                Duration = duration,
                DepthTest = depthTest,
            });
        }

        /// <summary>
        /// Debugs a cone.
        /// </summary>
        /// <param name="position">The position for the tip of the cone.</param>
        /// <param name="direction">The direction for the cone gets wider in.</param>
        /// <param name="color">The angle of the cone.</param>
        /// <param name="angle">The color of the cone.</param>
        /// <param name="duration">How long to draw the cone.</param>
        /// <param name="depthTest">Whether or not the cone should be faded when behind other objects.</param>
        [Conditional("UNITY_EDITOR")]
        public static void DrawCone(int threadIndex, Vector3 position, Vector3 direction, Color color = default, float angle = 45, float scale = 1f, float duration = 0, bool depthTest = true)
        {
            QueueDrawing(threadIndex, new Cone
            {
                Type = DebugDrawingType.Cone,
                Position = position,
                Direction = direction,
                Color = color != default ? color : DefaultColor,
                Angle = angle,
                Scale = scale,
                Duration = duration,
                DepthTest = depthTest,
            });
        }

        /// <summary>
        /// Draws a cube made with lines
        /// </summary>
        /// <param name="center">center of the code in world space</param>
        /// <param name="size">size of the cube (extents*2 or max-min)</param>
        /// <param name="color">the color of the cube lines</param>
        /// <param name="gapSize">size of empty spaces in the lines</param>
        [Conditional("UNITY_EDITOR")]
        public static void DrawDottedWireCube(Vector3 center, Vector3 size, Color? color = null, float gapSize = 1)
        {
            QueueDrawing(new DottedWireCube
            {
                Type = DebugDrawingType.DottedWireCube,
                Color = color ?? DefaultColor,
                Center = center,
                Size = size,
                GapSize = gapSize,
            });
        }

        /// <summary>
        /// Draws a cube made with lines
        /// </summary>
        /// <param name="center">center of the code in world space</param>
        /// <param name="size">size of the cube (extents*2 or max-min)</param>
        /// <param name="color">the color of the cube lines</param>
        [Conditional("UNITY_EDITOR")]
        public static void DrawWireCube(Vector3 center, Vector3 size, Color color = default)
        {
            QueueDrawing(new WireCube(center, size, color));
        }

        public static void Log(int threadIndex, NativeString512 text)
        {
            QueueDrawing(new Log
            {
                Type = DebugDrawingType.Log,
                DisplayType = Common.Log.LogDisplayType.Info,
                FromJob = JobsUtility.IsExecutingJob,
                ThreadId = threadIndex,
                Message = text
            });
        }

        public static void LogWarning(NativeString512 text)
        {
            QueueDrawing(new Log
            {
                Type = DebugDrawingType.Log,
                DisplayType = Common.Log.LogDisplayType.Warning,
                Message = text
            });
        }

        public static void LogError(NativeString512 text)
        {
            QueueDrawing(new Log
            {
                Type = DebugDrawingType.Log,
                DisplayType = Common.Log.LogDisplayType.Error,
                Message = text
            });
        }
    }

    public struct Sphere : INativeDebuggable
    {
        public DebugDrawingType Type;
        public Color Color;
        public float Radius;
        public Vector3 Center;

        public void Execute()
        {
#if UNITY_EDITOR
            Handles.color = Color;
            Handles.SphereHandleCap(0, Center, Quaternion.identity, Radius, EventType.Repaint);
#endif
        }
    }

    public struct RectangleWithOutline : INativeDebuggable
    {
        public DebugDrawingType Type;
        public Color FaceColor;
        public Color OutlineColor;
        public Vector3 VertA;
        public Vector3 VertB;
        public Vector3 VertC;
        public Vector3 VertD;

        public void Execute()
        {
#if UNITY_EDITOR
            Handles.color = FaceColor;
            Handles.DrawSolidRectangleWithOutline(new Vector3[]
            {
                VertA,VertB,VertC,VertD

            }, FaceColor, OutlineColor);
#endif
        }
    }

    public unsafe struct Polygon : INativeDebuggable
    {
        public DebugDrawingType Type;
        public Color Color;
        public float3* Verts;
        public int Count;
        public float3 Offset;
        private bool _offsetApplied;

        public void Execute()
        {
#if UNITY_EDITOR
            Handles.color = Color;
            var arr = UnsafeToArray<Vector3>(Verts, Count);
            if (math.all(Offset != float3.zero))
            {
                for (int i = 0; i < arr.Length; i++)
                {
                    arr[i] += (Vector3)Offset;
                }
            }
            Handles.DrawAAConvexPolygon(arr);
#endif
        }

        public static T[] UnsafeToArray<T>(void* src, int length) where T : struct
        {
            T[] dst = new T[length];
            if (dst == null)
                throw new ArgumentNullException(nameof(dst));
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "length must be equal or greater than zero.");
            GCHandle gcHandle = GCHandle.Alloc((object)dst, GCHandleType.Pinned);
            UnsafeUtility.MemCpy((void*)((IntPtr)(void*)gcHandle.AddrOfPinnedObject()), (void*)((IntPtr)src), (long)(length * UnsafeUtility.SizeOf<T>()));
            gcHandle.Free();
            return dst;
        }
    }

    public struct Line : INativeDebuggable
    {
        public DebugDrawingType Type;
        public Color Color;
        public Vector3 Start;
        public Vector3 End;

        public void Execute()
        {
#if UNITY_EDITOR
            Debug.DrawLine(Start, End, Color);
#endif
        }
    }

    public struct Ray : INativeDebuggable
    {
        public DebugDrawingType Type;
        public Color Color;
        public Vector3 Position;
        public Vector3 Direction;
        public float Duration;
        public bool DepthTest;

        public void Execute()
        {
#if UNITY_EDITOR
            Debug.DrawRay(Position, Direction, Color, Duration, DepthTest);
#endif
        }
    }

    public struct Cone : INativeDebuggable
    {
        public DebugDrawingType Type;
        public Vector3 Position;
        public Vector3 Direction;
        public float Angle;
        public float Duration;
        public bool DepthTest;
        public Color Color;
        public float Scale;

        public void Execute()
        {
#if UNITY_EDITOR
            // Debug Extension
            // By Arkham Interactive
            // Source: https://assetstore.unity.com/packages/tools/debug-drawing-extension-11396
            // 	- Static class that extends Unity's debugging functionallity.
            // 	- Attempts to mimic Unity's existing debugging behaviour for ease-of-use.
            // 	- Includes gizmo drawing methods for less memory-intensive debug visualization.

            float length = Direction.magnitude;
            Vector3 _forward = Direction * Scale;
            Vector3 _up = Vector3.Slerp(_forward, -_forward, 0.5f);
            Vector3 _right = Vector3.Cross(_forward, _up).normalized * length;
            Vector3 slerpedVector = Vector3.Slerp(_forward, _up, Angle / 90.0f);

            float dist;
            var farPlane = new Plane(-_forward, Position + _forward);
            var distRay = new UnityEngine.Ray(Position, slerpedVector);

            farPlane.Raycast(distRay, out dist);


            Color = Color != default ? Color : Color.white;

            Debug.DrawRay(Position, slerpedVector.normalized * dist, Color);
            //Handles.SphereHandleCap(0, Position, quaternion.identity, 0.1f, EventType.Repaint);

            Debug.DrawRay(Position, Vector3.Slerp(_forward, -_up, Angle / 90.0f).normalized * dist, Color, Duration, DepthTest);
            Debug.DrawRay(Position, Vector3.Slerp(_forward, _right, Angle / 90.0f).normalized * dist, Color, Duration, DepthTest);
            Debug.DrawRay(Position, Vector3.Slerp(_forward, -_right, Angle / 90.0f).normalized * dist, Color, Duration, DepthTest);

            new Circle
            {
                Position = Position + _forward,
                Up = _forward,
                Radius = (_forward - (slerpedVector.normalized * dist)).magnitude,
                Color = Color,
                Duration = Duration,
                DepthTest = DepthTest

            }.Execute();

            new Circle
            {
                Position = Position + (_forward * 0.5f),
                Up = _forward,
                Radius = ((_forward * 0.5f) - (slerpedVector.normalized * (dist * 0.5f))).magnitude,
                Color = Color,
                Duration = Duration,
                DepthTest = DepthTest

            }.Execute();
#endif
        }
    }

    public struct Circle : INativeDebuggable
    {
        public DebugDrawingType Type;
        public Vector3 Position;
        public Vector3 Up;
        public float Radius;
        public float Duration;
        public bool DepthTest;
        public Color Color;

        public void Execute()
        {
#if UNITY_EDITOR
            // Debug Extension
            // By Arkham Interactive
            // Source: https://assetstore.unity.com/packages/tools/debug-drawing-extension-11396
            // 	- Static class that extends Unity's debugging functionallity.
            // 	- Attempts to mimic Unity's existing debugging behaviour for ease-of-use.
            // 	- Includes gizmo drawing methods for less memory-intensive debug visualization.


            Vector3 _up = Up.normalized * Radius;
            Vector3 _forward = Vector3.Slerp(_up, -_up, 0.5f);
            Vector3 _right = Vector3.Cross(_up, _forward).normalized * Radius;

            Matrix4x4 matrix = new Matrix4x4();

            matrix[0] = _right.x;
            matrix[1] = _right.y;
            matrix[2] = _right.z;

            matrix[4] = _up.x;
            matrix[5] = _up.y;
            matrix[6] = _up.z;

            matrix[8] = _forward.x;
            matrix[9] = _forward.y;
            matrix[10] = _forward.z;

            Vector3 _lastPoint = Position + matrix.MultiplyPoint3x4(new Vector3(Mathf.Cos(0), 0, Mathf.Sin(0)));
            Vector3 _nextPoint = Vector3.zero;

            for (var i = 0; i < 91; i++)
            {
                _nextPoint.x = Mathf.Cos((i * 4) * Mathf.Deg2Rad);
                _nextPoint.z = Mathf.Sin((i * 4) * Mathf.Deg2Rad);
                _nextPoint.y = 0;

                _nextPoint = Position + matrix.MultiplyPoint3x4(_nextPoint);

                Debug.DrawLine(_lastPoint, _nextPoint, Color, Duration, DepthTest);
                _lastPoint = _nextPoint;
            }
#endif
        }
    }

    public struct DottedLine : INativeDebuggable
    {
        public DebugDrawingType Type;
        public Color Color;
        public Vector3 Start;
        public Vector3 End;

        /// <summary>
        /// The spacing between the dots in pixels.
        /// </summary>
        public float GapSize;

        public void Execute()
        {
#if UNITY_EDITOR
            Handles.color = Color;
            Handles.DrawDottedLine(Start, End, GapSize);
#endif
        }
    }

    public struct DottedWireCube : INativeDebuggable
    {
        public DebugDrawingType Type;
        public Color Color;
        public Vector3 Center;
        public Vector3 Size;

        /// <summary>
        /// The spacing between the dots in pixels.
        /// </summary>
        public float GapSize;

        public void Execute()
        {
#if UNITY_EDITOR
            Handles.color = Color;

            Vector3 lbb = Center + ((-Size) * 0.5f);
            Vector3 rbb = Center + (new Vector3(Size.x, -Size.y, -Size.z) * 0.5f);

            Vector3 lbf = Center + (new Vector3(Size.x, -Size.y, Size.z) * 0.5f);
            Vector3 rbf = Center + (new Vector3(-Size.x, -Size.y, Size.z) * 0.5f);

            Vector3 lub = Center + (new Vector3(-Size.x, Size.y, -Size.z) * 0.5f);
            Vector3 rub = Center + (new Vector3(Size.x, Size.y, -Size.z) * 0.5f);

            Vector3 luf = Center + ((Size) * 0.5f);
            Vector3 ruf = Center + (new Vector3(-Size.x, Size.y, Size.z) * 0.5f);

            Handles.DrawDottedLine(lbb, rbb, GapSize);
            Handles.DrawDottedLine(rbb, lbf, GapSize);
            Handles.DrawDottedLine(lbf, rbf, GapSize);
            Handles.DrawDottedLine(rbf, lbb, GapSize);

            Handles.DrawDottedLine(lub, rub, GapSize);
            Handles.DrawDottedLine(rub, luf, GapSize);
            Handles.DrawDottedLine(luf, ruf, GapSize);
            Handles.DrawDottedLine(ruf, lub, GapSize);

            Handles.DrawDottedLine(lbb, lub, GapSize);
            Handles.DrawDottedLine(rbb, rub, GapSize);
            Handles.DrawDottedLine(lbf, luf, GapSize);
            Handles.DrawDottedLine(rbf, ruf, GapSize);
#endif
        }
    }

    public struct WireCube : INativeDebuggable
    {
        public DebugDrawingType Type;
        public Color Color;
        public float3 Lbb;
        public float3 Rbb;
        public float3 Rbf;
        public float3 Lbf;
        public float3 Lub;
        public float3 Rub;
        public float3 Luf;
        public float3 Ruf;

        public WireCube(float3 center, float3 size, Color color)
        {
            Type = DebugDrawingType.WireCube;
            Color = color;
            Lbb = center + ((-size) * 0.5f);
            Rbb = center + (new float3(size.x, -size.y, -size.z) * 0.5f);
            Lbf = center + (new float3(size.x, -size.y, size.z) * 0.5f);
            Rbf = center + (new float3(-size.x, -size.y, size.z) * 0.5f);
            Lub = center + (new float3(-size.x, size.y, -size.z) * 0.5f);
            Rub = center + (new float3(size.x, size.y, -size.z) * 0.5f);
            Luf = center + ((size) * 0.5f);
            Ruf = center + (new float3(-size.x, size.y, size.z) * 0.5f);
        }

        public void Execute()
        {
#if UNITY_EDITOR
            Handles.color = Color;
            Handles.DrawLine(Lbb, Rbb);
            Handles.DrawLine(Rbb, Lbf);
            Handles.DrawLine(Lbf, Rbf);
            Handles.DrawLine(Rbf, Lbb);
            Handles.DrawLine(Lub, Rub);
            Handles.DrawLine(Rub, Luf);
            Handles.DrawLine(Luf, Ruf);
            Handles.DrawLine(Ruf, Lub);
            Handles.DrawLine(Lbb, Lub);
            Handles.DrawLine(Rbb, Rub);
            Handles.DrawLine(Lbf, Luf);
            Handles.DrawLine(Rbf, Ruf);
#endif
        }
    }

    public struct Label : INativeDebuggable
    {
        public DebugDrawingType Type;
        public Vector3 Position;
        public NativeString512 Text;
        public NativeLabelStyles Style;

        public void Execute()
        {
#if UNITY_EDITOR
            GUIDrawingUtility.CenteredLabel(Position, Text.ToString(), GUIDrawingUtility.GetStyle(Style));
#endif
        }
    }

    public struct Log : INativeDebuggable
    {
        public DebugDrawingType Type;
        public NativeString512 Message;
        public LogDisplayType DisplayType;
        public bool FromJob;
        public int ThreadId;

        public void Execute()
        {
#if UNITY_EDITOR
            switch (DisplayType)
            {
                case LogDisplayType.None: break;
                case LogDisplayType.Info:
                    Debug.Log(Message + (FromJob ? "[Job]" : "") + $" Thread={ThreadId}");
                    break;
                case LogDisplayType.Warning:
                    Debug.LogWarning(Message);
                    break;
                case LogDisplayType.Error:
                    Debug.LogError(Message);
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
#endif
        }

        public enum LogDisplayType
        {
            None = 0,
            Info,
            Warning,
            Error,
        }
    }

    public class GUIDrawingUtility
    {
        public static GUIStyle GetStyle(NativeLabelStyles style)
        {
            GUIStyle result = GUIStyle.none;
            switch (style)
            {
                case NativeLabelStyles.None: break;
                case NativeLabelStyles.Default:
                    result = GUIDrawingUtility.SceneBoldLabelWithBackground.Value;
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(style), style, null);
            }
            return result;
        }

        public static void CenteredLabel(Vector3 position, string text, GUIStyle style)
        {
#if UNITY_EDITOR
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
#endif
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

    public enum NativeLabelStyles
    {
        None = 0,
        Default,
    }

    public static class NativeDebugSharedData
    {
        private static readonly SharedStatic<Container> SharedData;

        private class Key { }

        static NativeDebugSharedData()
        {
            SharedData = SharedStatic<Container>.GetOrCreate<Key>();
        }

        public struct TimeData
        {
            public int CurrentFrame;
            public int LastQueuedFrame;
            public int Depth;
        }

        public struct StateData
        {
            public bool IsTransitioning;
        }

        private struct Container
        {
            public TimeData TimeData;
            public UnsafeStreamRotation StreamRotation;
            public StateData StateData;
        }

        public static ref TimeData Time => ref SharedData.Data.TimeData;

        public static ref UnsafeStreamRotation Stream => ref SharedData.Data.StreamRotation;

        public static ref StateData State => ref SharedData.Data.StateData;

    }

    public static unsafe class UnsafeStreamExtensions
    {
        public unsafe struct UnsafeStreamBlockData
        {
            public const int AllocationSize = 4 * 1024;
            public Allocator Allocator;

            public UnsafeStreamBlock** Blocks;
            public int BlockCount;

            public UnsafeStreamRange* Ranges;
            public int RangeCount;
        }

        public unsafe struct UnsafeStreamBlock
        {
            public UnsafeStreamBlock* Next;
            public fixed byte Data[1];
        }

        public unsafe struct UnsafeStreamRange
        {
            public UnsafeStreamBlock* Block;
            public int OffsetInFirstBlock;
            public int ElementCount;

            /// One byte past the end of the last byte written
            public int LastOffset;
            public int NumberOfBlocks;
        }

        public unsafe struct UnsafeStreamWriter
        {
            [NativeDisableUnsafePtrRestriction]
            public UnsafeStreamBlockData* m_BlockStream;

            [NativeDisableUnsafePtrRestriction]
            public UnsafeStreamBlock* m_CurrentBlock;

            [NativeDisableUnsafePtrRestriction]
            public byte* m_CurrentPtr;

            [NativeDisableUnsafePtrRestriction]
            public byte* m_CurrentBlockEnd;

            public int m_ForeachIndex;
            public int m_ElementCount;

            [NativeDisableUnsafePtrRestriction]
            public UnsafeStreamBlock* m_FirstBlock;

            public int m_FirstOffset;
            public int m_NumberOfBlocks;

            [NativeSetThreadIndex]
            public int m_ThreadIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetThreadIndex(this UnsafeStream.Writer writer, int threadIndex)
        {
            ((UnsafeStreamWriter*)&writer)->m_ThreadIndex = threadIndex;
        }

        public static void Clear(this UnsafeStream stream)
        {
            var blockData = *(UnsafeStreamBlockData**)&stream;
            if (blockData == null)
                return;

            var blockSize = sizeof(UnsafeStreamBlock*) * blockData->BlockCount;
            for (int index = 0; index != blockData->BlockCount; ++index)
            {
                UnsafeStreamBlock* next;
                for (UnsafeStreamBlock* blockPtr = blockData->Blocks[index]; (IntPtr)blockPtr != IntPtr.Zero; blockPtr = next)
                {
                    next = blockPtr->Next;
                    UnsafeUtility.MemClear(blockPtr, blockSize);
                }
            }

            for (int i = 0; i < blockData->RangeCount; i++)
            {
                UnsafeStreamRange range = blockData->Ranges[i];
            }

            long forEachAllocationSize = sizeof(UnsafeStreamRange) * stream.ForEachCount;
            UnsafeUtility.MemClear(blockData->Ranges, forEachAllocationSize);
        }

        public static UnsafeStreamBlockData* GetUnsafePtr(this UnsafeStream stream)
        {
            return *(UnsafeStreamBlockData**)&stream;
        }
    }

    public unsafe struct UnsafeSharedStream
    {
        public UnsafeStream Stream;
        public UnsafeStream.Writer* Writers;
        public UnsafeList<int> Indices;

        private int _size;
        private Allocator _allocator;

        private int ActiveWrites;

        public UnsafeSharedStream(int size, Allocator allocator)
        {
            _size = size;
            _allocator = allocator;

            Writers = (UnsafeStream.Writer*)UnsafeUtility.Malloc(size * sizeof(UnsafeStream.Writer), UnsafeUtility.AlignOf<UnsafeStream.Writer>(), allocator);
            Indices = new UnsafeList<int>(size, Allocator.Persistent);
            Stream = new UnsafeStream(size, allocator);
            ActiveWrites = default;

            Clear();
        }

        public void Clear()
        {
            UnsafeUtility.MemClear(Writers, _size * sizeof(UnsafeStream.Writer));
            Indices.Clear();
            Stream.Clear();
        }

        public void Write<T>(int threadIndex, T value) where T : struct
        {
            //Interlocked.Increment(ref ActiveWrites);

            if (!Indices.Contains(threadIndex))
            {
                var writer = Stream.AsWriter();
                writer.SetThreadIndex(threadIndex);
                writer.BeginForEachIndex(threadIndex);
                Writers[threadIndex] = writer;
                Indices.Add(threadIndex);
            }
            Writers[threadIndex].Write(value);

            //Interlocked.Decrement(ref ActiveWrites);
        }

        public void Close()
        {
            for (int i = 0; i < Indices.Length; i++)
            {
                var index = Indices.Ptr[i];
                Writers[index].EndForEachIndex();
            }
        }
    }

    public unsafe struct UnsafeStreamRotation
    {
        public UnsafeSharedStream Current;

        //public UnsafeStream.Reader Reader;

        //public UnsafeStream.Writer* CurrentWriters;
        //public UnsafeList<int> CurrentWriterIndices;

        //public UnsafeStream.Writer* ProcessingWriters;
        //public UnsafeList<int> ProcessingWriterIndices;

        public UnsafeSharedStream AvailableStream;
        public UnsafeSharedStream ProcessingStream;

        //public int Count;
        public bool IsCreated;
        //public int ProcessingStreamId;
        //private int _size;
        //private int _writersAllocationSize;

        //public UnsafeList<int> CurrentWriterIndices;

        public UnsafeStreamRotation(int size, Allocator allocator) : this()
        {
            Allocate(size, allocator);
        }

        //public void Write<T>(int threadIndex, T value) where T : struct
        //{
        //    //if (CurrentWriterIndices.Contains(threadIndex))
        //    //{
        //    //    CurrentWriters[threadIndex] = Current.AsWriter();
        //    //    CurrentWriters[threadIndex].BeginForEachIndex(threadIndex);
        //    //    //CurrentWriterIndices.Add(threadIndex);
        //    //}

        //    //Debug.Assert(CurrentWriters != null);

        //    //var offset = sizeof(UnsafeStream.Writer) * threadIndex;

        //    //Debug.Assert(threadIndex < _size && offset < _writersAllocationSize);

        //    //UnsafeStream.Writer* writerPtr = (CurrentWriters + offset);

        //    ////Debug.Assert(writerPtr < (byte*)CurrentWriters + _writersAllocationSize);

        //    //void* unsafeStreamBlockDataPtr = *(void**)writerPtr;

        //    //// Check if the writer contains a pointer at offset 0.
        //    //if (unsafeStreamBlockDataPtr == null)
        //    //{
        //    //    //UnsafeStream.Writer newWriter = Current.AsWriter();
        //    //    //UnsafeUtility.CopyStructureToPtr(ref newWriter, writerPtr);

        //    //    *writerPtr = Current.AsWriter();
        //    //    writerPtr->BeginForEachIndex(threadIndex);
        //    //}

        //    //ref var writer = ref CurrentWriters[threadIndex];

        //    //if (writer.ForEachCount != 0) // exists?
        //    //{
        //    //    writer = Current.AsWriter();
        //    //    writer.BeginForEachIndex(threadIndex);
        //    //}

        //    //GetOrCreateWriter(threadIndex)->Write(value);

        //    UnsafeStream.Writer writer;

        //    if (!CurrentWriterIndices.Contains(threadIndex))
        //    {
        //        //UnsafeStream.Writer newWriter = Current.AsWriter();
        //        //UnsafeUtility.CopyStructureToPtr(ref newWriter, writerPtr);

        //        //*writerPtr = Current.AsWriter();
        //        //writerPtr->BeginForEachIndex(threadIndex);
        //        writer = Current.AsWriter();
        //        writer.BeginForEachIndex(threadIndex);
        //        CurrentWriters[threadIndex] = writer;
        //        CurrentWriterIndices.Add(threadIndex);

        //        //Debug.Log($"Writer for thread {threadIndex} created");
        //    }
        //    else
        //    {
                
        //    }
            

        //    CurrentWriters[threadIndex].Write(value);
        //}

        //public bool WriterExists(int threadIndex)
        //{
        //    Debug.Assert(CurrentWriters != null);

        //    var offset = sizeof(UnsafeStream.Writer) * threadIndex;

        //    Debug.Assert(threadIndex < _size && offset < _writersAllocationSize);

        //    UnsafeStream.Writer* writerPtr = (CurrentWriters + offset);

        //    void* unsafeStreamBlockDataPtr = *(void**)writerPtr;

        //    return unsafeStreamBlockDataPtr != null;
        //}

        //public UnsafeStream.Writer* GetWriter(int threadIndex)
        //{
        //    Debug.Assert(CurrentWriters != null);

        //    var offset = sizeof(UnsafeStream.Writer) * threadIndex;

        //    Debug.Assert(threadIndex < _size && offset < _writersAllocationSize);

        //    UnsafeStream.Writer* writerPtr = (CurrentWriters + offset);

        //    void* unsafeStreamBlockDataPtr = *(void**)writerPtr;

        //    Debug.Assert(unsafeStreamBlockDataPtr != null);

        //    return writerPtr;
        //}

        //public UnsafeStream.Writer* GetOrCreateWriter(int threadIndex)
        //{
        //    //Debug.Assert(CurrentWriters != null);

        //    //var offset = sizeof(UnsafeStream.Writer) * threadIndex;

        //    //Debug.Assert(threadIndex < _size && offset < _writersAllocationSize);

        //    //UnsafeStream.Writer* writerPtr = (CurrentWriters + offset);

        //    //Debug.Assert(writerPtr < (byte*)CurrentWriters + _writersAllocationSize);

        //    //void* unsafeStreamBlockDataPtr = *(void**)writerPtr;

        //    // Check if the writer contains a pointer at offset 0.
        //    //if (unsafeStreamBlockDataPtr == null)
        //    if(!WriterIndices.Contains(threadIndex))
        //    {
        //        //UnsafeStream.Writer newWriter = Current.AsWriter();
        //        //UnsafeUtility.CopyStructureToPtr(ref newWriter, writerPtr);

        //        //*writerPtr = Current.AsWriter();
        //        //writerPtr->BeginForEachIndex(threadIndex);
        //        var newWriter = Current.AsWriter();
        //        newWriter.BeginForEachIndex(threadIndex);
        //        CurrentWriters[threadIndex] = newWriter;
        //        WriterIndices.Add(threadIndex);

        //        Debug.Log($"Writer for thread {threadIndex} created");
        //    }
        //    //return writerPtr;
        //}


        public void Allocate(int size, Allocator allocator)
        {
            if (IsCreated)
                return;

            //_size = size;

            Current = new UnsafeSharedStream(size, allocator);
            AvailableStream = new UnsafeSharedStream(size, allocator);
            ProcessingStream = new UnsafeSharedStream(size, allocator);

            //Reader = Current.AsReader();

            //_writersAllocationSize = sizeof(UnsafeStream.Writer) * size;

            //CurrentWriters = (UnsafeStream.Writer*)UnsafeUtility.Malloc(_writersAllocationSize, UnsafeUtility.AlignOf<UnsafeStream.Writer>(), Allocator.Persistent);
            //UnsafeUtility.MemClear(CurrentWriters, _writersAllocationSize);

            //ProcessingWriters = (UnsafeStream.Writer*)UnsafeUtility.Malloc(_writersAllocationSize, UnsafeUtility.AlignOf<UnsafeStream.Writer>(), Allocator.Persistent);
            //UnsafeUtility.MemClear(ProcessingWriters, _writersAllocationSize);

            //CurrentWriterIndices = new UnsafeList<int>(512, Allocator.Persistent);
            //ProcessingWriterIndices = new UnsafeList<int>(512, Allocator.Persistent);

            IsCreated = true;
        }

        //public void Deallocate()
        //{
        //    throw new NotImplementedException();
        //}

        public void Rotate()
        {
            if (!IsCreated)
                return;

            var available = AvailableStream;
            var processing = ProcessingStream;
            var current = Current;

            AvailableStream = processing;
            
            //var currentWriters = CurrentWriters;
            //var processingWriters = ProcessingWriters;
            //var currentWriterIndices = CurrentWriterIndices;
            //var processingWriterIndices = ProcessingWriterIndices;
            
            // Switch out writers so that a batch can be processed while new writes still being collected
            //UnsafeUtility.MemClear(processingWriters, _writersAllocationSize);
            //processingWriterIndices.Clear();
            available.Clear();

            //CurrentWriterIndices = processingWriterIndices;
            //CurrentWriters = processingWriters;
            Current = available;

            //ProcessingWriterIndices = currentWriterIndices;
            //ProcessingWriters = currentWriters;
            ProcessingStream = current;

            ProcessingStream.Close();

            //// Finalize the writers for the batch to be processed.
            //for (int i = 0; i < ProcessingWriterIndices.Length; i++)
            //{
            //    var index = ProcessingWriterIndices.Ptr[i];
            //    ProcessingWriters[index].EndForEachIndex();
            //}

            //Debug.Assert(Current.GetUnsafePtr() != ProcessingStream.GetUnsafePtr() 
            //    && Current.GetUnsafePtr() != AvailableStream.GetUnsafePtr() 
            //    && AvailableStream.GetUnsafePtr() != ProcessingStream.GetUnsafePtr());
        }

        //public void Clear()
        //{
        //    if (!IsCreated)
        //        return;

        //    NativeDebugSharedData.Stream.Current.Clear();
        //    NativeDebugSharedData.Stream.Count = 0;
        //}

        //public void Dispose()
        //{
        //    //AvailableStream.Dispose();
        //    //ProcessingStream.Dispose();
        //    //Current.Dispose();
        //    IsCreated = false;
        //}
    }

    public static class UnityColors
    {
        // Unity Default Colors
        public static Color Black { get; } = Color.black;
        public static Color Blue { get; } = Color.blue;
        public static Color Clear { get; } = Color.clear;
        public static Color Cyan { get; } = Color.cyan;
        public static Color Gray { get; } = Color.gray;
        public static Color Green { get; } = Color.green;
        public static Color Grey { get; } = Color.grey;
        public static Color Magenta { get; } = Color.magenta;
        public static Color Red { get; } = Color.red;
        public static Color White { get; } = Color.white;
        public static Color Yellow { get; } = Color.yellow;

        // Custom Colors (Note Unity uses a 0-1 scale instead of 0-255)
        public static Color GhostDodgerBlue { get; } = new Color(30 / 255f, 144 / 255f, 255 / 255f, 0.65f);
        public static Color DarkDodgerBlue { get; } = new Color(19 / 255f, 90 / 255f, 159 / 255f, 1f);

        // Standard Colors
        public static Color Transparent { get; } = FromArgb(16777215);
        public static Color AliceBlue { get; } = FromArgb(-984833);
        public static Color AntiqueWhite { get; } = FromArgb(-332841);
        public static Color Aqua { get; } = FromArgb(-16711681);
        public static Color Aquamarine { get; } = FromArgb(-8388652);
        public static Color Azure { get; } = FromArgb(-983041);
        public static Color Beige { get; } = FromArgb(-657956);
        public static Color Bisque { get; } = FromArgb(-6972);
        //public static Color Black { get; } = FromArgb(-16777216);
        public static Color BlanchedAlmond { get; } = FromArgb(-5171);
        //public static Color Blue { get; } = FromArgb(-16776961);
        public static Color BlueViolet { get; } = FromArgb(-7722014);
        //public static Color Brown { get; } = FromArgb(-5952982, )
        public static Color BurlyWood { get; } = FromArgb(-2180985);
        public static Color CadetBlue { get; } = FromArgb(-10510688);
        public static Color Chartreuse { get; } = FromArgb(-8388864);
        public static Color Chocolate { get; } = FromArgb(-2987746);
        public static Color Coral { get; } = FromArgb(-32944);
        public static Color CornflowerBlue { get; } = FromArgb(-10185235);
        public static Color Cornsilk { get; } = FromArgb(-1828);
        public static Color Crimson { get; } = FromArgb(-2354116);
        //public static Color Cyan { get; } = FromArgb(-16711681);
        public static Color DarkBlue { get; } = FromArgb(-16777077);
        public static Color DarkCyan { get; } = FromArgb(-16741493);
        public static Color DarkGoldenrod { get; } = FromArgb(-4684277);
        public static Color DarkGray { get; } = FromArgb(-5658199);
        public static Color DarkGreen { get; } = FromArgb(-16751616);
        public static Color DarkKhaki { get; } = FromArgb(-4343957);
        public static Color DarkMagenta { get; } = FromArgb(-7667573);
        public static Color DarkOliveGreen { get; } = FromArgb(-11179217);
        public static Color DarkOrange { get; } = FromArgb(-29696);
        public static Color DarkOrchid { get; } = FromArgb(-6737204);
        public static Color DarkRed { get; } = FromArgb(-7667712);
        public static Color DarkSalmon { get; } = FromArgb(-1468806);
        public static Color DarkSeaGreen { get; } = FromArgb(-7357301);
        public static Color DarkSlateBlue { get; } = FromArgb(-12042869);
        public static Color DarkSlateGray { get; } = FromArgb(-13676721);
        public static Color DarkTurquoise { get; } = FromArgb(-16724271);
        public static Color DarkViolet { get; } = FromArgb(-7077677);
        public static Color DeepPink { get; } = FromArgb(-60269);
        public static Color DeepSkyBlue { get; } = FromArgb(-16728065);
        public static Color DimGray { get; } = FromArgb(-9868951);
        public static Color DodgerBlue { get; } = FromArgb(-14774017);
        public static Color Firebrick { get; } = FromArgb(-5103070);
        public static Color FloralWhite { get; } = FromArgb(-1296);
        public static Color ForestGreen { get; } = FromArgb(-14513374);
        public static Color Fuchsia { get; } = FromArgb(-65281);
        public static Color Gainsboro { get; } = FromArgb(-2302756);
        public static Color GhostWhite { get; } = FromArgb(-460545);
        public static Color Gold { get; } = FromArgb(-10496);
        public static Color Goldenrod { get; } = FromArgb(-2448096);
        //public static Color Gray { get; } = FromArgb(-8355712);
        //public static Color Green { get; } = FromArgb(-16744448);
        public static Color GreenYellow { get; } = FromArgb(-5374161);
        public static Color Honeydew { get; } = FromArgb(-983056);
        public static Color HotPink { get; } = FromArgb(-38476);
        public static Color IndianRed { get; } = FromArgb(-3318692);
        public static Color Indigo { get; } = FromArgb(-11861886);
        public static Color Ivory { get; } = FromArgb(-16);
        public static Color Khaki { get; } = FromArgb(-989556);
        public static Color Lavender { get; } = FromArgb(-1644806);
        public static Color LavenderBlush { get; } = FromArgb(-3851);
        public static Color LawnGreen { get; } = FromArgb(-8586240);
        public static Color LemonChiffon { get; } = FromArgb(-1331);
        public static Color LightBlue { get; } = FromArgb(-5383962);
        public static Color LightCoral { get; } = FromArgb(-1015680);
        public static Color LightCyan { get; } = FromArgb(-2031617);
        public static Color LightGoldenrodYellow { get; } = FromArgb(-329006);
        public static Color LightGray { get; } = FromArgb(-2894893);
        public static Color LightGreen { get; } = FromArgb(-7278960);
        public static Color LightPink { get; } = FromArgb(-18751);
        public static Color LightSalmon { get; } = FromArgb(-24454);
        public static Color LightSeaGreen { get; } = FromArgb(-14634326);
        public static Color LightSkyBlue { get; } = FromArgb(-7876870);
        public static Color LightSlateGray { get; } = FromArgb(-8943463);
        public static Color LightSteelBlue { get; } = FromArgb(-5192482);
        public static Color LightYellow { get; } = FromArgb(-32);
        public static Color Lime { get; } = FromArgb(-16711936);
        public static Color LimeGreen { get; } = FromArgb(-13447886);
        public static Color Linen { get; } = FromArgb(-331546);
        //public static Color Magenta { get; } = FromArgb(-65281);
        public static Color Maroon { get; } = FromArgb(-8388608);
        public static Color MediumAquamarine { get; } = FromArgb(-10039894);
        public static Color MediumBlue { get; } = FromArgb(-16777011);
        public static Color MediumOrchid { get; } = FromArgb(-4565549);
        public static Color MediumPurple { get; } = FromArgb(-7114533);
        public static Color MediumSeaGreen { get; } = FromArgb(-12799119);
        public static Color MediumSlateBlue { get; } = FromArgb(-8689426);
        public static Color MediumSpringGreen { get; } = FromArgb(-16713062);
        public static Color MediumTurquoise { get; } = FromArgb(-12004916);
        public static Color MediumVioletRed { get; } = FromArgb(-3730043);
        public static Color MidnightBlue { get; } = FromArgb(-15132304);
        public static Color MintCream { get; } = FromArgb(-655366);
        public static Color MistyRose { get; } = FromArgb(-6943);
        public static Color Moccasin { get; } = FromArgb(-6987);
        public static Color NavajoWhite { get; } = FromArgb(-8531);
        public static Color Navy { get; } = FromArgb(-16777088);
        public static Color OldLace { get; } = FromArgb(-133658);
        public static Color Olive { get; } = FromArgb(-8355840);
        public static Color OliveDrab { get; } = FromArgb(-9728477);
        public static Color Orange { get; } = FromArgb(-23296);
        public static Color OrangeRed { get; } = FromArgb(-47872);
        public static Color Orchid { get; } = FromArgb(-2461482);
        public static Color PaleGoldenrod { get; } = FromArgb(-1120086);
        public static Color PaleGreen { get; } = FromArgb(-6751336);
        public static Color PaleTurquoise { get; } = FromArgb(-5247250);
        public static Color PaleVioletRed { get; } = FromArgb(-2396013);
        public static Color PapayaWhip { get; } = FromArgb(-4139);
        public static Color PeachPuff { get; } = FromArgb(-9543);
        public static Color Peru { get; } = FromArgb(-3308225);
        public static Color Pink { get; } = FromArgb(-16181);
        public static Color Plum { get; } = FromArgb(-2252579);
        public static Color PowderBlue { get; } = FromArgb(-5185306);
        public static Color Purple { get; } = FromArgb(-8388480);
        //public static Color Red { get; } = FromArgb(-65536);
        public static Color RosyBrown { get; } = FromArgb(-4419697);
        public static Color RoyalBlue { get; } = FromArgb(-12490271);
        public static Color SaddleBrown { get; } = FromArgb(-7650029);
        public static Color Salmon { get; } = FromArgb(-360334);
        public static Color SandyBrown { get; } = FromArgb(-744352);
        public static Color SeaGreen { get; } = FromArgb(-13726889);
        public static Color SeaShell { get; } = FromArgb(-2578);
        public static Color Sienna { get; } = FromArgb(-6270419);
        public static Color Silver { get; } = FromArgb(-4144960);
        public static Color SkyBlue { get; } = FromArgb(-7876885);
        public static Color SlateBlue { get; } = FromArgb(-9807155);
        public static Color SlateGray { get; } = FromArgb(-9404272);
        public static Color Snow { get; } = FromArgb(-1286);
        public static Color SpringGreen { get; } = FromArgb(-16711809);
        public static Color SteelBlue { get; } = FromArgb(-12156236);
        public static Color Tan { get; } = FromArgb(-2968436);
        public static Color Teal { get; } = FromArgb(-16744320);
        public static Color Thistle { get; } = FromArgb(-2572328);
        public static Color Tomato { get; } = FromArgb(-40121);
        public static Color Turquoise { get; } = FromArgb(-12525360);
        public static Color Violet { get; } = FromArgb(-1146130);
        public static Color Wheat { get; } = FromArgb(-663885);
        //public static Color White { get; } = FromArgb(-1);
        public static Color WhiteSmoke { get; } = FromArgb(-657931);
        //public static Color Yellow { get; } = FromArgb(-256);
        public static Color YellowGreen { get; } = FromArgb(-6632142);



        private static long ToArgb(this Color color)
        {
            return (long)(uint)((byte)color.r * 255 << 16 | (byte)color.g * 255 << 8 | (byte)color.b * 255 | (byte)color.a * 255 << 24) & uint.MaxValue;
        }

        public static Color FromArgb(long argb)
        {
            var r = (byte)((ulong)(argb >> 16) & (ulong)byte.MaxValue);
            var g = (byte)((ulong)(argb >> 8) & (ulong)byte.MaxValue);
            var b = (byte)((ulong)argb & (ulong)byte.MaxValue);
            var a = (byte)((ulong)(argb >> 24) & (ulong)byte.MaxValue);
            return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
        }

        //public static Color FromHex(string colorcode)
        //{
        //    return FromArgb(int.Parse(colorcode.Replace("#", ""), NumberStyles.HexNumber));
        //}

        private static string ToHex(this Color color)
        {
            return "#" + color.a.ToString("X2") + color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
        }

        public static float GetBrightness(this Color color)
        {
            float num1 = (float)color.r * 255 / (float)byte.MaxValue;
            float num2 = (float)color.g * 255 / (float)byte.MaxValue;
            float num3 = (float)color.b * 255 / (float)byte.MaxValue;
            float num4 = num1;
            float num5 = num1;
            if ((double)num2 > (double)num4)
                num4 = num2;
            if ((double)num3 > (double)num4)
                num4 = num3;
            if ((double)num2 < (double)num5)
                num5 = num2;
            if ((double)num3 < (double)num5)
                num5 = num3;
            return (float)(((double)num4 + (double)num5) / 2.0);
        }

        public static float GetHue(this Color color)
        {
            if ((int)color.r == (int)color.g && (int)color.g == (int)color.b)
                return 0.0f;
            float num1 = (float)color.r * 255 / (float)byte.MaxValue;
            float num2 = (float)color.g * 255 / (float)byte.MaxValue;
            float num3 = (float)color.b * 255 / (float)byte.MaxValue;
            float num4 = 0.0f;
            float num5 = num1;
            float num6 = num1;
            if ((double)num2 > (double)num5)
                num5 = num2;
            if ((double)num3 > (double)num5)
                num5 = num3;
            if ((double)num2 < (double)num6)
                num6 = num2;
            if ((double)num3 < (double)num6)
                num6 = num3;
            float num7 = num5 - num6;
            if ((double)num1 == (double)num5)
                num4 = (num2 - num3) / num7;
            else if ((double)num2 == (double)num5)
                num4 = (float)(2.0 + ((double)num3 - (double)num1) / (double)num7);
            else if ((double)num3 == (double)num5)
                num4 = (float)(4.0 + ((double)num1 - (double)num2) / (double)num7);
            float num8 = num4 * 60f;
            if ((double)num8 < 0.0)
                num8 += 360f;
            return num8;
        }

        public static float GetSaturation(this Color color)
        {
            float num1 = (float)color.r * 255 / (float)byte.MaxValue;
            float num2 = (float)color.g * 255 / (float)byte.MaxValue;
            float num3 = (float)color.b * 255 / (float)byte.MaxValue;
            float num4 = 0.0f;
            float num5 = num1;
            float num6 = num1;
            if ((double)num2 > (double)num5)
                num5 = num2;
            if ((double)num3 > (double)num5)
                num5 = num3;
            if ((double)num2 < (double)num6)
                num6 = num2;
            if ((double)num3 < (double)num6)
                num6 = num3;
            if ((double)num5 != (double)num6)
                num4 = ((double)num5 + (double)num6) / 2.0 > 0.5 ? (float)(((double)num5 - (double)num6) / (2.0 - (double)num5 - (double)num6)) : (float)(((double)num5 - (double)num6) / ((double)num5 + (double)num6));
            return num4;
        }
    }
}

