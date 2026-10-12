using UnityEngine;

/// <summary>
/// Pure C# static helpers that bridge the world/camera boundary — "is this
/// sprite in the camera view?" and "where should I spawn N objects across the
/// top of the camera?".
///
/// Plain C# static class (no MonoBehaviour, no per-object state).
/// Uses Camera.main — the standard scene camera.
/// </summary>
public static class CameraHelper
{
    /// <summary>
    /// True if the sprite's bounds intersect the camera's view frustum.
    /// Used by bullets to know when they have left the play area and should
    /// be returned to the pool.
    ///
    /// Strategy: check the CENTER first (reliable for small objects like
    /// bullets), then fall back to the 4 corners (catches large objects that
    /// straddle the screen edge).
    /// </summary>
    public static bool SpriteBoundsInCameraView(SpriteRenderer sr, Camera cam)
    {
        if (sr == null || cam == null) return false;

        // Check the CENTER first: for small objects (bullets) the 4 corners of
        // the bounds are often just outside the viewport even while the object
        // is clearly on screen, so a corner-only test recycles them too early.
        // The center is the reliable "is it on screen" signal.
        Bounds b = sr.bounds;
        Vector3 center = b.center;
        Vector3 centerViewport = cam.WorldToViewportPoint(center);
        if (centerViewport.x >= 0f && centerViewport.x <= 1f &&
            centerViewport.y >= 0f && centerViewport.y <= 1f &&
            centerViewport.z > 0f)
        {
            return true;
        }

        // Fall back to the corners so a large object that straddles the edge is
        // still considered "in view" while part of it is on screen.
        Vector3[] corners = new Vector3[]
        {
            new Vector3(b.min.x, b.min.y, b.center.z),
            new Vector3(b.max.x, b.min.y, b.center.z),
            new Vector3(b.min.x, b.max.y, b.center.z),
            new Vector3(b.max.x, b.max.y, b.center.z),
        };

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 viewport = cam.WorldToViewportPoint(corners[i]);
            if (viewport.x >= 0f && viewport.x <= 1f &&
                viewport.y >= 0f && viewport.y <= 1f &&
                viewport.z > 0f)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Spreads `count` spawn positions horizontally across the top of the
    /// camera view. Returns the position for the given `index` (0-based).
    /// Used by enemy spawn.
    ///
    /// The sprite's bounds height is used to push the spawn slightly ABOVE
    /// the top edge so the object enters the view naturally.
    /// </summary>
    public static Vector3 GetSpawnPositionAbove(SpriteRenderer sr, Camera cam, int index, int count)
    {
        if (cam == null) return new Vector3(0f, 5f, 0f);
        if (count <= 0) count = 1;
        if (index < 0) index = 0;
        if (index >= count) index = count - 1;

        // Fractional position across the width: 0.1 .. 0.9 (avoids the very edges).
        float t = (index + 0.5f) / count;
        float viewportX = 0.1f + t * 0.8f;

        // The camera's TOP edge in world space, at the camera's center depth
        // (z=1 in viewport). We push the spawn slightly ABOVE that edge so
        // the object enters the view naturally (and, if a sprite is provided,
        // we account for its bounds height so the whole object starts off-screen).
        Vector3 camTopWorld = cam.ViewportToWorldPoint(new Vector3(viewportX, 1f, 1f));
        float pushAbove = 0.5f;
        if (sr != null)
        {
            pushAbove = Mathf.Max(0.5f, sr.bounds.size.y * 0.75f);
        }
        camTopWorld += Vector3.up * pushAbove;
        return new Vector3(camTopWorld.x, camTopWorld.y, 0f);
    }
}
