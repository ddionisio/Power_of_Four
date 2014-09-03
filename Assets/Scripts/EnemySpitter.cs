using UnityEngine;
using System.Collections;

public class EnemySpitter : Enemy {
    public enum Action {
        None,
        Move,
        Shoot
    }

    public float idleDelay;

    public Transform moveDest; //set to null to not move
    public float moveSpeed = 5f;

    public float projWait = 1.0f;
    public string projType = "spitball";
    public Transform projPt;
    public int projCount = 3;
    public float projRepeatDelay = 1.0f;
    public float projAngleRange = 45;
    public int projAngleRangeCount = 5;

    public string takeIdle = "idle";
    public string takeMove = "move";
    public string takeAttackPrep = "attackprep";
    public string takeAttack = "attack";
    public string takeAttackFinish = "attackfinish";

    private Action mCurActionType = Action.None;
    private IEnumerator mCurAction;

    private Vector3[] mMoveDest;
    private int mMoveCurInd;

    protected override void OnDisable() {
        mCurAction = null;

        base.OnDisable();
    }

    protected override void StateChanged() {
        if(mCurAction != null) { StopCoroutine(mCurAction); mCurAction = null; }

        base.StateChanged();
    }

    protected override void RunStateAction() {
        base.RunStateAction();

        switch((EntityState)state) {
            case EntityState.Normal:
                switch(mCurActionType) {
                    case Action.None:
                    case Action.Move:
                        StartCoroutine(mCurAction = DoActionMove());
                        break;
                    case Action.Shoot:
                        StartCoroutine(mCurAction = DoActionShoot());
                        break;
                }
                break;
        }
    }

    protected override void Awake() {
        base.Awake();

        if(moveDest) {
            mMoveDest = new Vector3[2];
            mMoveDest[0] = moveDest.position;
            mMoveDest[1] = transform.position;
        }
    }

    IEnumerator DoActionMove() {
        mCurActionType = Action.Move;
                
        if(mMoveDest != null) {
            PlayAnim(takeMove);

            Vector3 dest = mMoveDest[mMoveCurInd];
            Vector3 dir = dest - transform.position;
            float dist = dir.magnitude;
            if(dist > 0) {
                dir /= dist;

                float delay = dist/moveSpeed;
                float lastTime = Time.fixedTime;

                WaitForFixedUpdate wait = new WaitForFixedUpdate();

                while(Time.fixedTime - lastTime < delay) {
                    mBody.MovePosition(mBody.position + dir*moveSpeed*Time.fixedDeltaTime);
                    yield return wait;
                }

                mBody.MovePosition(dest);
            }

            mMoveCurInd++; if(mMoveCurInd == mMoveDest.Length) mMoveCurInd = 0;
        }

        if(idleDelay > 0) {
            PlayAnim(takeIdle);
            yield return new WaitForSeconds(idleDelay);
        }

        StartCoroutine(mCurAction = DoActionShoot());
    }

    IEnumerator DoActionShoot() {
        mCurActionType = Action.Shoot;

        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        PlayAnim(takeAttackPrep);
        yield return new WaitForSeconds(projWait);

        PlayAnim(takeAttack);
        while(isAnimPlaying)
            yield return wait;

        //shoot stuff
        WaitForSeconds shootWait = new WaitForSeconds(projRepeatDelay);

        Vector3 spawnPt = projPt.position; spawnPt.z = 0;
        Vector3 spawnDir = projPt.up;
        float hAngle = projAngleRange*0.5f;
        float fAngleRange = (float)projAngleRangeCount;

        for(int i = 0; i < projCount; i++) {
            Vector3 dir = Quaternion.Euler(0f, 0f, Mathf.Lerp(-hAngle, hAngle, (float)Random.Range(0, projAngleRangeCount+1)/fAngleRange))*spawnDir;
            Projectile.Create(projGroup, projType, spawnPt, dir, null);
            yield return shootWait;
        }

        PlayAnim(takeAttackFinish);
        while(isAnimPlaying)
            yield return wait;

        StartCoroutine(mCurAction = DoActionMove());
    }
}
