using System;
using System.Collections;
using UnityEngine;

// The player's car, parked along the curb in the barrier gap. Knows whether the player is in reach
// (through its ExitCarZone trigger child) and plays the drive-off / arrival moves of the level cutscene.
// The car lies along +X: it drives off east and arrives from the west, along the road below the gap.
public class ExitCar : MonoBehaviour
{
    [Tooltip("How far down (cells) the car pulls out of the gap into the driving lane.")]
    [SerializeField] private float laneDrop = 1f;
    [Tooltip("How far (cells) the car drives along the road, off or in.")]
    [SerializeField] private float driveDistance = 14f;
    [Tooltip("Fraction of a move spent pulling out of / into the gap.")]
    [Range(0.05f, 0.5f)] [SerializeField] private float pullFraction = 0.2f;

    private Vector3 parked;
    private Collider2D[] solids;

    public bool PlayerInRange { get; private set; }
    public event Action RangeChanged;

    void Awake()
    {
        parked = transform.position;
        // Every non-trigger collider is the car body; the reach zone is a trigger and stays on.
        var all = GetComponentsInChildren<Collider2D>(true);
        solids = Array.FindAll(all, c => !c.isTrigger);
    }

    internal void SetPlayerInRange(bool inRange)
    {
        if (PlayerInRange == inRange) return;
        PlayerInRange = inRange;
        RangeChanged?.Invoke();
    }

    public IEnumerator DriveOff(float duration)
    {
        SetSolid(false);
        Vector3 lane = parked + Vector3.down * laneDrop;
        yield return Move(parked, lane, duration * pullFraction, EaseInOut);
        yield return Move(lane, lane + Vector3.right * driveDistance, duration * (1f - pullFraction), EaseIn);
    }

    // Puts the car at the start of its arrival, out of sight down the road.
    public void PrepareArrival()
    {
        SetSolid(false);
        transform.position = parked + Vector3.down * laneDrop + Vector3.left * driveDistance;
    }

    public IEnumerator Arrive(float duration)
    {
        Vector3 lane = parked + Vector3.down * laneDrop;
        PrepareArrival();
        yield return Move(transform.position, lane, duration * (1f - pullFraction), EaseOut);
        yield return Move(lane, parked, duration * pullFraction, EaseInOut);
        SetSolid(true);
    }

    private void SetSolid(bool solid)
    {
        foreach (var c in solids) c.enabled = solid;
    }

    private IEnumerator Move(Vector3 from, Vector3 to, float duration, Func<float, float> ease)
    {
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            transform.position = Vector3.LerpUnclamped(from, to, ease(t / duration));
            yield return null;
        }
        transform.position = to;
    }

    private static float EaseIn(float t) => t * t;
    private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
    private static float EaseInOut(float t) => t * t * (3f - 2f * t);
}
