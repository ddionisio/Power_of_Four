using UnityEngine;
using System.Collections;

public class LevelController : MonoBehaviour {
    public enum State {
        None, //unfinished
        BossDoorUnlocked, //all eye orbs placed, boss undefeated
        BossDefeated //all eye orbs placed, boss defeated
    }

    public enum EyeOrbState {
        Available,
        Collected,
        Placed
    }

    public const string eyeInsertTagCheck = "EyeInsert"; //these are the eye inserts
    public const string bossDoorTagCheck = "BossDoor";
    public const string spawnPointTagCheck = "Spawnpoint";

    const string saveSpawnPointNameKey = "lvlpt";
    const string saveLevelNameKey = "lvl";

    //used when entering new level, not persistent
    const string spawnPointLevelNameKey = "splvlName";
    const string spawnPointNameKey = "spName";
    
    public string mainLevel; //should be the name of the main level, even if this stage is a sub of a sub

    public bool sceneTransOverride;
    public string sceneTransOut;
    public string sceneTransIn;

    private State mMainLevelState; //if mainLevel, then this is the state of the parent level
    private EyeOrbState[] mEyeOrbStates; //this is global to the main level and its sub levels
    private bool[] mEyeInsertIsFilled; //if this particular eye insert is filled.

    private GameObject[] mEyeInserts; //these are filled for main level

    private SpecialTriggerDoor mBossDoor;

    private int mPickUpBits; //filled upon start

    private static LevelController mInstance;

    public static LevelController instance { get { return mInstance; } }

    public static string savedLevelName { get { return UserData.instance.GetString(saveLevelNameKey); } }
    public static string savedLevelSpawnPointName {
        get {
            if(savedLevelName == Application.loadedLevelName)
                return UserData.instance.GetString(saveSpawnPointNameKey);
            return "";
        }
    }

    public bool isMain { get { return string.IsNullOrEmpty(mainLevel); } }

    public State mainLevelState { get { return mMainLevelState; } set { mMainLevelState = value; } }

    public int eyeOrbCount { get { return mEyeOrbStates.Length; } }

    public static State GetLevelState(string levelName) {
        return (State)UserData.instance.GetInt(levelName+"_s", 0);
    }

    public static void SetSavedLevel(string levelName, string spawnPoint) {
        if(string.IsNullOrEmpty(levelName)) {
            UserData.instance.Delete(saveLevelNameKey);
            UserData.instance.Delete(saveSpawnPointNameKey);
        }
        else {
            UserData.instance.SetString(saveLevelNameKey, levelName);

            if(string.IsNullOrEmpty(spawnPoint))
                UserData.instance.Delete(saveSpawnPointNameKey);
            else {
                UserData.instance.SetString(saveSpawnPointNameKey, spawnPoint);

                //set stored spawnpoint if it's the same level name
                if(levelName == SceneState.instance.GetGlobalValueString(spawnPointLevelNameKey))
                    SceneState.instance.SetGlobalValueString(spawnPointNameKey, spawnPoint, false);
            }
        }
    }

    public static Transform GetSpawnPoint() {
        string goName;

        string spawnPointScene = SceneState.instance.GetGlobalValueString(spawnPointLevelNameKey, "");
        if(spawnPointScene == Application.loadedLevelName) {
            goName = SceneState.instance.GetGlobalValueString(spawnPointNameKey, "");
            if(string.IsNullOrEmpty(goName)) {
                SceneState.instance.DeleteGlobalValue(spawnPointLevelNameKey, false);
                SceneState.instance.DeleteGlobalValue(spawnPointNameKey, false);

                goName = savedLevelSpawnPointName;
            }
        }
        else {
            SceneState.instance.DeleteGlobalValue(spawnPointLevelNameKey, false);
            SceneState.instance.DeleteGlobalValue(spawnPointNameKey, false);

            goName = savedLevelSpawnPointName;
        }

        if(!string.IsNullOrEmpty(goName)) {
            //check for check point gos
            GameObject[] gos = GameObject.FindGameObjectsWithTag(Checkpoint.checkpointTagCheck);
            for(int i = 0; i < gos.Length; i++) {
                if(gos[i].name == goName)
                    return gos[i].transform;
            }

            //check for spawn point gos
            gos = GameObject.FindGameObjectsWithTag(spawnPointTagCheck);
            for(int i = 0; i < gos.Length; i++) {
                if(gos[i].name == goName)
                    return gos[i].transform;
            }
        }

        return null;
    }

    /// <summary>
    /// Called when playing the game from main menu.
    /// If no saved scene exists, then load the new game level
    /// </summary>
    public static void LoadSavedLevel() {
        SceneState.instance.DeleteGlobalValue(PlayerStats.currentHPKey, false);

        string toScene = savedLevelName;
        if(string.IsNullOrEmpty(toScene)) {
            SetSavedLevel(Scenes.newGame, "");
            SceneManager.instance.LoadScene(Scenes.newGame);
        }
        else
            SceneManager.instance.LoadScene(toScene);
    }

    /// <summary>
    /// Use this to make use of the transition override and if you want to set player to a particular spot
    /// </summary>
    public void LoadScene(string scene, string spawnPoint) {
        if(string.IsNullOrEmpty(spawnPoint)) {
            SceneState.instance.DeleteGlobalValue(spawnPointLevelNameKey, false);
            SceneState.instance.DeleteGlobalValue(spawnPointNameKey, false);
        }
        else {
            SceneState.instance.SetGlobalValueString(spawnPointLevelNameKey, scene, false);
            SceneState.instance.SetGlobalValueString(spawnPointNameKey, spawnPoint, false);
        }

        if(sceneTransOverride)
            SceneManager.instance.LoadScene(scene, sceneTransOut, sceneTransIn);
        else
            SceneManager.instance.LoadScene(scene);
    }

    /// <summary>
    /// Used by ItemPickUp, only call during Start or after
    /// </summary>
    public bool PickUpBitIsSet(int bit) {
        return M8.Util.FlagCheckBit(mPickUpBits, bit);
    }

    /// <summary>
    /// Used by ItemPickUp, only call during Start or after
    /// </summary>
    public void PickUpBitSet(int bit, bool set) {
        if(M8.Util.FlagCheckBit(mPickUpBits, bit) != set) {
            mPickUpBits = M8.Util.FlagSetBit(mPickUpBits, bit, set);
            SaveMainLevelStates();
        }
    }

    public EyeOrbState eyeOrbGetState(int index) {
        return mEyeOrbStates[index];
    }

    public void eyeOrbSetState(int index, EyeOrbState state, bool save) {
        if(mEyeOrbStates[index] != state) {
            mEyeOrbStates[index] = state;
            if(save) SaveMainLevelStates();
            if(state == EyeOrbState.Placed) {
                //check if all is placed, then boss door open
                if(mMainLevelState == State.None) {
                    int placedCount = 0;
                    for(int i = 0; i < mEyeOrbStates.Length; i++) {
                        if(mEyeOrbStates[i] == EyeOrbState.Placed)
                            placedCount++;
                    }

                    if(placedCount == mEyeOrbStates.Length) {
                        mMainLevelState = State.BossDoorUnlocked;

                        if(mBossDoor) {
                            mBossDoor.interactive = true;
                            mBossDoor.Open();
                        }

                        if(save) Player.instance.Save();
                    }
                }
            }
        }
    }

    public bool eyeInsertIsFilled(int index) {
        return mEyeInsertIsFilled[index];
    }

    public void eyeInsertSetFilled(int index, bool filled, bool save) {
        if(mEyeInsertIsFilled[index] != filled) {
            mEyeInsertIsFilled[index] = filled;
            if(save) SaveMainLevelStates();
        }
    }

    public void Save() {
        SaveMainLevelStates();
    }

    void OnDestroy() {
        if(mInstance == this) {
            mInstance = null;
        }
    }

    void Awake() {
        if(mInstance == null) {
            mInstance = this;

            if(isMain) {
                mEyeInserts = GameObject.FindGameObjectsWithTag(eyeInsertTagCheck);

                GameObject bossDoorGO = GameObject.FindGameObjectWithTag(bossDoorTagCheck);
                if(bossDoorGO)
                    mBossDoor = bossDoorGO.GetComponent<SpecialTriggerDoor>();
            }
        }
    }

    void Start() {
        //load up stuff
        string mainLevelName;
        int eyeCount;

        if(isMain) {
            mainLevelName = Application.loadedLevelName;
            eyeCount = mEyeInserts.Length;

            UserData.instance.SetInt(mainLevelName+"_eyeNum", mEyeInserts.Length);
        }
        else {
            mainLevelName = mainLevel;

            eyeCount = UserData.instance.GetInt(mainLevelName+"_eyeNum", 4); //fail-safe = 4
        }

        mEyeOrbStates = new EyeOrbState[eyeCount];
        mEyeInsertIsFilled = new bool[eyeCount];

        LoadMainLevelStates();

        if(mBossDoor) {
            if(mMainLevelState == State.BossDoorUnlocked) {
                mBossDoor.startClosed = false;
                mBossDoor.interactive = true;
            }
            else {
                mBossDoor.startClosed = true;
                mBossDoor.interactive = false;
            }
        }

        //add eye orbs to player based on state
        StartCoroutine(DoAddPlayerEyeOrbs());
    }

    void LoadMainLevelStates() {
        string levelName = string.IsNullOrEmpty(mainLevel) ? Application.loadedLevelName : mainLevel;

        mMainLevelState = GetLevelState(levelName);

        int eyeStates = UserData.instance.GetInt(levelName+"_se", 0);
        int eyeInserts = UserData.instance.GetInt(levelName+"_sei", 0);
        for(int i = 0; i < mEyeOrbStates.Length; i++) {
            eyeOrbSetState(i, (EyeOrbState)(eyeStates&3), false); //this will change main level state if all is collected
            eyeInsertSetFilled(i, (eyeInserts&1) != 0, false);

            eyeStates>>=2;
            eyeInserts>>=1;
        }

        mPickUpBits = UserData.instance.GetInt(Application.loadedLevelName+"_pb");
    }

    void SaveMainLevelStates() {
        string levelName = string.IsNullOrEmpty(mainLevel) ? Application.loadedLevelName : mainLevel;

        UserData.instance.SetInt(levelName+"_s", (int)mMainLevelState);

        int eyeStates = 0;
        int eyeInserts = 0;
        for(int i = mEyeOrbStates.Length-1; i >= 0; i--) {
            eyeStates |= (int)mEyeOrbStates[i];
            if(mEyeInsertIsFilled[i]) eyeInserts |= 1;

            if(i > 0) {
                eyeStates<<=2;
                eyeInserts<<=1;
            }
        }

        UserData.instance.SetInt(levelName+"_se", eyeStates);
        UserData.instance.SetInt(levelName+"_sei", eyeInserts);

        string pickUpKey = Application.loadedLevelName+"_pb";
        UserData.instance.SetInt(pickUpKey, mPickUpBits);
        UserData.instance.SnapshotPreserve(pickUpKey);
    }

    void OnPlayerSpawn(EntityBase ent) {

    }

    IEnumerator DoAddPlayerEyeOrbs() {
        yield return new WaitForFixedUpdate();

        Vector3 playerPos = Player.instance.transform.position;
        EyeOrbPlayer eyeOrbPlayer = EyeOrbPlayer.instance;

        for(int i = 0; i < mEyeOrbStates.Length; i++) {
            if(mEyeOrbStates[i] == EyeOrbState.Collected) {
                eyeOrbPlayer.Add(playerPos, i);
            }
        }
    }
}
