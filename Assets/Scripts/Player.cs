using UnityEngine;
using System.Collections;

public class Player : EntityBase {
    public const string takeHurt = "hurt";

    public enum LookDir {
        Front,
        Up,
        Down
    }

    public float hurtForce = 15.0f;
    public float hurtDelay = 0.5f; //how long the hurt state lasts
    public float hurtInvulDelay = 0.5f;
    public float deathFinishDelay = 2.0f;
    public float slideForce;
    public float slideSpeedMax;
    public float slideDelay;
    public float slideHeight = 0.79f;
    public LayerMask solidMask; //use for standing up, etc.

    public Transform look;

    public Transform cameraPoint;
    public float cameraPointWallCheckDelay = 0.2f;
    public float cameraPointRevertDelay = 2.0f;

    public Buddy[] buddies;

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
    private PlayMakerFSM mSpecialTriggerFSM; //special trigger activated
    private LookDir mCurLook = LookDir.Front;
    
    public static Player instance { get { return mInstance; } }

    public int currentBuddyIndex {
        get { return mCurBuddyInd; }
        set {
            if(mCurBuddyInd != value && (value == -1 || PlayerSave.BuddyIsUnlock(value))) {
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

    public LookDir lookDir {
        get { return mCurLook; }
        set {
            if(mCurLook != value) {
                mCurLook = value;
                switch(mCurLook) {
                    case LookDir.Front:
                        look.rotation = Quaternion.identity;
                        break;
                    case LookDir.Up:
                        look.rotation = Quaternion.Euler(0, 0, 90);
                        break;
                    case LookDir.Down:
                        look.rotation = Quaternion.Euler(0, 0, -90);
                        break;
                }
            }
        }
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
                inputEnabled = false;
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

        base.OnDestroy();
    }

    public override void SpawnFinish() {
        state = (int)EntityState.Normal;

        if(SceneState.instance.GetGlobalValue("cheat") > 0) {
            stats.damageReduction = 1.0f;
        }
    }

    protected override void SpawnStart() {
        //initialize some things

        //start ai, player control, etc
        currentBuddyIndex = PlayerSave.BuddySelected();

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

        for(int i = 0; i < buddies.Length; i++) {
            Buddy buddy = buddies[i];
            buddy.level = PlayerSave.BuddyLevel(i);
        }
    }

    // Use this for initialization
    protected override void Start() {
        base.Start();

        //initialize hp stuff
    }

    void OnTriggerEnter(Collider col) {
        if(col.CompareTag("SpecialTrigger")) {
            PlayMakerFSM fsm = col.GetComponent<PlayMakerFSM>();
            if(fsm) {
                mSpecialTriggerFSM = fsm;
            }
        }
    }

    void OnTriggerExit(Collider col) {
        if(mSpecialTriggerFSM && col == mSpecialTriggerFSM.collider) {
            mSpecialTriggerFSM = null;
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
        else {
            float inpY = input.GetAxis(0, InputAction.MoveY);

            if(inpY < -0.25f) {
                lookDir = mCtrl.isGrounded ? LookDir.Front : LookDir.Down;

                if(currentBuddy)
                    currentBuddy.dir = mCtrl.isGrounded ? Buddy.Dir.Front : Buddy.Dir.Down;
            }
            else if(inpY > 0.25f) {
                lookDir = LookDir.Up;

                if(currentBuddy)
                    currentBuddy.dir = Buddy.Dir.Up;
            }
            else {
                lookDir = LookDir.Front;

                if(currentBuddy)
                    currentBuddy.dir = Buddy.Dir.Front;
            }
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

    /*void OnWeaponEnergyCallback(Weapon weapon, float delta) {
        if(weapon == weapons[mCurWeaponInd]) {
            if(delta <= 0.0f) {
                HUD.instance.barEnergy.current = Mathf.CeilToInt(weapon.currentEnergy);
            }
            else {
                if(!HUD.instance.barEnergy.isAnimating)
                    Pause(true);

                HUD.instance.barEnergy.currentSmooth = Mathf.CeilToInt(weapon.currentEnergy);
            }
        }
    }*/

    #endregion

    #region Anim
    #endregion

    #region Input

    void OnInputPrimary(InputManager.Info dat) {
        if(dat.state == InputManager.State.Pressed) {
            if(mSpecialTriggerFSM) {
                if(currentBuddy)
                    currentBuddy.FireStop();

                mSpecialTriggerFSM.SendEvent(EntityEvent.Interact);
            }
            else if(currentBuddy)
                currentBuddy.FireStart();
        }
        else if(dat.state == InputManager.State.Released) {
            if(!mSpecialTriggerFSM && currentBuddy)
                currentBuddy.FireStop();
        }
    }

    void OnInputSecondary(InputManager.Info dat) {
        if(dat.state == InputManager.State.Pressed) {
        }
        else if(dat.state == InputManager.State.Released) {
        }
    }

    void OnInputPowerNext(InputManager.Info dat) {
        if(dat.state == InputManager.State.Pressed) {
            for(int i = 0, max = buddies.Length, toBuddyInd = currentBuddyIndex + 1; i < max; i++, toBuddyInd++) {
                if(toBuddyInd >= buddies.Length)
                    toBuddyInd = 0;

                if(buddies[toBuddyInd] && PlayerSave.BuddyIsUnlock(toBuddyInd)) {
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

                if(buddies[toBuddyInd] && PlayerSave.BuddyIsUnlock(toBuddyInd)) {
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
                if(input.GetAxis(0, InputAction.MoveY) < -0.1f && mCtrl.isGrounded) {
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
                if(input.GetAxis(0, InputAction.MoveY) >= 0.0f) {
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
        
    #endregion

    void OnSuddenDeath() {
        stats.curHP = 0;
    }

    void OnUIModalActive(bool a) {
        Pause(a);
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
        } else {
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

                //sfxSlide.Play();
            } else {
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
                    } else {
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
                } else {
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

    void OnSceneChange(string nextScene) {
        //save stuff
    }

    IEnumerator DoDeathFinishDelay() {
        yield return new WaitForSeconds(deathFinishDelay);

        SceneManager.instance.Reload();
    }

    IEnumerator DoCameraPointWallCheck() {
        WaitForSeconds waitCheck = new WaitForSeconds(cameraPointWallCheckDelay);
        
        float lastCheckTime = Time.fixedTime;
        bool isCameraPointSet = false;

        while(true) {
            if(isCameraPointSet && Time.fixedTime - lastCheckTime >= cameraPointRevertDelay) {
                cameraPoint.localPosition = mDefaultCameraPointLPos;
                isCameraPointSet = false;
            }

            if(mCtrl.isWallStick) {
                lastCheckTime = Time.fixedTime;
                if(!isCameraPointSet) {
                    cameraPoint.localPosition = Vector3.zero;
                    isCameraPointSet = true;
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
}
