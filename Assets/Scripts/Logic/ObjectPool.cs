using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// (Task 4) Fully dynamic, generic object pool — PLAIN C# (rule 2.4: NO
/// MonoBehaviour, no Unity lifecycle). Keyed by the SpaceObject PREFAB as a
/// GameObject — which is what the Inspector shows you when you pick a prefab
/// (this is also the same shape as every other prefab field in the project,
/// e.g. Weapon.BulletPrefab).
///
/// Two dictionaries, exactly as the brief's diagram:
///   pools         : Dictionary&lt;GameObject, Queue&lt;SpaceObject&gt;&gt;  (prefab -> its queue)
///   objectPrefabs : Dictionary&lt;SpaceObject, GameObject&gt;          (instance -> prefab)
///
/// The concrete SpaceObject component (Bullet / Enemy / Collectable / ...) is
/// resolved ONCE on the prefab's ROOT at Register/Get time — the pool never
/// hard-codes a concrete type, so a new poolable type still needs ZERO C#
/// changes (rule 2.8).
///
/// Register(prefab) pre-fills the queue with `initialSize` inactive instances.
/// Get&lt;T&gt;(prefab)  dequeues from the front (or instantiates one if empty),
///                   activates the object and calls its Init() hook.
/// Return&lt;T&gt;(obj)   calls the object's ResetObject() hook, deactivates it,
///                   and enqueues it to the back.
///
/// Owned by ApplicationController (the existing singleton) — the pool is a
/// service, not a controller.
/// </summary>
public class ObjectPool
{
    private readonly Dictionary<GameObject, Queue<SpaceObject>> pools = new Dictionary<GameObject, Queue<SpaceObject>>();
    private readonly Dictionary<SpaceObject, GameObject> objectPrefabs = new Dictionary<SpaceObject, GameObject>();

    /// <summary>Pre-fill a pool for one prefab (called once per SOPoolSpaceObjects entry).</summary>
    public void Register(GameObject prefab, int initialSize = 10)
    {
        if (prefab == null) return;

        if (prefab.GetComponent<SpaceObject>() == null)
        {
            Debug.LogError("[ObjectPool] Prefab '" + prefab.name +
                "' has no SpaceObject-derived component on its ROOT — cannot pool it. " +
                "Attach a Bullet / Enemy / Collectable / ... script to the root GameObject of the prefab.");
            return;
        }

        if (pools.ContainsKey(prefab))
        {
            Debug.LogWarning("[ObjectPool] Prefab '" + prefab.name +
                "' registered more than once — check your SOPoolSpaceObjects list for duplicates.");
            return;
        }

        Queue<SpaceObject> queue = new Queue<SpaceObject>();
        for (int i = 0; i < initialSize; i++)
        {
            GameObject go = Object.Instantiate(prefab);
            go.SetActive(false);
            SpaceObject obj = go.GetComponent<SpaceObject>();
            if (obj == null)
            {
                Object.Destroy(go);
                continue;
            }
            queue.Enqueue(obj);
            objectPrefabs.Add(obj, prefab);
        }
        pools.Add(prefab, queue);
    }

    /// <summary>
    /// Take one active instance of `prefab`. Dequeues a recycled instance if
    /// available; otherwise grows the pool by instantiating a fresh one. The
    /// generic T is a static-type hint for the returned object (it must be a
    /// SpaceObject-derived component on the prefab's ROOT).
    /// </summary>
    public T Get<T>(GameObject prefab) where T : SpaceObject
    {
        if (prefab == null) return null;

        if (!pools.TryGetValue(prefab, out Queue<SpaceObject> queue))
        {
            Debug.LogError("[ObjectPool] Prefab '" + prefab.name +
                "' is NOT registered — add it to SOPoolSpaceObjects and run InitObjectPool().");
            return null;
        }

        SpaceObject so;
        bool wasNew = false;
        if (queue.Count > 0)
        {
            so = queue.Dequeue();
        }
        else
        {
            GameObject go = Object.Instantiate(prefab);
            so = go.GetComponent<SpaceObject>();
            if (so == null)
            {
                Object.Destroy(go);
                Debug.LogError("[ObjectPool] Prefab '" + prefab.name +
                    "' has no SpaceObject-derived component on its ROOT.");
                return null;
            }
            wasNew = true;
        }

        if (wasNew) objectPrefabs.Add(so, prefab);

        so.gameObject.SetActive(true);
        so.Init();

        T typed = so as T;
        if (typed == null)
        {
            Debug.LogError("[ObjectPool] Prefab '" + prefab.name +
                "' carries a '" + so.GetType().Name + "' component, but " +
                typeof(T).Name + " was requested — the ROOT component type must match the Get<T>() call.");
            return null;
        }
        return typed;
    }

    /// <summary>Recycle an instance back to its pool (reset + deactivate + enqueue to the back).</summary>
    public void Return<T>(T obj) where T : SpaceObject
    {
        if (obj == null) return;

        if (!objectPrefabs.TryGetValue(obj, out GameObject prefab))
        {
            Debug.LogWarning("[ObjectPool] '" + obj.name +
                "' was returned but was never created from a registered prefab — skipping.");
            return;
        }

        obj.ResetObject();
        obj.gameObject.SetActive(false);
        pools[prefab].Enqueue(obj);
    }
}
