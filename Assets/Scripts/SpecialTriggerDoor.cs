using UnityEngine;
using System.Collections;

public class SpecialTriggerDoor : SpecialTrigger {
    public string toScene;
    public string startPointRef; //which start point to spawn player to

    public bool startClosed = false;

    public string takeClosed = "closed";
    public string takeOpening = "opening";
    public string takeOpened = "opened";

    private AnimatorData mAnim;
    private bool mClosed;

    protected override IEnumerator Act() {
        if(mClosed) {
            if(mAnim) {
                WaitForFixedUpdate wait = new WaitForFixedUpdate();

                mAnim.Play(takeOpening);
                while(mAnim.isPlaying)
                    yield return wait;

                mAnim.Play(takeOpened);
            }

            mClosed = false;
        }
    }

    protected override void ActExecute() {
        //save states and stuff then enter scene
        Debug.Log("fuck door go");
    }

    void Awake() {
        mAnim = GetComponent<AnimatorData>();
    }

    void Start() {
        mClosed = startClosed;
        if(mAnim) {
            if(mClosed)
                mAnim.Play(takeClosed);
            else
                mAnim.Play(takeOpened);
        }
    }
}
