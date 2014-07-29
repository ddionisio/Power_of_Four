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

    public const string eyeInsertTag = "EyeInsert"; //these are the eye inserts

    public string mainLevel; //should be the name of the main level, even if this stage is a sub of a sub

    private State mMainLevelState; //if mainLevel, then this is the state of the parent level
    private EyeOrbState[] mEyeOrbStates; //this is global to the main level and its sub levels
    private bool[] mEyeInsertIsFilled; //if this particular eye insert is filled.

    private GameObject[] mEyeInserts; //these are filled for main level

    private static LevelController mInstance;

    public static LevelController instance { get { return mInstance; } }

    public bool isMain { get { return string.IsNullOrEmpty(mainLevel); } }

    public State mainLevelState { get { return mMainLevelState; } }

    public int eyeOrbCount { get { return mEyeOrbStates.Length; } }

    public EyeOrbState eyeOrbGetState(int index) {
        return mEyeOrbStates[index];
    }

    public void eyeOrbSetState(int index, EyeOrbState state) {
        mEyeOrbStates[index] = state;
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
                }
            }
        }
    }

    public bool eyeInsertIsFilled(int index) {
        return mEyeInsertIsFilled[index];
    }

    public void eyeInsertSetFilled(int index, bool filled) {
        mEyeInsertIsFilled[index] = filled;
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

            //load up stuff
            string mainLevelName;
            int eyeCount;

            if(isMain) {
                mainLevelName = Application.loadedLevelName;
                mEyeInserts = GameObject.FindGameObjectsWithTag(eyeInsertTag);
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
        }
    }

    void Start() {
        //add eye orbs to player based on state
        Vector3 playerPos = Player.instance.transform.position;
        EyeOrbPlayer eyeOrbPlayer = EyeOrbPlayer.instance;

        for(int i = 0; i < mEyeOrbStates.Length; i++) {
            if(mEyeOrbStates[i] == EyeOrbState.Collected) {
                eyeOrbPlayer.Add(playerPos);
            }
        }
    }

    void LoadMainLevelStates() {
        string levelName = string.IsNullOrEmpty(mainLevel) ? Application.loadedLevelName : mainLevel;

        mMainLevelState = (State)UserData.instance.GetInt(levelName+"_s", 0);

        int eyeStates = UserData.instance.GetInt(levelName+"_se", 0);
        int eyeInserts = UserData.instance.GetInt(levelName+"_sei", 0);
        for(int i = 0; i < mEyeOrbStates.Length; i++) {
            eyeOrbSetState(i, (EyeOrbState)(eyeStates&3)); //this will change main level state if all is collected
            eyeInsertSetFilled(i, (eyeInserts&1) != 0);

            eyeStates>>=2;
            eyeInserts>>=1;
        }
    }

    void SaveMainLevelStates() {
        string levelName = string.IsNullOrEmpty(mainLevel) ? Application.loadedLevelName : mainLevel;

        UserData.instance.SetInt(levelName+"_s", (int)mMainLevelState);

        int eyeStates = 0;
        int eyeInserts = 0;
        for(int i = mEyeOrbStates.Length-1; i >= 0; i--) {
            eyeStates |= (int)mEyeOrbStates[i];
            if(mEyeInsertIsFilled[i]) eyeInserts |= 1;

            eyeStates<<=2;
            eyeInserts<<=1;
        }

        UserData.instance.SetInt(levelName+"_se", eyeStates);
        UserData.instance.SetInt(levelName+"_sei", eyeInserts);
    }
}
