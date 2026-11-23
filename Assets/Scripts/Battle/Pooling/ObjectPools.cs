using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

// One pool per prefab. Use Get instead of Instantiate and PooledObject.Release instead of Destroy.
public static class ObjectPools
{
    private static readonly Dictionary<GameObject, ObjectPool<PooledObject>> pools = new Dictionary<GameObject, ObjectPool<PooledObject>>();
    private static bool hooked;

    public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        HookSceneUnload();

        if (!pools.TryGetValue(prefab, out var pool))
        {
            pool = CreatePool(prefab);
            pools[prefab] = pool;
        }

        PooledObject item = pool.Get();
        item.transform.SetPositionAndRotation(position, rotation);
        item.gameObject.SetActive(true);
        return item.gameObject;
    }

    private static ObjectPool<PooledObject> CreatePool(GameObject prefab)
    {
        ObjectPool<PooledObject> pool = null;
        pool = new ObjectPool<PooledObject>(
            createFunc: () =>
            {
                GameObject instance = Object.Instantiate(prefab);
                instance.SetActive(false);
                instance.name = prefab.name;
                if (!instance.TryGetComponent(out PooledObject pooled))
                    pooled = instance.AddComponent<PooledObject>();
                pooled.Pool = pool;
                return pooled;
            },
            actionOnRelease: item => item.gameObject.SetActive(false),
            actionOnDestroy: item => { if (item != null) Object.Destroy(item.gameObject); },
            collectionCheck: false,
            defaultCapacity: 16,
            maxSize: 256);
        return pool;
    }

    // Pooled instances belong to the scene; forget them when it unloads.
    private static void HookSceneUnload()
    {
        if (hooked) return;
        hooked = true;
        SceneManager.sceneUnloaded += _ => pools.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        pools.Clear();
        hooked = false;
    }
}
