using UnityEngine;
using HutongGames.PlayMaker;
using M8.PlayMaker;

[ActionCategory("Game")]
public class EnemyBossHPEnter : FsmStateAction {
    public bool finishWait;
    public FsmEvent finishEvent;

    public override void OnEnter() {
        HUD.instance.boss.Enter();

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
