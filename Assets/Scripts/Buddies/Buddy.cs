using UnityEngine;
using System.Collections;

public abstract class Buddy : MonoBehaviour {
        
    public float fireRate;

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

    private IEnumerator mFiring;
    private IEnumerator mFiringEnter;
    private IEnumerator mFiringExit;

    private Transform mFollowPoint;
    private Transform mFirePoint;

    private bool mStarted = false;
    private bool mStartSetActivate = false;
    private bool mStartActivate = false;

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
    public bool isFiring { get { return mFiring != null; } }
    public Transform followPoint { get { return mFollowPoint; } set { mFollowPoint = value; } }
    public Transform firePoint { get { return mFirePoint; } set { mFirePoint = value; } }
    
    /// <summary>
    /// Call with act=true to make this buddy your active weapon.  Call with act=false when switching buddy.
    /// </summary>
    /// <param name="act"></param>
    public void Activate(bool act) {
        if(mStarted) {
            if(act) {
                if(gameObject.activeSelf) {
                    if(mFiringExit != null) { StopCoroutine(mFiringExit); mFiringExit = null; }

                    //enter if we are not firing or already entering
                    if(mFiring == null && mFiringEnter == null) {
                        StartCoroutine(mFiringEnter = DoFireEnter());
                    }   
                }
                else {
                    gameObject.SetActive(true);

                    StartCoroutine(mFiringEnter = DoFireEnter());
                }
            }
            else {
                if(gameObject.activeSelf) {
                    //cancel other actions
                    if(mFiringEnter != null) { StopCoroutine(mFiringEnter); mFiringEnter = null; }
                    if(mFiring != null) { FireStop(false); }

                    StartCoroutine(mFiringExit = DoFireExit());
                }
            }
        }
        else { //we haven't started, so call Activate during Start
            mStartSetActivate = true;
            mStartActivate = act;
        }
    }

    public void FireStart() {
        if(gameObject.activeSelf) {
            //cancel other actions
            if(mFiringEnter != null) { StopCoroutine(mFiringEnter); mFiringEnter = null; }
            if(mFiringExit != null) { StopCoroutine(mFiringExit); mFiringExit = null; }

            mFollow.target = null;

            if(mTakeFiringInd != -1)
                mAnim.Play(mTakeFiringInd);

            OnFireStart();

            //start
            StartCoroutine(mFiring = DoFiring());
        }
    }

    public void FireStop(bool playIdle = true) {
        if(gameObject.activeSelf) {
            if(mFiring != null) { StopCoroutine(mFiring); mFiring = null; }

            if(playIdle && mTakeIdleInd != -1)
                mAnim.Play(mTakeIdleInd);

            mFollow.target = mFollowPoint;

            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;

            OnFireStop();
        }
    }

    void OnDisable() {
        if(mFiring != null) { StopCoroutine(mFiring); mFiring = null; }
        if(mFiringEnter != null) { StopCoroutine(mFiringEnter); mFiringEnter = null; }
        if(mFiringExit != null) { StopCoroutine(mFiringExit); mFiringExit = null; }

        transform.localScale = Vector3.one;
        transform.rotation = Quaternion.identity;
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

    IEnumerator DoFiring() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        float lastTime = Time.fixedTime;

        while(true) {
            if(Time.fixedTime - lastTime >= fireRate) {
                lastTime = Time.fixedTime;
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

        mFiringEnter = null;

        if(mTakeIdleInd != -1)
            mAnim.Play(mTakeIdleInd);

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

        mFiringExit = null;

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
