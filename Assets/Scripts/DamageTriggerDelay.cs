using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DamageTriggerDelay : DamageTrigger {

    public float delay;

    private Dictionary<Collider, IEnumerator> mActives = new Dictionary<Collider, IEnumerator>(8);

    void OnTriggerExit(Collider col) {
        IEnumerator route;
        if(mActives.TryGetValue(col, out route)) {
            StopCoroutine(route);
            mActives.Remove(col);
        }
    }

    void OnTriggerStay(Collider col) {
        if(!mActives.ContainsKey(col)) {
            IEnumerator route;
            StartCoroutine(route = DamageRoutine(col));
            mActives.Add(col, route);
        }
    }

    void OnDisable() {
        mActives.Clear();
    }

    IEnumerator DamageRoutine(Collider col) {
        WaitForSeconds wait = new WaitForSeconds(delay);
        while(col && col.gameObject.activeInHierarchy) {
            DoDamage(col);
            yield return wait;
        }

        mActives.Remove(col);
    }
}
