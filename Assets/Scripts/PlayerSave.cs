using UnityEngine;
using System.Collections;

public struct PlayerSave {
    private static bool mLoaded = false;

    private static int mBuddyLevelsDat;
    private static int mHeartDat; //includes heart upgrade count and heart tank count

    /// <summary>
    /// Number of heart upgrade acquired.
    /// </summary>
    public static int heartUpgradeCount {
        get {
            if(!mLoaded) LoadData();
            return M8.Util.GetLoWord((uint)mHeartDat);
        }
        set {
            mHeartDat = (int)M8.Util.MakeLong((ushort)heartTankCount, (ushort)value);
        }
    }

    public static int heartTankCount {
        get {
            if(!mLoaded) LoadData();
            return M8.Util.GetHiWord((uint)mHeartDat);
        }
        set {
            mHeartDat = (int)M8.Util.MakeLong((ushort)value, (ushort)heartUpgradeCount);
        }
    }

    /// <summary>
    /// Return 0 if not acquired.
    /// </summary>
    public static int BuddyGetLevel(int ind) {
        if(!mLoaded) LoadData();

        int dat = mBuddyLevelsDat;
        if(ind > 0)
            dat >>= ind*3;
        
        //CHEAT
        //return 1;

        return dat & 7;
    }

    public static void BuddySetLevel(int ind, int level) {
        int shift = ind*3;
        int val = (level & 7) << shift;
        mBuddyLevelsDat &= ~(7<<shift); //clear
        mBuddyLevelsDat |= val; //set
    }
        
    /// <summary>
    /// Number of heart tanks acquired.
    /// </summary>
    /// <returns></returns>
    public static int HeartTankCount() {
        return 0;
    }

    public static void LoadData() {
        int curSlot = UserSlotData.currentSlot;

        mBuddyLevelsDat = UserSlotData.GetSlotValueInt(curSlot, "buds");
        mHeartDat = UserSlotData.GetSlotValueInt(curSlot, "hs");

        mLoaded = true;
    }

    public static void SaveData() {
        if(mLoaded) {
            int curSlot = UserSlotData.currentSlot;

            UserSlotData.SetSlotValueInt(curSlot, "buds", mBuddyLevelsDat);
            UserSlotData.SetSlotValueInt(curSlot, "hs", mHeartDat);
        }
    }

    public static void UnloadData() {
        mLoaded = false;

        mBuddyLevelsDat = 0;
        mHeartDat = 0;
    }
}
