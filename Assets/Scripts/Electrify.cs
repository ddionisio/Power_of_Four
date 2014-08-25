using UnityEngine;
using System.Collections;

public class Electrify : MonoBehaviour {
    public delegate void Callback(Electrify e);

    public Transform render;
    public float duration = 0.3f;

    public event Callback despawnCallback;

    private Transform mAttachStart;
    private Transform mAttachEnd;

    private Transform mTrans;
    private BoxCollider mColl;

    public Transform attachStart { get { return mAttachStart; } }
    public Transform attachEnd { get { return mAttachEnd; } }

    public void SetPoints(Transform start, Transform end) {
        mAttachStart = start;
        mAttachEnd = end;
    }

    void OnSpawned() {
        Invoke("OnRelease", duration);
    }

    void OnDespawned() {
        mAttachStart = null;
        mAttachEnd = null;

        CancelInvoke("OnRelease");

        if(despawnCallback != null)
            despawnCallback(this);
    }

    void Awake() {
        mTrans = transform;
        mColl = collider as BoxCollider;
    }

    void Update() {
        if(mAttachStart && mAttachEnd && mAttachStart.gameObject.activeInHierarchy && mAttachEnd.gameObject.activeInHierarchy) {
            Vector3 p1 = mAttachStart.position; p1.z = 0f;
            Vector3 p2 = mAttachEnd.position; p2.z = 0f;
            Vector3 dir = p2 - p1;
            float dist = dir.magnitude;
            dir /= dist;

            mTrans.position = p1;
            mTrans.up = dir;

            mColl.center = new Vector3(0, dist*0.5f, 0);

            Vector3 collSize = mColl.size; collSize.y = dist;
            mColl.size = collSize;

            Vector3 renderS = render.localScale;
            renderS.y = dist;
            render.localScale = renderS;
        }
        else
            OnRelease();
    }

    void OnRelease() {
        PoolController.ReleaseAuto(mTrans);
    }
}
