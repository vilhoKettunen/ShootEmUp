using System;
using UnityEngine; // Vector2 ONLY — a plain math struct (see header note)

// PURE C# (rule 2.4): no MonoBehaviour, no GameObject, no Transform, no Time,
// no Find*, no engine services. The only UnityEngine type touched is Vector2 —
// a plain math struct, acceptable for this course (the reference SO uses it too).
// Time is PASSED IN as a float → every function is a pure function of its
// arguments: same input, same output, testable in any C# project.

/// <summary>
/// Pure, stateless movement pattern math shared by enemies AND bullets.
/// One implementation, zero duplication (rule 2.7): each object passes its
/// OWN SOMovementParameters and an elapsed time; the class never knows which
/// object asked.
///
/// Convention: Straight/Sine return a DELTA (offset to add to the origin);
/// MoveToTarget returns an ABSOLUTE position (it already includes the origin).
/// GetPosition normalizes both conventions and ALWAYS returns an absolute
/// position — objects call GetPosition and assign transform.position.
///
/// The template's Kryzarel easing set (Assets/Scripts/Helper/EassingFunctions.cs,
/// namespace Kryz.Tweening) is the approved utility for BUILDING future
/// patterns (eased approaches, curved shots, boss moves) — see the Eased
/// pattern below, which is the first consumer of that easing set.
/// </summary>
public static class MovementPatterns
{
    /// <summary>Constant velocity along p.Direction. delta(t) = Direction * Speed * t.</summary>
    public static Vector2 Straight(SOMovementParameters p, float t)
    {
        return p.Direction * (p.Speed * t);
    }

    /// <summary>
    /// Straight motion + a perpendicular sine offset (the "wiggle" used by the
    /// test enemies and several weapon bullets).
    /// offset(t) = Amplitude * sin(2π * Frequency * t), perpendicular to Direction.
    /// Amplitude = 0 degenerates to Straight (backward compatible).
    /// </summary>
    public static Vector2 Sine(SOMovementParameters p, float t)
    {
        Vector2 straight = p.Direction * (p.Speed * t);
        Vector2 perpendicular = new Vector2(-p.Direction.y, p.Direction.x);
        float wave = p.Amplitude * MathF.Sin(2f * MathF.PI * p.Frequency * t);
        return straight + perpendicular * wave;
    }

    /// <summary>
    /// Travel from origin toward (origin + p.TargetOffset), then HOLD at the
    /// destination. Linear over (|TargetOffset| / Speed) seconds, clamped so it
    /// never overshoots. t = 0 → origin; t ≥ travel time → exact destination.
    /// </summary>
    public static Vector2 MoveToTarget(SOMovementParameters p, float t, Vector2 origin)
    {
        Vector2 target = origin + p.TargetOffset;
        float distance = p.TargetOffset.magnitude;
        if (distance <= 0f) return origin;

        float travelTime = distance / p.Speed;
        float k = Math.Clamp(t / travelTime, 0f, 1f);
        return Vector2.Lerp(origin, target, k);
    }

    /// <summary>
    /// Eased travel (the easing-utility consumer): same as MoveToTarget but the
    /// progress k is passed through Kryzarel's easing curve (Kryz.Tweening.
    /// EasingFunctions, t ∈ [0,1] → eased [0,1]). Start fast / end slow with
    /// Out* curves, slow / fast with In*, etc. t = 0 → origin; t ≥ travel time
    /// → exact destination (easing maps 1 → 1, so no overshoot).
    /// </summary>
    public static Vector2 Eased(SOMovementParameters p, float t, Vector2 origin)
    {
        Vector2 target = origin + p.TargetOffset;
        float distance = p.TargetOffset.magnitude;
        if (distance <= 0f) return origin;

        float travelTime = distance / p.Speed;
        float k = Math.Clamp(t / travelTime, 0f, 1f);
        float eased = Kryz.Tweening.EasingFunctions.OutCubic(k);
        return Vector2.Lerp(origin, target, eased);
    }

    /// <summary>
    /// Convenience dispatcher — objects call ONE line:
    ///   transform.position = MovementPatterns.GetPosition(so, elapsed, origin);
    /// ALWAYS returns an ABSOLUTE position for every pattern.
    /// </summary>
    public static Vector2 GetPosition(SOMovementParameters p, float t, Vector2 origin)
    {
        switch (p.Pattern)
        {
            case SOMovementParameters.MovePattern.Straight:
                return origin + Straight(p, t);
            case SOMovementParameters.MovePattern.Sine:
                return origin + Sine(p, t);
            case SOMovementParameters.MovePattern.Target:
                return MoveToTarget(p, t, origin); // already absolute
            case SOMovementParameters.MovePattern.Eased:
                return Eased(p, t, origin);        // already absolute
            default:
                return origin;
        }
    }
}
