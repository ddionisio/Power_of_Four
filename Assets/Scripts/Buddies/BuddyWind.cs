using UnityEngine;
using System.Collections;

public class BuddyWind : Buddy {
    public string projType = "wind";

    public Transform eye;
    public float eyeAngleForward = -45f;
    public float eyeAngleUp = 45f;
    public float eyeAngleDown = -135f;
    public float eyeDelay = 0.3f;

    public AnimatorData readyAnim;

    public TransAnimSpinner fireSpinner;
    public float fireInactiveSpeed = -400f;
    public float fireActiveSpeed = -500f;

    private AnimatorData mAnim;
    private int mTakeEnter;
    private int mTakeExit;

    private float mCurProjAngleDir;

    private IEnumerator mEyeAction;
    private IEnumerator mFireReadyAction;

    protected override void OnInit() {
        mAnim = GetComponent<AnimatorData>();
        mTakeEnter = mAnim.GetTakeIndex("enter");
        mTakeExit = mAnim.GetTakeIndex("exit");
    }

    protected override void OnDeinit() {
    }

    protected override void OnEnter() {
        fireSpinner.rotatePerSecond.z = fireInactiveSpeed;
        eye.localEulerAngles = new Vector3(0, 0, GetEyeRot());

        if(mFireReadyAction == null)
            StartCoroutine(mFireReadyAction = DoFireSpinnerActive());

        mCurProjAngleDir = 1.0f;
    }

    protected override IEnumerator OnEntering() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        mAnim.Play(mTakeEnter);
        while(mAnim.isPlaying)
            yield return wait;
    }

    protected override void OnExit() {
        if(mEyeAction != null) { StopCoroutine(mEyeAction); mEyeAction = null; }
        if(mFireReadyAction != null) { StopCoroutine(mFireReadyAction); mFireReadyAction = null; }
    }

    protected override IEnumerator OnExiting() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        mAnim.Play(mTakeExit);
        while(mAnim.isPlaying)
            yield return wait;
    }

    protected override void OnDirChange() {
        if(mEyeAction == null) {
            StartCoroutine(mEyeAction = DoEyeRotate());
        }
    }

    protected override void OnFireStart() {
        fireSpinner.rotatePerSecond.z = fireActiveSpeed;

        readyAnim.gameObject.SetActive(false);
        if(mFireReadyAction != null) { StopCoroutine(mFireReadyAction); mFireReadyAction = null; }
    }

    void DoFire(bool applyElectricity) {
        ProjectileWave proj = Projectile.Create(projGrp, projType, firePos, fireDirWorld, null) as ProjectileWave;
        proj.angleInitialDir = mCurProjAngleDir;
        mCurProjAngleDir *= -1.0f;

        if(applyElectricity) {
            ElectrifyConductor ec = proj.GetComponent<ElectrifyConductor>();
            ec.Run();
        }
    }

    protected override void OnFire() {
        bool applyElectricity = level > 2;

        //fire stuff
        DoFire(applyElectricity);

        if(level > 1)
            DoFire(applyElectricity);
    }

    protected override void OnFireStop() {
        fireSpinner.rotatePerSecond.z = fireInactiveSpeed;

        ApplyFireActive();
    }

    float GetEyeRot() {
        switch(dir) {
            case Player.LookDir.Front:
                return eyeAngleForward;
            case Player.LookDir.Down:
                return eyeAngleDown;
            default:
                return eyeAngleUp;
        }
    }

    void ApplyFireActive() {
        if(Time.fixedTime - mLastFireTime < fireRate || !canFire) {
            if(mFireReadyAction == null)
                StartCoroutine(mFireReadyAction = DoFireSpinnerActive());
        }
        else
            readyAnim.gameObject.SetActive(true);
    }

    IEnumerator DoFireSpinnerActive() {
        readyAnim.gameObject.SetActive(false);

        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        while(Time.fixedTime - mLastFireTime < fireRate || !canFire)
            yield return wait;

        readyAnim.gameObject.SetActive(true);

        mFireReadyAction = null;
    }

    IEnumerator DoEyeRotate() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        float curTime = 0.0f;
        float eyeStartRot = eye.localEulerAngles.z;

        while(true) {
            yield return wait;

            curTime += Time.deltaTime;

            if(curTime < eyeDelay) {
                float t = Holoville.HOTween.Core.Easing.Sine.EaseOut(curTime, 0, 1, eyeDelay, 0, 0);
                eye.localEulerAngles = new Vector3(0, 0, Mathf.LerpAngle(eyeStartRot, GetEyeRot(), t));
            }
            else {
                eye.localEulerAngles = new Vector3(0, 0, GetEyeRot());
                break;
            }
        }

        mEyeAction = null;
    }
}
