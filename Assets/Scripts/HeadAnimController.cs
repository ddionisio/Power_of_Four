using UnityEngine;
using System.Collections;

public class HeadAnimController : MonoBehaviour {
    public enum HeadState {
        Open,
        Close
    }

    public enum Frame {
        Invalid = -1,
        Normal,
        Down0,
        Down1,
        Up0,
        Up1,
        CloseEyes
    }

    public SpriteRenderer sprite;

    // used by main animator
    public Sprite[] closeFrames;
    public Sprite[] openFrames;

    public AnimatorData eyesAnim;

    private AnimatorData mAnim;
    private IEnumerator mEyesIdleAction;

    private int mEyeTakeUp;
    private int mEyeTakeDown;
    private int mEyeTakeForward;
    private int[] mEyeTakeIdles;

    public Frame frame {
        get {
            Buddy bud = Player.instance ? Player.instance.currentBuddy : null;

            Frame ret = Frame.Invalid;
            if(sprite) {
                Sprite[] refs = bud == null || bud.headClosed ? closeFrames : openFrames;
                if(refs != null) {
                    for(int i = 0; i < refs.Length; i++) {
                        if(refs[i] == sprite.sprite) {
                            ret = (Frame)i;
                            break;
                        }
                    }
                }
            }
            return ret;
        }
        set {
            Player p = Player.instance;
            Buddy bud = p ? p.currentBuddy : null;

            if(Application.isPlaying) {
                if(p && mAnim.isPlaying && p.controller.moveSide == 0.0f && p.controller.isGrounded)
                    return;

                if(mAnim.isPlaying)
                    mAnim.Stop();
            }

            if(sprite) {
                int ind = (int)value;
                Sprite[] refs = Application.isPlaying && (bud == null || bud.headClosed) ? closeFrames : openFrames;
                if(refs != null) {
                    sprite.sprite = refs[ind >= 0 && ind < refs.Length ? ind : 0];
                }
            }
        }
    }

    public void Blink() {
        Player p = Player.instance;
        Buddy bud = p.currentBuddy;
        if(p.controller.isGrounded && p.controller.moveSide == 0.0f)
            mAnim.Play(bud == null || bud.headClosed ? "close_blink" : "open_blink");
    }

    void OnDisable() {
        mEyesIdleAction = null;
    }

    void Awake() {
        mAnim = GetComponent<AnimatorData>();

        Player.instance.spawnCallback += OnPlayerSpawn;
        Player.instance.lookDirChangedCallback += OnLookDirChange;
        Player.instance.buddyChangedCallback += OnBuddyChange;

        Player.instance.controllerAnim.overrideSetCallback += OnPlatAnimOverride;

        mEyeTakeUp = eyesAnim.GetTakeIndex("up");
        mEyeTakeDown = eyesAnim.GetTakeIndex("down");
        mEyeTakeForward = eyesAnim.GetTakeIndex("forward");
        mEyeTakeIdles = new int[3];
        for(int i = 0; i < mEyeTakeIdles.Length; i++)
            mEyeTakeIdles[i] = eyesAnim.GetTakeIndex("neutral_"+i);

        frame = 0;
    }

    void OnPlayerSpawn(EntityBase ent) {
        Player p = ent as Player;
        OnBuddyChange(p);
        OnLookDirChange(p);
    }

    void OnLookDirChange(Player player) {
        switch(player.lookDir) {
            case Player.LookDir.Down:
                if(mEyesIdleAction != null) { StopCoroutine(mEyesIdleAction); mEyesIdleAction = null; }
                eyesAnim.Play(mEyeTakeDown);
                break;
            case Player.LookDir.Front:
                if(mEyesIdleAction == null) { StartCoroutine(mEyesIdleAction = DoEyesIdle()); }
                break;
            case Player.LookDir.Up:
                if(mEyesIdleAction != null) { StopCoroutine(mEyesIdleAction); mEyesIdleAction = null; }
                eyesAnim.Play(mEyeTakeUp);
                break;
        }
    }

    void OnBuddyChange(Player player) {
        Frame lastFrame = frame;
        mAnim.Stop();
        frame = lastFrame;
    }

    void OnPlatAnimOverride(PlatformerAnimatorController ctrl) {
        if(!string.IsNullOrEmpty(ctrl.overrideTakeName)) {
            if(mEyesIdleAction != null) { StopCoroutine(mEyesIdleAction); mEyesIdleAction = null; }
            eyesAnim.Stop();

            mAnim.Stop();

            frame = 0;
        }
        else {
            Player player = Player.instance;
            OnBuddyChange(player);
            OnLookDirChange(player);
        }
    }

    IEnumerator DoEyesForward() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        yield return wait;

        Player player = Player.instance;

        eyesAnim.Play(mEyeTakeForward);

        while(eyesAnim.isPlaying) //assume not looping
            yield return wait;

        while(player.controller.moveSide != 0.0f || !player.controller.isGrounded || player.controller.isWallStick)
            yield return wait;

        StartCoroutine(mEyesIdleAction = DoEyesIdle());
    }

    IEnumerator DoEyesIdle() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        yield return wait;

        Player player = Player.instance;

        int curIdleInd = 0;

        bool isDone = false;
        while(!isDone) {
            eyesAnim.Play(mEyeTakeIdles[curIdleInd]);

            while(eyesAnim.isPlaying && !isDone) {
                isDone = player.controller.moveSide != 0.0f || !player.controller.isGrounded || player.controller.isWallStick;
                yield return wait;
            }

            if(isDone)
                continue;

            //wait a bit
            for(float curTime = 0.0f; curTime < 1.0f && !isDone; curTime += Time.fixedDeltaTime) {
                isDone = player.controller.moveSide != 0.0f || !player.controller.isGrounded || player.controller.isWallStick;
                yield return wait;
            }

            if(isDone)
                continue;

            //blink
            Blink();
            while(mAnim.isPlaying && !isDone) {
                isDone = player.controller.moveSide != 0.0f || !player.controller.isGrounded || player.controller.isWallStick;
                yield return wait;
            }

            if(isDone)
                continue;

            curIdleInd++;
            if(curIdleInd == mEyeTakeIdles.Length) {
                curIdleInd = 0;
                M8.ArrayUtil.Shuffle(mEyeTakeIdles);
            }
        }

        StartCoroutine(mEyesIdleAction = DoEyesForward());
    }

    //IEnumerator
}
