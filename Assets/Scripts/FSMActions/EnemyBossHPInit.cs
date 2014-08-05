using UnityEngine;
using HutongGames.PlayMaker;
using M8.PlayMaker;

[ActionCategory("Game")]
public class EnemyInitBossHP : FSMActionComponentBase<Enemy> {
    public override void OnEnter() {
        base.OnEnter();

        HUD.instance.boss.Init(mComp);
    }
}
