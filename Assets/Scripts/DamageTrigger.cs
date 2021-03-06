﻿using UnityEngine;
using System.Collections;

public class DamageTrigger : MonoBehaviour {
    public delegate void GenericCallback(DamageTrigger trigger, GameObject victim);

    public event GenericCallback damageCallback;

    private Damage mDmg;

    public Damage damage { get { return mDmg; } }

    protected void DoDamage(Collider col) {
        if(mDmg.CallDamageTo(col.gameObject, transform.position, (col.bounds.center - transform.position).normalized)) {
            if(damageCallback != null) {
                damageCallback(this, col.gameObject);
            }
        }
    }

    protected void DoDamage(Stats stats, Vector3 center) {
        if(mDmg.CallDamageTo(stats, transform.position, (center - transform.position).normalized)) {
            if(damageCallback != null) {
                damageCallback(this, stats.gameObject);
            }
        }
    }

    void OnDestroy() {
        damageCallback = null;
    }

    void Awake() {
        mDmg = GetComponent<Damage>();
    }

    void OnTriggerStay(Collider col) {
        DoDamage(col);
    }
}
