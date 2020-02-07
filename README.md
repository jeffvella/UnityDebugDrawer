# UnityDebugDrawer 

Draw debug handles, text and other GUI elements in the scene view from anywhere in your code base. 

##### Now with Burst/Jobs support! #####

Draw from within Burst jobs thanks to SharedStatic<T> and UnsafeStream.

How it works: Its a static class wrapping the "SceneView.duringSceneGui" event to allow calling of GUI only debug features from outside of OnGUI contexts. You can just use it hassle-free like you would with Debug.DrawLine() etc.

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

 * Unity 2019.3.0F6+
 * Unsafe Compilation
 * Unity.Collections
 * Unity.Mathematics
 * Entities 0.4.0
 * Burst

##### Old Versions #####

The original project was moved to 'non-jobs-version-backup' branch and can't be used with jobs. It does however have fewer dependencies so might be worth looking at if you're working in older versions of Unity and MonoBehaviors.
