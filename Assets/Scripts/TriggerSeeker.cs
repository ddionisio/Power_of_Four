using UnityEngine;
using System.Collections;

public class TriggerSeeker : MonoBehaviour {
    public Rigidbody seekerBody;

    public float force;
    public ForceMode forceMode = ForceMode.Force;
    public float updateDelay = 0.1f;

    private IEnumerator mSeekAction;
    private Collider mCol;

    private bool mAlive = true;

    public bool alive {
        get { return mAlive; }
        set {
            if(mAlive != value) {
                mAlive = value;
                if(!mAlive) {
                    if(mSeekAction != null) { StopCoroutine(mSeekAction); mSeekAction = null; }
                    mCol = null;
                }
            }
        }
    }

    void OnEnable() {
        mAlive = true;
    }

    void OnTriggerEnter(Collider col) {
        if(mAlive && mCol == null) {
            if(mSeekAction != null) { StopCoroutine(mSeekAction); mSeekAction = null; }

            mCol = col;
            StartCoroutine(mSeekAction = DoSeek(col.transform));
        }
    }

    void OnTriggerExit(Collider col) {
        if(mCol == col) {
            if(mSeekAction != null) {
                StopCoroutine(mSeekAction);
                mSeekAction = null;
            }

            mCol = null;
        }
    }

    IEnumerator DoSeek(Transform targetT) {
        WaitForSeconds wait = new WaitForSeconds(updateDelay);

        Transform t = transform;

        while(t && t.gameObject.activeSelf) {
            Vector3 dir = targetT.position - t.position; dir.z = 0.0f;
            dir.Normalize();

            seekerBody.AddForce(dir*force, forceMode);

            yield return wait;
        }

        mSeekAction = null;
    }
}
