using UnityEngine;
using System.Collections;

public class BuddyRock : Buddy {
    [System.Serializable]
    public class ProjData {
        public string type;
        public float angleRange;

        public Projectile Fire(Vector3 pos, Vector3 dir) {
            dir = Quaternion.Euler(0, 0, Random.Range(-angleRange, angleRange))*dir;

            Projectile proj = Projectile.Create(projGrp, type, pos, dir, null);

            return proj;
        }
    }

    public ProjData[] projs; //based on level

    protected override void OnInit() { }

    protected override void OnEnter() { }

    protected override void OnExit() { }

    protected override void OnFireStart() { }

    protected override void OnFire() {
        projs[level - 1].Fire(firePos, fireDirWorld);
    }

    protected override void OnFireStop() { }
}
