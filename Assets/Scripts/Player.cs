using UnityEngine;
using System.Collections;

public class Player : EntityBase {
    public const string savedBuddySelectedKey = "sb";
    public const string lastBuddySelectedKey = "lb"; //when going to the next level, stored in SceneState global

    public const string takeHurt = "hurt";
    public const string takeCharge = "charge";

    public const float inputDirThreshold = 0.5f;

    public enum LookDir {
        Invalid = -1,
        Front,
        Up,
        Down
    }

    public delegate void BuddyCallback(Player player, Buddy bud);
    public delegate void Callback(Player player);

    public bool startLocked;

    public float hurtForce = 15.0f;
    public float hurtDelay = 0.5f; //how long the hurt state lasts
    public float hurtInvulDelay = 0.5f;
    public float deathFinishDelay = 2.0f;
    public float slideForce;
    public float slideSpeedMax;
    public float slideDelay;
    public float slideHeight = 0.79f;
    public GameObject slideGOActive; //object to activate while sliding
    public LayerMask solidMask; //use for standing up, etc.

    public bool cameraPointStartAttached;

    public Buddy[] buddies;

    public Transform eyeOrbPoint;
    public Transform rotatePoint; //used for charging

    public Transform actionIconHolder;
    public string actionIconDefault = "generic";

    public LayerMask triggerSpecialMask;

    public float attacherDetachImpulse;

    public float groundSetPosDelay = 1.0f;

    public event BuddyCallback buddyUnlockCallback;
    public event Callback buddyChangedCallback;
    public event Callback lookDirChangedCallback;

    private static Player mInstance;
    private PlayerStats mStats;
    private PlatformerController mCtrl;
    private PlatformerAnimatorController mCtrlAnim;
    private Blinker mBlinker;
    private Rigidbody mBody;
    private float mDefaultCtrlMoveForce;
    private float mDefaultCtrlMoveMaxSpeed;
    private Vector3 mDefaultColliderCenter;
    private float mDefaultColliderHeight;
    private float mDefaultGravity;
    private CapsuleCollider mCapsuleColl;
    private bool mInputEnabled;
    private bool mSliding;
    private float mSlidingLastTime;
    private bool mHurtActive;
    private int mCurBuddyInd = -1;
    private int mPauseCounter;
    private bool mAllowPauseTime = true;
    private SpecialTrigger mSpecialTrigger; //trigger that can be activated by action
    private LookDir mCurLook = LookDir.Front;
    private GameObject[] mActionIcons;
    private int mCurActionIconInd = -1;
    private bool mUpIsPressed = false;
    private bool mSpawned;
    private TriggerAttacher mAttacher;
    private CameraAttachPlayer mCamAttach;

    private IEnumerator mGroundSetPosAction;
    private Vector3 mGroundLastValidPos;

    public static Player instance { get { return mInstance; } }

    public int currentBuddyIndex {
        get { return mCurBuddyInd; }
        set {
            if(mCurBuddyInd != value && (value == -1 || buddies[value].level > 0)) {
                int prevBuddyInd = mCurBuddyInd;
                mCurBuddyInd = value;

                //deactivate previous
                if(prevBuddyInd >= 0)
                    buddies[prevBuddyInd].Deactivate();

                //activate new one
                if(mCurBuddyInd >= 0)
                    buddies[mCurBuddyInd].Activate();

                if(buddyChangedCallback != null)
                    buddyChangedCallback(this);
            }
        }
    }

    public Buddy currentBuddy {
        get {
            if(mCurBuddyInd >= 0)
                return buddies[mCurBuddyInd];
            return null;
        }
    }

    public float controllerDefaultMaxSpeed {
        get { return mDefaultCtrlMoveMaxSpeed; }
    }

    public float controllerDefaultForce {
        get { return mDefaultCtrlMoveForce; }
    }

    public bool inputEnabled {
        get { return mInputEnabled; }
        set {
            if(mInputEnabled != value) {
                mInputEnabled = value;

                InputManager input = InputManager.instance;
                if(input) {
                    if(mInputEnabled) {
                        input.AddButtonCall(0, InputAction.Jump, OnInputJump);
                        input.AddButtonCall(0, InputAction.Primary, OnInputPrimary);
                        input.AddButtonCall(0, InputAction.Secondary, OnInputSecondary);
                        input.AddButtonCall(0, InputAction.Previous, OnInputPowerPrev);
                        input.AddButtonCall(0, InputAction.Next, OnInputPowerNext);
                    }
                    else {
                        input.RemoveButtonCall(0, InputAction.Jump, OnInputJump);
                        input.RemoveButtonCall(0, InputAction.Primary, OnInputPrimary);
                        input.RemoveButtonCall(0, InputAction.Secondary, OnInputSecondary);
                        input.RemoveButtonCall(0, InputAction.Previous, OnInputPowerPrev);
                        input.RemoveButtonCall(0, InputAction.Next, OnInputPowerNext);

                        mUpIsPressed = false;

                        lookDir = LookDir.Front;
                        if(mCamAttach) mCamAttach.lookDir = LookDir.Front;
                    }
                }

                mCtrl.inputEnabled = mInputEnabled;
            }
        }
    }

    public bool allowPauseTime {
        get { return mAllowPauseTime; }
        set {
            if(mAllowPauseTime != value) {
                mAllowPauseTime = value;

                if(!mAllowPauseTime && mPauseCounter > 0)
                    SceneManager.instance.Resume();
            }
        }
    }

    public PlatformerController controller { get { return mCtrl; } }

    public PlatformerAnimatorController controllerAnim { get { return mCtrlAnim; } }

    public PlayerStats stats { get { return mStats; } }

    public bool isSpawned { get { return mSpawned; } }

    public LookDir lookDir {
        get { return mCurLook; }
        set {
            if(mCurLook != value) {
                mCurLook = value;

                if(lookDirChangedCallback != null)
                    lookDirChangedCallback(this);
            }
        }
    }

    /// <summary>
    /// All saving happens here: player stats, current level, spawn point, collected orbs, etc.
    /// Only works if player instance is valid
    /// </summary>
    public void Save() {
        LevelController.instance.Save();

        mStats.SaveState();

        UserData.instance.SetInt(savedBuddySelectedKey, mCurBuddyInd);

        PlayerSave.SaveData();
                
        UserData.instance.Save();

        PlayerPrefs.Save();
                
        SceneState.instance.GlobalSnapshotDelete();
        UserData.instance.SnapshotDelete();

        //show pop-up if available
    }

    public void UnlockBuddy(int budInd) {
        buddies[budInd].level = 1;

        if(buddyUnlockCallback != null)
            buddyUnlockCallback(this, buddies[budInd]);

        //select new buddy
        currentBuddyIndex = budInd;
    }

    public void WarpToLastGroundPosition() {
        transform.position = mGroundLastValidPos;
    }

    protected override void StateChanged() {
        switch((EntityState)prevState) {
            case EntityState.Hurt:
                mHurtActive = false;
                break;

            case EntityState.Lock:
                inputEnabled = true;

                InputManager input = InputManager.instance;
                if(input) {
                    input.AddButtonCall(0, InputAction.MenuCancel, OnInputPause);
                }

                mStats.isInvul = false;

                mCtrl.moveSideLock = false;

                if(mCurActionIconInd != -1)
                    mActionIcons[mCurActionIconInd].SetActive(true);
                break;

            case EntityState.Charge:
                inputEnabled = true;

                mStats.isInvul = false;

                mCtrl.moveSideLock = false;
                mCtrl.lockDrag = false;
                mCtrl.wallStick = true;
                mCtrl.moveForce = mDefaultCtrlMoveForce;
                mCtrl.moveMaxSpeed = mDefaultCtrlMoveMaxSpeed;
                mCtrl.gravityController.gravity = mDefaultGravity;
                mCtrlAnim.StopOverrideClip();

                rotatePoint.localRotation = Quaternion.identity;
                break;
        }

        switch((EntityState)state) {
            case EntityState.Normal:
                inputEnabled = true;

                if(InputManager.instance.IsDown(0, InputAction.Primary)) {
                    if(currentBuddy)
                        currentBuddy.FireStart();
                }

                if(mGroundSetPosAction == null) { StartCoroutine(mGroundSetPosAction = DoGroundSetPos()); }
                break;

            case EntityState.Hurt:
                mBlinker.Blink(hurtInvulDelay);

                if(currentBuddy)
                    currentBuddy.FireStop();

                //attempt to end sliding
                SetSlide(false);
                if(!mSliding) {
                    inputEnabled = false;

                    //push slightly
                    StartCoroutine(DoHurtForce(mStats.lastDamageNormal));

                    //hurt delay
                    StartCoroutine(DoHurt());
                }
                else
                    state = (int)EntityState.Normal;
                break;

            case EntityState.Dead:
                UIModalManager.instance.ModalCloseAll();

                if(currentBuddy)
                    currentBuddy.FireStop();

                SetSlide(false);

                mCtrl.enabled = false;
                mBody.isKinematic = true;
                mBody.detectCollisions = false;
                collider.enabled = false;

                //disable all input
                inputEnabled = false;

                InputManager input = InputManager.instance;
                if(input) {
                    input.RemoveButtonCall(0, InputAction.MenuCancel, OnInputPause);
                }
                //

                //detach
                AttacherDetach();

                //spawn death thing

                StartCoroutine(DoDeathFinishDelay());
                break;

            case EntityState.Lock:
                UIModalManager.instance.ModalCloseAll();

                if(currentBuddy)
                    currentBuddy.FireStop();

                if(mCurActionIconInd != -1)
                    mActionIcons[mCurActionIconInd].SetActive(false);

                LockControls();
                break;

            case EntityState.Charge:
                LookDir lastLookDir = lookDir;

                LockControls(false);
                mCtrl.gravityController.gravity = 0.0f;
                mCtrl.lockDrag = true;
                mCtrl.wallStick = false;
                mCtrlAnim.PlayOverrideClip(takeCharge);

                lookDir = lastLookDir;
                if(mCamAttach) mCamAttach.lookDir = lastLookDir;

                switch(mCurLook) {
                    case LookDir.Up:
                        rotatePoint.localRotation = Quaternion.identity;
                        break;
                    case LookDir.Front:
                        rotatePoint.localRotation = Quaternion.Euler(0, 0, -90);
                        break;
                    case LookDir.Down:
                        rotatePoint.localRotation = Quaternion.Euler(0, 0, 180);
                        break;
                }
                break;

            case EntityState.Victory:
                UIModalManager.instance.ModalCloseAll();

                if(currentBuddy)
                    currentBuddy.FireStop();

                currentBuddyIndex = -1;
                LockControls();
                //mCtrlSpr.PlayOverrideClip("victory");
                break;

            case EntityState.Final:
                UIModalManager.instance.ModalCloseAll();

                if(currentBuddy)
                    currentBuddy.FireStop();

                currentBuddyIndex = -1;
                LockControls();

                //save?
                break;

            case EntityState.Exit:
                UIModalManager.instance.ModalCloseAll();

                if(currentBuddy)
                    currentBuddy.FireStop();

                currentBuddyIndex = -1;
                LockControls();
                break;

            case EntityState.Invalid:
                AttacherDetach();
                mSpawned = false;
                inputEnabled = false;
                mUpIsPressed = false;
                SetActionIcon(-1);
                break;
        }
    }

    void LockControls(bool includePause = true) {
        SetSlide(false);

        //disable all input
        inputEnabled = false;

        if(includePause) {
            InputManager input = InputManager.instance;
            if(input) {
                input.RemoveButtonCall(0, InputAction.MenuCancel, OnInputPause);
            }
        }
        //

        mBlinker.Stop();
        mStats.isInvul = true;

        mCtrl.moveSideLock = true;
        mCtrl.moveSide = 0.0f;

        mBody.velocity = Vector3.zero;
        //mCtrl.ResetCollision();
    }

    void OnBlinkActive(bool blink) {
        mStats.isInvul = blink;
    }

    protected override void OnDespawned() {
        //reset stuff here
        state = (int)EntityState.Invalid;

        base.OnDespawned();
    }

    protected override void OnDestroy() {
        mInstance = null;

        if(UIModalManager.instance)
            UIModalManager.instance.activeCallback -= OnUIModalActive;

        if(SceneManager.instance)
            SceneManager.instance.sceneChangeCallback -= OnSceneChange;

        //dealloc here
        inputEnabled = false;

        InputManager input = InputManager.instance;
        if(input) {
            input.RemoveButtonCall(0, InputAction.MenuCancel, OnInputPause);
        }

        buddyUnlockCallback = null;
        buddyChangedCallback = null;
        lookDirChangedCallback = null;

        base.OnDestroy();
    }

    public override void SpawnFinish() {
        mSpawned = true;

        state = (int)(startLocked ? EntityState.Lock : EntityState.Normal);

        if(SceneState.instance.GetGlobalValue("cheat") > 0) {
            stats.damageReduction = 1.0f;
        }

        //set buddy selected
        int buddyIndex;
        if(SceneState.instance.HasGlobalValue(lastBuddySelectedKey)) {
            buddyIndex = SceneState.instance.GetGlobalValue(lastBuddySelectedKey, 0);
            SceneState.instance.DeleteGlobalValue(lastBuddySelectedKey, false);
        }
        else {
            buddyIndex = UserData.instance.GetInt(savedBuddySelectedKey, 0);
        }
        currentBuddyIndex = buddyIndex;
    }

    protected override void SpawnStart() {
        //initialize some things

        //start ai, player control, etc
        //StartCoroutine(DoCameraPointWallCheck());
    }

    protected override void Awake() {
        mInstance = this;

        base.Awake();

        mBody = rigidbody;

        //setup callbacks
        UIModalManager.instance.activeCallback += OnUIModalActive;
        SceneManager.instance.sceneChangeCallback += OnSceneChange;

        //initialize variables
        InputManager.instance.AddButtonCall(0, InputAction.MenuCancel, OnInputPause);

        mCtrl = GetComponent<PlatformerController>();
        mCtrl.moveInputX = InputAction.MoveX;
        mCtrl.moveInputY = InputAction.MoveY;
        mCtrl.collisionEnterCallback += OnRigidbodyCollisionEnter;
        mCtrl.landCallback += OnLand;
        mCtrl.jumpCallback += OnJump;

        mDefaultCtrlMoveMaxSpeed = mCtrl.moveMaxSpeed;
        mDefaultCtrlMoveForce = mCtrl.moveForce;

        mDefaultGravity = mCtrl.gravityController.gravity;

        mCtrlAnim = GetComponent<PlatformerAnimatorController>();

        mCapsuleColl = collider as CapsuleCollider;
        mDefaultColliderCenter = mCapsuleColl.center;
        mDefaultColliderHeight = mCapsuleColl.height;

        mStats = GetComponent<PlayerStats>();
        mStats.changeHPCallback += OnStatsHPChange;

        mBlinker = GetComponent<Blinker>();
        mBlinker.activeCallback += OnBlinkActive;

        mActionIcons = new GameObject[actionIconHolder.childCount];
        for(int i = 0; i < mActionIcons.Length; i++) {
            mActionIcons[i] = actionIconHolder.GetChild(i).gameObject;
            mActionIcons[i].SetActive(false);
        }

        for(int i = 0; i < buddies.Length; i++) {
            buddies[i].Init(this, i);
            buddies[i].gameObject.SetActive(false);
        }

        mCamAttach = GetComponent<CameraAttachPlayer>();
    }

    // Use this for initialization
    protected override void Start() {
        base.Start();

        //set player's starting location based on saved spawn point, if there is one.
        Transform spawnPt = LevelController.GetSpawnPoint();
        if(spawnPt) {
            mGroundLastValidPos = spawnPt.position + spawnPt.up*collider.bounds.extents.y;
            transform.position = mGroundLastValidPos;
            transform.rotation = spawnPt.rotation;
        }

        if(cameraPointStartAttached) {
            CameraController.instance.attach = GetComponent<CameraAttach>();
        }
    }

    void OnTriggerEnter(Collider col) {
        if(IsSpecialTrigger(col)) {
            mSpecialTrigger = col.GetComponent<SpecialTrigger>();

            if(mSpecialTrigger && mSpecialTrigger.enabled) {
                //set icon
                string iconRef = mSpecialTrigger && !string.IsNullOrEmpty(mSpecialTrigger.iconRef) ? mSpecialTrigger.iconRef : actionIconDefault;
                if(!string.IsNullOrEmpty(iconRef)) {
                    SetActionIcon(-1);
                    for(int i = 0; i < mActionIcons.Length; i++) {
                        if(mActionIcons[i].name == iconRef) {
                            SetActionIcon(i);
                            break;
                        }
                    }
                }
            }
            else {
                mSpecialTrigger = null;
            }
        }
    }

    void OnTriggerExit(Collider col) {
        if(IsSpecialTrigger(col)) {
            if(mSpecialTrigger && col == mSpecialTrigger.collider) {
                mSpecialTrigger = null;
                SetActionIcon(-1);
            }
        }
    }

    void Update() {
        InputManager input = InputManager.instance;

        if(mSliding) {
            float inpX = input.GetAxis(0, InputAction.MoveX);
            if(inpX < -0.1f)
                mCtrl.moveSide = -1.0f;
            else if(inpX > 0.1f)
                mCtrl.moveSide = 1.0f;

            //barf some particles

            if(Time.time - mSlidingLastTime >= slideDelay) {
                SetSlide(false);
            }
        }
        else if(mInputEnabled) {
            float inpY = input.GetAxis(0, InputAction.MoveY);

            if(inpY < -inputDirThreshold) {
                lookDir = mCtrl.isGrounded ? LookDir.Front : LookDir.Down;
                if(mCamAttach) mCamAttach.lookDir = mSliding ? LookDir.Front : LookDir.Down;

                mUpIsPressed = false;
            }
            else if(inpY > inputDirThreshold) {
                lookDir = LookDir.Up;
                if(mCamAttach) mCamAttach.lookDir = LookDir.Up;

                //check for pressed
                if(!mUpIsPressed) {
                    UpPressed();
                    mUpIsPressed = true;
                }
            }
            else {
                lookDir = LookDir.Front;
                if(mCamAttach) mCamAttach.lookDir = LookDir.Front;

                mUpIsPressed = false;
            }
        }

        //TODO: mSpecialTrigger, check if can interact, then enable overlap 'no sign'
    }

    #region Stats/Weapons

    void OnStatsHPChange(Stats stat, float delta) {
        if(delta < 0.0f) {
            if(stat.curHP <= 0.0f) {
                state = (int)EntityState.Dead;
            }
            else {
                state = (int)EntityState.Hurt;
            }

            //HUD.instance.barHP.current = Mathf.CeilToInt(stat.curHP);
        }
        else {
            //healed
            //if(!HUD.instance.barHP.isAnimating)
            //Pause(true);

            //HUD.instance.barHP.currentSmooth = Mathf.CeilToInt(stat.curHP);
        }
    }

    #endregion

    #region Anim
    #endregion

    #region Input

    void OnInputPrimary(InputManager.Info dat) {
        if(dat.state == InputManager.State.Pressed) {
            if(currentBuddy)
                currentBuddy.FireStart();
        }
        else if(dat.state == InputManager.State.Released) {
            if(currentBuddy)
                currentBuddy.FireStop();
        }
    }

    void OnInputSecondary(InputManager.Info dat) {
        if(dat.state == InputManager.State.Pressed) {
            if(!mSliding) {
                if(mCtrl.isGrounded)
                    SetSlide(true);
            }
        }
    }

    void OnInputPowerNext(InputManager.Info dat) {
        if(dat.state == InputManager.State.Pressed) {
            for(int i = 0, max = buddies.Length, toBuddyInd = currentBuddyIndex + 1; i < max; i++, toBuddyInd++) {
                if(toBuddyInd >= buddies.Length)
                    toBuddyInd = 0;

                if(buddies[toBuddyInd] && buddies[toBuddyInd].level > 0) {
                    currentBuddyIndex = toBuddyInd;
                    break;
                }
            }
        }
    }

    void OnInputPowerPrev(InputManager.Info dat) {
        if(dat.state == InputManager.State.Pressed) {
            for(int i = 0, max = buddies.Length, toBuddyInd = currentBuddyIndex - 1; i < max; i++, toBuddyInd--) {
                if(toBuddyInd < 0)
                    toBuddyInd = buddies.Length - 1;

                if(buddies[toBuddyInd] && buddies[toBuddyInd].level > 0) {
                    currentBuddyIndex = toBuddyInd;
                    break;
                }
            }
        }
    }

    void OnInputJump(InputManager.Info dat) {
        if(dat.state == InputManager.State.Pressed) {
            InputManager input = InputManager.instance;

            if(mAttacher) {
                AttacherDetach();
                rigidbody.AddForce(rigidbody.velocity.normalized*attacherDetachImpulse, ForceMode.Impulse);
            }
            else if(!mSliding) {
                if(input.GetAxis(0, InputAction.MoveY) <= -inputDirThreshold && mCtrl.isGrounded) {
                    //Weapon curWpn = weapons[mCurWeaponInd];
                    //if(!curWpn.isFireActive || curWpn.allowSlide)
                    SetSlide(true);
                }
                else {
                    mCtrl.Jump(true);
                    if(mCtrl.isJumpWall) {
                        //Vector2 p = mCtrlSpr.wallStickParticle.transform.position;
                        //PoolController.Spawn("fxp", "wallSpark", "wallSpark", null, p);
                        //sfxWallJump.Play();
                    }
                }
            }
            else {
                if(input.GetAxis(0, InputAction.MoveY) > -inputDirThreshold) {
                    //if we can stop sliding, then jump
                    SetSlide(false, false);
                    if(!mSliding) {
                        mCtrl.Jump(true);
                    }
                }
            }
        }
        else if(dat.state == InputManager.State.Released) {
            mCtrl.Jump(false);
        }
    }

    void OnInputSlide(InputManager.Info dat) {
        if(dat.state == InputManager.State.Pressed) {
            if(!mSliding && mCtrl.isGrounded) {
                SetSlide(true);
            }
        }
    }

    void OnInputPause(InputManager.Info dat) {
        if(dat.state == InputManager.State.Pressed) {
            if(state != (int)EntityState.Dead && state != (int)EntityState.Victory && state != (int)EntityState.Final) {
                if(UIModalManager.instance.activeCount == 0 && !UIModalManager.instance.ModalIsInStack("pause")) {
                    UIModalManager.instance.ModalOpen("pause");
                }
            }
        }
    }

    void UpPressed() {
        if(mSpecialTrigger) {
            if(currentBuddy)
                currentBuddy.FireStop();

            if(mSpecialTrigger && mSpecialTrigger.enabled && !mSpecialTrigger.isActing && mSpecialTrigger.interactive)
                mSpecialTrigger.Action(OnSpecialTriggerActFinish);
        }
    }

    #endregion

    void OnSuddenDeath() {
        stats.curHP = 0;
    }

    void OnUIModalActive(bool a) {
        Pause(a);
    }

    //set to -1 to disable
    void SetActionIcon(int ind) {
        if(mCurActionIconInd != -1)
            mActionIcons[mCurActionIconInd].SetActive(false);

        if(ind != -1) {
            mActionIcons[ind].SetActive(true);
        }

        mCurActionIconInd = ind;
    }

    IEnumerator DoHurtForce(Vector3 normal) {
        mHurtActive = true;

        mCtrl.enabled = false;
        mBody.velocity = Vector3.zero;
        mBody.drag = 0.0f;

        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        normal.x = Mathf.Sign(normal.x);
        normal.y = 0.0f;
        normal.z = 0.0f;

        while(mHurtActive) {
            yield return wait;

            mBody.AddForce(normal * hurtForce);
        }

        mCtrl.enabled = true;
        mCtrl.ResetCollision();

        mHurtActive = false;
    }

    IEnumerator DoHurt() {
        //hurtin'
        mCtrlAnim.PlayOverrideClip(takeHurt);

        yield return new WaitForSeconds(hurtDelay);

        if(mCtrlAnim.overrideTakeName == takeHurt)
            mCtrlAnim.StopOverrideClip();

        //done hurtin'
        if(state == (int)EntityState.Hurt)
            state = (int)EntityState.Normal;
    }

    public void Pause(bool pause) {
        if(pause) {
            mPauseCounter++;
            if(mPauseCounter == 1) {
                if(mAllowPauseTime)
                    SceneManager.instance.Pause();

                inputEnabled = false;

                InputManager.instance.RemoveButtonCall(0, InputAction.MenuCancel, OnInputPause);
            }
        }
        else {
            mPauseCounter--;
            if(mPauseCounter == 0) {
                if(mAllowPauseTime)
                    SceneManager.instance.Resume();

                if(state != (int)EntityState.Lock && state != (int)EntityState.Invalid) {
                    inputEnabled = true;

                    InputManager.instance.AddButtonCall(0, InputAction.MenuCancel, OnInputPause);
                }
            }
        }
    }

    void SetSlide(bool slide, bool clearVelocity = true) {
        if(mSliding != slide) {
            mSliding = slide;

            if(mSliding) {
                mSlidingLastTime = Time.time;

                mCapsuleColl.height = slideHeight;
                mCapsuleColl.center = new Vector3(mDefaultColliderCenter.x, mDefaultColliderCenter.y - (mDefaultColliderHeight - slideHeight) * 0.5f, mDefaultColliderCenter.z);

                mCtrl.moveMaxSpeed = slideSpeedMax;
                mCtrl.moveForce = slideForce;
                mCtrl.moveSideLock = true;
                mCtrl.moveSide = mCtrlAnim.isLeft ? -1.0f : 1.0f;

                mCtrlAnim.state = PlatformerAnimatorController.State.Slide;

                if(slideGOActive) slideGOActive.SetActive(true);

                if(mCamAttach) mCamAttach.lookDir = LookDir.Front;

                //sfxSlide.Play();
            }
            else {
                //cannot set to false if we can't stand
                if(CanStand()) {
                    //revert
                    mCapsuleColl.height = mDefaultColliderHeight;
                    mCapsuleColl.center = mDefaultColliderCenter;

                    mCtrl.moveMaxSpeed = mDefaultCtrlMoveMaxSpeed;
                    mCtrl.moveSideLock = false;
                    mCtrl.moveForce = mDefaultCtrlMoveForce;

                    if(clearVelocity) {
                        mCtrl.moveSide = 0.0f;

                        if(!mBody.isKinematic) {
                            Vector3 v = mBody.velocity; v.x = 0.0f; v.z = 0.0f;
                            mBody.velocity = v;
                        }
                    }
                    else {
                        //limit x velocity
                        Vector3 v = mBody.velocity;
                        if(Mathf.Abs(v.x) > 12.0f) {
                            v.x = Mathf.Sign(v.x) * 12.0f;
                            mBody.velocity = v;
                        }
                    }

                    mCtrlAnim.state = PlatformerAnimatorController.State.None;

                    //Vector3 pos = transform.position;
                    //pos.y += (mDefaultColliderHeight - slideHeight) * 0.5f - 0.1f;
                    //transform.position = pos;

                    //slideParticle.Stop();
                    //slideParticle.Clear();

                    if(slideGOActive) slideGOActive.SetActive(false);
                }
                else {
                    mSliding = true;
                }
            }
        }
    }

    bool CanStand() {
        const float ofs = 0.2f;

        float r = mCapsuleColl.radius - mCapsuleColl.radius*0.25f;

        Vector3 c = transform.position + mDefaultColliderCenter;
        Vector3 u = new Vector3(c.x, c.y + (mDefaultColliderHeight * 0.5f - mCapsuleColl.radius) + ofs, c.z);
        Vector3 d = new Vector3(c.x, (c.y - (mDefaultColliderHeight * 0.5f - mCapsuleColl.radius)) + ofs, c.z);

        return !Physics.CheckCapsule(u, d, r, solidMask);
    }

    bool IsSpecialTrigger(Component comp) {
        return (triggerSpecialMask & (1<<comp.gameObject.layer)) != 0;
    }

    void AttacherDetach() {
        if(mAttacher) {
            mAttacher.Detach();
            mAttacher = null;
        }
    }

    void OnSceneChange(string nextScene) {
        //save stuff

    }

    IEnumerator DoDeathFinishDelay() {
        yield return new WaitForSeconds(deathFinishDelay);

        SceneState.instance.GlobalSnapshotRestore();
        UserData.instance.SnapshotRestore();
        SceneManager.instance.Reload();
    }

    void OnRigidbodyCollisionEnter(RigidBodyController controller, Collision col) {
    }

    void OnJump(PlatformerController ctrl) {
    }

    void OnLand(PlatformerController ctrl) {
        if(state != (int)EntityState.Invalid) {
            //effects

            RigidBodyController.CollideInfo inf = mCtrl.GetCollideInfo(CollisionFlags.Below);
            if(inf != null && inf.collider.rigidbody == null)
                mGroundLastValidPos = transform.position;
        }
    }

    void OnSpecialTriggerActFinish() {
        if(mSpecialTrigger) {
            if(!mSpecialTrigger.enabled || !mSpecialTrigger.interactive) {
                mSpecialTrigger = null;
                SetActionIcon(-1);
            }
        }
        else {
            SetActionIcon(-1);
        }
    }

    void OnTriggerAttacherAttach(TriggerAttacher attacher) {
        mAttacher = attacher;
    }

    IEnumerator DoGroundSetPos() {
        WaitForSeconds wait = new WaitForSeconds(groundSetPosDelay);
        while(state == (int)EntityState.Normal) {
            if(mCtrl.isGrounded) {
                //check ground collision and make sure it doesn't have a body
                RigidBodyController.CollideInfo inf = mCtrl.GetCollideInfo(CollisionFlags.Below);
                if(inf != null && inf.collider.rigidbody == null)
                    mGroundLastValidPos = transform.position;
            }

            yield return wait;
        }

        mGroundSetPosAction = null;
    }
}
