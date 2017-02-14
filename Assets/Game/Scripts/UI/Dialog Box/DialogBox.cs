using UnityEngine;

public class DialogBox : MonoBehaviour
{
    public virtual void Show()
    {
        WorldController.Instance.IsModal = true;
        gameObject.SetActive(true);
    }

    public virtual void Close()
    {
        WorldController.Instance.IsModal = false;
        gameObject.SetActive(false);
    }

    public virtual void OnClick() { }
}
