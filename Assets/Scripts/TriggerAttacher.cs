using UnityEngine;
using System.Collections;

public class TriggerAttacher : MonoBehaviour {
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
        mGrav = GetComponent<GravityController>();
    }

    void OnTriggerEnter(Collider col) {
        if(mAlive && mJoint == null) {
            seeker.alive = false;

            mJoint = col.gameObject.AddComponent<FixedJoint>();
            mJoint.connectedBody = rigidbody;

            col.transform.position = collider.bounds.center;

            col.SendMessage("OnTriggerAttacherAttach", this);

            if(mGrav) mGrav.enabled = false;
        }
    }

    void OnReactive() {
        mAlive = true;

        if(seeker)
            seeker.alive = true;
    }
}
