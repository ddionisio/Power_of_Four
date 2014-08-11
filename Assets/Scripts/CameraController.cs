using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour {
    public enum Mode {
        Lock, //stop camera motion by controller
        Bound, //camera follows attach, restricted by bounds
        HorizontalLock, //camera X is forced at center of bounds, Y still follows attach
        VerticalLock, //camera Y is forced at center of bounds, X still follows attach
        Free
    }

    public Mode mode = Mode.Lock;
    public float delay = 0.1f; //reposition delay
    public float transitionDelay = 0.5f;
    public float transitionExpire = 1.0f;

    public bool rotateEnabled;
    public float rotateSpeed = 90f;

    private static CameraController mInstance;

    private Camera2D mCam;
    private Transform mAttach;
    private Vector3 mCurVel;
    private Bounds mBounds;
    private bool mDoTrans;
    private float mLastTransTime;
    private float mCurDelay;
    private bool mFirstTimeSnap;
    private float mDelayScale = 1.0f;

    public static CameraController instance { get { return mInstance; } }

    /// <summary>
    /// Lower value makes movement go faster. 1.0 = normal
    /// </summary>
    public float delayScale { get { return mDelayScale; } set { mDelayScale = value; } }

    public Transform attach {
        get { return mAttach; }
        set {
            if(mAttach != value) {
                mAttach = value;

                if(!mFirstTimeSnap) {
                    if(mAttach) {
                        transform.position = GetDest();
                        transform.rotation = mAttach.rotation;
                    }
                    mFirstTimeSnap = true;
                }

                //mCurVel = Vector3.zero;
            }
        }
    }

    public Bounds bounds {
        get { return mBounds; }
        set {
            mBounds = value;
            //mCurVel = Vector3.zero;
        }
    }

    public Camera2D camera2D { get { return mCam; } }

    public void SetTransition(bool transition) {
        mDoTrans = transition;
        mLastTransTime = Time.fixedTime;
        mCurVel = Vector3.zero;
        mCurDelay = transitionDelay;
    }

    void Awake() {
        if(mInstance == null) {
            mInstance = this;

            //init stuff
            mCam = GetComponentInChildren<Camera2D>();

            mCurDelay = delay;
        }
        else {
            DestroyImmediate(gameObject);
        }
    }

    Vector3 GetDest() {
        Vector3 curPos = transform.position;
        Vector3 dest = mAttach ? mAttach.collider ? mAttach.collider.bounds.center : mAttach.position : curPos;
        dest.z = curPos.z;

        //apply bounds
        switch(mode) {
            case Mode.Bound:
                ApplyBounds(ref dest);
                break;

            case Mode.HorizontalLock:
                ApplyBounds(ref dest);
                dest.x = bounds.center.x;
                break;

            case Mode.VerticalLock:
                ApplyBounds(ref dest);
                dest.y = bounds.center.y;
                break;

            default:
                break;
        }
        return dest;
    }

    // Update is called once per frame
    void FixedUpdate() {
        if(mode == Mode.Lock)
            return;

        if(mDoTrans) {
            float curT = Time.fixedTime - mLastTransTime;
            if(curT >= transitionExpire) {
                mDoTrans = false;
                mCurDelay = delay;
            }
            else {
                float t = Mathf.Clamp(curT/transitionExpire, 0.0f, 1.0f);
                mCurDelay = Mathf.Lerp(transitionDelay, delay, t);
            }
        }

        Vector3 curPos = transform.position;
        Vector3 dest = GetDest();

        if(curPos != dest) {
            if(rigidbody) {
                rigidbody.MovePosition(Vector3.SmoothDamp(curPos, dest, ref mCurVel, mCurDelay*mDelayScale, Mathf.Infinity, Time.fixedDeltaTime));
            }
            else {
                transform.position = Vector3.SmoothDamp(curPos, dest, ref mCurVel, mCurDelay*mDelayScale, Mathf.Infinity, Time.fixedDeltaTime);
            }
        }

        if(rotateEnabled) {
            Quaternion toRot = mAttach.rotation;
            Quaternion curRot = transform.rotation;
            if(curRot != toRot) {
                if(rigidbody) {
                    rigidbody.MoveRotation(Quaternion.RotateTowards(curRot, toRot, rotateSpeed*Time.fixedDeltaTime));
                }
                else {
                    transform.rotation = Quaternion.RotateTowards(curRot, toRot, rotateSpeed*Time.fixedDeltaTime);
                }
            }
        }
    }

    void ApplyBounds(ref Vector3 pos) {
        if(bounds.size.x > 0.0f && bounds.size.y > 0.0f) {
            //convert bounds to pixels, then reconvert to actual pixel size
            Rect screen = mCam.screenExtent;

            if(pos.x - screen.width * 0.5f < bounds.min.x)
                pos.x = bounds.min.x + screen.width * 0.5f;
            else if(pos.x + screen.width * 0.5f > bounds.max.x)
                pos.x = bounds.max.x - screen.width * 0.5f;

            if(pos.y - screen.height * 0.5f < bounds.min.y)
                pos.y = bounds.min.y + screen.height * 0.5f;
            else if(pos.y + screen.height * 0.5f > bounds.max.y)
                pos.y = bounds.max.y - screen.height * 0.5f;
        }
    }
}
