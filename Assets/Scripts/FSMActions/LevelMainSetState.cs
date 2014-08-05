using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Game")]
public class LevelMainSetState : FsmStateAction {
    public LevelController.State state;

    public override void OnEnter() {
        LevelController.instance.mainLevelState = state;
        Finish();
    }
}
