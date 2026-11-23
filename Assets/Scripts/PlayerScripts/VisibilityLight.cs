using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// A light that reveals zombies. Its area comes from the Light2D on the same object:
// the outer radius, and for a cone the outer angle around transform.up (the way a 2D spot light points).
[RequireComponent(typeof(Light2D))]
public class VisibilityLight : MonoBehaviour
{
    private static readonly List<VisibilityLight> lights = new List<VisibilityLight>();
    public static IReadOnlyList<VisibilityLight> All => lights;

    private Light2D light2D;

    public Vector2 Position => transform.position;

    void Awake() => light2D = GetComponent<Light2D>();
    void OnEnable() => lights.Add(this);
    void OnDisable() => lights.Remove(this);

    public bool Covers(Vector2 point)
    {
        if (!light2D.enabled) return false;

        Vector2 toPoint = point - Position;
        if (toPoint.magnitude > light2D.pointLightOuterRadius) return false;

        float halfAngle = light2D.pointLightOuterAngle * 0.5f;
        return halfAngle >= 180f || Vector2.Angle(transform.up, toPoint) <= halfAngle;
    }
}
