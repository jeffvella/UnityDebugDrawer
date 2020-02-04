# UnityDebugDrawer 

Draw debug handles, text and other GUI elements in the scene view from anywhere in your code base. 

##### Now with Burst/Jobs support! Draw from within Burst jobs thanks to SharedStatic<T> and NativeStream #####

A static class wrapping the "SceneView.duringSceneGui" event to allow calling of GUI only debug features from outside of OnGUI contexts. You can just use it hassle-free like you would with Debug.DrawLine() etc.

<img src="https://i.imgur.com/SVQRQad.jpg" target="_blank" />

Note: this draws ONLY within the Scene view.

##### Supported Drawing Styles: #####

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

# Dependencies

 * Unity 2019.3.0b5+
 * Unsafe Compilation
 * Collections
 * Mathematics
 * Entities 0.1.1+ (for NativeStream)
 * Burst

##### Old Versions #####

The original project was moved to 'non-jobs-version-backup' branch and can't be used with jobs. It does however have fewer dependencies so might be worth looking at if you're working in older versions of Unity and MonoBehaviors.
