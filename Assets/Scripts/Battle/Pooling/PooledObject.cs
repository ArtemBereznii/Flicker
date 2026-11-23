using UnityEngine;
using UnityEngine.Pool;

// Added to every instance ObjectPools creates. Release returns it to its pool
// (or destroys it if it was never pooled).
public class PooledObject : MonoBehaviour
{
    public IObjectPool<PooledObject> Pool { get; set; }
    private bool released;

    void OnEnable()
    {
        released = false;
        foreach (TrailRenderer trail in GetComponentsInChildren<TrailRenderer>())
            trail.Clear();
    }

    void OnDisable()
    {
        CancelInvoke(nameof(Release));
    }

    public void Release()
    {
        if (released) return;
        released = true;

        if (TryGetComponent(out Rigidbody2D rb))
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (Pool != null) Pool.Release(this);
        else Destroy(gameObject);
    }

    public void ReleaseAfter(float seconds) => Invoke(nameof(Release), seconds);

    // Release this object if it is pooled, otherwise destroy it.
    public static void Despawn(GameObject target)
    {
        if (target.TryGetComponent(out PooledObject pooled)) pooled.Release();
        else Destroy(target);
    }
}
