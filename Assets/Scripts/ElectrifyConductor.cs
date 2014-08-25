using UnityEngine;
using System.Collections.Generic;

public class ElectrifyConductor : MonoBehaviour {
    public string electricPoolGrp;
    public string electricPoolType;

    public float startDelay = 0.1f;
    public float repeatDelay = 1.0f;

    public float checkRadius;
    public Vector3 checkOfs;
    public LayerMask checkMask;

    private HashSet<ElectrifyConductor> mTaggedConductors = new HashSet<ElectrifyConductor>();
    private List<Electrify> mElectrics = new List<Electrify>(16);

    void OnDisable() {
        mTaggedConductors.Clear();

        for(int i = 0; i < mElectrics.Count; i++) {
            mElectrics[i].despawnCallback -= OnElectricDespawn;
            PoolController.ReleaseAuto(mElectrics[i].transform);
        }

        mElectrics.Clear();

        CancelInvoke("DoAction");
    }

    public void Run() {
        InvokeRepeating("DoAction", startDelay, repeatDelay);
    }

    void DoAction() {
        mTaggedConductors.Clear();

        Vector3 pos = transform.localToWorldMatrix.MultiplyPoint3x4(checkOfs);
        Collider[] colls = Physics.OverlapSphere(pos, checkRadius, checkMask);
        for(int i = 0; i < colls.Length; i++) {
            if(colls[i] != collider && colls[i].gameObject.activeInHierarchy) {
                ElectrifyConductor ec = colls[i].GetComponent<ElectrifyConductor>();
                if(ec) {
                    if(!ec.mTaggedConductors.Contains(this)) {
                        mTaggedConductors.Add(ec);

                        Transform eT = PoolController.Spawn(electricPoolGrp, electricPoolType, null, null, pos);
                        Electrify e = eT.GetComponent<Electrify>();
                        e.SetPoints(transform, ec.transform);
                        e.despawnCallback += OnElectricDespawn;
                        mElectrics.Add(e);
                    }
                }
            }
        }
    }

    void OnElectricDespawn(Electrify e) {
        ElectrifyConductor ec = e.attachEnd ? e.attachEnd.GetComponent<ElectrifyConductor>() : null;
        mTaggedConductors.Remove(ec);

        e.despawnCallback -= OnElectricDespawn;
        mElectrics.Remove(e);
    }

    void OnDrawGizmosSelected() {
        if(checkRadius > 0.0f) {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.localToWorldMatrix.MultiplyPoint3x4(checkOfs), checkRadius);
        }
    }
}
