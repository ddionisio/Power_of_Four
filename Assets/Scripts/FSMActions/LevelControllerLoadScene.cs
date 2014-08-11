using UnityEngine;
using HutongGames.PlayMaker;
using M8.PlayMaker;

[ActionCategory("Game")]
public class LevelControllerLoadScene : FsmStateAction {
    public FsmString toScene;
    public FsmString spawnpoint;
    public FsmBool save;
    public FsmBool saveCheckpoint;

    public override void OnEnter() {
        if(save.Value) {
            LevelController.SetSavedLevel(toScene.Value, saveCheckpoint.Value ? spawnpoint.Value : "");
            Player.instance.Save();
        }

        LevelController.instance.LoadScene(toScene.Value, spawnpoint.Value);

        Finish();
    }
}
