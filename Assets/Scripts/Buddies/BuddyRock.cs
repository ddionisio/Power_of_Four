using UnityEngine;
using System.Collections;

public class BuddyRock : Buddy {
    [System.Serializable]
    public class ProjData {
        public string type;
        public float angleRange;

        public Projectile Fire(Vector3 pos, Vector3 dir) {
            //dir = Quaternion.Euler(0, 0, Random.Range(-angleRange, angleRange))*dir;

            //Projectile proj = Projectile.Create(projGrp, type, pos, dir, null);

            //return proj;
            return null;
        }
    }

    public ProjData[] projs; //based on level

    protected override void OnInit() { }

    protected override void OnEnter() { }

    protected override void OnExit() { }

    protected override void OnFireStart() { }

    protected override void OnFire() {
        /*Matrix4x4 posMtx = firePoint.localToWorldMatrix;
        Vector3 pos = firePoint.position; pos.z = 0.0f;
        Vector3 dir = posMtx.MultiplyVector(Vector3.right);
        projs[level].Fire(pos, dir);*/
    }

    protected override void OnFireStop() { }
}
