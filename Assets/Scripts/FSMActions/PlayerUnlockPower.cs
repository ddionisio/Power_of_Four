using UnityEngine;
using HutongGames.PlayMaker;

[ActionCategory("Game")]
public class PlayerUnlockPower : FsmStateAction {
    public FsmInt index;

    public override void OnEnter() {
        Player.instance.UnlockBuddy(index.Value);
        Finish();
    }
}
