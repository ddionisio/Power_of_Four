using UnityEngine;
using System.Collections;

public class SpecialTriggerTreasure : SpecialTrigger {
    public ItemType type;
    public int pickupBit;
    public int value;

    public string sound;

    public string takeClosed = "closed";
    public string takeOpening = "opening";
    public string takeOpened = "opened";

    private AnimatorData mAnim;
    private bool mClosed;

    protected override IEnumerator Act() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        if(mClosed) {
            mAnim.Play("opening");
            while(mAnim.isPlaying)
                yield return wait;
            mClosed = false;
        }

        mAnim.Play(takeOpened);
    }

    protected override void ActExecute() {
        LevelController.instance.PickUpBitSet(pickupBit, true);

        Player player = Player.instance;

        switch(type) {
            case ItemType.DNA:
                player.stats.DNA += value;
                player.stats.SaveDNAState();
                break;
        }
    }

    void Awake() {
        mAnim = GetComponent<AnimatorData>();
    }

    void Start() {
        mClosed = !LevelController.instance.PickUpBitIsSet(pickupBit);
        mAnim.Play(mClosed ? takeClosed : takeOpened);
        interactive = mClosed;
        collider.enabled = mClosed;
    }
}
