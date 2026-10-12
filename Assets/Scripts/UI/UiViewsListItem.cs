using UnityEngine;
using System;

/// <summary>
/// One entry in the UiViewsList: pairs a UI view prefab with the canvas
/// layer it should be parented under.
/// </summary>
[Serializable]
public struct UiViewsListItem
{
    public UiElement.ElementLayers UiLayer;
    public UiView View;
}
