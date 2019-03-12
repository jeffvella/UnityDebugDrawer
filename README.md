# UnityDebugDrawer
Draw debug handles and other GUI elements in the scene view from anywhere in your code base.

A static class wrapping the "SceneView.duringSceneGui" event to allow calling of GUI only debug features from outside of OnGUI contexts. You can just use it hassle-free like you would with Debug.DrawLine() etc.

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
