using UnityEngine;
using System.Collections;

public class Checkpoint : SpecialTrigger {
    public const string checkpointTagCheck = "Checkpoint";

    public string takeInactive ="inactive";
    public string takeActivate = "activate";
    public string takeActive = "active";

    private bool mActivated;
    private AnimatorData mAnimDat;

    public bool isActivated { get { return mActivated; } }

    void OnTriggerEnter(Collider col) {
        Player player = Player.instance;

        if(col == player.collider) {
            //ensure player is in proper state
            if(player.isSpawned) {
                _Activate();

                SceneState.instance.DeleteGlobalValue(PlayerStats.currentHPKey, false);

                //set spawn point
                LevelController.SetSavedLevel(Application.loadedLevelName, name);

                //save game
                player.Save();
            }
        }
    }

    void Awake() {
        mAnimDat = GetComponent<AnimatorData>();
    }

    void Start() {
        mActivated = SceneState.instance.GetValue(name) != 0;
        if(mAnimDat)
            mAnimDat.Play(mActivated ? takeActive : takeInactive);
    }

    protected override IEnumerator Act() {
        yield return null;
    }

    protected override void ActExecute() {
        //pop-up UI
    }

    void _Activate() {
        if(!mActivated) {
            mActivated = true;
            SceneState.instance.SetValue(name, 1, true);
            StartCoroutine(DoActivate());
        }
    }

    IEnumerator DoActivate() {
        if(mAnimDat) {
            WaitForFixedUpdate wait = new WaitForFixedUpdate();
            mAnimDat.Play(takeActivate);
            while(mAnimDat.isPlaying)
                yield return wait;
            mAnimDat.Play(takeActive);
        }
    }
}
