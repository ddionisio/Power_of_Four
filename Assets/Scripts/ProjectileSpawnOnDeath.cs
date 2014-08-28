using UnityEngine;
using System.Collections;

public class ProjectileSpawnOnDeath : MonoBehaviour {
    public string group;
    public string type;

    public int count;

    public Vector3 ofs;

    [SerializeField]
    bool _startAlive;

    private Projectile mProj;
    private bool mAlive;

    public bool alive {
        get { return mAlive; }
        set {
            if(mAlive != value) {
                mAlive = value;

                if(!mProj) mProj = GetComponent<Projectile>();

                if(mAlive) mProj.setStateCallback += OnStateChange;
                else mProj.setStateCallback -= OnStateChange;
            }
        }
    }

    void Start() {
        if(!alive)
            alive = _startAlive;
    }

    void OnStateChange(EntityBase ent) {
        switch((Projectile.State)mProj.state) {
            case Projectile.State.Dying:
                Vector3 pos = transform.localToWorldMatrix.MultiplyPoint3x4(ofs);
                Vector3 dir = transform.up;
                Quaternion rot = Quaternion.Euler(0, 0, 360.0f/count);
                for(int i = 0; i < count; i++) {
                    Projectile.Create(group, type, pos, dir, null);
                    dir = rot*dir;
                }
                break;
        }
    }

    void OnDrawGizmosSelected() {
        Gizmos.color = new Color(0f,0.5f,0f,0.5f);
        Gizmos.DrawSphere(transform.localToWorldMatrix.MultiplyPoint3x4(ofs), 0.2f);
    }
}
