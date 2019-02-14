# UnityDebugDrawer
Draw debug handles and other GUI elements in the scene view from anywhere in your code base.

A static class wrapping the "SceneView.duringSceneGui" event to allow calling of GUI only Unity features from outside of OnGUI contexts, so just like you would with Debug.DrawLine() etc.

##### Currently Supports: #####

  Custom:
  * DrawLabel(Vector3 position, string text, GUIStyle style = null)

  Handles:
  * DrawSolidRectangleWithOutline(Rect rectangle, Color faceColor, Color outlineColor)
  * DrawSolidRectangleWithOutline(Vector3[] verts, Color faceColor, Color outlineColor)
  * DrawAAConvexPolygon(Vector3[] verts, Color color)

... and is easily extensible to other methods you might need.
