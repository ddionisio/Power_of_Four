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
        mCamCtrl.mode = mode;

        Bounds setBounds = bounds;
        setBounds.center += transform.position;

        mCamCtrl.bounds = setBounds;

        mCamCtrl.attach = attach;

        mCamCtrl.SetTransition(doTransition);
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

            Gizmos.color = clr;
            Gizmos.DrawWireCube(transform.position + bounds.center, bounds.size);

            clr.a = 0.2f;
            Gizmos.color = clr;
            Gizmos.DrawCube(transform.position + bounds.center, new Vector3(0.3f, 0.3f, bounds.size.z));
        }
    }
}
