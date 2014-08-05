using UnityEngine;
using System.Collections;

public class Enemy : EntityBase {
    public const string projGroup = "projEnemy";

    public const string knockTag = "Knocker";

    public delegate void EnemyCallback(Enemy enemy);

    public AnimatorData animatorControl; //if an animator is controlling this enemy's movement

    public string takeDeath = "die";
    public string takeImmolate = "immolate";
    public string takeGrabbed = "grabbed";
    public string takeThrown = "thrown";
    public string takeKnocked = "knock";
    public string takeKnockedGround = "knockGround"; //upon landing

    public bool disablePhysicsOnDeath = true;
    public bool releaseOnDeath = false;

    public string deathSpawnGroup; //upon death, spawn this
    public string deathSpawnType;
    public Vector3 deathSpawnOfs;

    public float knockJumpDelay = 0.1f;
    public float knockDuration = 2.0f;

    private PlatformerController mCtrl;
    private GravityController mCtrlGravity;
    private PlatformerAnimatorController mCtrlAnim;
    private AnimatorData mAnim; //if controller animator is not available
    private EnemyStats mStats;
    private Blinker mBlink;

    private Rigidbody mBody;
    private bool mBodyKinematicDefault;

    private Damage[] mDamageTriggers;
    private MaterialFloatPropertyControl[] mMatPropCtrls;

    private IEnumerator mCurStateAction;

    public EnemyStats stats { get { return mStats; } }
    public PlatformerController bodyCtrl { get { return mCtrl; } }
    public GravityController gravityCtrl { get { return mCtrlGravity; } }
    public PlatformerAnimatorController bodySpriteCtrl { get { return mCtrlAnim; } }

    public void Jump(float delay) {
        if(mCtrl) {
            CancelInvoke(JumpFinishKey);

            if(delay > 0) {
                mCtrl.Jump(true);
                Invoke(JumpFinishKey, delay);
            }
            else
                mCtrl.Jump(false);
        }
    }

    public void PlayAnim(string take) {
        if(!string.IsNullOrEmpty(take)) {
            if(mCtrlAnim)
                mCtrlAnim.PlayOverrideClip(take);
            else if(mAnim)
                mAnim.Play(take);
        }
    }

    public void StopAnim() {
        if(mCtrlAnim)
            mCtrlAnim.StopOverrideClip();
        else if(mAnim)
            mAnim.Stop();
    }

    void OnTriggerEnter(Collider col) {
        GameObject go = col.gameObject;

        //knock target
        if(go.CompareTag(knockTag)) {
            if(stats && stats.isKnockable && (state == (int)EntityState.Normal || (mCtrl && state == (int)EntityState.Knocked && mCtrl.isGrounded))) {
                //jump
                if(state != (int)EntityState.Knocked)
                    state = (int)EntityState.Knocked;
                else {
                    Jump(knockJumpDelay);
                    RestartStateAction();
                }
            }
        }
    }

    protected override void ActivatorWakeUp() {
        base.ActivatorWakeUp();

        if(animatorControl && animatorControl.onDisableAction == AnimatorData.DisableAction.Pause) {
            animatorControl.Resume();
        }

        //resume any actions
        RunStateAction();
    }

    protected override void ActivatorSleep() {
        base.ActivatorSleep();

        mCurStateAction = null;

        switch((EntityState)state) {
            case EntityState.Dead:
                if(releaseOnDeath) {
                    activator.ForceActivate();
                    Release();
                }
                break;
        }
    }

    protected override void OnDespawned() {
        //reset stuff here
        state = (int)EntityState.Invalid;

        base.OnDespawned();
    }

    protected override void OnDestroy() {
        //dealloc here

        base.OnDestroy();
    }

    protected override void StateChanged() {
        switch((EntityState)prevState) {
            case EntityState.Normal:
                Jump(0.0f);
                break;
        }

        if(mCurStateAction != null) {
            StopCoroutine(mCurStateAction);
            mCurStateAction = null;
        }

        StopAnim();

        switch((EntityState)state) {
            case EntityState.Normal:
                if(animatorControl)
                    animatorControl.PlayDefault();

                SetDamageTriggerActive(true);
                break;

            case EntityState.Knocked:
                SetDamageTriggerActive(false);

                Jump(knockJumpDelay);
                break;

            case EntityState.Dead:
                if(disablePhysicsOnDeath)
                    SetPhysicsActive(false, false);

                if(mBlink)
                    mBlink.Stop();

                if(mStats)
                    mStats.isInvul = true;

                if(!string.IsNullOrEmpty(deathSpawnGroup) && !string.IsNullOrEmpty(deathSpawnType)) {
                    Vector3 pt = transform.localToWorldMatrix.MultiplyPoint(deathSpawnOfs); pt.z = 0;
                    PoolController.Spawn(deathSpawnGroup, deathSpawnType, deathSpawnType, null, pt, Quaternion.identity);
                }

                //play death animation
                if(mCtrlAnim || mAnim)
                    PlayAnim(takeDeath);
                else if(releaseOnDeath)
                    Release();
                break;

            case EntityState.Invalid:
                //reset stuff
                if(animatorControl)
                    animatorControl.Stop();

                StopAnim();

                if(mBlink)
                    mBlink.Stop();

                if(mStats) {
                    mStats.Reset();
                    mStats.isInvul = true;
                }

                if(FSM)
                    FSM.Reset();

                if(mCtrl)
                    mCtrl.enabled = true;

                Jump(0.0f);

                mCurStateAction = null;

                for(int i = 0; i < mMatPropCtrls.Length; i++)
                    mMatPropCtrls[i].Revert();

                SetPhysicsActive(false, false);
                break;
        }

        RunStateAction();
    }

    public override void SpawnFinish() {
        //start ai, player control, etc
        if(mStats)
            mStats.isInvul = false;

        SetPhysicsActive(true, false);

        state = (int)EntityState.Normal;
    }

    protected override void SpawnStart() {
        //initialize some things
    }

    protected override void Awake() {
        base.Awake();

        mBody = rigidbody;
        if(mBody)
            mBodyKinematicDefault = mBody.isKinematic;

        //initialize variables
        mStats = GetComponent<EnemyStats>();
        mStats.changeHPCallback += OnStatsHPChange;
        mStats.applyDamageCallback += ApplyDamageCallback;

        mCtrl = GetComponent<PlatformerController>();
        if(mCtrl) {
            mCtrl.moveSideLock = true;
            mCtrlGravity = mCtrl.gravityController;
        }
        else
            mCtrlGravity = GetComponent<GravityController>();

        mCtrlAnim = GetComponent<PlatformerAnimatorController>();
        if(mCtrlAnim) {
            mCtrlAnim.clipFinishCallback += OnAnimControllerEnd;
        }
        else {
            mAnim = GetComponentInChildren<AnimatorData>();
            if(mAnim)
                mAnim.takeCompleteCallback += OnAnimEnd;
        }

        if(!FSM)
            autoSpawnFinish = true;

        mBlink = GetComponent<Blinker>();

        mMatPropCtrls = GetComponentsInChildren<MaterialFloatPropertyControl>(true);

        SetPhysicsActive(false, false);
    }

    // Use this for initialization
    protected override void Start() {
        base.Start();

        //initialize variables from other sources (for communicating with managers, etc.)
    }

    protected virtual void OnStatsHPChange(Stats stat, float delta) {
        if(stats.curHP <= 0.0f)
            state = (int)EntityState.Dead;
        else {
            mBlink.Blink();
        }
    }

    protected virtual void ApplyDamageCallback(Damage damage) {

    }

    protected virtual void OnSuddenDeath() {
        stats.curHP = 0;
    }

    protected void SetDamageTriggerActive(bool aActive) {
        if(mDamageTriggers == null || mDamageTriggers.Length == 0)
            mDamageTriggers = GetComponentsInChildren<Damage>(true);

        for(int i = 0, max = mDamageTriggers.Length; i < max; i++)
            mDamageTriggers[i].gameObject.SetActive(aActive);
    }

    protected void SetPhysicsActive(bool aActive, bool excludeCollision) {
        if(rigidbody) {
            if(!mBodyKinematicDefault) {
                rigidbody.isKinematic = !aActive;
            }

            if(aActive || !excludeCollision)
                rigidbody.detectCollisions = aActive;
        }

        if(collider && (aActive || !excludeCollision)) {
            collider.enabled = aActive;
        }

        if(mCtrlGravity) {
            mCtrlGravity.enabled = aActive;
        }

        if(animatorControl) {
            if(aActive) {
                if(animatorControl.isPaused)
                    animatorControl.Resume();
            }
            else {
                if(animatorControl.isPlaying)
                    animatorControl.Pause();
            }
        }

        if(mCtrl)
            mCtrl.enabled = aActive;

        SetDamageTriggerActive(aActive);
    }

    ///////////////////////////
    //Actions

    //Perform current state's action
    void RunStateAction() {
        switch((EntityState)state) {
            case EntityState.Knocked:
                StartCoroutine(mCurStateAction = DoKnock());
                break;
        }
    }

    void RestartStateAction() {
        if(mCurStateAction != null)
            StopCoroutine(mCurStateAction);
        RunStateAction();
    }

    IEnumerator DoKnock() {
        if(animatorControl)
            animatorControl.Pause();

        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        yield return wait;

        if(mCtrl) {
            if(mCtrl.isGrounded && !mCtrl.isJump) {
                PlayAnim(takeKnockedGround);
            }
            else {
                //wait till we hit the ground
                PlayAnim(takeKnocked);

                while(!mCtrl.isGrounded || mCtrl.isJump)
                    yield return wait;

                PlayAnim(takeKnockedGround);
            }
        }
        else
            PlayAnim(takeKnocked);

        yield return new WaitForSeconds(knockDuration);

        //return to normal
        state = (int)EntityState.Normal;
    }

    ///////////////////////////
    //Internal

    void AnimEndProcess(AMTakeData take) {
        switch((EntityState)state) {
            case EntityState.Dead:
                //release?
                if(releaseOnDeath)
                    Release();
                break;
        }
    }

    //If there is animation controller
    void OnAnimControllerEnd(PlatformerAnimatorController ctrl, AMTakeData take) {
        AnimEndProcess(take);
    }

    //If no animation controller
    void OnAnimEnd(AnimatorData animDat, AMTakeData take) {
        AnimEndProcess(take);
    }

    private const string JumpFinishKey = "JumpFinish";
    void JumpFinish() {
        mCtrl.Jump(false);
    }

    protected virtual void OnDrawGizmosSelected() {
        if(!string.IsNullOrEmpty(deathSpawnGroup) && !string.IsNullOrEmpty(deathSpawnType)) {
            Color clr = Color.cyan;
            clr.a = 0.3f;
            Gizmos.color = clr;
            Gizmos.DrawSphere(transform.localToWorldMatrix.MultiplyPoint(deathSpawnOfs), 0.25f);
        }
    }
}
