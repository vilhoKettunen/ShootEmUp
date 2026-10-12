using System;
using UnityEngine;

/// <summary>
/// (Task 4) One poolable prefab + how many to pre-instantiate.
/// [Serializable] so it can be a row in SOPoolSpaceObjects and edited in the Inspector.
///
/// The prefab field is a GAMEOBJECT reference (the actual prefab asset). The
/// Inspector shows its normal "Pick Prefab" browser for this field — the
/// prefab's ROOT must carry the SpaceObject-derived component (Bullet / Enemy
/// / Collectable / ...) that the pool will work with. ObjectPool resolves that
/// component at Register/Get time.
/// </summary>
[Serializable]
public class PoolEntry
{
    [Tooltip("The prefab to pool (a prefab asset). Its ROOT GameObject must have a SpaceObject-derived component (Bullet / Enemy / Collectable / ...).")]
    public GameObject prefab;

    [Tooltip("How many instances to pre-instantiate at boot (the pool grows on demand if more are needed).")]
    public int initialSize = 10;
}
