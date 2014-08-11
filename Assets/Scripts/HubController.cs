using UnityEngine;
using System.Collections;

public class HubController : MonoBehaviour {
    [System.Serializable]
    public class DoorData {
        public SpecialTriggerDoor door;
        public GameObject completedActiveGO;
    }

    public DoorData[] doors;

    void Start() {
        //first one is always unlocked
        int bossDefeatCount = 0;

        doors[0].door.interactive = true;
        doors[0].door.startClosed = false;

        LevelController.State lastLevelState = LevelController.GetLevelState(doors[0].door.toScene);
        if(lastLevelState == LevelController.State.BossDefeated) {
            doors[0].completedActiveGO.SetActive(true);
            bossDefeatCount++;
        }
        else
            doors[0].completedActiveGO.SetActive(false);

        //unlock doors based on last door's level state
        for(int i = 1; i < doors.Length; i++) {
            if(lastLevelState == LevelController.State.BossDefeated) {
                doors[i].door.interactive = true;
                doors[i].door.startClosed = false;
            }
            else {
                doors[i].door.interactive = false;
                doors[i].door.startClosed = true;
            }

            lastLevelState = LevelController.GetLevelState(doors[i].door.toScene);
            if(lastLevelState == LevelController.State.BossDefeated) {
                doors[i].completedActiveGO.SetActive(false);
                bossDefeatCount++;
            }
            else
                doors[i].completedActiveGO.SetActive(true);
        }

        //all boss defeated?
    }
}
