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

    public Transform hook;
    public Transform cursor;
    public Rigidbody attachPointBody;

    public float minLengthClip; //minimum length required to clip

    public float maxLength;
    public float minLength;

    public float fireSpeed;
    public float expandSpeed;

    public float pullForce; //when we are at max length, do pull

    public float ropeSpringForce;

    public LayerMask entityMask;
    public LayerMask geoMask;

    private IEnumerator mAction;

    private State mState = State.None;

    private float mRopeActiveLength;
    private float mRopeTotalLength;

    private List<Transform> mRopes = new List<Transform>(ropeMax);
    private Transform mActiveRope;

    private SpringJoint mRopeJoint;

    /// <summary>
    /// Called once for initialization
    /// </summary>
    protected override void OnInit() {

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

        cursor.gameObject.SetActive(false);

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

    void ClearRopes() {
        for(int i = 0; i < mRopes.Count; i++) {
            if(mRopes[i])
                PoolController.ReleaseAuto(mRopes[i]);
        }

        mRopes.Clear();

        if(mActiveRope) {
            PoolController.ReleaseAuto(mActiveRope);
            mActiveRope = null;
        }

        if(mRopeJoint) {
            Destroy(mRopeJoint);
            mRopeJoint = null;
        }
    }

    void Detach() {
        ClearRopes();

        Player player = Player.instance;
        player.transform.rotation = Quaternion.identity;
        player.rigidbody.constraints |= RigidbodyConstraints.FreezeRotationZ;
        player.controller.wallStick = true;
        player.controller.gravityController.orientUp = true;

        headClosed = true;
    }

    Vector3 GetCurrentDir() {
        InputManager input = InputManager.instance;
        Player player = Player.instance;

        //determine dir
        Vector3 dir = new Vector3(input.GetAxis(0, InputAction.MoveX), input.GetAxis(0, InputAction.MoveY));
        if(dir != Vector3.zero) {
            if(player.controller.isGrounded && dir.y < 0.0f)
                dir.y = 0.0f;

            dir = player.controller.dirHolder.rotation*dir;
            dir.Normalize();
        }
        return dir;
    }

    IEnumerator DoFire() {
        mState = State.Firing;

        headClosed = false;
        cursor.gameObject.SetActive(false);

        hook.gameObject.SetActive(true);

        mActiveRope = PoolController.Spawn(projGrp, ropeType, "", null);
                
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
            if(Physics.Raycast(basePt, dir, out hit, mRopeActiveLength, entityMask | geoMask)) {
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
                            
            mActiveRope.position = basePt;

            Vector3 s = mActiveRope.localScale;
            s.y = mRopeActiveLength;
            mActiveRope.localScale = s;
            mActiveRope.up = dir;

            hook.rotation = mActiveRope.rotation;
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

        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        Vector3 dir = Player.instance.controllerAnim.isLeft ? -Player.instance.controller.dirHolder.right : Player.instance.controller.dirHolder.right;
        while(true) {
            Vector3 basePt = firePos;

            Vector3 nDir = GetCurrentDir();
            if(nDir != Vector3.zero)
                dir = nDir;

            RaycastHit hit;
            if(Physics.Raycast(basePt, dir, out hit, maxLength, entityMask | geoMask)) {
                cursor.gameObject.SetActive(true);
                cursor.position = hit.point;
            }
            else {
                cursor.gameObject.SetActive(false);
            }

            yield return wait;
        }
    }

    IEnumerator DoAttachedUpdate(Vector3 attachPt) {
        mState = State.Attached;

        attachPointBody.transform.position = attachPt;

        InputManager input = InputManager.instance;

        Player player = Player.instance;
        Rigidbody playerBody = player.rigidbody;

        playerBody.constraints &= ~RigidbodyConstraints.FreezeRotationZ;
        player.controller.wallStick = false;
        player.controller.gravityController.orientUp = false;
                
        mRopeJoint = player.gameObject.AddComponent<SpringJoint>();
        mRopeJoint.connectedBody = attachPointBody;
        mRopeJoint.spring = ropeSpringForce;
        mRopeJoint.anchor = new Vector3(0f, firePos.y - playerBody.position.y, 0f);
        mRopeJoint.maxDistance = mRopeJoint.minDistance = 0.0f;

        float startRopeLength = mRopeActiveLength;

        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        while(true) {
            Vector3 dir = attachPt - firePos; 
            dir.Normalize();

            float yAxis = input.GetAxis(0, InputAction.MoveY);
            if(Mathf.Abs(yAxis) > Player.inputDirThreshold) {
                float newLength = Mathf.Clamp(mRopeActiveLength + yAxis*expandSpeed*Time.fixedDeltaTime, minLength, maxLength);
                if(mRopeActiveLength != newLength) {
                    mRopeActiveLength = newLength;
                    mRopeJoint.maxDistance = mRopeJoint.minDistance = mRopeActiveLength - startRopeLength;
                }
                else if(mRopeActiveLength == maxLength)
                    playerBody.AddForce(-dir*pullForce);
            }

            yield return wait;
        }
    }

    IEnumerator DoChargedToEntity(EntityBase ent) {
        yield break;
    }
}
