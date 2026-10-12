using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Manages all UI views in the game. On init it instantiates every view
/// listed in the UI prefab's UiViewsList, parents each under its assigned
/// canvas layer, and provides the show/hide/query API used by the rest of
/// the game code.
///
/// Key behaviors:
///   - ShowUiView(view) hides all OTHER views (or just the ones that
///     self-hide), then shows the requested view.
///   - GetUiView&lt;T&gt;() finds a view by its concrete type.
///   - HideAllUiViews() hides everything (e.g. on scene teardown).
/// </summary>
public class UIController : MonoBehaviour
{
    [SerializeField]
    private List<UiLayerRoot> GuiLayerRoots = new List<UiLayerRoot>();
    
    private UiViewsList UiViews;
    private List<UiView> UiViewInstances = new List<UiView>();

    /// <summary>
    /// Instantiates all UI views from the UiViewsList, parents them under
    /// their assigned canvas layers, and returns this controller for
    /// chaining (e.g. Instantiate(...).GetComponent<UIController>().Init()).
    /// </summary>
    public UIController Init()
    {
        UiViews = GetComponent<UiViewsList>();
        UiViewInstances = InstantiateViews(UiViews);

        return this;
    }

    /// <summary>
    /// Instantiates each view in the list and parents it under the correct
    /// canvas layer root.
    /// </summary>
    private List<UiView> InstantiateViews(UiViewsList uiViews)
    {
        List<UiView> viewInstances = new List<UiView>();

        uiViews.Views.ForEach(x =>
        {
            UiView view = Instantiate<UiView>(x.View);
            viewInstances.Add(view);
            ParentUiObjectToLayerRoot(x.UiLayer, view.gameObject);
        });

        return viewInstances;
    }

    /// <summary>
    /// Shows the given view and hides the others.
    ///   - If _hideOtherViews is true: ALL other views are hidden.
    ///   - If false: only views flagged CloseWhenAnotherViewOpens are hidden.
    /// Returns the shown view for chaining.
    /// </summary>
    public UiView ShowUiView(UiView view, bool _hideOtherViews = true, UiView _showViewWhenThisHides = null)
    {
        if (view != null)
        {
            if (_hideOtherViews)
                UiViewInstances.Where(viewInstance => viewInstance != view).ToList().ForEach(itemToClose => itemToClose.Hide());
            else
                UiViewInstances.Where(viewInstance => viewInstance != view && viewInstance.CloseWhenAnotherViewOpens).ToList().ForEach(itemToClose => itemToClose.Hide());
        }

        return view;
    }

    /// <summary>
    /// Shows a view by its concrete type. Searches the instantiated views
    /// for the first one whose type matches T, then delegates to ShowUiView.
    /// </summary>
    public UiView ShowUiView<T>(UiView _showViewWhenThisHides = null, bool _hideOtherViews = true)
    {
        UiView view = UiViewInstances.First(viewInstance => viewInstance.GetType() == typeof(T));
        if (view == null)
            Debug.LogError("UiView with \"" + typeof(T) + "\" not found.");

        return ShowUiView(view, _hideOtherViews, _showViewWhenThisHides);
    }

    /// <summary>Finds and returns the first instantiated view of type T, or null.</summary>
    public T GetUiView<T>()
    {
        return UiViewInstances.Select(v => v).OfType<T>().FirstOrDefault();
    }

    /// <summary>Returns the canvas layer root GameObject for the given layer.</summary>
    private GameObject GetUiLayerRootObject(UiElement.ElementLayers layer)
    {
        return GuiLayerRoots.Single(x => x.UiLayer == layer).LayerRoot;
    }

    /// <summary>
    /// Parents a UI view under its assigned canvas layer root, resets its
    /// local transform to identity, and stretches it to fill the canvas.
    /// </summary>
    private void ParentUiObjectToLayerRoot(UiElement.ElementLayers uiLayer, GameObject guiObject)
    {
        guiObject.transform.SetParent(GetUiLayerRootObject(uiLayer).transform);
        guiObject.transform.localScale = Vector3.one;
        guiObject.transform.localPosition = Vector3.zero;
        guiObject.GetComponent<RectTransform>().offsetMax = Vector2.zero;
        guiObject.GetComponent<RectTransform>().offsetMin = Vector2.zero;
    }

    /// <summary>Hides ALL instantiated views (stops their coroutines).</summary>
    public void HideAllUiViews()
    {
        UiViewInstances.ForEach(itemToClose => itemToClose.Hide(true));
    }   
}
