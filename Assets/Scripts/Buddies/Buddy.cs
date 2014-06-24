using UnityEngine;
using System.Collections;

public abstract class Buddy : MonoBehaviour {
    public const string projGrp = "playerProj";
        
    public float fireRate;
    public LayerMask fireWallCheck;

    public float idleFaceXMin = 0.15f;
    public float idleFaceXDelay = 0.25f;

    public string takeEnter = "enter";
    public string takeExit = "exit";
    public string takeIdle = "idle";
    public string takeFiring = "fire";

    public string iconSpriteRef;
    public string labelTextRef;

    private int mTakeEnterInd;
    private int mTakeExitInd;
    private int mTakeIdleInd;
    private int mTakeFiringInd;

    private int mLevel = 1;

    private TransFollow mFollow;
    private AnimatorData mAnim;

    private IEnumerator mCurAct;

    private Transform mFollowPoint;
    private Transform mFirePoint;

    private bool mStarted = false;
    private bool mStartSetActivate = false;
    private bool mStartActivate = false;
    private bool mIsFiring = false;
    private float mLastFireTime;

    public int level {
        get { return mLevel; }
        set {
            if(mLevel != value) {
                mLevel = value;
                //stuff
            }
        }
    }

    public TransFollow follow { get { return mFollow; } }
    public AnimatorData anim { get { return mAnim; } }
    public bool isFiring { get { return mIsFiring; } }
    public Transform followPoint { get { return mFollowPoint; } set { mFollowPoint = value; } }
    public Transform firePoint { get { return mFirePoint; } set { mFirePoint = value; } }

    /// <summary>
    /// Check to see if we can fire based on player's location
    /// </summary>
    public bool canFire {
        get {
            if(fireWallCheck == 0) return true;

            Vector3 playerPos = Player.instance.transform.position;
            Vector3 firePos = firePoint.position; firePos.z = playerPos.z;
            Vector3 dir = firePos - playerPos;
            float d = dir.magnitude;
            if(d > 0.0f) {
                dir /= d;
                return !Physics.Raycast(playerPos, dir, d, fireWallCheck);
            }
            return false;
        }
    }

    /// <summary>
    /// Call with act=true to make this buddy your active weapon.  Call with act=false when switching buddy.
    /// </summary>
    /// <param name="act"></param>
    public void Activate(bool act) {
        if(mStarted) {
            if(act) {
                if(gameObject.activeSelf) {
                    if(!mIsFiring) {
                        if(mCurAct != null) { StopCoroutine(mCurAct); mCurAct = null; }
                        mIsFiring = false;

                        StartCoroutine(mCurAct = DoFireEnter());
                    }
                }
                else {
                    gameObject.SetActive(true);

                    StartCoroutine(mCurAct = DoFireEnter());
                }
            }
            else {
                if(gameObject.activeSelf) {
                    //cancel other actions
                    if(mCurAct != null) { StopCoroutine(mCurAct); mCurAct = null; }
                    mIsFiring = false;

                    StartCoroutine(mCurAct = DoFireExit());
                }
            }
        }
        else { //we haven't started, so call Activate during Start
            mStartSetActivate = true;
            mStartActivate = act;
        }
    }

    public void FireStart() {
        if(!mIsFiring && gameObject.activeSelf) {
            mIsFiring = true;

            //cancel other actions
            if(mCurAct != null) { StopCoroutine(mCurAct); mCurAct = null; }

            mFollow.target = null;

            if(mTakeFiringInd != -1)
                mAnim.Play(mTakeFiringInd);

            OnFireStart();

            //start
            StartCoroutine(mCurAct = DoFiring());
        }
    }

    public void FireStop(bool playIdle = true) {
        if(mIsFiring) {
            mIsFiring = false;

            if(mCurAct != null) { StopCoroutine(mCurAct); mCurAct = null; }

            if(playIdle && mTakeIdleInd != -1)
                mAnim.Play(mTakeIdleInd);

            mFollow.target = mFollowPoint;

            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;

            StartCoroutine(mCurAct = DoIdle());

            OnFireStop();
        }
    }

    void OnDisable() {
        if(mCurAct != null) { StopCoroutine(mCurAct); mCurAct = null; }

        mIsFiring = false;

        transform.localScale = Vector3.one;
        transform.rotation = Quaternion.identity;

        mLastFireTime = 0.0f;
    }

    void Awake() {
        mFollow = GetComponent<TransFollow>();
        mAnim = GetComponent<AnimatorData>();
    }

	// Use this for initialization
	void Start() {
        mStarted = true;

        //setup data
        mTakeEnterInd = mAnim.GetTakeIndex(takeEnter);
        mTakeExitInd = mAnim.GetTakeIndex(takeExit);
        mTakeIdleInd = mAnim.GetTakeIndex(takeIdle);
        mTakeFiringInd = mAnim.GetTakeIndex(takeFiring);

        OnInit();

        //activate
        if(mStartSetActivate) {
            Activate(mStartActivate);
            mStartSetActivate = false;
        }
        else
            gameObject.SetActive(false);
	}

    IEnumerator DoIdle() {
        WaitForSeconds wait = new WaitForSeconds(idleFaceXDelay);

        bool isMoving = false;

        while(true) {
            if(Mathf.Abs(mFollow.currentVelocity.x) >= idleFaceXMin) {
                Vector3 s = transform.localScale;
                s.x = Mathf.Sign(mFollow.currentVelocity.x);
                transform.localScale = s;

                isMoving = true;
            }
            else if(isMoving) {
                //face based on player's dir
                Vector3 s = transform.localScale;
                s.x = Player.instance.controllerAnim.isLeft ? -1 : 1;
                transform.localScale = s;

                isMoving = false;
            }

            yield return wait;
        }
    }

    IEnumerator DoFiring() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        while(true) {
            if(Time.fixedTime - mLastFireTime >= fireRate) {
                mLastFireTime = Time.fixedTime;

                if(canFire)
                    OnFire();
            }

            //keep our position to fire pos
            Vector3 pos = transform.position;
            Vector3 newPos = mFirePoint.position; newPos.z = pos.z;

            transform.localScale = mFirePoint.lossyScale;
            transform.rotation = mFirePoint.rotation;
            transform.position = newPos;

            yield return wait;
        }
    }

    IEnumerator DoFireEnter() {
        mFollow.target = mFollowPoint;
        mFollow.SnapToTarget();

        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        yield return wait;

        if(mTakeEnterInd != -1) {
            //make sure it's not looped!
            mAnim.Play(mTakeEnterInd);

            while(mAnim.isPlaying)
                yield return wait;
        }

        if(mTakeIdleInd != -1)
            mAnim.Play(mTakeIdleInd);

        StartCoroutine(mCurAct = DoIdle());

        OnEnter();
    }

    IEnumerator DoFireExit() {
        mFollow.target = null;

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
