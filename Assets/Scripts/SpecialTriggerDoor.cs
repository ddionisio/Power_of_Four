using UnityEngine;
using System.Collections;

public class SpecialTriggerDoor : SpecialTrigger {
    public string toScene;
    public string toSceneSpawnPoint; //which start point to spawn player to
    public bool save; //act as a checkpoint, save player stats and set the saved level/spawn point
    public bool saveSpawnPoint;

    public bool startClosed = false;

    public string takeClosed = "closed";
    public string takeOpening = "opening";
    public string takeOpened = "opened";

    private AnimatorData mAnim;
    private bool mClosed;
    private bool mOpening;
    private bool mStarted;

    public void Open() {
        if(mClosed && !mOpening) {
            StartCoroutine(DoOpening());
        }
    }

    protected override IEnumerator Act() {
        if(mClosed) {
            Open();
            WaitForFixedUpdate wait = new WaitForFixedUpdate();
            while(mClosed)
                yield return wait;
        }
    }

    protected override void ActExecute() {
        //save states and stuff then enter scene
        if(!string.IsNullOrEmpty(toScene)) {
            if(save) {
                LevelController.SetSavedLevel(toScene, saveSpawnPoint ? toSceneSpawnPoint : "");
                Player.instance.Save();
            }

            //save last buddy selected
            SceneState.instance.SetGlobalValue(Player.lastBuddySelectedKey, Player.instance.currentBuddyIndex, false);

            //save current health
            SceneState.instance.SetGlobalValueFloat(PlayerStats.currentHPKey, Player.instance.stats.curHP, false);

            if(LevelController.instance)
                LevelController.instance.LoadScene(toScene, toSceneSpawnPoint);
            else
                SceneManager.instance.LoadScene(toScene);
        }
    }

    void OnEnable() {
        if(mStarted) {
            if(mAnim) {
                if(mClosed)
                    mAnim.Play(takeClosed);
                else
                    mAnim.Play(takeOpened);
            }
        }
    }

    protected override void OnDisable() {
        if(mOpening) {
            mOpening = false;
            mClosed = false;
        }

        base.OnDisable();
    }

    void Awake() {
        mAnim = GetComponent<AnimatorData>();
    }

    void Start() {
        mClosed = startClosed;
        mStarted = true;
        OnEnable();
    }

    IEnumerator DoOpening() {
        if(mAnim) {
            mOpening = true;

            WaitForFixedUpdate wait = new WaitForFixedUpdate();

            mAnim.Play(takeOpening);
            while(mAnim.isPlaying)
                yield return wait;

            mAnim.Play(takeOpened);
        }

        mClosed = false;
        mOpening = false;
    }
}
