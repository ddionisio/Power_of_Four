using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BuddyRope : Buddy {
    public enum State {
        None,
        Firing,
        Attached,
        AttachedEntity, //charge towards entity
    }
    public const int ropeMax = 20;

    public string ropeType;
    public float ropeWidth = 0.34f;

    public Transform hook;
    public Transform cursor;

    public Rigidbody attachPointBody;
    public float attachPlayerDrag = 0.015f; //player's drag when attached

    public TransJointAttachUpScale activeRope; //the rope for swinging
        
    public Transform indicator;
    public Color indicatorColorInvalid = Color.red;
    public Color indicatorColorValid = Color.green;
    public float indicatorOfs = 0.5f;

    public float minLengthClip; //minimum length required to clip

    public float maxLength;
    public float minLength;

    public float fireSpeed;
    public float expandSpeed;

    public float pullForce; //when we are at max length, do pull

    public float ropeSpringForce = 30f;
    public float ropeSpringDamp = 1f;

    public LayerMask entityMask;
    public LayerMask geoMask;

    private IEnumerator mAction;

    private State mState = State.None;

    private float mRopeActiveLength;
    private float mRopeTotalLength;

    private List<Transform> mRopes = new List<Transform>(ropeMax);

    private SpringJoint mRopeJoint;
    private FixedJoint mPlayerJoint;

    private Material mIndicatorMat;
    
    public override bool canFire {
        get {
            return mState != State.Firing;
        }
    }

    /// <summary>
    /// Called once for initialization
    /// </summary>
    protected override void OnInit() {
        hook.gameObject.SetActive(false);
        cursor.gameObject.SetActive(false);
        attachPointBody.gameObject.SetActive(false);
        activeRope.gameObject.SetActive(false);

        mIndicatorMat = indicator.renderer.material;
        indicator.gameObject.SetActive(false);
    }

    protected override void OnDeinit() {
    }

    /// <summary>
    /// Called once we enter during activate
    /// </summary>
    protected override void OnEnter() {
        StartCoroutine(mAction = DoCursorUpdate());
    }

    protected override IEnumerator OnEntering() {
        yield break;
    }

    /// <summary>
    /// Called once we exit during deactivate
    /// </summary>
    protected override void OnExit() {
        if(mAction != null) { StopCoroutine(mAction); mAction = null; }

        hook.gameObject.SetActive(false);
        cursor.gameObject.SetActive(false);
        indicator.gameObject.SetActive(false);

        Detach();

        mState = State.None;
    }

    protected override IEnumerator OnExiting() {
        yield break;
    }

    protected override void OnDirChange() {
    }

    /// <summary>
    /// Called when we are about to fire stuff
    /// </summary>
    protected override void OnFireStart() {
    }

    /// <summary>
    /// Called when ready to fire something
    /// </summary>
    protected override void OnFire() {
        switch(mState) {
            case State.None:
                if(mAction != null) { StopCoroutine(mAction); mAction = null; }
                StartCoroutine(mAction = DoFire());
                break;

            case State.Attached:
            case State.AttachedEntity:
                if(mAction != null) { StopCoroutine(mAction); mAction = null; }
                Detach();
                StartCoroutine(mAction = DoCursorUpdate());
                break;
        }
    }

    /// <summary>
    /// Called when we stop firing
    /// </summary>
    protected override void OnFireStop() {
    }

    protected override void OnFireClear() {
        OnExit();
    }

    void ClearRopes() {
        for(int i = 0; i < mRopes.Count; i++) {
            if(mRopes[i])
                PoolController.ReleaseAuto(mRopes[i]);
        }

        mRopes.Clear();

        if(mRopeJoint) {
            Destroy(mRopeJoint);
            mRopeJoint = null;
        }
                
        activeRope.alive = false;
        activeRope.gameObject.SetActive(false);

        if(mPlayerJoint) {
            Destroy(mPlayerJoint);
            mPlayerJoint = null;
        }
    }

    void Detach() {
        ClearRopes();

        Player player = Player.instance;
        player.transform.rotation = Quaternion.identity;
        player.controller.wallStick = true;
        player.controller.gravityController.orientUp = true;
        player.controller.slideDisable = false;
        player.controller.lockDrag = false;

        attachPointBody.gameObject.SetActive(false);

        headClosed = true;
    }

    void IndicatorUpdate(float dist, Vector3 dir, bool isValid) {
        float len = Mathf.Max(0f, dist - indicatorOfs);
        Vector3 pos = firePos + indicatorOfs*dir; pos.z = indicator.position.z;
        indicator.position = pos;

        indicator.up = dir;

        Vector3 s = indicator.localScale;
        s.y = len;
        indicator.localScale = s;

        mIndicatorMat.color = isValid ? indicatorColorValid : indicatorColorInvalid;
    }

    Vector3 GetCurrentDir() {
        InputManager input = InputManager.instance;
        Player player = Player.instance;

        //determine dir
        Vector3 dir = new Vector3(input.GetAxis(0, InputAction.MoveX), Mathf.Max(0.0f, input.GetAxis(0, InputAction.MoveY)));
        if(dir != Vector3.zero && !player.controller.isWallStick) {
            dir = player.controller.dirHolder.rotation*dir;
            dir.Normalize();
        }
        else {
            dir = player.controllerAnim.isLeft ? -player.controller.dirHolder.right : player.controller.dirHolder.right;
        }
        return dir;
    }

    IEnumerator DoFire() {
        mState = State.Firing;

        headClosed = false;
        cursor.gameObject.SetActive(false);

        activeRope.gameObject.SetActive(true);
        activeRope.rigidbody.isKinematic = true;
        activeRope.alive = false;

        indicator.gameObject.SetActive(false);

        hook.gameObject.SetActive(true);

        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        Vector3 dir = GetCurrentDir();
        if(dir == Vector3.zero)
            dir = Player.instance.controllerAnim.isLeft ? -Player.instance.controller.dirHolder.right : Player.instance.controller.dirHolder.right;
        
        //play anim

        //expand
        mRopeActiveLength = 0.0f;
        while(mRopeActiveLength < maxLength) {
            Vector3 basePt = firePos;

            mRopeActiveLength += fireSpeed*Time.fixedDeltaTime;

            RaycastHit hit;
            if(Physics.Raycast(basePt, dir, out hit, mRopeActiveLength, entityMask | geoMask) && hit.distance >= minLength) {
                //determine if it's geometry or enemy
                GameObject hitGo = hit.collider.gameObject;
                if((entityMask & (1<<hitGo.layer)) != 0) {
                    StartCoroutine(mAction = DoChargedToEntity(hitGo.GetComponent<EntityBase>()));
                    yield break;
                }
                else if((geoMask & (1<<hitGo.layer)) != 0) {
                    StartCoroutine(mAction = DoAttachedUpdate(hit.point));
                    yield break;
                }

                break;
            }

            activeRope.transform.position = basePt;
            activeRope.transform.up = dir;

            Vector3 s = activeRope.target.localScale;
            s.y = mRopeActiveLength;
            activeRope.target.localScale = s;

            hook.up = dir;
            hook.position = basePt + dir*mRopeActiveLength;

            yield return wait;
        }

        ClearRopes();
                
        StartCoroutine(mAction = DoCursorUpdate());
    }

    IEnumerator DoCursorUpdate() {
        mState = State.None;

        headClosed = true;

        hook.gameObject.SetActive(false);

        indicator.gameObject.SetActive(true);

        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        Vector3 dir = Player.instance.controllerAnim.isLeft ? -Player.instance.controller.dirHolder.right : Player.instance.controller.dirHolder.right;
        while(true) {
            Vector3 basePt = firePos;

            Vector3 nDir = GetCurrentDir();
            if(nDir != Vector3.zero)
                dir = nDir;

            RaycastHit hit;
            if(Physics.Raycast(basePt, dir, out hit, maxLength, entityMask | geoMask) && hit.distance >= minLength) {
                cursor.gameObject.SetActive(true);
                cursor.position = hit.point;

                IndicatorUpdate(hit.distance, dir, true);
            }
            else {
                cursor.gameObject.SetActive(false);

                IndicatorUpdate(maxLength, dir, false);
            }
                        
            yield return wait;
        }
    }

    IEnumerator DoAttachedUpdate(Vector3 attachPt) {
        mState = State.Attached;

        //indicator.gameObject.SetActive(true);
        indicator.gameObject.SetActive(false);

        hook.position = attachPt;

        attachPointBody.gameObject.SetActive(true);
        attachPointBody.transform.position = attachPt;

        Vector3 dir = (attachPt - firePos).normalized;

        InputManager input = InputManager.instance;

        Player player = Player.instance;
        Rigidbody playerBody = player.rigidbody;

        playerBody.drag = attachPlayerDrag;

        player.controller.slideDisable = true;
        player.controller.lockDrag = true;
        player.controller.wallStick = false;
        player.controller.gravityController.orientUp = false;
        player.transform.up = dir;
                
        dir = (attachPt - firePos).normalized;

        activeRope.transform.position = firePos;
        activeRope.transform.up = dir;

        mRopeJoint = activeRope.gameObject.AddComponent<SpringJoint>();
        mRopeJoint.autoConfigureConnectedAnchor = false;
        mRopeJoint.connectedAnchor = Vector3.zero;
        mRopeJoint.anchor = Vector3.zero;
        mRopeJoint.connectedBody = attachPointBody;
        mRopeJoint.spring = ropeSpringForce;
        mRopeJoint.minDistance = minLength;
        mRopeJoint.maxDistance = mRopeActiveLength;

        activeRope.rigidbody.isKinematic = false;
        activeRope.alive = true;

        mPlayerJoint = player.gameObject.AddComponent<FixedJoint>();
        mPlayerJoint.connectedBody = activeRope.rigidbody;
        
        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        while(true) {
            dir = (attachPt - firePos).normalized;

            hook.up = dir;

            float yAxis = input.GetAxis(0, InputAction.MoveY);
            if(Mathf.Abs(yAxis) > Player.inputDirThreshold) {
                float newLength = Mathf.Clamp(mRopeActiveLength - yAxis*expandSpeed*Time.fixedDeltaTime, minLength, maxLength);
                if(mRopeActiveLength != newLength) {
                    mRopeActiveLength = newLength;
                    mRopeJoint.maxDistance = mRopeActiveLength;
                }
                else if(mRopeActiveLength == maxLength)
                    playerBody.AddForce(-dir*pullForce);
            }

            playerBody.MoveRotation(hook.rotation);

            yield return wait;
        }
    }

    IEnumerator DoChargedToEntity(EntityBase ent) {
        yield break;
    }
}
