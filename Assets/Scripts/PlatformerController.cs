using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlatformerController : RigidBodyController {
    public delegate void Callback(PlatformerController ctrl);

    public int jumpCounter = 1;
    public float jumpImpulse = 2f;
    public float jumpWallImpulse = 8f;
    public float jumpWallUpImpulse = 4f;
    public float jumpWaterForce = 5f;
    public float jumpForce = 80.0f;
    public float jumpDelay = 0.1f;

    public bool jumpWall = false; //wall jump
    public float jumpWallLockDelay = 0.1f;

    public bool jumpDropAllow = true; //if true, player can jump when they are going down
    public float jumpAirDelay = 0.1f; //allow player to jump if they are off the ground for a short time.

    public bool slideAllowJump = false;

    public float airDampForceX; //force to try to reduce the horizontal speed while mid-air
    public float airDampMinSpeedX; //minimum criteria of horizontal speed when dampening

    public bool wallStick = true;
    public bool wallStickPush = false; //if true, player must press the direction towards the wall to stick
    public bool wallStickDownOnly = false; //if true, only stick to wall if we are going downwards
    public float wallStickAngleOfs = 10.0f; //what angle is acceptible to wall stick, within 90 based on dirHolder's up
    public float wallStickDelay = 0.2f; //delay to stick to wall when moving against one
    public float wallStickUpDelay = 0.2f; //how long to move up the wall once you stick
    public float wallStickUpForce = 60f; //slightly move up the wall
    public float wallStickForce = 40f; //move towards the wall
    [Tooltip("Ease towards wallStickDownSpeedCap, make sure to start at 0 and end at 1.")]
    public AnimationCurve wallStickDownEase;
    public float wallStickDownSpeedCap = 5.0f; //reduce speed upon sticking to wall if going downward, 'friction'
    public LayerMask wallStickInvalidMask; //layer masks that do not allow wall stick

    public LayerMask plankLayer;
    public float plankCheckDelay; //delay to check if we can revert plank collision

    public int player = 0;
    public int moveInputX = InputManager.ActionInvalid;
    public int moveInputY = InputManager.ActionInvalid;
    public int jumpInput = InputManager.ActionInvalid;

    public bool startInputEnabled = false;
    public bool jumpReleaseClearVelocity = false;
    public float jumpReleaseClearVelocityTo = 0.0f;

    public event Callback landCallback;
    public event Callback jumpCallback;

    private const string mInvokePlankEndIgnore = "OnPlankEndIgnore";

    private bool mInputEnabled = false;

    private bool mJump = false;
    private int mJumpCounter = 0;
    private float mJumpLastTime = 0.0f;
    private bool mJumpingWall = false;
    private bool mJumpInputDown = false;

    private bool mLastGround = false;

    private bool mWallSticking = false;
    private float mWallStickLastTime = 0.0f;
    private float mWallStickLastInputTime = 0.0f;
    private CollideInfo mWallStickCollInfo;
    private M8.MathUtil.Side mWallStickSide;
    private bool mWallStickWaitInput;

    private bool mIsOnPlatform;
    private int mIsOnPlatformLayerMask;

    private bool mMoveSideLock;

    private bool mPlankCheckActive;
    private Collider[] mPlankColliders = new Collider[8];
    private int mPlankCount;

    private float mLastGroundTime; //last time we were on ground

    private List<Collider> mTriggers = new List<Collider>(4); //triggers we entered

    public bool inputEnabled {
        get { return mInputEnabled; }
        set {
            if(mInputEnabled != value) {
                mInputEnabled = value;

                InputManager input = InputManager.instance;
                if(input != null) {
                    if(mInputEnabled) {
                        if(jumpInput != InputManager.ActionInvalid)
                            input.AddButtonCall(player, jumpInput, OnInputJump);
                    }
                    else {
                        if(jumpInput != InputManager.ActionInvalid)
                            input.RemoveButtonCall(player, jumpInput, OnInputJump);

                        mJumpInputDown = false;
                    }
                }
            }
        }
    }

    public int jumpCounterCurrent { get { return mJumpCounter; } set { mJumpCounter = value; } }

    /// <summary>
    /// Note: Fixed time.
    /// </summary>
    public float jumpLastTime { get { return mJumpLastTime; } }

    public bool isJump { get { return mJump; } }

    public bool isJumpWall { get { return mJumpingWall; } }

    public bool isWallStick { get { return mWallSticking; } }

    public bool isOnPlatform { get { return mIsOnPlatform; } }

    public float wallStickLastTime { get { return mWallStickLastTime; } }
    public CollideInfo wallStickCollide { get { return mWallStickCollInfo; } }
    public M8.MathUtil.Side wallStickSide { get { return mWallStickSide; } }

    public bool canWallJump {
        get { return jumpWall && mWallSticking; }
    }

    /// <summary>
    /// Set to true for manual use of moveSide
    /// </summary>
    public bool moveSideLock {
        get { return mMoveSideLock; }
        set { mMoveSideLock = value; }
    }

    public List<Collider> triggers { get { return mTriggers; } }

    public override void ResetCollision() {
        base.ResetCollision();

        mLastGround = false;
        mLastGroundTime = 0.0f;
        mJump = false;
        mJumpingWall = false;

        lockDrag = false;

        mWallSticking = false;
        mWallStickWaitInput = false;

        mIsOnPlatform = false;
        mIsOnPlatformLayerMask = 0;

        //clear planking
        if(mPlankCount > 0) {
            if(gameObject.activeInHierarchy && collider.enabled) {
                for(int i = 0; i < mPlankCount; i++) {
                    if(mPlankColliders[i] && mPlankColliders[i].gameObject.activeInHierarchy && mPlankColliders[i].enabled) {
                        Physics.IgnoreCollision(collider, mPlankColliders[i], false);
                    }
                    mPlankColliders[i] = null;
                }
            }
            else {
                for(int i = 0; i < mPlankCount; i++)
                    mPlankColliders[i] = null;
            }

            mPlankCount = 0;
        }
    }

    public void _PlatformSweep(bool isOn, int layer) {
        if(mIsOnPlatform != isOn) {
            mIsOnPlatform = isOn;
            mIsOnPlatformLayerMask = 1 << layer;

            RefreshCollInfo();
        }

        //if(!isOn && mJump)
        //mJumpLastTime = Time.fixedTime;
    }

    public bool CanWallStick(Vector3 up, Vector3 wallNormal) {
        float a = Vector3.Angle(up, wallNormal);
        return a >= 90.0f - wallStickAngleOfs && a <= 90.0f + wallStickAngleOfs;
    }

    /// <summary>
    /// Call this for manual input jumping
    /// </summary>
    public void Jump(bool down) {
        if(mJumpInputDown != down && !rigidbody.isKinematic) {
            mJumpInputDown = down;

            if(mJumpInputDown) {
                if(isUnderWater) {
                    mJumpingWall = false;
                    mJump = true;
                    mJumpCounter = 0;
                }
                else if(canWallJump) {

                    rigidbody.velocity = Vector3.zero;
                    lockDrag = true;
                    rigidbody.drag = airDrag;

                    Vector3 impulse = mWallStickCollInfo.normal * jumpWallImpulse;
                    impulse += dirHolder.up * jumpWallUpImpulse;

                    PrepJumpVel();
                    rigidbody.AddForce(impulse, ForceMode.Impulse);

                    mJumpingWall = true;
                    mJump = true;

                    mWallSticking = false;

                    mJumpLastTime = Time.fixedTime;
                    //mJumpCounter = Mathf.Clamp(mJumpCounter + 1, 0, jumpCounter);

                    mJumpCounter = 1;

                    if(jumpCallback != null)
                        jumpCallback(this);
                }
                else if(!isSlopSlide || slideAllowJump) {
                    if(isGrounded || isSlopSlide || (mJumpCounter < jumpCounter && (Time.fixedTime - mLastGroundTime < jumpAirDelay || jumpDropAllow || mJumpCounter > 0))) {
                        lockDrag = true;
                        rigidbody.drag = airDrag;

                        PrepJumpVel();

                        rigidbody.AddForce(dirHolder.up * jumpImpulse, ForceMode.Impulse);

                        mJumpCounter++;
                        mJumpingWall = false;

                        mWallSticking = false;

                        mJump = true;
                        mJumpLastTime = Time.fixedTime;

                        if(jumpCallback != null)
                            jumpCallback(this);
                    }
                }
            }
            else {
                if(jumpReleaseClearVelocity) {
                    Vector3 lv = localVelocity;
                    if(lv.y > jumpReleaseClearVelocityTo) {
                        lv.y = jumpReleaseClearVelocityTo;
                        localVelocity = lv;
                    }
                }
            }
        }
    }

    protected override void WaterEnter() {
        mJumpCounter = 0;
        mJumpingWall = false;
    }

    protected override void WaterExit() {
        if(mJump) {
            mJumpLastTime = Time.fixedTime;
        }
    }

    protected override bool CanMove(Vector3 dir, float maxSpeed) {

        //float x = localVelocity.x;
        float d = localVelocity.x * localVelocity.x;

        //disregard y (for better air controller)

        bool ret = d < maxSpeed * maxSpeed;

        //see if we are trying to move the opposite dir
        if(!ret) { //see if we are trying to move the opposite dir
            Vector3 velDir = rigidbody.velocity.normalized;
            ret = Vector3.Dot(dir, velDir) < moveCosCheck;
        }

        return ret;
    }

    float WallStickCurrentDownCap() {
        return wallStickDownEase.Evaluate(Time.fixedTime - mWallStickLastTime) * wallStickDownSpeedCap;
    }

    protected override void RefreshCollInfo() {
        //plank check, see if we need to ignore it
        if(plankLayer != 0) {
            bool plankFound = false;

            //check if there's a coll that is a plank
            for(int i = 0; i < mCollCount; i++) {
                CollideInfo inf = mColls[i];
                if(inf.collider == null || inf.collider.gameObject == null || !inf.collider.gameObject.activeInHierarchy) {
                    RemoveColl(i);
                    i--;
                }
                else if(((1 << inf.collider.gameObject.layer) & plankLayer) != 0 && inf.flag != CollisionFlags.Below) {
                    plankFound = true;

                    Collider coll = inf.collider;

                    RemoveColl(i);
                    i--;

                    SetPlankingIgnore(coll, true);
                }
            }

            if(plankFound)
                SetLocalVelocityToBody();
        }

        base.RefreshCollInfo();

        //bool isGroundColl = (mCollFlags & CollisionFlags.Below) != 0;

        if(mIsOnPlatform) {
            mCollFlags |= CollisionFlags.Below;
            mCollGroundLayerMask |= mIsOnPlatformLayerMask;
        }

        bool lastWallStick = mWallSticking;
        mWallSticking = false;

        if(isSlopSlide) {
            //Debug.Log("sliding");
            mLastGround = false;
            mJumpCounter = jumpCounter;
        }
        //refresh wallstick
        else if(wallStick && !mJumpingWall && collisionFlags == CollisionFlags.Sides) {
            //check if we are going up
            if(!wallStickDownOnly || localVelocity.y <= 0.0f) {
                Vector3 up = dirHolder.up;

                if(collisionFlags == CollisionFlags.Sides) {
                    for(int i = 0; i < mCollCount; i++) {
                        CollideInfo inf = mColls[i];
                        if(inf.flag == CollisionFlags.Sides && (wallStickInvalidMask == 0 || ((1<<inf.collider.gameObject.layer) & wallStickInvalidMask) == 0)) {
                            if(CanWallStick(up, inf.normal)) {
                                //wallStickForce
                                mWallStickCollInfo = inf;
                                mWallStickSide = M8.MathUtil.CheckSide(mWallStickCollInfo.normal, dirHolder.up);
                                mWallSticking = true;
                                break;
                            }
                        }
                    }
                }
            }

            if(mWallSticking) {
                if(wallStickPush) {
                    if(CheckWallStickIn(moveSide)) {
                        if(!mWallStickWaitInput) {
                            //cancel horizontal movement
                            Vector3 newVel = localVelocity;
                            newVel.x = 0.0f;

                            //reduce downward speed

                            //Debug.Log("la");

                            float yCap = WallStickCurrentDownCap();
                            if(newVel.y < -yCap) newVel.y = -yCap;

                            rigidbody.velocity = dirHolder.rotation * newVel;

                            mWallStickWaitInput = true;
                        }

                        if(!lastWallStick)
                            mWallStickLastTime = Time.fixedTime;

                        mWallStickLastInputTime = Time.fixedTime;
                    }
                    else {
                        bool wallStickExpired = Time.fixedTime - mWallStickLastInputTime > wallStickDelay;

                        if(wallStickExpired) {
                            mWallStickWaitInput = false;
                            mWallSticking = false;
                        }
                    }
                }
                else {
                    bool wallStickExpired = Time.fixedTime - mWallStickLastTime > wallStickDelay;

                    //see if we are moving away
                    if((wallStickExpired && CheckWallStickMoveAway(moveSide))) {
                        if(!mWallStickWaitInput) {
                            mWallSticking = false;
                        }
                    }
                    else if(!lastWallStick) {
                        mWallStickWaitInput = true;
                        mWallStickLastTime = Time.fixedTime;

                        //cancel horizontal movement
                        Vector3 newVel = localVelocity;
                        newVel.x = 0.0f;

                        //reduce downward speed
                        float yCap = WallStickCurrentDownCap();
                        if(newVel.y < -yCap) newVel.y = -yCap;

                        rigidbody.velocity = dirHolder.rotation * newVel;
                    }
                }
            }

            if(mWallSticking != lastWallStick) {
                if(mWallSticking) {
                    mJump = false;
                    lockDrag = false;
                }
                else {
                    if(wallStickPush)
                        mWallStickWaitInput = false;
                }
            }
        }

        if(mLastGround != isGrounded) {
            if(!mLastGround) {
                //Debug.Log("landed");
                //mJump = false;
                //mJumpingWall = false;
                mJumpCounter = 0;

                if(localVelocity.y <= 0.0f) {
                    if(landCallback != null)
                        landCallback(this);
                }
            }
            else {
                //falling down?
                /*if(mJumpCounter <= 0)
                    mJumpCounter = 1;*/

                mJumpLastTime = Time.fixedTime;
                mLastGroundTime = Time.fixedTime;
            }

            mLastGround = isGrounded;
        }
    }

    protected override void OnDestroy() {
        inputEnabled = false;

        landCallback = null;
        jumpCallback = null;

        base.OnDestroy();
    }

    protected override void OnDisable() {
        base.OnDisable();

    }
    
    // Use this for initialization
    void Start() {
        inputEnabled = startInputEnabled;
    }

    // Update is called once per frame
    protected override void FixedUpdate() {
        Rigidbody body = rigidbody;
        Quaternion dirRot = dirHolder.rotation;

        if(mInputEnabled) {
            InputManager input = InputManager.instance;

            float moveX;

            moveX = moveInputX != InputManager.ActionInvalid ? input.GetAxis(player, moveInputX) : 0.0f;

            //movement
            moveForward = 0.0f;

            if(!mMoveSideLock)
                moveSide = 0.0f;

            if(isUnderWater && !isGrounded) {
                //move forward upwards
                //Move(dirRot, Vector3.up, Vector3.right, new Vector2(moveX, moveY), moveForce);
                //TODO: use jump
            }
            else if(mWallSticking) {
                if(wallStickPush) {
                    if(!mMoveSideLock)
                        moveSide = moveX;
                }
                else {
                    if(mWallStickWaitInput) {
                        if(CheckWallStickMoveAway(moveX)) {
                            mWallStickWaitInput = false;
                            mWallStickLastTime = Time.fixedTime;
                        }
                    }
                    else if(Time.fixedTime - mWallStickLastTime > wallStickDelay) {
                        if(!mMoveSideLock)
                            moveSide = moveX;
                    }
                }
            }
            else if(!(isSlopSlide || mJumpingWall)) {
                //moveForward = moveY;
                if(!mMoveSideLock) {
                    moveSide = moveX;
                }
            }

            //jump
            if(mJump && !mWallSticking) {
                if(isUnderWater) {
                    body.AddForce(dirRot * Vector3.up * jumpWaterForce);
                }
                else {
                    if(!mJumpInputDown || Time.fixedTime - mJumpLastTime >= jumpDelay || collisionFlags == CollisionFlags.Above) {
                        mJump = false;
                        lockDrag = false;
                    }
                    else if(localVelocity.y < airMaxSpeed) {
                        body.AddForce(dirRot * Vector3.up * jumpForce);
                    }
                }
            }
        }
        else {
            moveForward = 0.0f;

            if(!mMoveSideLock)
                moveSide = 0.0f;

            mJump = false;
        }

        base.FixedUpdate();

        //stick to wall
        if(mWallSticking) {
            //reduce speed falling down
            float yCap = WallStickCurrentDownCap();
            if(localVelocity.y < -yCap) {
                //ComputeLocalVelocity();
                Vector3 newVel = new Vector3(localVelocity.x, -yCap, localVelocity.z);
                body.velocity = dirHolder.rotation * newVel;
            }
            //boost up
            else if(localVelocity.y >= 0.0f) {
                float curT = Time.fixedTime - mWallStickLastTime;
                if(curT <= wallStickUpDelay && InputManager.instance.IsDown(0, jumpInput)) {
                    Vector3 upDir = dirRot * Vector3.up;
                    upDir = M8.MathUtil.Slide(upDir, mWallStickCollInfo.normal);

                    if(localVelocity.y < airMaxSpeed)
                        body.AddForce(upDir * wallStickUpForce);
                }
            }

            //push towards the wall
            body.AddForce(-mWallStickCollInfo.normal * wallStickForce);
        }
        else if(mCollCount == 0) {
            //check if no collision, then try to dampen horizontal speed
            if(airDampForceX != 0.0f && moveSide == 0.0f) {
                if(localVelocity.x < -airDampMinSpeedX || localVelocity.x > airDampMinSpeedX) {
                    Vector3 dir = localVelocity.x < 0.0f ? Vector3.right : Vector3.left;
                    body.AddForce(dirRot * dir * airDampForceX);
                }
            }
        }

        //see if we are jumping wall and falling, then cancel jumpwall
        if(mJumpingWall && Time.fixedTime - mJumpLastTime >= jumpWallLockDelay)
            mJumpingWall = false;

        //if(CheckPenetrate(0.1f, plankLayer))
        //Debug.Log("planking");
    }

    /*IEnumerator DoWallStick() {
        yield return new WaitForSeconds(wallStickDelay);

        //see if we still sticking
        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        PrepJumpVel();
        
        while(mWallSticking) {
            rigidbody.AddForce(gravityController.up * wallStickUpForce, ForceMode.Force);

            yield return wait;
        }
    }*/

    void PrepJumpVel() {
        ComputeLocalVelocity();

        Vector3 newVel = localVelocity;

        if(newVel.y < 0.0f)
            newVel.y = 0.0f; //cancel 'falling down'

        newVel = dirHolder.rotation * newVel;
        rigidbody.velocity = newVel;
    }

    void OnInputJump(InputManager.Info dat) {
        if(!enabled)
            return;

        if(dat.state == InputManager.State.Pressed) {
            Jump(true);
        }
        else {
            Jump(false);
        }
    }

    bool CheckWallStickMoveAway(float criteria) {
        return criteria != 0 && ((criteria < 0.0f && mWallStickSide == M8.MathUtil.Side.Right) || (criteria > 0.0f && mWallStickSide == M8.MathUtil.Side.Left));
    }

    bool CheckWallStickIn(float criteria) {
        return criteria != 0 && ((criteria < 0.0f && mWallStickSide == M8.MathUtil.Side.Left) || (criteria > 0.0f && mWallStickSide == M8.MathUtil.Side.Right));
    }

    //heh...
    void SetPlankingIgnore(Collider coll, bool ignore) {
        int ind = System.Array.IndexOf<Collider>(mPlankColliders, coll, 0, mPlankCount);
        if(ignore) {
            if(ind == -1 && mPlankCount < mPlankColliders.Length) {
                Physics.IgnoreCollision(collider, coll);
                mPlankColliders[mPlankCount] = coll;
                mPlankCount++;

                if(!mPlankCheckActive)
                    StartCoroutine(DoPlankCheck());
            }
        }
        else {
            if(ind != -1) {
                mPlankColliders[ind] = mPlankColliders[mPlankCount-1];
                mPlankColliders[mPlankCount-1] = null;
                mPlankCount--;
            }
        }
    }

    IEnumerator DoPlankCheck() {
        mPlankCheckActive = true;
        WaitForSeconds wait = new WaitForSeconds(plankCheckDelay);

        Collider col = collider;

        while(mPlankCount > 0) {
            yield return wait;

            //if(CheckPenetrate(0.01f, plankLayer)) {
            Vector3 vel = mBody.velocity;
            float speed = vel.magnitude;
            if(speed > 0) {
                RaycastHit[] hits = CheckAllCasts(Vector3.zero, 0.00f, vel/speed, speed*Time.fixedDeltaTime, plankLayer);

                //check if colliders are not in list
                for(int i = 0; i < mPlankCount; i++) {
                    bool del = false;

                    if(mPlankColliders[i] == null || !mPlankColliders[i].gameObject.activeSelf)
                        del = true;
                    else {
                        int ind = -1;
                        for(int j = 0; j < hits.Length; j++) {
                            if(hits[j].collider == mPlankColliders[i]) {
                                ind = j; break;
                            }
                        }

                        if(ind == -1) {
                            Physics.IgnoreCollision(col, mPlankColliders[i], false);
                            del = true;
                        }
                    }

                    if(del) {
                        mPlankColliders[i] = mPlankColliders[mPlankCount-1];
                        mPlankColliders[mPlankCount-1] = null;
                        mPlankCount--;
                        i--;
                    }
                }
            }
            //}
        }

        mPlankCheckActive = false;
    }
}
