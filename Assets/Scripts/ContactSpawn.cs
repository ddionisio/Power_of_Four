using UnityEngine;
using System.Collections;

public class ContactSpawn : MonoBehaviour {
    public string group;
    public string type;

    public bool setUpToNormal;

    public Vector3 ofs;

    void OnCollisionEnter(Collision collision) {
        ContactPoint cp = collision.contacts[0];

        Transform spawn = PoolController.Spawn(group, type, null, null);

        if(setUpToNormal) {
            spawn.rotation = Quaternion.LookRotation(Vector3.forward, cp.normal);
            spawn.position = cp.point + spawn.rotation*ofs;
        }
        else {
            spawn.position = cp.point + Quaternion.LookRotation(Vector3.forward, cp.normal)*ofs;
        }
    }
}
