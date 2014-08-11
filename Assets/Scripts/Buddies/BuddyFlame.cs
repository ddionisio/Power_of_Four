﻿using UnityEngine;
using System.Collections;

public class BuddyFlame : Buddy {
    [System.Serializable]
    public class ProjData {
        public string type;
        public float angleRange;
        public Vector2 speedRange;
        public Vector2 decayRange;
        public float yRange;

        public Projectile Fire(Vector3 pos, Vector3 dir) {
            Vector3 ofs = Vector3.Cross(dir, Vector3.forward)*Random.Range(-yRange, yRange);

            dir = Quaternion.Euler(0, 0, Random.Range(-angleRange, angleRange))*dir;

            Projectile proj = Projectile.Create(projGrp, type, pos + ofs, dir, null);

            proj.startVelocity = speedRange.x;
            proj.startVelocityAddRand = speedRange.y - speedRange.x;
            proj.decayDelay = Random.Range(decayRange.x, decayRange.y);

            return proj;
        }
    }

    public ProjData[] projs; //based on level

    protected override void OnInit() { }

    protected override void OnEnter() { }

    protected override void OnExit() { }

    protected override void OnFireStart() { }

    protected override void OnFire() {
        Vector3 pos = projPoint.position; pos.z = 0.0f;

        Vector3 fireDir;
        if(dir == Dir.Front) {
            fireDir = projPoint.lossyScale.x < 0.0f ? Vector3.left : Vector3.right;
        }
        else if(dir == Dir.Down)
            fireDir = Vector3.down;
        else
            fireDir = Vector3.up;

        projs[level - 1].Fire(pos, transform.rotation*fireDir);
    }

    protected override void OnFireStop() { }
}
