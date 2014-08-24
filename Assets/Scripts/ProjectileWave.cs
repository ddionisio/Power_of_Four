using UnityEngine;
using System.Collections;

public class ProjectileWave : Projectile {
    public float anglePerMeter;
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

    protected override void ApplyContact(GameObject go, Vector3 pos, Vector3 normal) {
        base.ApplyContact(go, pos, normal);

        //check to see if it's projectile, then kill it
        Projectile proj = go.GetComponent<Projectile>();
        if(proj)
            proj.state = (int)State.Dying;
    }

    protected override void FixedUpdate() {
        switch((State)state) {
            case State.Active:
                float dt = Time.fixedDeltaTime;

                mCurAngle += mCurAngleDir*anglePerMeter*dt;
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
