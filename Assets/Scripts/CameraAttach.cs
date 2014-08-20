using UnityEngine;
using System.Collections;

public class CameraAttach : MonoBehaviour {
    public virtual Vector3 position {
        get {
            return collider ? collider.bounds.center : transform.position;
        }
    }

    public virtual Quaternion rotation {
        get {
            return transform.rotation;
        }
    }
}
