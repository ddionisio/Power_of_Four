using UnityEngine;
using System.Collections;

public class PlayerStats : Stats {
    public const string currentHPKey = "chp";
    
    //preserve keys
    const string DNAPickupKey = "dnapk";

    public const int initialHeartCount = 4;
    public const float HitPerHeart = 2.0f;
    public const int heartPerTank = 4;

    public event ChangeCallbackInt changeMaxHeartCallback;
    public event ChangeCallbackInt changeHeartReserveCallback;
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

    /// <summary>
    /// Use this to count the hearts based on current max hitpoints.  Set when upgrading.
    /// </summary>
    public int heartMaxCount {
        get { return Mathf.RoundToInt(maxHP/HitPerHeart); }
        set {
            int curCount = heartMaxCount;
            int newCount = Mathf.Clamp(value, 0, int.MaxValue);
            if(curCount != value) {
                PlayerSave.heartUpgradeCount = newCount - initialHeartCount;

                int delta = newCount - curCount;

                maxHP = newCount*HitPerHeart;

                if(changeMaxHeartCallback != null)
                    changeMaxHeartCallback(this, delta);

                curHP += delta*HitPerHeart;
            }
        }
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

    public int DNAPickup {
        get { return UserData.instance.GetInt(DNAPickupKey); }
        set { UserData.instance.SetInt(DNAPickupKey, value); UserData.instance.SnapshotPreserve(DNAPickupKey); }
    }

    public void RevertDNAPickup() {
        mDNA -= DNAPickup;
        SaveDNAState();
        ClearDNAPickup();
    }

    public void ClearDNAPickup() {
        UserData.instance.Delete(DNAPickupKey);
    }

    public void SaveDNAState() {
        UserData.instance.SetInt("dna", mDNA);
        UserData.instance.SnapshotPreserve("dna");
    }

    public void SaveState() {
        UserData.instance.SetInt("hrc", mHeartReserveCur);
        SaveDNAState();
    }

    public void LoadState() {
        maxHP = (initialHeartCount + PlayerSave.heartUpgradeCount)*HitPerHeart;

        if(SceneState.instance.HasGlobalValue(currentHPKey))
            mCurHP = SceneState.instance.GetGlobalValueFloat(currentHPKey, maxHP);
        else
            mCurHP = maxHP;

        mHeartReserveCur = UserData.instance.GetInt("hrc");
        mHeartReserveMax = PlayerSave.heartTankCount*heartPerTank;

        mDNA = UserData.instance.GetInt("dna");
    }

    protected override void OnDestroy() {
        changeMaxHeartCallback = null;
        changeHeartReserveCallback = null;
        changeDNACallback = null;

        base.OnDestroy();
    }

    void Start() {
        LoadState();
    }
}
