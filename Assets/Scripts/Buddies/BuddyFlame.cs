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

    public Sprite eyeSpriteNormal;
    public Sprite eyeSpriteFire;

    public Vector2 eyeUp;
    public Vector2 eyeFront;
    public Vector2 eyeDown;

    public SpriteColorPulse[] fingerPulses;
    public float fingerPulseNormal = 0.2f;
    public float fingerPulseFire = 1.0f;
    public Color fingerColorNormal = Color.gray;
    public Color fingerColorFire = Color.white;

    public AnimatorData fingerAnim;
    public string fingerTakeIdle;
    public string fingerTakeMove;
    public string fingerTakeUp;
    public string fingerTakeDown;

    public float chargeDelay = 1.0f;
    public ParticleSystem chargeParticle;
    public GameObject chargeActiveGO;
    
    public ProjData[] projs; //based on level

    public string chargeProjType;

    private SpriteRenderer mEyeSpriteRenderer;

    private AnimatorData mAnim;
    private int mTakeEnter;
    private int mTakeExit;

    private int mFingerTakeIdle;
    private int mFingerTakeMove;
    private int mFingerTakeUp;
    private int mFingerTakeDown;

    private IEnumerator mFingerAnimAction;

    private IEnumerator mChargeAction;
    private bool mChargeIsActive;

    protected override void OnInit() {
        mFingerTakeIdle = fingerAnim.GetTakeIndex(fingerTakeIdle);
        mFingerTakeMove = fingerAnim.GetTakeIndex(fingerTakeMove);
        mFingerTakeUp = fingerAnim.GetTakeIndex(fingerTakeUp);
        mFingerTakeDown = fingerAnim.GetTakeIndex(fingerTakeDown);

        mAnim = GetComponent<AnimatorData>();
        mTakeEnter = mAnim.GetTakeIndex("enter");
        mTakeExit = mAnim.GetTakeIndex("exit");

        mEyeSpriteRenderer = projPoint.GetComponent<SpriteRenderer>();

        chargeActiveGO.SetActive(false);
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

        if(level > 2)
            ChargeActive(true);
    }

    protected override void OnExit() {
        if(mFingerAnimAction != null) { StopCoroutine(mFingerAnimAction); mFingerAnimAction = null; }

        ChargeActive(false);
    }

    protected override IEnumerator OnExiting() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        mAnim.Play(mTakeExit);
        while(mAnim.isPlaying)
            yield return wait;
    }

    protected override void OnFireStart() {
        SetFiring(true);

        if(mChargeAction != null) { StopCoroutine(mChargeAction); mChargeAction = null; }

        chargeParticle.loop = false;
        chargeParticle.Stop();
        chargeParticle.Clear();
    }

    protected override void OnFire() {
        if(mChargeIsActive) {
            //fire
            Projectile.Create(projGrp, chargeProjType, firePos, fireDirWorld, null);

            ChargeActive(false);
        }
        else {
            int projInd = level < 3 ? level - 1 : projs.Length - 1;
            projs[projInd].Fire(firePos, fireDirWorld);
        }
    }

    protected override void OnFireStop() {
        SetFiring(false);

        if(level > 2)
            ChargeActive(true);
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

    IEnumerator DoCharge() {
        chargeParticle.loop = true;
        chargeParticle.Play();
        
        yield return new WaitForSeconds(chargeDelay);

        mChargeIsActive = true;
        chargeParticle.loop = false;
        chargeActiveGO.SetActive(true);
    }

    protected override void OnDirChange() {
        Vector3 pos;
        switch(dir) {
            case Player.LookDir.Front:
                pos = new Vector3(eyeFront.x, eyeFront.y, projPoint.localPosition.z);
                break;
            case Player.LookDir.Down:
                pos = new Vector3(eyeDown.x, eyeDown.y, projPoint.localPosition.z);
                break;
            default:
                pos = new Vector3(eyeUp.x, eyeUp.y, projPoint.localPosition.z);
                break;
        }
        projPoint.localPosition = pos;
    }

    void ChargeActive(bool active) {
        mChargeIsActive = false;
        chargeParticle.loop = false;
        chargeParticle.Stop();
        chargeActiveGO.SetActive(false);

        if(active) {
            StartCoroutine(mChargeAction = DoCharge());
        }
        else {
            if(mChargeAction != null) { StopCoroutine(mChargeAction); mChargeAction = null; }
        }
    }

    void SetFiring(bool firing) {
        if(firing) {
            body.sprite = bodySpriteFire;
            mEyeSpriteRenderer.sprite = eyeSpriteFire;

            for(int i = 0; i < fingerPulses.Length; i++) {
                fingerPulses[i].startColor = fingerColorFire;
                fingerPulses[i].pulsePerSecond = fingerPulseFire;
            }
        }
        else {
            body.sprite = bodySpriteNormal;
            mEyeSpriteRenderer.sprite = eyeSpriteNormal;

            for(int i = 0; i < fingerPulses.Length; i++) {
                fingerPulses[i].startColor = fingerColorNormal;
                fingerPulses[i].pulsePerSecond = fingerPulseNormal;
            }
        }
    }
}
