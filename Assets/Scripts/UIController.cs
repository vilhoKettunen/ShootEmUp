using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class UIController : MonoBehaviour
{
    [SerializeField]
    private List<UiLayerRoot> GuiLayerRoots = new List<UiLayerRoot>();
    
    private UiViewsList UiViews;
    private List<UiView> UiViewInstances = new List<UiView>();

    public UIController Init()
    {
        //Instantiate all the UI views from UiViewsList component
        UiViews = GetComponent<UiViewsList>();
        UiViewInstances = InstantiateViews(UiViews);

        return this;
    }

    //UI view management
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

    public UiView ShowUiView(UiView view, bool _hideOtherViews = true, UiView _showViewWhenThisHides = null)
    {
        if (view != null)
        {
            //Hide all other views
            if (_hideOtherViews)
                UiViewInstances.Where(viewInstance => viewInstance != view).ToList().ForEach(itemToClose => itemToClose.Hide());
            //Or only views that are marked to hide when other views open
            else
                UiViewInstances.Where(viewInstance => viewInstance != view && viewInstance.CloseWhenAnotherViewOpens).ToList().ForEach(itemToClose => itemToClose.Hide());
        }

        return view;
    }

    public UiView ShowUiView<T>(UiView _showViewWhenThisHides = null, bool _hideOtherViews = true)
    {
        UiView view = UiViewInstances.First(viewInstance => viewInstance.GetType() == typeof(T));
        if (view == null)
            Debug.LogError("UiView with \"" + typeof(T) + "\" not found.");

        return ShowUiView(view, _hideOtherViews, _showViewWhenThisHides);
    }

    public T GetUiView<T>()
    {
        return UiViewInstances.Select(v => v).OfType<T>().FirstOrDefault();
    }

    private GameObject GetUiLayerRootObject(UiElement.ElementLayers layer)
    {
        return GuiLayerRoots.Single(x => x.UiLayer == layer).LayerRoot;
    }

    private void ParentUiObjectToLayerRoot(UiElement.ElementLayers uiLayer, GameObject guiObject)
    {
        guiObject.transform.SetParent(GetUiLayerRootObject(uiLayer).transform);
        guiObject.transform.localScale = Vector3.one;
        guiObject.transform.localPosition = Vector3.zero;
        guiObject.GetComponent<RectTransform>().offsetMax = Vector2.zero;
        guiObject.GetComponent<RectTransform>().offsetMin = Vector2.zero;
    }

    public void HideAllUiViews()
    {
        UiViewInstances.ForEach(itemToClose => itemToClose.Hide(true));
    }   
}
