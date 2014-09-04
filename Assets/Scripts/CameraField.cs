using UnityEngine;
using System.Collections;

/// <summary>
/// Make sure it has a collider trigger to activate the field
/// </summary>
public class CameraField : MonoBehaviour {
    public Bounds bounds;
    public CameraController.Mode mode;
    public bool doTransition = true;

    public string attachFindTag = "Player"; //if attach is null
    public CameraAttach attach;

    public Color boundColor = Color.blue; //for gizmo
    private CameraController mCamCtrl;

    void OnTriggerEnter(Collider col) {
        mCamCtrl.cameraField = this;
    }

    void Awake() {
        mCamCtrl = CameraController.instance;

        if(attach == null) {
            GameObject go = GameObject.FindGameObjectWithTag(attachFindTag);
            if(go)
                attach = go.GetComponent<CameraAttach>();
        }
    }

    void OnDrawGizmos() {
        if(bounds.size.x > 0 && bounds.size.y > 0 && bounds.size.z > 0) {
            Color clr = boundColor;

            Vector3 p = transform.position;
            Vector3 bc = bounds.center;

            Gizmos.color = clr;
            Gizmos.DrawWireCube(p + bc, bounds.size);

            clr.a = 0.3f;
            Gizmos.color = clr;
            Gizmos.DrawCube(new Vector3(p.x+bc.x, p.y+bc.y, 0), new Vector3(0.3f, 0.3f, 10));
        }
    }
}
