# UnityDebugDrawer

Draw debug handles and other GUI elements in the scene view from anywhere in your code base.

A static class wrapping the "SceneView.duringSceneGui" event to allow calling of GUI only debug features from outside of OnGUI contexts. You can just use it hassle-free like you would with Debug.DrawLine() etc.

# Experimental Version

The project in this branch is being re-written for burst/jobs and now has the following dependencies:

Unity 2019.3.0b5+
Unsafe
Collections
Mathematics
Entities 0.1.1+ (for NativeStream)
Burst

<img src="https://i.imgur.com/SVQRQad.jpg" target="_blank" />

##### Currently Supports: #####

  Custom:
  * DrawLabel

  Handles:
  * DrawSolidRectangleWithOutline
  * DrawAAConvexPolygon
  * DrawSphere
  * DrawDottedLine
  * DrawDottedWireCube
  
  Debug:
  * DrawArrow
  * DrawLine
  * DrawPoint
  * DrawCircle
  * DrawCone
  * DrawWireCube

... and is easily extensible to other methods you might need.
