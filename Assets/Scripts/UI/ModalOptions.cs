using UnityEngine;
using System.Collections;

public class ModalOptions : UIController {
    public UIEventListener temp;
    public UIEventListener back;

    protected override void OnActive(bool active) {
        if(active) {
            UICamera.selectedObject = temp.gameObject;

            back.onClick = OnBackClick;
        }
        else {
            back.onClick = null;
        }
    }

    void OnBackClick(GameObject go) {
        UIModalManager.instance.ModalCloseTop();
    }
}
