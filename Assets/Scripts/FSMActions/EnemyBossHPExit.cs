using UnityEngine;
using HutongGames.PlayMaker;
using M8.PlayMaker;

[ActionCategory("Game")]
public class EnemyBossHPExit : FsmStateAction {
    public bool finishWait;
    public FsmEvent finishEvent;

    public override void OnEnter() {
        HUD.instance.boss.Exit();

        if(!finishWait)
            Finish();
    }

    public override void OnUpdate() {
        if(!HUD.instance.boss.isAnimPlaying) {
            if(!FsmEvent.IsNullOrEmpty(finishEvent))
                Fsm.Event(finishEvent);
            Finish();
        }
    }
}
