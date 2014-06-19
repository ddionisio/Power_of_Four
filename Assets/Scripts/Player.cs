using UnityEngine;
using System.Collections;

public class Player : EntityBase {
    public const string clipHurt = "hurt";
    public float hurtForce = 15.0f;
    public float hurtDelay = 0.5f; //how long the hurt state lasts
    public float hurtInvulDelay = 0.5f;
    public float deathFinishDelay = 2.0f;
    public float slideForce;
    public float slideSpeedMax;
    public float slideDelay;
    public float slideHeight = 0.79f;
    public LayerMask solidMask; //use for standing up, etc.

    private static Player mInstance;
    private PlayerStats mStats;
    private PlatformerController mCtrl;
    private SpriteColorBlink[] mBlinks;
    private float mDefaultCtrlMoveForce;
    private float mDefaultCtrlMoveMaxSpeed;
    private Vector3 mDefaultColliderCenter;
    private float mDefaultColliderHeight;
    private CapsuleCollider mCapsuleColl;
    private bool mInputEnabled;
    private bool mSliding;
    private float mSlidingLastTime;
    private bool mHurtActive;
    private int mCurWeaponInd = -1;
    private int mPauseCounter;
    private bool mAllowPauseTime = true;
    private PlayMakerFSM mSpecialTriggerFSM; //special trigger activated

    public static Player instance { get { return mInstance; } }

    public int currentWeaponIndex {
        get { return mCurWeaponInd; }
        set {
        }
    }

    /*public Weapon currentWeapon {
        get {
            if(mCurWeaponInd >= 0)
                return weapons[mCurWeaponInd];
            return null;
        }
    }*/

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
                    }
                    else {

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

    //public PlatformerSpriteController controllerSprite { get { return mCtrlSpr; } }

    public PlayerStats stats { get { return mStats; } }


    protected override void StateChanged() {
        switch((EntityState)prevState) {
            case EntityState.Hurt:
                mHurtActive = false;
                break;

            case EntityState.Lock:
                inputEnabled = true;

                InputManager input = InputManager.instance;
                if(input) {
                    //input.AddButtonCall(0, InputAction.MenuCancel, OnInputPause);
                }

                //mStats.isInvul = false;

                mCtrl.moveSideLock = false;
                break;
        }

        switch((EntityState)state) {
            case EntityState.Normal:
                inputEnabled = true;
                break;

            case EntityState.Hurt:
                Blink(hurtInvulDelay);
                break;

            case EntityState.Dead:
                UIModalManager.instance.ModalCloseAll();

                SetSlide(false);
                    
                mCtrl.enabled = false;
                rigidbody.isKinematic = true;
                rigidbody.detectCollisions = false;
                collider.enabled = false;

                //disable all input
                inputEnabled = false;

                InputManager input = InputManager.instance;
                if(input) {
                    //input.RemoveButtonCall(0, InputAction.MenuCancel, OnInputPause);
                }
                //

                //spawn death thing

                //StartCoroutine(DoDeathFinishDelay());
                break;

            case EntityState.Lock:
                UIModalManager.instance.ModalCloseAll();
                //if(currentWeapon)
                    //currentWeapon.FireStop();

                //LockControls();
                break;

            case EntityState.Victory:
                UIModalManager.instance.ModalCloseAll();
                //if(currentWeapon)
                    //currentWeapon.FireStop();

                currentWeaponIndex = -1;
                //LockControls();
                //mCtrlSpr.PlayOverrideClip("victory");
                break;

            case EntityState.Final:
                UIModalManager.instance.ModalCloseAll();
                //if(currentWeapon)
                    //currentWeapon.FireStop();

                currentWeaponIndex = -1;
                //LockControls();

                //save?
                break;

            case EntityState.Exit:
                UIModalManager.instance.ModalCloseAll();
                //if(currentWeapon)
                    //currentWeapon.FireStop();

                currentWeaponIndex = -1;
                //LockControls();
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
            //input.RemoveButtonCall(0, InputAction.MenuCancel, OnInputPause);
        }
        //

        Blink(0.0f);
        //mStats.isInvul = true;

        mCtrl.moveSideLock = true;
        mCtrl.moveSide = 0.0f;
        //mCtrl.ResetCollision();
    }

    protected override void SetBlink(bool blink) {
        foreach(SpriteColorBlink blinker in mBlinks) {
            blinker.enabled = blink;
        }

        //mStats.isInvul = blink;
    }

    protected override void OnDespawned() {
        //reset stuff here

        base.OnDespawned();
    }

    protected override void OnDestroy() {
        mInstance = null;

        //dealloc here
        inputEnabled = false;

        InputManager input = InputManager.instance;
        if(input) {
            //input.RemoveButtonCall(0, InputAction.MenuCancel, OnInputPause);
        }

        base.OnDestroy();
    }

    public override void SpawnFinish() {
        state = (int)EntityState.Normal;

        if(SceneState.instance.GetGlobalValue("cheat") > 0) {
            //stats.damageReduction = 1.0f;
        }
    }

    protected override void SpawnStart() {
        //initialize some things

        //start ai, player control, etc
        currentWeaponIndex = 0;
    }

    protected override void Awake() {
        mInstance = this;

        base.Awake();

        //initialize variables
        //InputManager.instance.AddButtonCall(0, InputAction.MenuCancel, OnInputPause);

        mCtrl = GetComponent<PlatformerController>();
        mCtrl.moveInputX = InputAction.MoveX;
        mCtrl.moveInputY = InputAction.MoveY;
        //mCtrl.collisionEnterCallback += OnRigidbodyCollisionEnter;
        //mCtrl.landCallback += OnLand;

        mDefaultCtrlMoveMaxSpeed = mCtrl.moveMaxSpeed;
        mDefaultCtrlMoveForce = mCtrl.moveForce;

        //mCtrlSpr = GetComponent<PlatformerSpriteController>();
        //mCtrlSpr.clipFinishCallback += OnSpriteCtrlOneTimeClipEnd;

        mCapsuleColl = collider as CapsuleCollider;
        mDefaultColliderCenter = mCapsuleColl.center;
        mDefaultColliderHeight = mCapsuleColl.height;

        mStats = GetComponent<PlayerStats>();

        mBlinks = GetComponentsInChildren<SpriteColorBlink>(true);
        foreach(SpriteColorBlink blinker in mBlinks) {
            blinker.enabled = false;
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
        if(mSliding) {
            InputManager input = InputManager.instance;

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
    }

    #region Stats/Weapons

    void OnStatsHPChange(Stats stat, float delta) {
        if(delta < 0.0f) {
            /*if(stat.curHP <= 0.0f) {
                state = (int)EntityState.Dead;
            }
            else {
                state = (int)EntityState.Hurt;
            }*/

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

    void OnInputFire(InputManager.Info dat) {
        if(dat.state == InputManager.State.Pressed) {
            if(mSpecialTriggerFSM) {
                //if(currentWeapon)
                    //currentWeapon.FireStop();

                mSpecialTriggerFSM.SendEvent(EntityEvent.Interact);
            }
            /*else if(currentWeapon) {
                if(currentWeapon.allowSlide || !mSliding) {
                    if(currentWeapon.hasEnergy) {
                        currentWeapon.FireStart();
                    }
                    else {
                        HUD.instance.barEnergy.Flash(true);
                    }
                }
            }*/
        }
        else if(dat.state == InputManager.State.Released) {
            //if(!mFireFSM && currentWeapon) {
                //currentWeapon.FireStop();
            //}
        }
    }

    void OnInputPowerNext(InputManager.Info dat) {
        if(dat.state == InputManager.State.Pressed) {
            /*for(int i = 0, max = weapons.Length, toWeaponInd = currentWeaponIndex + 1; i < max; i++, toWeaponInd++) {
                if(toWeaponInd >= weapons.Length)
                    toWeaponInd = 0;

                if(weapons[toWeaponInd] && SlotInfo.WeaponIsUnlock(toWeaponInd)) {
                    currentWeaponIndex = toWeaponInd;
                    break;
                }
            }*/
        }
    }

    void OnInputPowerPrev(InputManager.Info dat) {
        if(dat.state == InputManager.State.Pressed) {
            /*for(int i = 0, max = weapons.Length, toWeaponInd = currentWeaponIndex - 1; i < max; i++, toWeaponInd--) {
                if(toWeaponInd < 0)
                    toWeaponInd = weapons.Length - 1;

                if(weapons[toWeaponInd] && SlotInfo.WeaponIsUnlock(toWeaponInd)) {
                    currentWeaponIndex = toWeaponInd;
                    break;
                }
            }*/
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
            if(!mSliding) {
                //Weapon curWpn = weapons[mCurWeaponInd];
                //if((!curWpn.isFireActive || curWpn.allowSlide) && mCtrl.isGrounded)
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
        //stats.curHP = 0;
    }

    void OnUIModalActive() {
        Pause(true);
    }

    void OnUIModalInactive() {
        Pause(false);
    }

    IEnumerator DoHurtForce(Vector3 normal) {
        mHurtActive = true;

        mCtrl.enabled = false;
        rigidbody.velocity = Vector3.zero;
        rigidbody.drag = 0.0f;

        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        normal.x = Mathf.Sign(normal.x);
        normal.y = 0.0f;
        normal.z = 0.0f;

        while(mHurtActive) {
            yield return wait;

            rigidbody.AddForce(normal * hurtForce);
        }

        mCtrl.enabled = true;
        mCtrl.ResetCollision();

        mHurtActive = false;
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
                //mCtrl.moveSide = mCtrlSpr.isLeft ? -1.0f : 1.0f;

                //mCtrlSpr.state = PlatformerSpriteController.State.Slide;

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

                        if(!rigidbody.isKinematic) {
                            Vector3 v = rigidbody.velocity; v.x = 0.0f; v.z = 0.0f;
                            rigidbody.velocity = v;
                        }
                    } else {
                        //limit x velocity
                        Vector3 v = rigidbody.velocity;
                        if(Mathf.Abs(v.x) > 12.0f) {
                            v.x = Mathf.Sign(v.x) * 12.0f;
                            rigidbody.velocity = v;
                        }
                    }

                    //mCtrlSpr.state = PlatformerSpriteController.State.None;

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

        float r = mCapsuleColl.radius - 0.05f;

        Vector3 c = transform.position + mDefaultColliderCenter;
        Vector3 u = new Vector3(c.x, c.y + (mDefaultColliderHeight * 0.5f - mCapsuleColl.radius) + ofs, c.z);
        Vector3 d = new Vector3(c.x, (c.y - (mDefaultColliderHeight * 0.5f - mCapsuleColl.radius)) + ofs, c.z);

        return !Physics.CheckCapsule(u, d, r, solidMask);
    }

    void SceneChange(string nextScene) {
        //save stuff
    }

    IEnumerator DoDeathFinishDelay() {
        yield return new WaitForSeconds(deathFinishDelay);

        SceneManager.instance.Reload();
    }

    void OnRigidbodyCollisionEnter(RigidBodyController controller, Collision col) {
    }

    void OnLand(PlatformerController ctrl) {
        if(state != (int)EntityState.Invalid) {
            //effects
        }
    }
}
