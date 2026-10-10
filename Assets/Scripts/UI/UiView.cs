/**
 * Base class for all visible UI views
 */

using UnityEngine;

public class UiView : MonoBehaviour
{
    public enum State { Visible, Hidden }
    public State DefaultState;
    public GameObject HideChildObject;        
    public bool CloseWhenAnotherViewOpens = false;
    
    protected virtual void Awake()
    {
        //Initially show or hide UIView depending on it's DefaultState
        if (DefaultState == State.Visible)
            Show();
        else
            Hide();
    }

    /**
     * Show UI view
     */
    public virtual void Show()
    {
        if (HideChildObject == null)
            gameObject.SetActive(true);
        else
            HideChildObject.SetActive(true);
    }

    /**
     * Hide UI view
     */
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



    protected virtual void CancelInputClose()
    {
        if (gameObject.activeInHierarchy)
            Hide(true);        
    }
}
