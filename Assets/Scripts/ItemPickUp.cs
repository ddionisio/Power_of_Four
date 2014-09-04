using UnityEngine;
using System.Collections;

public class ItemPickUp : EntityBase {
    public enum State {
        Invalid = -1,
        Active,
        PickUp
    }

    public const float destroyDelay = 5.0f;
    public const float destroyBlinkDelay = 1.0f;

    public delegate void OnPickUp(ItemPickUp item);

    public ItemType type;
    public int bit; //which bit in the flag, used by certain types
    public bool savePickUp; //if true, sets flag in savePickUpBit to save item as picked up
    public int savePickUpBit; //make sure not to set >= 30
    public int value; //used by certain types
    public string sound;

    public float dropRadius;
    public LayerMask dropLayerMask; //which layers the drop will stop when hit
    public float dropSpeed;

    public float pickUpDelay = 0.5f;

    public event OnPickUp pickUpCallback;

    private SpriteColorBlink[] mBlinkers;

    private IEnumerator mAction;

    private bool mBlinking;
        
    /// <summary>
    /// Force pickup
    /// </summary>
    public void PickUp(Player player) {
        if(player && player.state != (int)EntityState.Dead && player.state != (int)EntityState.Invalid && gameObject.activeInHierarchy) {
            switch(type) {
                case ItemType.Heart:
                    float hp = value*PlayerStats.HitPerHeart;

                    if(player.stats.curHP < player.stats.maxHP) {
                        //add leftover to reserve
                        if(player.stats.curHP + hp > player.stats.maxHP)
                            player.stats.heartReserveCurrent += Mathf.RoundToInt((player.stats.curHP + hp) - player.stats.maxHP);

                        player.stats.curHP += hp;
                    }
                    else { //hp is full, put in reserve
                        if(player.stats.heartReserveCurrent < player.stats.heartReserveMax) {
                            player.stats.heartReserveCurrent += value;
                        }
                        else
                            return; //can't pick up yet
                    }
                    break;

                case ItemType.DNA:
                    player.stats.DNA += value;
                    break;
            }
            //set action to getting picked up
            state = (int)State.PickUp;
        }
    }

    void OnTriggerEnter(Collider col) {
        Player player = col.GetComponent<Player>();
        if(player)
            PickUp(player);
    }

    protected override void OnDespawned() {
        //reset stuff here
        state = (int)State.Invalid;

        base.OnDespawned();
    }

    protected override void OnDestroy() {
        //dealloc here
        pickUpCallback = null;

        base.OnDestroy();
    }

    protected override void StateChanged() {
        if(mAction != null) {
            StopCoroutine(mAction);
            mAction = null;
        }

        if(mBlinking) {
            foreach(SpriteColorBlink blinker in mBlinkers)
                blinker.enabled = false;
            mBlinking = false;
        }

        switch((State)state) {
            case State.Active:
                if(collider)
                    collider.enabled = true;

                StartCoroutine(mAction = DoActive());
                break;

            case State.PickUp:
                if(savePickUp)
                    LevelController.instance.PickUpBitSet(savePickUpBit, true);

                if(collider)
                    collider.enabled = false;

                if(pickUpCallback != null)
                    pickUpCallback(this);

                StartCoroutine(mAction = DoPickUp());
                break;

            case State.Invalid:
                if(collider)
                    collider.enabled = false;
                break;
        }
    }

    public override void SpawnFinish() {
        //start ai, player control, etc
        state = (int)State.Active;
    }

    protected override void OnSpawned() {
        base.OnSpawned();

        if(collider)
            collider.enabled = false;

    }

    protected override void Awake() {
        base.Awake();

        mBlinkers = GetComponentsInChildren<SpriteColorBlink>(true);
        foreach(SpriteColorBlink blinker in mBlinkers)
            blinker.enabled = false;

        autoSpawnFinish = true;
    }

    protected override void Start() {
        base.Start();

        bool doDisable = savePickUp ? LevelController.instance.PickUpBitIsSet(savePickUpBit) : false;
        if(doDisable) {
            if(activator)
                activator.ForceActivate();
            gameObject.SetActive(false);
        }
    }

    void OnDrawGizmosSelected() {
        if(dropRadius > 0.0f && collider) {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(collider.bounds.center, dropRadius);
        }
    }

    IEnumerator DoActive() {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        bool drop = true;
        while(drop) {
            yield return wait;

            float moveY = dropSpeed * Time.fixedDeltaTime;
            Vector3 pos = transform.position;

            RaycastHit hit;
            if(Physics.SphereCast(collider.bounds.center, dropRadius, Vector3.down, out hit, moveY, dropLayerMask)) {
                pos = hit.point + hit.normal * dropRadius;
                drop = false;
            }
            else {
                pos.y -= moveY;
            }

            transform.position = pos;
        }

        //wait
        yield return new WaitForSeconds(destroyDelay);

        //blink
        mBlinking = true;
        foreach(SpriteColorBlink blinker in mBlinkers)
            blinker.enabled = true;

        yield return new WaitForSeconds(destroyBlinkDelay);

        if(state == (int)State.Active)
            Release();
    }

    IEnumerator DoPickUp() {
        Collider playerCol = Player.instance.collider;
        Transform trans = transform;

        Vector3 startPos = trans.position;

        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        float curTime = 0.0f;

        while(true) {
            yield return wait;

            curTime += Time.fixedDeltaTime;
            if(curTime < pickUpDelay) {
                float t = Holoville.HOTween.Core.Easing.Quad.EaseInOut(curTime, 0.0f, 1.0f, pickUpDelay, 1.0f, 1.0f);
                trans.position = Vector3.Lerp(startPos, playerCol.bounds.center, t);
            }
            else {
                trans.position = playerCol.bounds.center;
                break;
            }
        }

        if(!string.IsNullOrEmpty(sound))
            SoundPlayerGlobal.instance.Play(sound);

        Release();
    }
}
