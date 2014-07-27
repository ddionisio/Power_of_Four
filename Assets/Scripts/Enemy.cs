using UnityEngine;
using System.Collections;

public class Enemy : EntityBase {
    public const string projGroup = "projEnemy";

    public const string knockTag = "Knocker";

    public AnimatorData animatorControl; //if an animator is controlling this enemy's movement

    public string takeDeath = "die";
    public string takeImmolate = "immolate";
    public string takeGrabbed = "grabbed";
    public string takeThrown = "thrown";
    public string takeKnocked = "knock";

    public bool disablePhysicsOnDeath = true;
    public bool releaseOnDeath = false;

    public string deathSpawnGroup; //upon death, spawn this
    public string deathSpawnType;
    public Vector3 deathSpawnOfs;

    private PlatformerController mCtrl;
    private GravityController mCtrlGravity;
    private PlatformerAnimatorController mCtrlAnim;
    private AnimatorData mAnim; //if controller animator is not available
    private EnemyStats mStats;
    private Blinker mBlink;

    private Rigidbody mBody;
    private bool mBodyKinematicDefault;

    private Damage[] mDamageTriggers;

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
        if(mCtrlAnim)
            mCtrlAnim.PlayOverrideClip(take);
        else if(mAnim)
            mAnim.Play(take);
    }

    public void StopAnim() {
        if(mCtrlAnim)
            mCtrlAnim.StopOverrideClip();
        else if(mAnim)
            mAnim.Stop();
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

        switch((EntityState)state) {
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
                break;
        }
    }

    public override void SpawnFinish() {
        //start ai, player control, etc
        if(mStats)
            mStats.isInvul = false;

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

    void AnimEndProcess(AMTakeData take) {

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
