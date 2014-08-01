using UnityEngine;
using System.Collections;

public class PlayerStats : Stats {
    public const int initialHeartCount = 4;
    public const float HitPerHeart = 2.0f;
    public const int heartPerTank = 4;

    public event ChangeCallback changeMaxHPCallback;
    public event ChangeCallbackInt changeDNACallback;

    private int mHeartReserveCur;
    private int mHeartReserveMax;

    private int mDNA;

    public int heartReserveCurrent {
        get { return mHeartReserveCur; }
        set {
            mHeartReserveCur = Mathf.Clamp(value, 0, mHeartReserveMax);
        }
    }

    public int heartReserveMax {
        get { return mHeartReserveMax; }
    }

    public int DNA {
        get { return mDNA; }
        set {
            int val = Mathf.Clamp(value, 0, int.MaxValue);
            if(mDNA != val) {
                int prev = mDNA;
                mDNA = val;

                if(changeDNACallback != null)
                    changeDNACallback(this, mDNA - prev);
            }
        }
    }

    public void SaveState() {
        UserData.instance.SetInt("hrc", mHeartReserveCur);
        UserData.instance.SetInt("dna", mDNA);
    }

    public void LoadState() {
        maxHP = (initialHeartCount + PlayerSave.heartUpgradeCount)*HitPerHeart;

        mCurHP = maxHP;

        mHeartReserveCur = UserData.instance.GetInt("hrc");
        mHeartReserveMax = PlayerSave.heartTankCount;

        mDNA = UserData.instance.GetInt("dna");
    }

    protected override void OnDestroy() {
        changeMaxHPCallback = null;
        changeDNACallback = null;

        base.OnDestroy();
    }

    void Start() {
        LoadState();
    }
}
