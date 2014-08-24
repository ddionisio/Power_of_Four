using UnityEngine;
using System.Collections;

/// <summary>
/// Make sure to not have a collider, it would be pointless otherwise
/// </summary>
public class DamageSphereCheckDelay : DamageTrigger {
    public float radius = 1.0f;
    public Vector3 ofs;
    public LayerMask mask;
    public float delay = 0.2f;

    void OnEnable() {
        StartCoroutine(DoUpdate());
    }

    IEnumerator DoUpdate() {
        WaitForSeconds wait = new WaitForSeconds(delay);
        while(true) {
            Vector3 pos = transform.localToWorldMatrix.MultiplyPoint3x4(ofs);

            Collider[] colls = Physics.OverlapSphere(pos, radius, mask);

            for(int i = 0; i < colls.Length; i++) {
                DoDamage(colls[i]);
            }

            yield return wait;
        }
    }

    void OnDrawGizmosSelected() {
        if(radius > 0.0f) {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.localToWorldMatrix.MultiplyPoint3x4(ofs), radius);
        }
    }
}