using UnityEngine;
using System.Collections;

public class ProjectileWave : Projectile {
    public float anglePerSecond;
    public float angleRange; //e.g. 90 = [-45, 45]
    public float angleInitialDir = 1.0f;

    public float accel;

    private float mCurAngle;
    private float mCurAngleDir;
    private float mCurSpeed;

    public override void SpawnFinish() {
        base.SpawnFinish();

        mCurAngle = 0.0f;
        mCurAngleDir = angleInitialDir;

        mCurSpeed = mInitialVelocity;
    }

    protected override void FixedUpdate() {
        switch((State)state) {
            case State.Active:
                float dt = Time.fixedDeltaTime;

                mCurAngle += mCurAngleDir*anglePerSecond*dt;
                if(Mathf.Abs(mCurAngle) >= angleRange*0.5f) {
                    mCurAngle = Mathf.Sign(mCurAngleDir)*angleRange*0.5f;
                    mCurAngleDir *= -1.0f;
                }

                Vector3 dir = Quaternion.Euler(0, 0, mCurAngle)*mInitDir;

                if(mCurSpeed < speedLimit) {
                    mCurSpeed += accel*dt;
                    if(mCurSpeed > speedLimit) mCurSpeed = speedLimit;
                }

                DoSimpleMove(dir, mCurSpeed * mMoveScale * Time.fixedDeltaTime);
                break;
        }
    }
}
