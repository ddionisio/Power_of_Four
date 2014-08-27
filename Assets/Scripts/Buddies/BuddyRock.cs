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
        //int projInd = Mathf.Clamp(level - 1, 0, projs.Length - 1);
        //projs[projInd].Fire(firePos, fireDirWorld);

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
