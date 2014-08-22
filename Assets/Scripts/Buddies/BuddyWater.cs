using UnityEngine;
using System.Collections;

public class BuddyWater : Buddy {
    public const string grabHolderName = "waterGrabbed";

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

    public Transform grabbedContainer; //display purpose
    public Vector3 grabbedContainerOfs = new Vector3(0, 0, -2);
    public float grabbedScaleOfs = 0.1f;

    public Transform reticle;
    public float reticleScale = 1.15f;
    public float reticleUpdateDelay = 0.1f;
    public float reticleDistance = 5.5f;
    public LayerMask reticleCheckMask;

    private AnimatorData mBodyAnim;

    private int mTakeBodyEnter;
    private int mTakeBodyExit;
    private int mTakeBodyNormal;

    private int mTakeTentacleAttack;

    private bool mCharging;
    private IEnumerator mSubAction;
    private IEnumerator mReticleAction;

    private Transform mGrabbedContainerParent;

    public override bool canFire { get { return activeGO.activeSelf && !mCharging; } }

    protected override void OnInit() {
        mBodyAnim = GetComponent<AnimatorData>();

        mTakeBodyEnter = mBodyAnim.GetTakeIndex("enter");
        mTakeBodyExit = mBodyAnim.GetTakeIndex("exit");
        mTakeBodyNormal = mBodyAnim.GetTakeIndex("normal");

        mTakeTentacleAttack = tentaclesAnim.GetTakeIndex("attack");

        chargeAttackGO.SetActive(false);

        grabber.grabCallback += OnGrabber;

        mGrabbedContainerParent = grabbedContainer.parent;
        grabbedContainer.gameObject.SetActive(false);

        int retCount = reticle.childCount;
        if(retCount > 0) {
            float angleInc = 360.0f/retCount;
            float angle = 0;
            for(int i = 0; i < retCount; i++) {
                Transform t = reticle.GetChild(i);
                t.localRotation = Quaternion.Euler(0, 0, angle);
                angle += angleInc;
            }
        }

        reticle.gameObject.SetActive(false);

        UserData.instance.actCallback += OnUserDataAction;
        SceneManager.instance.sceneChangeCallback += OnSceneChange;
    }

    protected override void OnDeinit() {
        if(UserData.instance) UserData.instance.actCallback -= OnUserDataAction;
        if(SceneManager.instance) SceneManager.instance.sceneChangeCallback -= OnSceneChange;
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

        //check if we previously had a holder
        Transform holder = SceneManager.instance.StoreGetObject(grabHolderName);
        if(holder) { //duplicate
            GameObject go = Instantiate(holder.gameObject) as GameObject;
            go.transform.parent = null;
            go.SetActive(true);
            Grab g = go.GetComponent<Grab>();
            grabber.grab = g;
            g.Grabbed(grabber);
            OnGrabber(grabber);
        }
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

        if(mReticleAction != null) {
            StopCoroutine(mReticleAction);
            mReticleAction = null;
        }

        reticle.gameObject.SetActive(false);
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

        Transform grabT = g.grab.transform;
        Bounds grabB = g.grab.collider.bounds;
        float grabbedContainerS = Mathf.Max(grabB.extents.x, grabB.extents.y) + grabbedScaleOfs;

        grabbedContainer.gameObject.SetActive(true);
        grabbedContainer.parent = grabT;
        grabbedContainer.localPosition = grabT.worldToLocalMatrix.MultiplyPoint(grabB.center) + grabbedContainerOfs;
        grabbedContainer.localScale = new Vector3(grabbedContainerS, grabbedContainerS, 1.0f);

        FireStop();
    }

    void OnUserDataAction(UserData ud, UserData.Action act) {
        if(act == UserData.Action.Save) {
            SceneManager smgr = SceneManager.instance;
            smgr.StoreDestroyObject(grabHolderName);
            if(grabber.grab) { //duplicate
                GameObject store = Instantiate(grabber.grab.gameObject) as GameObject;
                FixedJoint joint = store.GetComponent<FixedJoint>();
                if(joint) Destroy(joint);
                store.name = grabHolderName;
                EntityBase ent = store.GetComponent<EntityBase>();
                if(ent) {
                    ent.state = (int)EntityState.Invalid;
                    if(ent.activator) { Destroy(ent.activator); ent.activator = null; }
                    ent.activateOnStart = true;
                }
                Transform container = store.transform.Find(grabbedContainer.name);
                if(container) Destroy(container.gameObject);
                smgr.StoreAddObject(store.transform);
            }
        }
    }

    void OnSceneChange(string nextScene) {
        if(Application.loadedLevelName != nextScene) {
            SceneManager smgr = SceneManager.instance;
            smgr.StoreDestroyObject(grabHolderName);
            if(grabber.grab) {
                grabbedContainer.parent = mGrabbedContainerParent;
                FixedJoint joint = grabber.grab.GetComponent<FixedJoint>();
                if(joint) Destroy(joint);
                EntityBase ent = grabber.grab.GetComponent<EntityBase>();
                if(ent) {
                    ent.state = (int)EntityState.Invalid;
                    if(ent.activator) { Destroy(ent.activator); ent.activator = null; }
                    ent.activateOnStart = true;
                }
                grabber.grab.name = grabHolderName;
                smgr.StoreAddObject(grabber.grab.transform);
            }
        }
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

    IEnumerator DoReticle() {
        WaitForSeconds wait = new WaitForSeconds(reticleUpdateDelay);

        GameObject reticleGO = reticle.gameObject;

        reticleGO.SetActive(true);

        SphereCollider scoll = grabber.collider as SphereCollider;
        float radius = scoll ? scoll.radius : 0.5f;

        while(activeGO.activeSelf && !grabber.grab) {
            Player p = Player.instance;

            Vector3 pos = p.collider.bounds.center;
            Vector3 dir = fireDirWorld;

            RaycastHit hit;
            Grab grab;
            if(Physics.SphereCast(pos, radius, dir, out hit, reticleDistance, reticleCheckMask) && (grab = hit.collider.GetComponent<Grab>()) && grab.isGrabbable) {
                Transform t = hit.collider.transform;
                if(reticle.parent != t) {
                    Bounds hitB = hit.collider.bounds;
                    reticleGO.SetActive(true);
                    reticle.parent = t;
                    reticle.localPosition = t.worldToLocalMatrix.MultiplyPoint(hitB.center);
                    reticle.localScale = Vector3.one;
                    UpdateReticle(Mathf.Max(hitB.extents.x, hitB.extents.y));
                }
            }
            else if(reticleGO.activeSelf) {
                reticle.parent = transform;
                reticleGO.SetActive(false);
            }

            yield return wait;
        }

        reticleGO.SetActive(false);

        mReticleAction = null;
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

            if(!grabber.grab && mReticleAction == null)
                StartCoroutine(mReticleAction = DoReticle());
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

        grabbedContainer.parent = mGrabbedContainerParent;
        grabbedContainer.gameObject.SetActive(false);
    }

    void UpdateReticle(float dist) {
        int retCount = reticle.childCount;
        for(int i = 0; i < retCount; i++) {
            Transform t = reticle.GetChild(i);
            Quaternion r = t.localRotation;
            Vector3 newPos = (r*Vector3.right)*(dist*reticleScale);
            t.localPosition = new Vector3(newPos.x, newPos.y, t.localPosition.z);
        }
    }
}
