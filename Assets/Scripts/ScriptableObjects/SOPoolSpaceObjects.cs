using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The pool CONFIG as a data asset. One instance per game.
/// Adding a new poolable type = add a row here, ZERO C# changes.
///
/// Loaded from a Resources/ folder by ApplicationController.
/// </summary>
[CreateAssetMenu(fileName = "PoolSpaceObjects", menuName = "Game/Pool Space Objects", order = 3)]
public class SOPoolSpaceObjects : ScriptableObject
{
    [Header("Pool entries (one row per poolable prefab)")]
    [SerializeField] private List<PoolEntry> poolObjects = new List<PoolEntry>();

    /// <summary>Read-only view of the pool entries (data is not mutated at runtime).</summary>
    public IReadOnlyList<PoolEntry> PoolObjects => poolObjects;
}
