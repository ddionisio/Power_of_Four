using UnityEngine;
using System.Collections;

public class Player : EntityBase {
    public const string savedBuddySelectedKey = "sb";
    public const string lastBuddySelectedKey = "lb"; //when going to the next level, stored in SceneState global

    public const string takeHurt = "hurt";

    public const float inputDirThreshold = 0.5f;

    public enum LookDir {
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

    public Transform look;

    public Transform cameraPoint;
    public float cameraPointWallCheckDelay = 0.2f;
    public float cameraPointRevertDelay = 2.0f;
    public bool cameraPointStartAttached;
    public float cameraPointFallOfs = 2.0f;

    public float cameraSpeedNorm = 10.0f;
    public float cameraSpeedMinScale = 0.5f;
    public float cameraSpeedMaxScale = 1.0f;

    public Buddy[] buddies;

    public Transform eyeOrbPoint;

    public Transform actionIconHolder;
    public string actionIconDefault = "generic";

    public LayerMask triggerSpecialMask;

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
    private Vector3 mDefaultCameraPointLPos;
    private CapsuleCollider mCapsuleColl;
    private bool mInputEnabled;
    private bool mSliding;
    private float mSlidingLastTime;
    private bool mHurtActive;
    private int mCurBuddyInd = -1;
    private int mPauseCounter;
    private bool mAllowPauseTime = true;
    private PlayMakerFSM mSpecialTriggerFSM;
    private SpecialTrigger mSpecialTrigger; //trigger that can be activated by action
    private LookDir mCurLook = LookDir.Front;
    private GameObject[] mActionIcons;
    private int mCurActionIconInd = -1;
    private bool mUpIsPressed = false;
    private bool mSpawned;
    private bool mCameraIsWallStick;

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
                if(mCurBuddyInd >= 0) {
                    buddies[mCurBuddyInd].Activate();

                    //change hud elements
                }
                else {
                    //remove hud elements
                }

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
                if(look) {
                    switch(mCurLook) {
                        case LookDir.Front:
                            look.localRotation = Quaternion.identity;
                            break;
                        case LookDir.Up:
                            look.localRotation = Quaternion.Euler(0, 0, 90);
                            break;
                        case LookDir.Down:
                            look.localRotation = Quaternion.Euler(0, 0, -90);
                            break;
                    }
                }

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

        PlayerSave.SaveData();

        UserData.instance.Save();

        PlayerPrefs.Save();

        UserData.instance.SetInt(savedBuddySelectedKey, mCurBuddyInd);

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
        }

        switch((EntityState)state) {
            case EntityState.Normal:
                inputEnabled = true;
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
                mSpawned = false;
                inputEnabled = false;
                mUpIsPressed = false;
                SetActionIcon(-1);
                mCameraIsWallStick = false;
                break;
        }
    }

    void LockControls() {
        SetSlide(false);

        //disable all input
        inputEnabled = false;

        InputManager input = InputManager.instance;
        if(input) {
            input.RemoveButtonCall(0, InputAction.MenuCancel, OnInputPause);
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
        StartCoroutine(DoCameraPointWallCheck());
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

        mDefaultCtrlMoveMaxSpeed = mCtrl.moveMaxSpeed;
        mDefaultCtrlMoveForce = mCtrl.moveForce;

        mCtrlAnim = GetComponent<PlatformerAnimatorController>();

        mCapsuleColl = collider as CapsuleCollider;
        mDefaultColliderCenter = mCapsuleColl.center;
        mDefaultColliderHeight = mCapsuleColl.height;

        mDefaultCameraPointLPos = cameraPoint.localPosition;

        mStats = GetComponent<PlayerStats>();
        mStats.changeHPCallback += OnStatsHPChange;

        mBlinker = GetComponent<Blinker>();
        mBlinker.activeCallback += OnBlinkActive;

        mActionIcons = new GameObject[actionIconHolder.childCount];
        for(int i = 0; i < mActionIcons.Length; i++) {
            mActionIcons[i] = actionIconHolder.GetChild(i).gameObject;
            mActionIcons[i].SetActive(false);
        }
    }

    // Use this for initialization
    protected override void Start() {
        base.Start();

        //set player's starting location based on saved spawn point, if there is one.
        Transform spawnPt = LevelController.GetSpawnPoint();
        if(spawnPt) {
            transform.position = spawnPt.position + spawnPt.up*collider.bounds.extents.y;
            transform.rotation = spawnPt.rotation;
        }

        if(cameraPointStartAttached) {
            CameraController.instance.attach = cameraPoint;
        }
    }

    void OnTriggerEnter(Collider col) {
        if(IsSpecialTrigger(col)) {
            mSpecialTriggerFSM = col.GetComponent<PlayMakerFSM>();
            mSpecialTrigger = col.GetComponent<SpecialTrigger>();

            if((mSpecialTrigger && mSpecialTrigger.enabled && !mSpecialTrigger.isActing && mSpecialTrigger.interactive) || mSpecialTriggerFSM) {
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
            if((mSpecialTrigger && col == mSpecialTrigger.collider) || (mSpecialTriggerFSM && col == mSpecialTriggerFSM.collider)) {
                mSpecialTriggerFSM = null;
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

                mUpIsPressed = false;
            }
            else if(inpY > inputDirThreshold) {
                lookDir = LookDir.Up;

                //check for pressed
                if(!mUpIsPressed) {
                    UpPressed();
                    mUpIsPressed = true;
                }
            }
            else {
                lookDir = LookDir.Front;

                mUpIsPressed = false;
            }
        }

        //
        CameraController.instance.delayScale = Mathf.Clamp(1.0f - (mCtrl.isGrounded ? Mathf.Abs(mCtrl.localVelocity.x) : mCtrl.localVelocity.magnitude)/cameraSpeedNorm, cameraSpeedMinScale, cameraSpeedMaxScale);
        if(!(mCtrl.isWallStick || mCameraIsWallStick)) {
            if(mCtrl.isGrounded || mCtrl.localVelocity.y > 0)
                cameraPoint.localPosition = mDefaultCameraPointLPos;
            else
                cameraPoint.localPosition = mDefaultCameraPointLPos - transform.up*cameraPointFallOfs;
        }
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

            if(!mSliding) {
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
        if(mSpecialTrigger || mSpecialTriggerFSM) {
            if(currentBuddy)
                currentBuddy.FireStop();

            if(mSpecialTriggerFSM)
                mSpecialTriggerFSM.SendEvent(EntityEvent.Interact);

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

    void OnSceneChange(string nextScene) {
        //save stuff

    }

    IEnumerator DoDeathFinishDelay() {
        yield return new WaitForSeconds(deathFinishDelay);

        SceneState.instance.GlobalSnapshotRestore();
        UserData.instance.SnapshotRestore();
        SceneManager.instance.Reload();
    }

    IEnumerator DoCameraPointWallCheck() {
        WaitForSeconds waitCheck = new WaitForSeconds(cameraPointWallCheckDelay);

        float lastCheckTime = Time.fixedTime;
        mCameraIsWallStick = false;

        while(true) {
            if(mCameraIsWallStick && Time.fixedTime - lastCheckTime >= cameraPointRevertDelay) {
                cameraPoint.localPosition = mDefaultCameraPointLPos;
                mCameraIsWallStick = false;
            }

            if(mCtrl.isWallStick) {
                lastCheckTime = Time.fixedTime;
                if(!mCameraIsWallStick) {
                    cameraPoint.localPosition = Vector3.zero;
                    mCameraIsWallStick = true;
                }
            }

            yield return waitCheck;
        }
    }

    void OnRigidbodyCollisionEnter(RigidBodyController controller, Collision col) {
    }

    void OnLand(PlatformerController ctrl) {
        if(state != (int)EntityState.Invalid) {
            //effects
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
}
