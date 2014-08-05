using UnityEngine;
using HutongGames.PlayMaker;
using M8.PlayMaker;

[ActionCategory("Game")]
public class PlatformerSpritePlayOverrideClip : FSMActionComponentBase<PlatformerAnimatorController> {
    public FsmString clip;
    public bool finishWait;
    public FsmEvent finishEvent;

    // Code that runs on entering the state.
    public override void OnEnter() {
        base.OnEnter();
        mComp.PlayOverrideClip(clip.Value);
        if(!finishWait)
            Finish();
        else
            mComp.clipFinishCallback += FinishCallback;
    }

    public override void OnExit() {
        mComp.clipFinishCallback -= FinishCallback;    
    }

    void FinishCallback(PlatformerAnimatorController aCtrl, AMTakeData take) {
        if(take.name == clip.Value) {
            if(!FsmEvent.IsNullOrEmpty(finishEvent))
                Fsm.Event(finishEvent);
            Finish();
        }
    }
}
