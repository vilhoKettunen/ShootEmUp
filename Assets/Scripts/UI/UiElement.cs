using UnityEngine;

[System.Serializable]
public class UiElement {

	public Transform Prefab;
	public enum ElementLayers { LayerHUD, LayerContextMenus, LayerFullScreenMenus, LayerLoadingView, LayerAlwaysOnTop };
	public ElementLayers ElementUILayer;
}

[System.Serializable]
public class UiLayerRoot {
    public GameObject LayerRoot;
    public UiElement.ElementLayers UiLayer;
}
