using UnityEngine;
using System.Collections;

public class SpecialTriggerDoor : SpecialTrigger {
    public string toScene;
    public string toSceneSpawnPoint; //which start point to spawn player to
    public bool save; //act as a checkpoint, save player stats and set the saved level/spawn point

    public bool startClosed = false;

    public string takeClosed = "closed";
    public string takeOpening = "opening";
    public string takeOpened = "opened";

    private AnimatorData mAnim;
    private bool mClosed;

    protected override IEnumerator Act() {
        if(mClosed) {
            if(mAnim) {
                WaitForFixedUpdate wait = new WaitForFixedUpdate();

                mAnim.Play(takeOpening);
                while(mAnim.isPlaying)
                    yield return wait;

                mAnim.Play(takeOpened);
            }

            mClosed = false;
        }
    }

    protected override void ActExecute() {
        //save states and stuff then enter scene
        if(!string.IsNullOrEmpty(toScene)) {
            if(save) {
                LevelController.SetSavedLevel(toScene, toSceneSpawnPoint);

                Player.instance.Save();
            }
            else {
                LevelController.SetTempSpawnPoint(toSceneSpawnPoint);
            }

            SceneManager.instance.LoadScene(toScene);
        }
    }

    void Awake() {
        mAnim = GetComponent<AnimatorData>();
    }

    void Start() {
        mClosed = startClosed;
        if(mAnim) {
            if(mClosed)
                mAnim.Play(takeClosed);
            else
                mAnim.Play(takeOpened);
        }
    }
}
