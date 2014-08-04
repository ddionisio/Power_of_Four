using UnityEngine;
using System.Collections;

public class UIDNA : MonoBehaviour {
    public enum UpdateState {
        None,
        Entering,
        TextUpdating,
        ActiveWait,
        Exiting
    }

    const string takeActive = "active";
    const string takeInactive = "inactive";
    const string takeActivate = "activate";
    const string takeDeactivate = "deactivate";

    public UILabel numLabel;
    public float numChangeDelay = 1.0f;
    public float numActiveWaitDelay = 3.0f;

    private AnimatorData mAnim;
    private IEnumerator mAction;
    private float mCurNum;
    private float mNextNum;
    private UpdateState mActionUpdate = UpdateState.None;

    public void ForceSetActive(bool active) {
        if(mAction != null) {
            StopCoroutine(mAction);
            mAction = null;
        }

        mActionUpdate = UpdateState.None;

        mCurNum = mNextNum = Player.instance.stats.DNA;
        SetNumLabelToCurrent();

        if(active) {
            mAnim.Play(takeActive);
        }
        else {
            mAnim.Play(takeInactive);
        }
    }

    void OnDisable() {
        mAction = null;
        mActionUpdate = UpdateState.None;

        mCurNum = mNextNum;
        SetNumLabelToCurrent();
    }

    void Awake() {
        mAnim = GetComponent<AnimatorData>();
    }

    void Start() {
        PlayerStats stats = Player.instance.stats;
        stats.changeDNACallback += OnDNAChange;

        mCurNum = mNextNum = stats.DNA;
        SetNumLabelToCurrent();

        mAnim.Play(takeInactive);
    }

    void OnDNAChange(Stats s, int delta) {
        mNextNum = ((PlayerStats)s).DNA;

        if(mAction != null) {
            switch(mActionUpdate) {
                case UpdateState.Exiting:
                    StopCoroutine(mAction);
                    StartCoroutine(mAction = DoUpdateEnter());
                    break;

                case UpdateState.None:
                case UpdateState.Entering:
                case UpdateState.TextUpdating:
                    break;

                case UpdateState.ActiveWait:
                    StopCoroutine(mAction);
                    StartCoroutine(mAction = DoUpdateText());
                    break;
            }
        }
        else {
            StartCoroutine(mAction = DoUpdateEnter());
        }
    }

    IEnumerator DoUpdateEnter() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        //enter
        mActionUpdate = UpdateState.Entering;
        mAnim.Play(takeActivate);
        while(mAnim.isPlaying)
            yield return wait;

        yield return StartCoroutine(mAction = DoUpdateText());
    }

    IEnumerator DoUpdateText() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        //update text
        mActionUpdate = UpdateState.TextUpdating;

        mAnim.Play(takeActive);

        float startNum = mCurNum;
        float curT = 0.0f;
        while(true) {
            yield return wait;

            curT += Time.fixedDeltaTime;
            if(curT < numChangeDelay) {
                mCurNum = Holoville.HOTween.Core.Easing.Quad.EaseOut(curT, startNum, mNextNum - startNum, numChangeDelay, 0f, 0f);
                SetNumLabelToCurrent();
            }
            else {
                mCurNum = mNextNum;
                SetNumLabelToCurrent();
                break;
            }
        }

        mActionUpdate = UpdateState.ActiveWait;

        //wait
        yield return new WaitForSeconds(numActiveWaitDelay);

        //exit
        mActionUpdate = UpdateState.Exiting;

        mAnim.Play(takeDeactivate);
        while(mAnim.isPlaying)
            yield return wait;

        //done
        mActionUpdate = UpdateState.None;
        mAnim.Play(takeInactive);
        mAction = null;
    }

    void SetNumLabelToCurrent() {
        if(numLabel)
            numLabel.text = ((int)mCurNum).ToString();
    }
}
