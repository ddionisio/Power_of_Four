using UnityEngine;
using System.Collections;

public class BuddyWater : Buddy {
    public SpriteRenderer bodySpriteRender;
    public Sprite bodyActiveSprite;
    public Sprite bodyInactiveSprite;

    public Transform eye;

    public GameObject activeGO;
    public AnimatorData tentaclesAnim;

    //public GameObject 
    public float chargeDelay = 1.0f;
    public Vector2 chargeForce;
    public float chargeMaxSpeed = 16.0f;
    public float chargeDrag = 0;
    public GameObject chargeAttackGO;

    public Grabber grabber;
    public string grabberProjType; //for throwing

    private AnimatorData mBodyAnim;

    private int mTakeBodyEnter;
    private int mTakeBodyExit;
    private int mTakeBodyNormal;

    private int mTakeTentacleAttack;

    private bool mCharging;
    private IEnumerator mSubAction;

    public override bool canFire { get { return activeGO.activeSelf && !mCharging; } }

    protected override void OnInit() {
        mBodyAnim = GetComponent<AnimatorData>();

        mTakeBodyEnter = mBodyAnim.GetTakeIndex("enter");
        mTakeBodyExit = mBodyAnim.GetTakeIndex("exit");
        mTakeBodyNormal = mBodyAnim.GetTakeIndex("normal");

        mTakeTentacleAttack = tentaclesAnim.GetTakeIndex("attack");

        chargeAttackGO.SetActive(false);

        grabber.grabCallback += OnGrabber;
    }

    protected override void OnEnter() {
        if(!ApplyActive())
            StartCoroutine(mSubAction = DoActivate());
    }

    protected override IEnumerator OnEntering() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        mBodyAnim.Play(mTakeBodyEnter);
        while(mBodyAnim.isPlaying)
            yield return wait;

        mBodyAnim.Play(mTakeBodyNormal);
    }

    protected override void OnExit() {
        if(mSubAction != null) {
            StopCoroutine(mSubAction);
            mSubAction = null;
        }

        if(mCharging)
            ApplyCharge(false);

        if(grabber.grab)
            Throw();
    }

    protected override IEnumerator OnExiting() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        mBodyAnim.Play(mTakeBodyExit);
        while(mBodyAnim.isPlaying)
            yield return wait;
    }

    protected override void OnDirChange() {
        if(activeGO.activeSelf) {
            switch(dir) {
                case Player.LookDir.Up:
                    eye.localRotation = Quaternion.identity;
                    break;
                case Player.LookDir.Front:
                    eye.localRotation = Quaternion.Euler(0f, 0f, -90f);
                    break;
                case Player.LookDir.Down:
                    eye.localRotation = Quaternion.Euler(0f, 0f, 180f);
                    break;
            }
        }
    }

    protected override void OnFire() {
        if(grabber.grab) {
            Throw();

            if(!ApplyActive())
                StartCoroutine(mSubAction = DoActivate());
        }
        else
            StartCoroutine(mSubAction = DoCharge());
    }

    void OnGrabber(Grabber g) {
        if(mSubAction != null) {
            StopCoroutine(mSubAction);
            mSubAction = null;
        }

        tentaclesAnim.PlayDefault();

        mLastFireTime = 0.0f;
        ApplyCharge(false);
        ApplyActive();

        FireStop();
    }

    IEnumerator DoCharge() {
        ApplyCharge(true);

        tentaclesAnim.Play(mTakeTentacleAttack);

        Player p = Player.instance;

        float force = 0.0f;
        Vector3 move = Vector3.zero;
        switch(p.lookDir) {
            case Player.LookDir.Front:
                move.x = p.controllerAnim.isLeft ? -1.0f : 1.0f;
                move.y = 0.0f;
                force = chargeForce.x;
                break;
            case Player.LookDir.Down:
                move.x = 0.0f;
                move.y = -1.0f;
                force = chargeForce.y;
                break;
            default:
                move.x = 0.0f;
                move.y = 1.0f;
                force = chargeForce.y;
                break;
        }

        Rigidbody pbody = p.rigidbody;

        pbody.drag = chargeDrag;

        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        while(Time.fixedTime - mLastFireTime < chargeDelay && p.state == (int)EntityState.Charge) {
            Vector3 moveDir = p.controller.dirHolder.rotation*move;

            //move
            if(pbody.velocity.sqrMagnitude <= chargeMaxSpeed*chargeMaxSpeed) {
                pbody.AddForce(moveDir*force);
            }
            else
                pbody.velocity = moveDir*chargeMaxSpeed;

            yield return wait;
        }

        mLastFireTime = Time.fixedTime; //refresh last fire time

        ApplyCharge(false);
        ApplyActive();
        StartCoroutine(mSubAction = DoActivate());
    }

    IEnumerator DoActivate() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        while(Time.fixedTime - mLastFireTime < fireRate)
            yield return wait;

        ApplyActive();

        mSubAction = null;
    }

    /// <summary>
    /// Returns true if activated
    /// </summary>
    bool ApplyActive() {
        bool yes = Time.fixedTime - mLastFireTime >= fireRate;
        if(yes) {
            bodySpriteRender.sprite = bodyActiveSprite;
            activeGO.SetActive(true);
            OnDirChange();
        }
        else {
            bodySpriteRender.sprite = bodyInactiveSprite;
            activeGO.SetActive(false);
        }

        return yes;
    }

    void ApplyCharge(bool yes) {
        mCharging = yes;

        Player p = Player.instance;

        chargeAttackGO.SetActive(yes);

        if(mCharging) {
            p.state = (int)EntityState.Charge;
        }
        else {
            if(p.state == (int)EntityState.Charge) {
                p.state = (int)EntityState.Normal;

                //reset velocity x to appropriate max speed
                Vector3 lv = p.controller.localVelocity;
                float maxSpeed = p.controller.isGrounded ? p.controller.moveMaxSpeed : p.controller.airMaxSpeed;
                float curSpeed = Mathf.Abs(lv.x);
                if(curSpeed > maxSpeed) {
                    lv.x = Mathf.Sign(lv.x)*maxSpeed;
                    p.controller.localVelocity = lv;
                }
            }
        }
    }

    void Throw() {
        Grab grab = grabber.grab;
        grabber.grab = null;
                
        Vector3 pos = grab.collider.bounds.center; pos.z = 0.0f;
        Vector3 dir = fireDirWorld;
                
        GrabProjectile proj = Projectile.Create(projGrp, grabberProjType, pos, dir, null) as GrabProjectile;
        proj.grab = grab;

        grab.Throw(grabber, pos, dir);
    }
}
