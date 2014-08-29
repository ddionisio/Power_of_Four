using UnityEngine;
using System.Collections;

public class TransJointAttachUpScale : MonoBehaviour {
    public Transform target; //should be child of this
    public float ofs = 0.01f;

    public bool applyToSpringMaxDistance;

    private Joint mJoint;

    void Awake() {
        mJoint = GetComponent<Joint>();

        if(applyToSpringMaxDistance) {
            Vector3 dir = mJoint.connectedBody.position - target.position; dir.z = 0f;
            float len = dir.magnitude;

            SpringJoint sj = mJoint as SpringJoint;
            if(sj) {
                sj.maxDistance = len;
            }
        }
    }

    // Update is called once per frame
    void Update() {
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
