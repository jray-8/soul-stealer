using UnityEngine;

public class PortalController : MonoBehaviour
{
    private GameObject portalWarp;
    private GameObject gargoyle;

    private void SetActive(bool a){ // true to activate portal (can enter)
        gargoyle.SetActive(!a);
        portalWarp.SetActive(a);
    }
    public void Activate(){
        SetActive(true);
    }
    public void Deactivate(){
        SetActive(false);
    }

    void Start(){
        portalWarp = transform.Find("PortalWarp").gameObject;
        gargoyle = transform.Find("FelineGargoyle").gameObject;
        Deactivate();
    }
}
