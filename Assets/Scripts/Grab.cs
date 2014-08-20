using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Grab : MonoBehaviour {
    public enum Action {
        Grabbed,
        Thrown,
        Impact
    }

    public delegate void Callback(Grab grab, Action act);

    [System.NonSerialized]
    public bool isGrabbable = false;

    public event Callback actionCallback;

    public void Grabbed(Grabber grabber) {
        isGrabbable = false;

        if(actionCallback != null)
            actionCallback(this, Action.Grabbed);
    }

    public void Throw(Grabber grabber, Vector3 point, Vector3 dir) {
        isGrabbable = false;

        if(actionCallback != null)
            actionCallback(this, Action.Thrown);
    }

    public void Impact(Vector3 point, Vector3 normal) {
        isGrabbable = false;

        if(actionCallback != null)
            actionCallback(this, Action.Impact);
    }

    void OnDestroy() {
        actionCallback = null;
    }
}
