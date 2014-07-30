using UnityEngine;
using System.Collections;

/// <summary>
/// Note: eyeorbinsert should encompass the wiring and the socket
/// the takeActivate animates the process of activating, may include socket opening
/// the takeActive should take care of having the socket open
/// </summary>
public class EyeOrbInsert : MonoBehaviour {
    public int index;

    public string takeActivate = "activate";
    public string takeActive = "active"; //after travel is finish, or orb is already inserted

    public Transform eyeInsertPoint;

    public float eyeDelay = 0.5f;

    private AnimatorData mAnim;
    private Transform mEyeOrb;

    void OnTriggerEnter(Collider col) {
        LevelController lvlCtrl = LevelController.instance;
        //check if not inserted
        if(!lvlCtrl.eyeInsertIsFilled(index)) {
            //valid eye orb, grab it and place
            EyeOrbPlayer.OrbData dat = EyeOrbPlayer.instance.Remove();
            if(dat.index != -1) {
                lvlCtrl.eyeOrbSetState(dat.index, LevelController.EyeOrbState.Placed);
                lvlCtrl.eyeInsertSetFilled(index, true);

                mEyeOrb = dat.orbT;
                if(mEyeOrb)
                    StartCoroutine(DoIt());
                else {
                    if(mAnim)
                        mAnim.Play(takeActive);
                }
            }
        }
    }

    void Awake() {
        mAnim = GetComponent<AnimatorData>();
    }

    void Start() {
        //see if we are already inserted
        if(LevelController.instance.eyeInsertIsFilled(index)) {
            if(mAnim)
                mAnim.Play(takeActive);
        }
    }

    IEnumerator DoIt() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        Vector3 srcPt = mEyeOrb.position;
        Vector3 destPt = eyeInsertPoint ? eyeInsertPoint.position : transform.position;

        float t = 0;
        while(true) {
            yield return wait;

            t += Time.fixedDeltaTime;
            if(t >= eyeDelay) {
                mEyeOrb.position = destPt;
                break;
            }
            else {
                mEyeOrb.position = Vector3.Lerp(srcPt, destPt, Holoville.HOTween.Core.Easing.Sine.EaseInOut(t, 0, 1, eyeDelay, 0, 0));
            }
        }

        mEyeOrb.gameObject.SetActive(false);
        mEyeOrb = null;

        //do animation
        if(mAnim) {
            mAnim.Play(takeActivate);
            while(mAnim.isPlaying)
                yield return wait;

            mAnim.Play(takeActive);
        }
    }
}
