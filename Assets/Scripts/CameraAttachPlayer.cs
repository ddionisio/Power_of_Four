using UnityEngine;
using System.Collections;

public class CameraAttachPlayer : CameraAttach {
    public Transform[] points; //based on Player.LookDir value

    public float wallCheckDelay = 0.2f;
    public float revertDelay = 1.5f;
    public float pointFallOfs = 2.0f;

    public float speedNorm = 10.0f;
    public float speedMinScale = 0.5f;
    public float speedMaxScale = 2.0f;
        
    private Player mPlayer;
    private PlatformerController mPlayerCtrl;
    private CameraController mCamCtrl;
    private bool mCameraIsWallStick = false;
    private Player.LookDir mLookDir = Player.LookDir.Invalid;

    public Player.LookDir lookDir { get { return mLookDir; } set { mLookDir = value; } }

    public override Vector3 position {
        get {
            if(mLookDir == Player.LookDir.Invalid) return base.position;

            int ind = (int)mLookDir;
            Transform t = points[ind];
            Vector3 pos;

            if(mCameraIsWallStick) {
                pos = t.localPosition;
                pos.x = 0.0f;
                pos = t.parent.localToWorldMatrix.MultiplyPoint3x4(pos);
            }
            else
                pos = t.position;

            if(pointFallOfs != 0.0f && !(mPlayerCtrl.isGrounded || mPlayerCtrl.isWallStick || mCameraIsWallStick) && mPlayerCtrl.localVelocity.y < 0.0f)
                pos += t.localToWorldMatrix.MultiplyVector(new Vector3(0, -pointFallOfs));

            return pos;
        }
    }

    void Awake() {
        mPlayer = GetComponent<Player>();
        mPlayer.spawnCallback += OnPlayerSpawn;
        mPlayer.setStateCallback += OnPlayerChangeState;

        mPlayerCtrl = mPlayer.controller;
        mCamCtrl = CameraController.instance;
    }

    void Update() {
        mCamCtrl.delayScale = Mathf.Clamp(1.0f - (mPlayerCtrl.isGrounded ? Mathf.Abs(mPlayerCtrl.localVelocity.x) : mPlayerCtrl.localVelocity.magnitude)/speedNorm, speedMinScale, speedMaxScale);
    }

    void OnPlayerChangeState(EntityBase ent) {
        if(ent.state == (int)EntityState.Invalid) {
            mCameraIsWallStick = false;
        }
    }

    void OnPlayerSpawn(EntityBase ent) {
        StartCoroutine(DoCameraPointWallCheck());
    }

    IEnumerator DoCameraPointWallCheck() {
        WaitForSeconds waitCheck = new WaitForSeconds(wallCheckDelay);

        float lastCheckTime = Time.fixedTime;
        mCameraIsWallStick = false;

        while(true) {
            if(mCameraIsWallStick && Time.fixedTime - lastCheckTime >= revertDelay) {
                mCameraIsWallStick = false;
            }

            if(mPlayerCtrl.isWallStick) {
                lastCheckTime = Time.fixedTime;
                if(!mCameraIsWallStick)
                    mCameraIsWallStick = true;
            }

            yield return waitCheck;
        }
    }
}
