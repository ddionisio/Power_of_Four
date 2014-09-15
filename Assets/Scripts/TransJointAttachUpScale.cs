using UnityEngine;
using System.Collections;

public class TransJointAttachUpScale : MonoBehaviour {
    public Transform target; //should be child of this
    public float ofs = 0.01f;
    public bool aliveOnAwake = true;

    private Joint mJoint;

    public bool alive {
        get { return mJoint != null; }
        set {
            mJoint = value ? GetComponent<Joint>() : null;
            target.localRotation = Quaternion.identity;
        }
    }

    void Awake() {
        if(aliveOnAwake)
            alive = true;
    }

    // Update is called once per frame
    void Update() {
        if(mJoint) {
            Vector3 dir = mJoint.connectedBody.position - target.position; dir.z = 0f;
            float len = dir.magnitude;
            if(len > 0) {
                dir /= len;

                Vector3 s = target.localScale; s.y = len + ofs;
                target.localScale = s;

                target.up = dir;
            }
        }
    }
}
