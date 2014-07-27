using UnityEngine;
using System.Collections;

public abstract class Buddy : MonoBehaviour {
    public const string projGrp = "playerProj";

    public enum Dir {
        Front,
        Up,
        Down
    }

    public float fireRate;

    public float idleFaceXMin = 0.15f;
    public float idleFaceXDelay = 0.25f;

    public string takeEnter = "enter";
    public string takeExit = "exit";
    public string takeNormal = "normal";
    public string takeNormalUp = "normal_up";
    public string takeNormalDown = "normal_down";
    public string takeAttack = "attack";
    public string takeAttackUp = "attack_up";
    public string takeAttackDown = "attack_down";

    public Transform projPoint;

    public string iconSpriteRef;
    public string labelTextRef;

    private int mTakeEnterInd;
    private int mTakeExitInd;
    private int mTakeNormalInd;
    private int mTakeNormalUpInd;
    private int mTakeNormalDownInd;
    private int mTakeAttackInd;
    private int mTakeAttackUpInd;
    private int mTakeAttackDownInd;

    private int mLevel = 1;

    private AnimatorData mAnim;

    private IEnumerator mCurAct;

    private bool mStarted = false;
    private bool mStartSetActivate = false;
    private Transform mStartToParent;
    private bool mIsFiring = false;
    private float mLastFireTime;

    private Dir mDir;

    public int level {
        get { return mLevel; }
        set {
            if(mLevel != value) {
                mLevel = value;
                //stuff
            }
        }
    }

    public virtual Dir dir {
        get { return mDir; }
        set {
            //note: change this behaviour for tentacle, lock direction while charging
            if(mDir != value) {
                mDir = value;
                ApplyAnimation();
            }
        }
    }

    public AnimatorData anim { get { return mAnim; } }
    public bool isFiring { get { return mIsFiring; } }

    /// <summary>
    /// Check to see if we can fire
    /// </summary>
    public virtual bool canFire {
        get {
            return true;
        }
    }

    /// <summary>
    /// Call with act=true to make this buddy your active weapon.  Call with act=false when switching buddy.
    /// </summary>
    /// <param name="act"></param>
    public void Activate() {
        if(mStarted) {
            if(gameObject.activeSelf) {
                if(!mIsFiring) {
                    if(mCurAct != null) { StopCoroutine(mCurAct); mCurAct = null; }
                    mIsFiring = false;

                    StartCoroutine(mCurAct = DoEnter());
                }
            }
            else {
                gameObject.SetActive(true);

                StartCoroutine(mCurAct = DoEnter());
            }
        }
        else { //we haven't started, so call Activate during Start
            mStartSetActivate = true;
        }
    }

    public void Deactivate() {
        if(gameObject.activeSelf) {
            //cancel other actions
            if(mCurAct != null) { StopCoroutine(mCurAct); mCurAct = null; }
            mIsFiring = false;

            StartCoroutine(mCurAct = DoExit());
        }
    }

    public void FireStart() {
        if(!mIsFiring && gameObject.activeSelf) {
            mIsFiring = true;

            //cancel other actions
            if(mCurAct != null) { StopCoroutine(mCurAct); mCurAct = null; }

            ApplyAnimation();

            //start
            StartCoroutine(mCurAct = DoFiring());

            OnFireStart();
        }
    }

    public void FireStop() {
        if(mIsFiring) {
            mIsFiring = false;

            if(mCurAct != null) { StopCoroutine(mCurAct); mCurAct = null; }

            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;

            ApplyAnimation();

            OnFireStop();
        }
    }

    void OnDisable() {
        if(mCurAct != null) { StopCoroutine(mCurAct); mCurAct = null; }

        mIsFiring = false;

        transform.localScale = Vector3.one;
        transform.rotation = Quaternion.identity;
    }

    void Awake() {
        mAnim = GetComponent<AnimatorData>();
    }

    // Use this for initialization
    void Start() {
        mStarted = true;

        //setup data
        mTakeEnterInd = mAnim.GetTakeIndex(takeEnter);
        mTakeExitInd = mAnim.GetTakeIndex(takeExit);
        mTakeNormalInd = mAnim.GetTakeIndex(takeNormal);
        mTakeNormalUpInd = mAnim.GetTakeIndex(takeNormalUp);
        mTakeNormalDownInd = mAnim.GetTakeIndex(takeNormalDown);
        mTakeAttackInd = mAnim.GetTakeIndex(takeAttack);
        mTakeAttackUpInd = mAnim.GetTakeIndex(takeAttackUp);
        mTakeAttackDownInd = mAnim.GetTakeIndex(takeAttackDown);

        OnInit();

        //activate
        if(mStartSetActivate) {
            Activate();
            mStartSetActivate = false;
            mStartToParent = null;
        }
        else
            gameObject.SetActive(false);
    }

    IEnumerator DoFiring() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        while(true) {
            if(Time.fixedTime - mLastFireTime >= fireRate) {
                mLastFireTime = Time.fixedTime;

                if(canFire)
                    OnFire();
            }

            yield return wait;
        }
    }

    IEnumerator DoEnter() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        yield return wait;

        if(mTakeEnterInd != -1) {
            //make sure it's not looped!
            mAnim.Play(mTakeEnterInd);

            while(mAnim.isPlaying)
                yield return wait;
        }

        mCurAct = null;

        ApplyAnimation();

        OnEnter();
    }

    IEnumerator DoExit() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        yield return wait;

        if(mTakeExitInd != -1) {
            //make sure it's not looped!
            mAnim.Play(mTakeExitInd);

            while(mAnim.isPlaying)
                yield return wait;
        }

        OnExit();

        gameObject.SetActive(false);
    }

    //Internal

    void ApplyAnimation() {
        //change animation
        switch(mDir) {
            case Dir.Up:
                mAnim.Play(isFiring ? mTakeAttackUpInd : mTakeNormalUpInd);
                break;

            case Dir.Down:
                mAnim.Play(isFiring ? mTakeAttackDownInd : mTakeNormalDownInd);
                break;

            case Dir.Front:
                mAnim.Play(isFiring ? mTakeAttackInd : mTakeNormalInd);
                break;
        }
    }

    //Implements

    /// <summary>
    /// Called once for initialization
    /// </summary>
    protected virtual void OnInit() { }

    /// <summary>
    /// Called once we enter during activate
    /// </summary>
    protected virtual void OnEnter() { }

    /// <summary>
    /// Called once we exit during deactivate
    /// </summary>
    protected virtual void OnExit() { }

    /// <summary>
    /// Called when we are about to fire stuff
    /// </summary>
    protected virtual void OnFireStart() { }

    /// <summary>
    /// Called when ready to fire something
    /// </summary>
    protected virtual void OnFire() { }

    /// <summary>
    /// Called when we stop firing
    /// </summary>
    protected virtual void OnFireStop() { }
}
