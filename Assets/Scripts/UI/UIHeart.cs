using UnityEngine;
using System.Collections;

public class UIHeart : MonoBehaviour {
    public UISprite fill;
    public UIWidget back;

    public float blinkDelay = 0.1f;
    public float blinkRestDelay;

    private float mFillValue = 1.0f;
    private Color mDefaultBackColor;
    private bool mBlinking;

    private int mBlinkCount;
    
    private Color mBlinkColor;
    private bool mBlinkInfinite;

    /// <summary>
    /// 0-1
    /// </summary>
    public float fillValue {
        get { return mFillValue; }
        set {
            if(mFillValue != value) {
                mFillValue = value;
                fill.fillAmount = mFillValue;
            }
        }
    }

    public void Blink(bool infinite, int count, Color color) {
        mBlinkInfinite = infinite;
        mBlinkCount = count;
        mBlinkColor = color;

        if(!mBlinking) {
            mBlinking = true;
            StartCoroutine(DoBlink());
        }
    }

    public void BlinkStop() {
        mBlinking = false;
        back.color = mDefaultBackColor;
    }

    void OnDisable() {
        BlinkStop();
    }

    void Awake() {
        mDefaultBackColor = back.color;
    }

    IEnumerator DoBlink() {
        WaitForSeconds bd = new WaitForSeconds(blinkDelay);
        WaitForSeconds brd = new WaitForSeconds(blinkRestDelay);

        while(mBlinking) {
            for(int i = 0; i < mBlinkCount; i++) {
                back.color = mBlinkColor;
                yield return bd;
                back.color = mDefaultBackColor;
                yield return bd;
            }

            if(mBlinkInfinite)
                yield return brd;
            else
                mBlinking = false;
        }
    }
}
