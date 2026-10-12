using UnityEngine;

/// <summary>
/// Describes a single UI element's prefab and which layer it belongs to.
/// Used in the UI prefab's view list to map views to their canvas layers.
/// </summary>
[System.Serializable]
public class UiElement {

	public Transform Prefab;
	/// <summary>The available UI canvas layers, ordered from bottom to top.</summary>
	public enum ElementLayers { LayerHUD, LayerContextMenus, LayerFullScreenMenus, LayerLoadingView, LayerAlwaysOnTop };
	public ElementLayers ElementUILayer;
}

/// <summary>
/// Maps a UI layer to its root Canvas GameObject. The UIController uses these
/// to parent instantiated views under the correct canvas.
/// </summary>
[System.Serializable]
public class UiLayerRoot {
    public GameObject LayerRoot;
    public UiElement.ElementLayers UiLayer;
}
