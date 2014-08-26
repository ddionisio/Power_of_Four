using UnityEngine;
using System.Collections;

public class BuddyFlame : Buddy {
    [System.Serializable]
    public class ProjData {
        public string type;
        public float angleRange;
        public Vector2 speedRange;
        public Vector2 decayRange;
        public float yRange;

        public Projectile Fire(Vector3 pos, Vector3 dir) {
            Vector3 ofs = Vector3.Cross(dir, Vector3.forward)*Random.Range(-yRange, yRange);

            dir = Quaternion.Euler(0, 0, Random.Range(-angleRange, angleRange))*dir;

            Projectile proj = Projectile.Create(projGrp, type, pos + ofs, dir, null);

            proj.startVelocity = speedRange.x;
            proj.startVelocityAddRand = speedRange.y - speedRange.x;
            proj.decayDelay = Random.Range(decayRange.x, decayRange.y);

            return proj;
        }
    }

    public SpriteRenderer body;
    public Sprite bodySpriteNormal;
    public Sprite bodySpriteFire;

    public SpriteRenderer eye;
    public Sprite eyeSpriteNormal;
    public Sprite eyeSpriteFire;

    public Vector2 eyeUp;
    public Vector2 eyeFront;
    public Vector2 eyeDown;

    public SpriteColorPulse[] fingerPulses;
    public float fingerPulseNormal = 0.2f;
    public float fingerPulseFire = 1.0f;

    public AnimatorData fingerAnim;
    public string fingerTakeIdle;
    public string fingerTakeMove;
    public string fingerTakeUp;
    public string fingerTakeDown;

    public ProjData[] projs; //based on level

    private AnimatorData mAnim;
    private int mTakeEnter;
    private int mTakeExit;

    private int mFingerTakeIdle;
    private int mFingerTakeMove;
    private int mFingerTakeUp;
    private int mFingerTakeDown;

    private IEnumerator mFingerAnimAction;

    protected override void OnInit() {
        mFingerTakeIdle = fingerAnim.GetTakeIndex(fingerTakeIdle);
        mFingerTakeMove = fingerAnim.GetTakeIndex(fingerTakeMove);
        mFingerTakeUp = fingerAnim.GetTakeIndex(fingerTakeUp);
        mFingerTakeDown = fingerAnim.GetTakeIndex(fingerTakeDown);

        mAnim = GetComponent<AnimatorData>();
        mTakeEnter = mAnim.GetTakeIndex("enter");
        mTakeExit = mAnim.GetTakeIndex("exit");
    }

    protected override void OnEnter() {
        StartCoroutine(mFingerAnimAction = DoFingerAnim());

        OnDirChange();
        SetFiring(false);
    }

    protected override IEnumerator OnEntering() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        mAnim.Play(mTakeEnter);
        while(mAnim.isPlaying)
            yield return wait;
    }

    protected override void OnExit() {
        if(mFingerAnimAction != null) { StopCoroutine(mFingerAnimAction); mFingerAnimAction = null; }
    }

    protected override IEnumerator OnExiting() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        mAnim.Play(mTakeExit);
        while(mAnim.isPlaying)
            yield return wait;
    }

    protected override void OnFireStart() {
        SetFiring(true);
    }

    protected override void OnFire() {
        //projs[level - 1].Fire(firePos, fireDirWorld);
    }

    protected override void OnFireStop() {
        SetFiring(false);
    }

    IEnumerator DoFingerAnim() {
        PlatformerController ctrl = Player.instance.controller;

        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        while(true) {
            if(ctrl.isGrounded) {
                int take = ctrl.moveSide != 0.0f ? mFingerTakeMove : mFingerTakeIdle;
                fingerAnim.Play(take);
            }
            else {
                int take = Mathf.Sign(ctrl.localVelocity.y) < 0.0f ? mFingerTakeDown : mFingerTakeUp;
                if(take != -1 && (fingerAnim.isPlaying || fingerAnim.lastPlayingTakeIndex != take))
                    fingerAnim.Play(take);
            }

            yield return wait;
        }
    }

    protected override void OnDirChange() {
        Transform eyeT = eye.transform;
        Vector3 pos;
        switch(dir) {
            case Player.LookDir.Front:
                pos = new Vector3(eyeFront.x, eyeFront.y, eyeT.localPosition.z);
                break;
            case Player.LookDir.Down:
                pos = new Vector3(eyeDown.x, eyeDown.y, eyeT.localPosition.z);
                break;
            default:
                pos = new Vector3(eyeUp.x, eyeUp.y, eyeT.localPosition.z);
                break;
        }
        eyeT.localPosition = pos;
    }

    void SetFiring(bool firing) {
        if(firing) {
            body.sprite = bodySpriteFire;
            eye.sprite = eyeSpriteFire;

            for(int i = 0; i < fingerPulses.Length; i++)
                fingerPulses[i].pulsePerSecond = fingerPulseFire;
        }
        else {
            body.sprite = bodySpriteNormal;
            eye.sprite = eyeSpriteNormal;

            for(int i = 0; i < fingerPulses.Length; i++)
                fingerPulses[i].pulsePerSecond = fingerPulseNormal;
        }
    }
}
