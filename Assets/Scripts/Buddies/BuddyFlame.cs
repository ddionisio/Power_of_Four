using UnityEngine;
using System.Collections;

public class BuddyFlame : Buddy {
    public class ProjData {
        public string type;
        public Vector2 angleRange;
        public Vector2 speedRange;
        public Vector2 decayRange;

        public Projectile Fire(Vector3 pos, Vector3 dir) {
            Projectile proj = null;
            return proj;
        }
    }

    public ProjData[] projs; //based on level

    protected override void OnInit() { }

    protected override void OnEnter() { }

    protected override void OnExit() { }

    protected override void OnFireStart() { }

    protected override void OnFire() {
        Vector3 pos = firePoint.position; pos.z = 0.0f;
        Vector3 dir = firePoint.right;
        projs[level].Fire(pos, dir);
    }

    protected override void OnFireStop() { }
}
