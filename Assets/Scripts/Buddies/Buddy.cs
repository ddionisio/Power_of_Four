using UnityEngine;
using System.Collections;

public abstract class Buddy : MonoBehaviour {
    public const string projGrp = "playerProj";

    [System.Serializable]
    public struct LevelInfo {
        public string iconSpriteRef;
        public string labelTextRef;
    }

    public delegate void Callback(Buddy bud);

    [SerializeField]
    float fireRate;

    [SerializeField]
    protected Transform projPoint;

    public LevelInfo[] levelInfos;

    public event Callback activateCallback;
    public event Callback deactivateCallback;
    public event Callback levelChangeCallback;

    /// <summary>
    /// This is in fixed time when last call to OnFire occurred. (can be overriden by certain buddies)
    /// </summary>
    protected float mLastFireTime;

    private int mLevel = 0;

    private IEnumerator mCurAct;

    private bool mIsFiring = false;
    
    private int mInd = -1;

    /// <summary>
    /// Get Level, 0 == locked. Subtract 1 when using as index.
    /// </summary>
    public int level {
        get { return mLevel; }
        set {
            if(mLevel != value) {
                mLevel = value;

                if(mInd >= 0)
                    PlayerSave.BuddySetLevel(mInd, mLevel);

                if(levelChangeCallback != null)
                    levelChangeCallback(this);
            }
        }
    }

    public int index { get { return mInd; } }

    public virtual Player.LookDir dir { get { return Player.instance ? Player.instance.lookDir : Player.LookDir.Invalid; } }

    public virtual float currentFireRate { get { return fireRate; } }

    public Vector3 firePos {
        get {
            Vector3 ret = projPoint.position; ret.z = 0.0f;
            return ret;
        }
    }

    public Vector3 fireDirLocal {
        get {
            Vector3 fireDir;
            switch(dir) {
                case Player.LookDir.Front:
                    fireDir = Mathf.Sign(projPoint ? projPoint.lossyScale.x : transform.lossyScale.x) < 0.0f ? Vector3.left : Vector3.right;
                    break;
                case Player.LookDir.Down:
                    fireDir = Vector3.down;
                    break;
                default:
                    fireDir = Vector3.up;
                    break;
            }
            return fireDir;
        }
    }

    public Vector3 fireDirWorld {
        get {
            return transform.rotation*fireDirLocal;
        }
    }

    public bool isActive { get { return gameObject.activeSelf; } }
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

            OnFireStart();

            //start
            StartCoroutine(mCurAct = DoFiring());
        }
    }

    public void FireStop() {
        if(mIsFiring) {
            mIsFiring = false;

            if(mCurAct != null) { StopCoroutine(mCurAct); mCurAct = null; }

            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;

            OnFireStop();
        }
    }

    public void Init(Player player, int index) {
        mInd = index;

        //load level from save
        mLevel = PlayerSave.BuddyGetLevel(mInd);
                
        OnInit();
    }

    void OnDisable() {
        if(mCurAct != null) { StopCoroutine(mCurAct); mCurAct = null; }

        mIsFiring = false;

        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;
    }

    void OnDestroy() {
        OnDeinit();

        activateCallback = null;
        deactivateCallback = null;
        levelChangeCallback = null;
    }

    //Internal

    void OnPlayerChangeDir(Player player) {
        OnDirChange();
    }

    IEnumerator DoFiring() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        while(true) {
            if(Time.fixedTime - mLastFireTime >= currentFireRate && canFire) {
                mLastFireTime = Time.fixedTime;
                OnFire();
            }

            yield return wait;
        }
    }

    IEnumerator DoEnter() {
        yield return new WaitForFixedUpdate();

        if(activateCallback != null)
            activateCallback(this);

        OnEnter();

        yield return StartCoroutine(mCurAct = OnEntering());
                
        mCurAct = null;

        Player.instance.lookDirChangedCallback += OnPlayerChangeDir;
    }

    IEnumerator DoExit() {
        Player.instance.lookDirChangedCallback -= OnPlayerChangeDir;

        if(deactivateCallback != null)
            deactivateCallback(this);

        OnExit();

        yield return StartCoroutine(mCurAct = OnExiting());

        mCurAct = null;

        gameObject.SetActive(false);
    }


    //Implements

    /// <summary>
    /// Called once for initialization
    /// </summary>
    protected virtual void OnInit() { }

    protected virtual void OnDeinit() { }

    /// <summary>
    /// Called once we enter during activate
    /// </summary>
    protected virtual void OnEnter() { }

    protected virtual IEnumerator OnEntering() { yield break; }

    /// <summary>
    /// Called once we exit during deactivate
    /// </summary>
    protected virtual void OnExit() { }

    protected virtual IEnumerator OnExiting() { yield break; }

    protected virtual void OnDirChange() { }

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
