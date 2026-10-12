using UnityEngine;

/// <summary>
/// Base class for all visible UI views. Manages show/hide state and supports
/// an optional HideChildObject (a child GameObject to toggle instead of the
/// root, useful when the root must stay active for other reasons).
///
/// Subclasses (UiViewHud, UiViewMainMenu, ...) add their own widgets and
/// logic on top of this show/hide machinery.
/// </summary>
public class UiView : MonoBehaviour
{
    /// <summary>Whether this view starts visible or hidden.</summary>
    public enum State { Visible, Hidden }
    public State DefaultState;
    /// <summary>
    /// Optional child GameObject to toggle instead of the root.
    /// When set, Show/Hide toggle this child's active state.
    /// </summary>
    public GameObject HideChildObject;        
    /// <summary>
    /// If true, this view hides itself when another view is shown
    /// (used by the UIController's ShowUiView logic).
    /// </summary>
    public bool CloseWhenAnotherViewOpens = false;
    
    /// <summary>
    /// Applies the DefaultState on scene load: shows or hides the view.
    /// </summary>
    protected virtual void Awake()
    {
        if (DefaultState == State.Visible)
            Show();
        else
            Hide();
    }

    /// <summary>Makes this view visible (activates the root or HideChildObject).</summary>
    public virtual void Show()
    {
        if (HideChildObject == null)
            gameObject.SetActive(true);
        else
            HideChildObject.SetActive(true);
    }

    /// <summary>
    /// Hides this view (deactivates the root or HideChildObject).
    /// Optionally stops all running coroutines on this view.
    /// </summary>
    public virtual void Hide(bool _stopCoroutines = false)
    {
        if (!gameObject.activeSelf || HideChildObject != null && !HideChildObject.activeSelf)
            return;

        if (_stopCoroutines)
            StopAllCoroutines();

        if (HideChildObject == null)
            gameObject.SetActive(false);
        else
            HideChildObject.SetActive(false);
    }

    /// <summary>Hides the view and stops its coroutines (called on input cancel).</summary>
    protected virtual void CancelInputClose()
    {
        if (gameObject.activeInHierarchy)
            Hide(true);        
    }
}
