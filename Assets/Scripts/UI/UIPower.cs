using UnityEngine;
using System.Collections;

public class UIPower : MonoBehaviour {
    public const string takeActive = "active";
    public const string takeInactive = "inactive";
    public const string takeActivate = "activate";
    public const string takeDeactivate = "deactivate";

    public UISprite icon;

    private AnimatorData mAnim;
    private int mInd;
    private Buddy mBuddy;
    private IEnumerator mAction;

    public int index { get { return mInd; } }

    public void Init(int index, Buddy buddy) {
        mInd = index;

        mBuddy = buddy;
        mBuddy.activateCallback += OnBuddyActivate;
        mBuddy.deactivateCallback += OnBuddyDeactivate;
        mBuddy.levelChangeCallback += OnBuddyLevelChange;

        OnBuddyLevelChange(mBuddy);

        mAnim.Play(mBuddy.isActive ? takeActive : takeInactive);
    }

    void OnDisable() {
        mAction = null;
    }

    void Awake() {
        mAnim = GetComponent<AnimatorData>();
    }

    void OnBuddyActivate(Buddy bud) {
        if(mAnim.currentPlayingTakeName != takeActive && mAnim.currentPlayingTakeName != takeActivate) {
            if(mAction != null) StopCoroutine(mAction);
            StartCoroutine(mAction = DoAnim(takeActivate, takeActive));
        }
    }

    void OnBuddyDeactivate(Buddy bud) {
        if(mAnim.currentPlayingTakeName != takeInactive && mAnim.currentPlayingTakeName != takeDeactivate) {
            if(mAction != null) StopCoroutine(mAction);
            StartCoroutine(mAction = DoAnim(takeDeactivate, takeInactive));
        }
    }

    void OnBuddyLevelChange(Buddy bud) {
        icon.spriteName = bud.levelInfos[bud.level-1].iconSpriteRef;
    }

    IEnumerator DoAnim(string takeAction, string takeEnd) {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        mAnim.Play(takeAction);
        while(mAnim.isPlaying)
            yield return wait;
        mAnim.Play(takeEnd);
    }
}
