using UnityEngine;
using System.Collections;

public class PlatformerAnimatorController : MonoBehaviour {
    public delegate void Callback(PlatformerAnimatorController ctrl);
    public delegate void CallbackClip(PlatformerAnimatorController ctrl, AMTakeData take);
    public delegate void CallbackClipFrame(PlatformerAnimatorController ctrl, AMTakeData take, AMKey key, AMTriggerData data);

    public enum State {
        None,
        Slide,
        Climb
    }

    public bool leftFlip = true;
    public bool defaultLeft = false;

    public AnimatorData anim;
    public PlatformerController controller;

    public string idleClip = "idle";
    public string moveClip = "move";

    public string[] upClips = { "up" }; //based on jump counter
    public string[] downClips = { "down" }; //based on jump counter

    public string wallStickClip = "wall";
    public string wallJumpClip = "wallJump";

    public string slideClip = "slide";
    //public string climbClip = "climb";

    public float minSpeed = 0.5f;//used if useVelocitySpeed=true 
    public float framePerMeter = 0.1f; //used if useVelocitySpeed=true

    public ParticleSystem wallStickParticle;

    public event Callback flipCallback;
    public event Callback overrideSetCallback;
    public event CallbackClip clipFinishCallback;
    public event CallbackClipFrame clipFrameEventCallback;

    private int mIdle;
    private int mMove;
    private int[] mUps;
    private int[] mDowns;
    private int mWallStick;
    private int mWallJump;

    private int mSlide;
    //private int mClimb;

    private bool mIsLeft;
    private string mOverrideTakeName;
    private State mState;

    private bool mAnimVelocitySpeedEnabled;
    private bool mLockFacing;

    public string overrideTakeName { get { return mOverrideTakeName; } }

    public bool isLeft {
        get { return mIsLeft; }
        set {
            if(mIsLeft != value) {
                mIsLeft = value;

                RefreshFacing();

                if(flipCallback != null)
                    flipCallback(this);
            }
        }
    }

    public bool lockFacing { get { return mLockFacing; } set { mLockFacing = value; } }

    public State state {
        get { return mState; }
        set {
            if(mState != value) {
                mState = value;
            }
        }
    }

    /// <summary>
    /// Set to true to make framerate based on velocity
    /// </summary>
    public bool useVelocitySpeed {
        get { return mAnimVelocitySpeedEnabled; }
        set {
            if(mAnimVelocitySpeedEnabled != value) {
                mAnimVelocitySpeedEnabled = value;
                //TODO
            }
        }
    }

    public bool overrideIsPlaying {
        get { return !string.IsNullOrEmpty(mOverrideTakeName) && anim.isPlaying; }
    }

    public void RefreshFacing() {
        SetFlipX(mIsLeft ? leftFlip : !leftFlip);
    }

    public void ResetAnimation() {
        mAnimVelocitySpeedEnabled = false;
        mOverrideTakeName = "";
        mIsLeft = defaultLeft;

        SetFlipX(mIsLeft ? leftFlip : !leftFlip);

        if(wallStickParticle) {
            wallStickParticle.Stop();
            wallStickParticle.Clear();
        }
    }

    public void PlayOverrideClip(string takeName) {
        //assume its loop type is 'once'
        if(!string.IsNullOrEmpty(takeName)) {
            mOverrideTakeName = takeName;

            if(overrideSetCallback != null)
                overrideSetCallback(this);

            anim.Play(takeName);
        }
    }

    public void StopOverrideClip() {
        if(!string.IsNullOrEmpty(mOverrideTakeName)) {
            mOverrideTakeName = "";

            if(overrideSetCallback != null)
                overrideSetCallback(this);

            anim.Stop();
        }
    }

    void OnDestroy() {
        flipCallback = null;
        clipFinishCallback = null;
        clipFrameEventCallback = null;
        overrideSetCallback = null;
    }

    void Awake() {
        if(anim == null)
            anim = GetComponent<AnimatorData>();

        mIsLeft = defaultLeft;

        if(anim) {
            //callbacks
            anim.takeTriggerCallback += OnAnimTrigger;
            anim.takeCompleteCallback += OnAnimFinish;

            SetFlipX(mIsLeft ? leftFlip : !leftFlip);

            //set indices
            mIdle = anim.GetTakeIndex(idleClip);
            mMove = anim.GetTakeIndex(moveClip);

            mUps = new int[upClips.Length];
            for(int i = 0; i < upClips.Length; i++)
                mUps[i] = anim.GetTakeIndex(upClips[i]);

            mDowns = new int[downClips.Length];
            for(int i = 0; i < downClips.Length; i++)
                mDowns[i] = anim.GetTakeIndex(downClips[i]);

            mWallStick = anim.GetTakeIndex(wallStickClip);
            mWallJump = anim.GetTakeIndex(wallJumpClip);
            mSlide = anim.GetTakeIndex(slideClip);
        }

        if(controller == null)
            controller = GetComponent<PlatformerController>();
    }

    void Update() {
        if(controller == null || !controller.enabled)
            return;

        if(mAnimVelocitySpeedEnabled) {
            //TODO
            //float spd = controller.rigidbody.velocity.magnitude;
            //anim.ClipFps = spd > minSpeed ? spd * framePerMeter : 0.0f;
        }

        if(!string.IsNullOrEmpty(mOverrideTakeName))
            return;

        bool left = mIsLeft;

        switch(mState) {
            case State.None:
                if(controller.isJumpWall) {
                    if(anim && mWallJump != -1) anim.Play(mWallJump);

                    left = controller.localVelocity.x < 0.0f;
                }
                else if(controller.isWallStick) {
                    if(wallStickParticle && !wallStickParticle.isPlaying) {
                        wallStickParticle.Play();
                    }

                    if(anim && mWallStick != -1 && (anim.isPlaying || anim.lastPlayingTakeIndex != mWallStick)) anim.Play(mWallStick);

                    left = M8.MathUtil.CheckSide(controller.wallStickCollide.normal, controller.dirHolder.up) == M8.MathUtil.Side.Right;

                }
                else {
                    if(wallStickParticle) {
                        wallStickParticle.Stop();
                        wallStickParticle.Clear();
                    }

                    if(anim) {
                        if(controller.isGrounded) {
                            if(controller.moveSide != 0.0f) {
                                if(mMove != -1) anim.Play(mMove);
                            }
                            else {
                                if(mIdle != -1) anim.Play(mIdle);
                            }
                        }
                        else {
                            int clipInd = -1;

                            if(controller.localVelocity.y <= 0.0f)
                                clipInd = GetMidAirClip(mDowns);
                            else
                                clipInd = GetMidAirClip(mUps);

                            if(clipInd != -1 && (anim.isPlaying || anim.lastPlayingTakeIndex != clipInd))
                                anim.Play(clipInd);
                        }
                    }

                    if(controller.moveSide != 0.0f) {
                        left = controller.moveSide < 0.0f;
                    }
                }
                break;

            case State.Slide:
                if(anim && mSlide != -1 && (anim.isPlaying || anim.lastPlayingTakeIndex != mSlide))
                    anim.Play(mSlide);

                if(controller.moveSide != 0.0f) {
                    left = controller.moveSide < 0.0f;
                }
                break;
        }

        if(!mLockFacing)
            isLeft = left;
    }

    int GetMidAirClip(int[] clips) {
        if(clips == null || clips.Length == 0 || (anim.isPlaying && anim.currentPlayingTakeIndex == mWallJump))
            return -1;

        int ind = controller.jumpCounterCurrent;
        return ind >= clips.Length ? clips[clips.Length - 1] : clips[ind];
    }

    void SetFlipX(bool flip) {
        if(anim) {
            Vector3 s = anim.transform.localScale;
            s.x = flip ? -Mathf.Abs(s.x) : Mathf.Abs(s.x);
            anim.transform.localScale = s;
        }
    }

    void OnAnimFinish(AnimatorData anim, AMTakeData take) {
        //if(take.name == mOverrideTakeName) {
            //mOverrideTakeName = "";

            //if(overrideSetCallback != null)
                //overrideSetCallback(this);
        //}

        if(clipFinishCallback != null)
            clipFinishCallback(this, take);
    }

    void OnAnimTrigger(AnimatorData anim, AMTakeData take, AMKey key, AMTriggerData data) {
        if(clipFrameEventCallback != null)
            clipFrameEventCallback(this, take, key, data);
    }
}
