using UnityEngine;
using System.Collections;

public struct PlayerSave {
    public static bool BuddyIsUnlock(int ind) {
        return true;
    }

    public static int BuddySelected() {
        //get the last saved buddy select
        return 0;
    }

    public static int BuddyLevel(int ind) {
        return 0;
    }

    /// <summary>
    /// Number of heart upgrade acquired.
    /// </summary>
    public static int HeartUpgradeCount() {
        return 0;
    }

    /// <summary>
    /// Number of heart tanks acquired.
    /// </summary>
    /// <returns></returns>
    public static int HeartTankCount() {
        return 0;
    }
}
