using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// (Task 4) The pool CONFIG as a data asset (rule 2.8). One instance per game.
/// Adding a new poolable type = add a row here, ZERO C# changes (rule 2.8).
///
/// Loaded from a Resources/ folder by ApplicationController (see Task 4 wiring).
/// </summary>
[CreateAssetMenu(fileName = "PoolSpaceObjects", menuName = "Game/Pool Space Objects", order = 3)]
public class SOPoolSpaceObjects : ScriptableObject
{
    [Header("Pool entries (one row per poolable prefab)")]
    [SerializeField] private List<PoolEntry> poolObjects = new List<PoolEntry>();

    /// <summary>Read-only view of the pool entries (rule 2.7 — data is not mutated at runtime).</summary>
    public IReadOnlyList<PoolEntry> PoolObjects => poolObjects;
}
