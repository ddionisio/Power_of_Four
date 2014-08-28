using UnityEngine;
using System.Collections;

public class BuddyRock : Buddy {
    public string projType;
    public string projExplodeSuperType;
    public float projAngleRange;

    public override bool canFire { get { return mDoActiveAction == null; } }

    private AnimatorData mAnim;
    private int mTakeEnter;
    private int mTakeInactive;
    private int mTakeExit;

    private IEnumerator mDoActiveAction;

    protected override void OnInit() {
        mAnim = GetComponent<AnimatorData>();
        mTakeEnter = mAnim.GetTakeIndex("enter");
        mTakeInactive = mAnim.GetTakeIndex("inactive");
        mTakeExit = mAnim.GetTakeIndex("exit");
    }

    protected override void OnEnter() {
        if(mDoActiveAction != null) { StopCoroutine(mDoActiveAction); mDoActiveAction = null; }
        StartCoroutine(mDoActiveAction = DoFireActive());
    }

    protected override IEnumerator OnExiting() {
        if(mDoActiveAction != null) {
            StopCoroutine(mDoActiveAction);
            mDoActiveAction = null;
            yield break;
        }
        else {
            WaitForFixedUpdate wait = new WaitForFixedUpdate();

            mAnim.Play(mTakeExit);
            while(mAnim.isPlaying)
                yield return wait;
        }
    }

    protected override void OnFire() {
        Vector3 dir = Quaternion.Euler(0, 0, Random.Range(-projAngleRange, projAngleRange))*fireDirWorld;
        Projectile proj = Projectile.Create(projGrp, projType, firePos, dir, null);

        if(level > 1) {
            ProjectileSpawnOnDeath projDeath = proj.GetComponent<ProjectileSpawnOnDeath>();

            projDeath.alive = true;

            if(level > 2)
                projDeath.type = projExplodeSuperType;
        }

        if(mDoActiveAction != null)
            StopCoroutine(mDoActiveAction);

        StartCoroutine(mDoActiveAction = DoFireActive());
    }

    IEnumerator DoFireActive() {
        mAnim.Play(mTakeInactive);

        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        while(Time.fixedTime - mLastFireTime < fireRate)
            yield return wait;

        mAnim.Play(mTakeEnter);

        while(mAnim.isPlaying)
            yield return wait;

        mDoActiveAction = null;
    }
}
