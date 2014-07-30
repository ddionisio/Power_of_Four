using UnityEngine;
using System.Collections;

[AddComponentMenu("")]
public abstract class SpecialTrigger : MonoBehaviour {
    public delegate void Callback();

    public string iconRef;

    public bool interactive = true; //set to false if you don't want this trigger to be interactive (e.g. boss door, set to true once all orbs are placed)
    public bool lockPlayerOnAct = true; //lock player's input while acting?
    public bool disableOnAct = true;

    private bool mIsActing;

    public bool isActing { get { return mIsActing; } }

    public void Action(Callback onFinish) {
        if(!mIsActing) {
            StartCoroutine(DoAct(onFinish));
        }
    }

    /// <summary>
    /// Act sequence, take as long as you want
    /// </summary>
    protected abstract IEnumerator Act();

    protected abstract void ActExecute();

    protected virtual void OnDisable() {
        mIsActing = false;
        StopAllCoroutines();
    }

    IEnumerator DoAct(Callback onFinish) {
        mIsActing = true;

        yield return StartCoroutine(Act());

        mIsActing = false;

        ActExecute();

        if(disableOnAct)
            enabled = false;

        if(onFinish != null)
            onFinish();
    }
}
