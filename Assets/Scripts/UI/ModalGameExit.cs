using UnityEngine;
using System.Collections;

public class ModalGameExit : UIController {
    public UIEventListener restart;
    public UIEventListener lastsave;
    public UIEventListener main;
    public UIEventListener back;

    protected override void OnActive(bool active) {
        if(active) {
            UICamera.selectedObject = restart.gameObject;

            restart.onClick = OnRestartClick;
            lastsave.onClick = OnLastSaveClick;
            main.onClick = OnMainClick;
            back.onClick = OnBackClick;
        }
        else {
            restart.onClick = null;
            lastsave.onClick = null;
            main.onClick = null;
            back.onClick = null;
        }
    }

    void OnRestartClick(GameObject go) {
        UIModalConfirm.Open(GameLocalize.instance.GetText("confirm_restart"), GameLocalize.instance.GetText("confirm_base_text"),
            delegate(bool aYes) {
                if(aYes) SceneManager.instance.Reload();
            });
    }

    void OnLastSaveClick(GameObject go) {
        UIModalConfirm.Open(GameLocalize.instance.GetText("confirm_lastsave"), GameLocalize.instance.GetText("confirm_base_text"), delegate(bool aYes) {
            if(aYes)
                LevelController.LoadSavedLevel();
        });
    }

    void OnMainClick(GameObject go) {
        UIModalConfirm.Open(GameLocalize.instance.GetText("confirm_mainmenu"), GameLocalize.instance.GetText("confirm_base_text"), delegate(bool aYes) {
            if(aYes)
                SceneManager.instance.LoadScene(Scenes.main);
        });
    }

    void OnBackClick(GameObject go) {
        UIModalManager.instance.ModalCloseTop();
    }
}
