using UnityEngine;
using System.Collections;

public class ModalPause : UIController {
    public UIEventListener resume;
    public UIEventListener options;
    public UIEventListener exit;

    protected override void OnActive(bool active) {
        if(active) {
            UICamera.selectedObject = resume.gameObject;

            resume.onClick = OnBackClick;
            options.onClick = OnOptionsClick;
            exit.onClick = OnExitClick;
        }
        else {
            resume.onClick = null;
            options.onClick = null;
            exit.onClick = null;
        }
    }

    void OnBackClick(GameObject go) {
        UIModalManager.instance.ModalCloseTop();
    }

    void OnOptionsClick(GameObject go) {
        UIModalManager.instance.ModalOpen("options");
    }

    void OnExitClick(GameObject go) {
        UIModalManager.instance.ModalOpen("exit");
    }
}
