using UnityEngine;
using System.Collections;

public class Enemy : EntityBase {

    private EnemyStats mStats;

    public EnemyStats stats { get { return mStats; } }

    protected override void OnDespawned() {
        //reset stuff here

        base.OnDespawned();
    }

    protected override void OnDestroy() {
        //dealloc here

        base.OnDestroy();
    }

    public override void SpawnFinish() {
        //start ai, player control, etc
    }

    protected override void SpawnStart() {
        //initialize some things
    }

    protected override void Awake() {
        base.Awake();

        //initialize variables
        mStats = GetComponent<EnemyStats>();
        mStats.changeHPCallback += OnStatsHPChange;
        mStats.applyDamageCallback += ApplyDamageCallback;
    }

    // Use this for initialization
    protected override void Start() {
        base.Start();

        //initialize variables from other sources (for communicating with managers, etc.)
    }

    protected virtual void OnStatsHPChange(Stats stat, float delta) {
        if(stats.curHP <= 0.0f)
            state = (int)EntityState.Dead;
        //else if()
    }

    protected virtual void ApplyDamageCallback(Damage damage) {

    }

    protected virtual void OnSuddenDeath() {
        stats.curHP = 0;
    }
}
