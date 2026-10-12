using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A list of UI views to instantiate. Attached to the UI prefab root.
/// Each entry specifies which view prefab to load and which canvas layer
/// to parent it under.
/// </summary>
public class UiViewsList : MonoBehaviour
{
    public List<UiViewsListItem> Views = new List<UiViewsListItem>();
}
