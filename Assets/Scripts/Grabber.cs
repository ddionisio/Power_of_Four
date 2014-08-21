using UnityEngine;
using System.Collections;

public class Grabber : MonoBehaviour {
    public delegate void Callback(Grabber grabber);

    public Rigidbody attachToBody;

    public event Callback grabCallback; //when grabbing/throwing has occured

    private Grab mGrabbed;
    private FixedJoint mJoint;

    public Grab grab {
        get { return mGrabbed; }
        set {
            if(mGrabbed != value) {
                if(mGrabbed) {
                    //detach
                    if(mJoint) {
                        Destroy(mJoint);
                        mJoint = null;
                    }
                }

                mGrabbed = value;

                //attach
                if(mGrabbed) {
                    Transform attachT = attachToBody.transform;
                    Vector3 attachUp = attachT.up;
                    Bounds attachBounds = attachToBody.collider.bounds;

                    //set position
                    Vector3 pos = attachBounds.center + attachUp*attachBounds.extents.y;

                    //offset based on grabbed's collider
                    Transform grabbedT = mGrabbed.transform;
                    Collider grabbedCol = mGrabbed.collider;
                    Bounds grabbedBounds = grabbedCol.bounds;

                    Vector3 ofs = grabbedT.worldToLocalMatrix.MultiplyPoint3x4(grabbedBounds.center);
                    ofs.y = grabbedBounds.extents.y - ofs.y;

                    grabbedT.rotation = attachT.rotation;
                    grabbedT.position = pos + grabbedT.localToWorldMatrix.MultiplyVector(ofs);

                    mGrabbed.rigidbody.isKinematic = false; //just in case

                    mJoint = mGrabbed.gameObject.AddComponent<FixedJoint>();
                    mJoint.connectedBody = attachToBody;
                }
            }
        }
    }

    void OnTriggerEnter(Collider col) {
        Grab newGrab = col.GetComponent<Grab>();
        if(newGrab && newGrab.isGrabbable) {
            newGrab.Grabbed(this);

            grab = newGrab;

            if(grabCallback != null)
                grabCallback(this);
        }
    }

    void OnDestroy() {
        grabCallback = null;
    }


}
