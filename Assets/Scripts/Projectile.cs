using UnityEngine;
using System.Collections;

public class Projectile : EntityBase {
    public enum State {
        Invalid = -1,
        Active,
        Seek,
        SeekForce,
        Dying
    }

    public enum ContactType {
        None,
        End,
        Stop,
        Bounce,
        BounceRotate
    }

    public enum ForceBounceType {
        None,
        ContactNormal,
        ReflectDir,
        ReflectDirXOnly,
        ContactNormalXOnly,
    }

    public struct HitInfo {
        public Collider col;
        public Vector3 normal;
        public Vector3 point;
    }

    public const string soundHurt = "enemyHit";

    public bool simple; //don't use rigidbody, make sure you have a sphere collider and set it to trigger
    public LayerMask simpleLayerMask;
    public string[] hitTags;
    public LayerMask ignoreCollisionMask; //ignore this collision upon contact
    public float startVelocity;
    public float startVelocityAddRand;
    public float force;
    [SerializeField]
    protected float speedLimit;
    public bool seekUseForce = false;
    public float seekAngleLimit = 360.0f; //angle limit from startDir
    public float seekForceDelay = 0.15f;
    public float seekStartDelay = 0.0f;
    public float seekVelocity;
    public float seekVelocityCap = 5.0f;
    public float seekTurnAngleCap = 360.0f;
    public float decayDelay;
    public float decayBlinkDelayScale;
    public bool releaseOnDie = true;
    public bool dieDisablePhysics = true;
    public LayerMask explodeMask;
    public float explodeForce;
    public ForceMode explodeForceMode;
    public float explodeUpwardMod;
    public Vector3 explodeOfs;
    public Transform explodeOfsTarget;
    public float explodeRadius;
    public ContactType contactType = ContactType.End;
    public ForceBounceType forceBounce = ForceBounceType.None;
    public int maxBounce = -1;
    public float bounceRotateAngle;
    public float bounceSurfaceOfs; //displace projectile slightly off surface based on normal
    public bool explodeOnDeath;
    public Transform applyDirToUp;
    public LayerMask EndByContactMask; //if not 0, use death contact mask to determine if we die based on layer
    public string deathSpawnGroup;
    public string deathSpawnType;
    public Vector3 deathSpawnOfs;
    public bool autoDisableCollision = true; //when not active, disable collision
    public int damageExpireCount = -1; //the number of damage dealt for it to die, -1 for infinite

    public SoundPlayer contactSfx;
    public SoundPlayer dyingSfx;

    /*public bool oscillate;
    public float oscillateForce;
    public float oscillateDelay;*/

    protected Rigidbody mBody;

    private bool mSpawning = false;
    protected Vector3 mActiveForce;
    protected Vector3 mInitDir = Vector3.zero;
    protected Transform mSeek = null;
    protected Vector3 mCurVelocity;
    protected float mInitialVelocity;

    private Damage mDamage;
    private float mDefaultSpeedLimit;
    protected SphereCollider mSphereColl;
    protected float mMoveScale = 1.0f;
    private Stats mStats;
    private Blinker mBlink;
    private int mCurBounce = 0;
    private int mCurDamageCount = 0;
    private HitInfo mLastHit;

    private Vector3 mSeekCurDir;
    private Vector3 mSeekCurDirVel;

    private IEnumerator mDecayAction;

    private float mDefaultStartVelocity;
    private float mDefaultForce;

    //private Vector2 mOscillateDir;
    //private bool mOscillateSwitch;

    public static Projectile Create(string group, string typeName, Vector3 startPos, Vector3 dir, Transform seek) {
        Projectile ret = Spawn<Projectile>(group, typeName, startPos);
        if(ret != null) {
            ret.mInitDir = dir;
            ret.mSeek = seek;
        }

        return ret;
    }

    public bool isAlive { get { return (State)state == State.Active || (State)state == State.Seek || (State)state == State.SeekForce; } }

    public Damage damage { get { return mDamage; } }

    public Transform seek {
        get { return mSeek; }
        set {
            mSeek = value;

            if(mSeek) {
                if((State)state == State.Active) {
                    state = (int)(seekUseForce ? State.SeekForce : State.Seek);
                }
            }
            else {
                if((State)state == State.Seek || (State)state == State.SeekForce) {
                    state = (int)State.Active;
                }
            }
        }
    }

    public bool spawning { get { return mSpawning; } }

    public Vector3 velocity {
        get { return mCurVelocity; }

        set {
            mCurVelocity = value;
            if(!simple && mBody)
                mBody.velocity = value;
        }
    }

    public Vector3 activeForce {
        get { return mActiveForce; }
        set { mActiveForce = value; }
    }

    public float moveScale {
        get { return mMoveScale; }
        set { mMoveScale = value; }
    }

    public Stats stats { get { return mStats; } }

    public HitInfo lastHit { get { return mLastHit; } }

    public void SetSpeedLimit(float limit) {
        speedLimit = limit;
    }

    public void RevertSpeedLimit() {
        speedLimit = mDefaultSpeedLimit;
    }

    protected override void Awake() {
        base.Awake();

        mSphereColl = collider ? collider as SphereCollider : null;

        mDefaultStartVelocity = startVelocity;
        mDefaultForce = force;
        mDefaultSpeedLimit = speedLimit;

        mBody = rigidbody;
        if(mBody != null && autoDisableCollision) {
            mBody.detectCollisions = false;
        }

        if(collider != null && autoDisableCollision)
            collider.enabled = false;

        mDamage = GetComponent<Damage>();

        Stats[] ss = GetComponentsInChildren<Stats>(true);
        mStats = ss.Length > 0 ? ss[0] : null;
        if(mStats) {
            mStats.changeHPCallback += OnHPChange;
            mStats.isInvul = true;
        }

        mBlink = GetComponent<Blinker>();

        if(!FSM)
            autoSpawnFinish = true;
    }

    // Use this for initialization
    protected override void Start() {
        base.Start();
    }

    protected override void ActivatorSleep() {
        base.ActivatorSleep();

        if(!mSpawning && isAlive) {
            activator.ForceActivate();
            Release();
        }
    }

    public override void SpawnFinish() {
        //Debug.Log("start dir: " + mStartDir);

        mSpawning = false;

        mCurBounce = 0;
        mCurDamageCount = 0;

        //starting direction and force
        if(simple) {
            mCurVelocity = mInitDir * startVelocity;
        }
        else {
            if(mBody != null && mInitDir != Vector3.zero) {
                //set velocity
                if(!mBody.isKinematic) {
                    if(startVelocityAddRand != 0.0f) {
                        mInitialVelocity = startVelocity + Random.value * startVelocityAddRand;
                    }
                    else {
                        mInitialVelocity = startVelocity;
                    }

                    mBody.velocity = mInitDir * mInitialVelocity;
                }

                mActiveForce = mInitDir * force;
            }
        }

        if(decayDelay > 0f)
            StartCoroutine(mDecayAction = DoDecay());

        if(seekStartDelay > 0.0f) {
            state = (int)State.Active;

            Invoke("OnSeekStart", seekStartDelay);
        }
        else {
            OnSeekStart();
        }

        if(applyDirToUp) {
            applyDirToUp.up = mInitDir;
            InvokeRepeating("OnUpUpdate", 0.1f, 0.1f);
        }
    }

    protected override void SpawnStart() {
        if(applyDirToUp && mInitDir != Vector3.zero) {
            applyDirToUp.up = mInitDir;
        }

        mSpawning = true;
    }

    public override void Release() {
        state = (int)State.Invalid;
        base.Release();
    }

    protected override void StateChanged() {
        switch((State)state) {
            case State.Seek:
            case State.SeekForce:
            case State.Active:
                if(collider)
                    collider.enabled = true;

                if(mBody)
                    mBody.detectCollisions = true;

                if(mStats)
                    mStats.isInvul = false;
                break;

            case State.Dying:
                CancelInvoke();

                if(dieDisablePhysics)
                    PhysicsDisable();

                Die();

                if(dyingSfx && !dyingSfx.isPlaying)
                    dyingSfx.Play();
                break;

            case State.Invalid:
                CancelInvoke();
                RevertSpeedLimit();

                startVelocity = mDefaultStartVelocity;
                force = mDefaultForce;

                PhysicsDisable();

                if(mStats) {
                    mStats.Reset();
                    mStats.isInvul = true;
                }

                if(mBlink)
                    mBlink.Stop();

                if(mDecayAction != null) { StopCoroutine(mDecayAction); mDecayAction = null; }

                mSpawning = false;
                break;
        }
    }

    void PhysicsDisable() {
        mCurVelocity = Vector3.zero;
        if(collider && autoDisableCollision)
            collider.enabled = false;

        if(mBody) {
            if(autoDisableCollision)
                mBody.detectCollisions = false;

            if(!mBody.isKinematic) {
                mBody.velocity = Vector3.zero;
                mBody.angularVelocity = Vector3.zero;
            }
        }
    }

    void Die() {
        if(!string.IsNullOrEmpty(deathSpawnGroup) && !string.IsNullOrEmpty(deathSpawnType)) {
            Vector2 p = transform.localToWorldMatrix.MultiplyPoint(deathSpawnOfs);

            PoolController.Spawn(deathSpawnGroup, deathSpawnType, deathSpawnType, null, p, Quaternion.identity);
        }

        if(explodeOnDeath && explodeRadius > 0.0f) {
            DoExplode();
        }

        if(releaseOnDie) {
            Release();
        }
    }

    protected virtual void OnHPChange(Stats stat, float delta) {
        if(stat.curHP == 0) {
            if(isAlive)
                state = (int)State.Dying;
        }
        else if(delta < 0.0f) {
            SoundPlayerGlobal.instance.Play(soundHurt);
        }
    }

    protected virtual void ApplyContact(GameObject go, Vector3 pos, Vector3 normal) {
        switch(contactType) {
            case ContactType.None:
                if(contactSfx && contactSfx.gameObject.activeInHierarchy)
                    contactSfx.Play();
                break;

            case ContactType.End:
                if(isAlive)
                    state = (int)State.Dying;
                break;

            case ContactType.Stop:
                if(simple)
                    mCurVelocity = Vector3.zero;
                else if(mBody != null)
                    mBody.velocity = Vector3.zero;
                break;

            case ContactType.Bounce:
                if(maxBounce > 0 && mCurBounce == maxBounce)
                    state = (int)State.Dying;
                else {
                    if(simple) {
                        mCurVelocity = Vector3.Reflect(mCurVelocity, normal);

                        if(bounceSurfaceOfs != 0.0f) {
                            Vector3 p = transform.position;
                            p += normal*bounceSurfaceOfs;
                            transform.position = p;
                        }
                    }
                    else {
                        if(mBody != null) {
                            if(bounceSurfaceOfs != 0.0f) {
                                Vector3 p = transform.position;
                                p += normal*bounceSurfaceOfs;
                                mBody.MovePosition(p);
                            }

                            Vector3 reflVel = Vector3.Reflect(mBody.velocity, normal);

                            //TODO: this is only for 2D
                            switch(forceBounce) {
                                case ForceBounceType.ContactNormal:
                                    mActiveForce.Set(normal.x, normal.y, 0.0f);
                                    mActiveForce.Normalize();
                                    mActiveForce *= force;
                                    break;

                                case ForceBounceType.ReflectDir:
                                    mActiveForce.Set(reflVel.x, reflVel.y, 0.0f);
                                    mActiveForce.Normalize();
                                    mActiveForce *= force;
                                    break;

                                case ForceBounceType.ReflectDirXOnly:
                                    if(Mathf.Abs(reflVel.x) > float.Epsilon) {
                                        mActiveForce.Set(Mathf.Sign(reflVel.x), 0.0f, 0.0f);
                                        mActiveForce *= force;
                                    }
                                    break;

                                case ForceBounceType.ContactNormalXOnly:
                                    if(Mathf.Abs(normal.x) > float.Epsilon) {
                                        mActiveForce.Set(normal.x, 0.0f, 0.0f);
                                        mActiveForce.Normalize();
                                        mActiveForce *= force;
                                    }
                                    break;
                            }
                        }

                        //mActiveForce = Vector3.Reflect(mActiveForce, normal);
                    }

                    if(maxBounce > 0)
                        mCurBounce++;

                    if(contactSfx && contactSfx.gameObject.activeInHierarchy)
                        contactSfx.Play();
                }
                break;

            case ContactType.BounceRotate:
                if(maxBounce > 0 && mCurBounce == maxBounce)
                    state = (int)State.Dying;
                else {
                    if(simple) {
                        mCurVelocity = Quaternion.AngleAxis(bounceRotateAngle, Vector3.forward) * mCurVelocity;

                        if(bounceSurfaceOfs != 0.0f) {
                            Vector3 p = transform.position;
                            p += normal*bounceSurfaceOfs;
                            transform.position = p;
                        }
                    }
                    else {
                        mActiveForce = Quaternion.AngleAxis(bounceRotateAngle, Vector3.forward) * mCurVelocity;

                        if(mBody != null) {
                            mBody.velocity = Quaternion.AngleAxis(bounceRotateAngle, Vector3.forward) * mBody.velocity;

                            if(bounceSurfaceOfs != 0.0f) {
                                Vector3 p = transform.position;
                                p += normal*bounceSurfaceOfs;
                                mBody.MovePosition(p);
                            }
                        }
                    }

                    if(maxBounce > 0)
                        mCurBounce++;

                    if(contactSfx && contactSfx.gameObject.activeInHierarchy)
                        contactSfx.Play();
                }
                break;
        }

        //do damage
        //if(!explodeOnDeath && CheckTag(go.tag)) {
        //mDamage.CallDamageTo(go, pos, normal);
        //}
    }

    void ApplyDamage(GameObject go, Vector3 pos, Vector3 normal) {
        if(mDamage && !explodeOnDeath && M8.Util.CheckTag(go, hitTags)) {
            if(mDamage.CallDamageTo(go, pos, normal)) {
                if(damageExpireCount != -1) {
                    mCurDamageCount++;
                    if(mCurDamageCount >= damageExpireCount)
                        state = (int)State.Dying;
                }
            }
        }
    }

    void ProcessContact(GameObject go, Vector3 pt, Vector3 n) {
        ApplyContact(go, pt, n);
        ApplyDamage(go, pt, n);

        if(contactType != ContactType.End && EndByContactMask.value != 0 && isAlive && ((1<<go.layer) & EndByContactMask) != 0) {
            state = (int)State.Dying;
        }
    }

    void OnCollisionEnter(Collision collision) {
        if(!collider.enabled || !gameObject.activeInHierarchy)
            return;

        int ignoreCount = 0;
        int contactCount = collision.contacts.Length;

        for(int i = 0; i < contactCount; i++) {
            ContactPoint cp = collision.contacts[i];
            if(cp.otherCollider.enabled) {
                GameObject otherGO = cp.otherCollider.gameObject;
                if(otherGO.activeInHierarchy) {
                    if(ignoreCollisionMask != 0 && (ignoreCollisionMask & (1<<otherGO.layer)) != 0) {
                        Physics.IgnoreCollision(collider, cp.otherCollider, true);
                        ignoreCount++;
                        continue;
                    }

                    mLastHit.col = cp.otherCollider;
                    mLastHit.normal = cp.normal;
                    mLastHit.point = cp.point;

                    ProcessContact(cp.otherCollider.gameObject, cp.point, cp.normal);
                }
            }
        }

        if(ignoreCount >= contactCount)
            mBody.velocity = mCurVelocity;
    }

    void OnCollisionStay(Collision collision) {
        if(state != (int)State.Invalid && mDamage) {
            //do damage
            foreach(ContactPoint cp in collision.contacts) {
                ApplyDamage(cp.otherCollider.gameObject, cp.point, cp.normal);
            }
        }
    }

    /*void OnTrigger(Collider collider) {
        ApplyContact(collider.gameObject, -mover.dir);
    }*/

    IEnumerator DoDecay() {
        if(mBlink && decayBlinkDelayScale > 0f) {
            float _decayBlinkDelay = decayBlinkDelayScale*decayDelay;
            
            yield return new WaitForSeconds(decayDelay - _decayBlinkDelay);

            mBlink.Blink(_decayBlinkDelay);
            yield return new WaitForSeconds(_decayBlinkDelay);
        }
        else
            yield return new WaitForSeconds(decayDelay);

        mLastHit.col = collider;
        mLastHit.normal = Vector3.down;
        mLastHit.point = transform.position;

        state = (int)State.Dying;
    }

    void OnSeekStart() {
        if((State)state != State.Dying) {
            if(mSeek) {
                if(seekUseForce) {
                    mSeekCurDir = mActiveForce.normalized;
                    mSeekCurDirVel = Vector3.zero;
                }

                state = (int)(seekUseForce ? State.SeekForce : State.Seek);
            }
            else
                state = (int)State.Active;
        }
    }

    void OnUpUpdate() {
        if(simple) {
            applyDirToUp.up = mCurVelocity;
        }
        else {
            if(mBody != null && mBody.velocity != Vector3.zero) {
                applyDirToUp.up = mBody.velocity;
            }
        }
    }

    protected void SimpleCheckContain() {
        if(mSphereColl) {
            Vector3 pos = mSphereColl.bounds.center;
            Collider[] cols = Physics.OverlapSphere(pos, mSphereColl.radius, simpleLayerMask);
            for(int i = 0, max = cols.Length; i < max; i++) {
                Collider col = cols[i];
                Vector3 dir = (col.bounds.center - pos).normalized;
                ProcessContact(col.gameObject, pos, dir);
            }
        }
    }

    protected void DoSimpleMove(Vector3 delta) {
        float d = delta.magnitude;

        if(d > 0.0f) {
            Vector3 dir = new Vector3(delta.x / d, delta.y / d, delta.z / d);
            DoSimpleMove(dir, d);
        }
        else
            SimpleCheckContain();
    }

    protected void DoSimpleMove(Vector3 dir, float distance) {
        if(mSphereColl) {
            Vector3 pos = mSphereColl.bounds.center;

            //check if hit something
            RaycastHit hit;
            if(Physics.SphereCast(pos, mSphereColl.radius, dir, out hit, distance, simpleLayerMask)) {
                mLastHit.col = hit.collider;
                mLastHit.normal = hit.normal;
                mLastHit.point = hit.point;

                if(mBody)
                    mBody.MovePosition(hit.point + hit.normal * mSphereColl.radius);
                else
                    transform.position = hit.point + hit.normal * mSphereColl.radius;

                ProcessContact(hit.collider.gameObject, hit.point, hit.normal);
            }
            else {
                //transform.position = transform.position + dir*distance;
                SimpleCheckContain();
            }
        }

        //make sure we are still active
        if(isAlive) {
            if(mBody)
                mBody.MovePosition(transform.position + dir*distance);
            else
                transform.position = transform.position + dir*distance;
        }
    }

    void DoSimple() {
        Vector3 curV = mCurVelocity * mMoveScale;

        Vector3 delta = curV * Time.fixedDeltaTime;
        DoSimpleMove(delta);
    }

    void OnSuddenDeath() {
        Release();
    }

    protected virtual void FixedUpdate() {
        switch((State)state) {
            case State.Active:
                if(simple) {
                    DoSimple();
                }
                else {
                    if(mBody != null) {
                        if(speedLimit > 0.0f) {
                            float sqrSpd = mBody.velocity.sqrMagnitude;
                            if(sqrSpd > speedLimit * speedLimit) {
                                mBody.velocity = (mBody.velocity / Mathf.Sqrt(sqrSpd)) * speedLimit;
                            }
                        }

                        if(mActiveForce != Vector3.zero)
                            mBody.AddForce(mActiveForce * mMoveScale);

                        mCurVelocity = mBody.velocity;
                    }
                }
                break;

            case State.SeekForce:
                if(mBody && mSeek != null) {
                    Vector3 pos = transform.position;
                    Vector3 dest = mSeek.position;
                    Vector3 _dir = dest - pos; _dir.z = 0.0f;
                    float dist = _dir.magnitude;

                    if(dist > 0.0f) {
                        _dir /= dist;

                        if(seekAngleLimit < 360.0f) {
                            _dir = M8.MathUtil.DirCap(mInitDir, _dir, seekAngleLimit);
                        }

                        if(seekForceDelay > 0.0f)
                            mSeekCurDir = Vector3.SmoothDamp(mSeekCurDir, _dir, ref mSeekCurDirVel, seekForceDelay, Mathf.Infinity, Time.fixedDeltaTime);
                        else
                            mSeekCurDir = _dir;
                    }

                    if(speedLimit > 0.0f) {
                        float sqrSpd = mBody.velocity.sqrMagnitude;
                        if(sqrSpd > speedLimit * speedLimit) {
                            mBody.velocity = (mBody.velocity / Mathf.Sqrt(sqrSpd)) * speedLimit;
                        }
                    }

                    mBody.AddForce(mSeekCurDir * force * mMoveScale);

                    mCurVelocity = mBody.velocity;
                }
                break;

            case State.Seek:
                if(simple) {
                    if(mSeek != null) {
                        //steer torwards seek
                        Vector3 pos = transform.position;
                        Vector3 dest = mSeek.position;
                        Vector3 _dir = dest - pos;
                        float dist = _dir.magnitude;

                        if(dist > 0.0f) {
                            _dir /= dist;

                            //restrict
                            if(seekTurnAngleCap < 360.0f) {
                                _dir = M8.MathUtil.DirCap(mBody.velocity.normalized, _dir, seekTurnAngleCap);
                            }

                            mCurVelocity = M8.MathUtil.Steer(mBody.velocity, _dir * seekVelocity, seekVelocityCap, mMoveScale);
                        }
                    }

                    DoSimple();
                }
                else {
                    if(mBody != null && mSeek != null) {
                        //steer torwards seek
                        Vector3 pos = transform.position;
                        Vector3 dest = mSeek.position;
                        Vector3 _dir = dest - pos;
                        float dist = _dir.magnitude;

                        if(dist > 0.0f) {
                            _dir /= dist;

                            //restrict
                            if(seekTurnAngleCap < 360.0f) {
                                _dir = M8.MathUtil.DirCap(mBody.velocity.normalized, _dir, seekTurnAngleCap);
                            }

                            Vector3 force = M8.MathUtil.Steer(mBody.velocity, _dir * seekVelocity, seekVelocityCap, 1.0f);
                            mBody.AddForce(force, ForceMode.VelocityChange);
                        }

                        mCurVelocity = mBody.velocity;
                    }
                }
                break;
        }
    }

    void OnDrawGizmos() {
        if(explodeRadius > 0.0f) {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere((explodeOfsTarget ? explodeOfsTarget : transform).localToWorldMatrix.MultiplyPoint(explodeOfs), explodeRadius);
        }

        if(!string.IsNullOrEmpty(deathSpawnGroup) && !string.IsNullOrEmpty(deathSpawnType)) {
            Color c = Color.gray; c.a = 0.5f;
            Gizmos.color = c;
            Vector2 p = transform.localToWorldMatrix.MultiplyPoint(deathSpawnOfs);
            Gizmos.DrawWireSphere(p, 0.1f);
        }
    }

    private void DoExplode() {
        Vector3 pos = (explodeOfsTarget ? explodeOfsTarget : transform).localToWorldMatrix.MultiplyPoint(explodeOfs);
        //float explodeRadiusSqr = explodeRadius * explodeRadius;

        //TODO: spawn fx

        Collider[] cols = Physics.OverlapSphere(pos, explodeRadius, explodeMask.value);

        foreach(Collider col in cols) {
            if(col != null && col.rigidbody != null && M8.Util.CheckTag(col, hitTags)) {
                //hurt?
                col.rigidbody.AddExplosionForce(explodeForce, pos, explodeRadius, explodeUpwardMod, explodeForceMode);

                //float distSqr = (col.transform.position - pos).sqrMagnitude;

                if(mDamage)
                    mDamage.CallDamageTo(col.gameObject, pos, (col.bounds.center - pos).normalized);
            }
        }
    }
}
