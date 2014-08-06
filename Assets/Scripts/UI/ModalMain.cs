using UnityEngine;
using System.Collections;

public class ModalMain : UIController {
    public UIEventListener play;
    public UIEventListener options;
    public UIEventListener credits;

    protected override void OnActive(bool active) {
        if(active) {
            UICamera.selectedObject = play.gameObject;

            play.onClick = OnPlayClick;
            options.onClick = OnOptionsClick;
        }
        else {
            play.onClick = null;
            options.onClick = null;
        }
    }

    protected override void OnOpen() {
    }

    protected override void OnClose() {
    }

    void OnPlayClick(GameObject go) {
        if(UserSlotData.IsSlotExist(0))
            UserSlotData.LoadSlot(0, false);
        else
            UserSlotData.CreateSlot(0, "temp");

        LevelController.LoadSavedLevel();
    }

    void OnOptionsClick(GameObject go) {
        UIModalManager.instance.ModalOpen("options");
    }
}
