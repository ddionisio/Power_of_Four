using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Game")]
public class PlayerSaveGame : FsmStateAction {
    public FsmBool levelSave;
    public FsmString levelSaveScene;
    public FsmString levelSaveCheckpoint;

    public override void OnEnter() {
        if(levelSave.Value)
            LevelController.SetSavedLevel(levelSaveScene.Value, levelSaveCheckpoint.Value);

        Player.instance.Save();

        Finish();
    }
}
