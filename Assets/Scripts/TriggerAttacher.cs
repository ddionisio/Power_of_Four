using UnityEngine;
using System.Collections;

public class TriggerAttacher : MonoBehaviour {
    public GameObject targetBody;
    public TriggerSeeker seeker; //call to turn off once we are attached, on again when detached
    public float reactiveDelay;

    private bool mAlive = true;
    private FixedJoint mJoint;
    private GravityController mGrav;

    public void Detach(bool doReactiveDelay = true) {
        if(mJoint) {
            Destroy(mJoint);
            mJoint = null;
        }
                
        if(mGrav) mGrav.enabled = true;

        if(doReactiveDelay) {
            mAlive = false;

            Invoke("OnReactive", reactiveDelay);
        }
        else if(seeker)
            seeker.alive = true;
    }

    void OnEnable() {
        mAlive = true;
    }

    void OnDisable() {
        Detach(false);
    }

    void Awake() {
        mGrav = targetBody.GetComponent<GravityController>();
    }

    void OnTriggerEnter(Collider col) {
        if(mAlive && mJoint == null) {
            //first check if collider has another joint belonging to another attacher
            Joint otherJoint = col.gameObject.GetComponent<Joint>();
            if(!(otherJoint && otherJoint.connectedBody.GetComponentInChildren<TriggerAttacher>())) {
                col.transform.position = collider.bounds.center;

                mJoint = col.gameObject.AddComponent<FixedJoint>();
                mJoint.connectedBody = targetBody.rigidbody;

                seeker.alive = false;
                if(mGrav) mGrav.enabled = false;

                col.SendMessage("OnTriggerAttacherAttach", this);
            }
        }
    }

    void OnReactive() {
        mAlive = true;

        if(seeker)
            seeker.alive = true;
    }
}
