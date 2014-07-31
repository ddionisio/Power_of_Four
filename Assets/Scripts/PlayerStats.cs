using UnityEngine;
using System.Collections;

public class PlayerStats : Stats {
    public const float HitPerHeart = 2.0f;
    public const int heartPerTank = 4;

    public event ChangeCallback changeMaxHPCallback;
    public event ChangeCallback changeDNACallback;

    private float mDefaultMaxHP;

    private int mHeartReserveCur;
    private int mHeartReserveMax;

    private float mDNA;

    public int heartReserveCurrent {
        get { return mHeartReserveCur; }
        set {
            mHeartReserveCur = Mathf.Clamp(value, 0, mHeartReserveMax);
        }
    }

    public int heartReserveMax {
        get { return mHeartReserveMax; }
    }

    public float DNA {
        get { return mDNA; }
        set {
            float val = Mathf.Clamp(value, 0.0f, float.MaxValue);
            if(mDNA != val) {
                float prev = mDNA;
                mDNA = val;

                if(changeDNACallback != null)
                    changeDNACallback(this, mDNA - prev);
            }
        }
    }

    public void SaveState() {
    }

    public void LoadState() {
        mCurHP = maxHP;
    }

    protected override void OnDestroy() {
        changeMaxHPCallback = null;
        changeDNACallback = null;

        base.OnDestroy();
    }

    protected override void Awake() {
        mDefaultMaxHP = maxHP;
    }

    void Start() {
        LoadState();
    }
}
