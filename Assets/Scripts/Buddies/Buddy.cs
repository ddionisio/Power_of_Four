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

    public float fireRate;

    [SerializeField]
    Transform projPoint;

    public LevelInfo[] levelInfos;

    public event Callback activateCallback;
    public event Callback deactivateCallback;
    public event Callback levelChangeCallback;

    private int mLevel = 0;

    private IEnumerator mCurAct;

    private bool mStarted = false;
    private bool mStartSetActivate = false;
    private bool mIsFiring = false;
    private float mLastFireTime;

    private int mInd = -1;

    private Player.LookDir mDir;

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

    public virtual Player.LookDir dir {
        get { return mDir; }
    }

    public Vector3 firePos {
        get {
            Vector3 ret = projPoint.position; ret.z = 0.0f;
            return ret;
        }
    }

    public Vector3 fireDirLocal {
        get {
            Vector3 fireDir;
            if(mDir == Player.LookDir.Front) {
                fireDir = Mathf.Sign(projPoint.lossyScale.x) < 0.0f ? Vector3.left : Vector3.right;
            }
            else if(mDir == Player.LookDir.Down)
                fireDir = Vector3.down;
            else
                fireDir = Vector3.up;
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

    void OnDisable() {
        if(mCurAct != null) { StopCoroutine(mCurAct); mCurAct = null; }

        mIsFiring = false;

        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;
    }

    void OnDestroy() {
        activateCallback = null;
        deactivateCallback = null;
        levelChangeCallback = null;
    }

    void Awake() {
        //determine index
        Player player = Player.instance;
        for(int i = 0; i < player.buddies.Length; i++) {
            if(player.buddies[i] == this) {
                mInd = i;
                break;
            }
        }

        player.lookDirChangedCallback += OnPlayerChangeDir;
    }

    // Use this for initialization
    void Start() {
        mStarted = true;

        //load level from save
        mLevel = PlayerSave.BuddyGetLevel(mInd);

        OnInit();

        //activate
        if(mStartSetActivate) {
            Activate();
            mStartSetActivate = false;
        }
        else
            gameObject.SetActive(false);
    }


    //Internal

    void OnPlayerChangeDir(Player player) {
        mDir = player.lookDir;
        OnDirChange();
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

        if(activateCallback != null)
            activateCallback(this);

        yield return StartCoroutine(OnEntering());

        mCurAct = null;

        OnEnter();
    }

    IEnumerator DoExit() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        yield return wait;

        if(deactivateCallback != null)
            deactivateCallback(this);

        yield return StartCoroutine(OnExiting());

        OnExit();

        mCurAct = null;

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
