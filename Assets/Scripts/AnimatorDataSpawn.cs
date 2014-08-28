using UnityEngine;
using System.Collections;

public class AnimatorDataSpawn : MonoBehaviour {
    public string take; //if null, use the default or first index from animator
    public float delay;

    private AnimatorData mAnim;
    private int mTakeInd;

    void Awake() {
        mAnim = GetComponent<AnimatorData>();

        if(!string.IsNullOrEmpty(take))
            mTakeInd = mAnim.GetTakeIndex(take);
        else if(mAnim.defaultTakeIndex == -1)
            mTakeInd = 0;
    }

    void OnSpawned() {
        StartCoroutine(DoPlay());
    }

    IEnumerator DoPlay() {
        if(delay > 0.0f)
            yield return new WaitForSeconds(delay);

        if(mTakeInd >= 0)
            mAnim.Play(mTakeInd);
        else if(!mAnim.playOnEnable)
            mAnim.PlayDefault();

        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        do {
            yield return wait;
        } while(mAnim.isPlaying);

        PoolController.ReleaseAuto(transform);
    }
}
