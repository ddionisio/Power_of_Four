using UnityEngine;
using System.Collections;

public class HUD : MonoBehaviour {
    public GameObject heartTemplate;
    public Transform heartHolder;

    public int lowHeartThreshold = 1;

    public UIPower[] powers;

    public UIHeartContainer heartContainer;
    public Color heartBlinkColor;
    public Color heartBlinkCritColor;

    public UIBoss boss;

    private static HUD mInstance;

    private UIHeart[] mHearts;

    public static HUD instance { get { return mInstance; } }

    void OnDestroy() {
        if(mInstance == this)
            mInstance = null;
    }

    void Awake() {
        mInstance = this;
    }

    void Start() {
        Player player = Player.instance;

        //initialize hearts
        player.stats.changeMaxHeartCallback += OnHeartMaxChange;
        player.stats.changeHPCallback += OnHPChange;

        //get initial hearts, if there are any
        mHearts = heartHolder.GetComponentsInChildren<UIHeart>(true);

        ApplyHeartMax();
        ApplyHeartCurrent(false);

        //initialize powers
        player.buddyUnlockCallback += OnBuddyUnlock;

        int i = 0;
        for(; i < player.buddies.Length; i++) {
            if(player.buddies[i].level > 0) {
                powers[i].Init(i, player.buddies[i]);
            }
            else
                powers[i].gameObject.SetActive(false);
        }
        for(; i < powers.Length; i++)
            powers[i].gameObject.SetActive(false);

        //heart jar
        player.stats.changeHeartReserveCallback += OnHeartReserveChange;

        heartContainer.curHeartCount = Mathf.Clamp(player.stats.heartReserveCurrent, 0, PlayerStats.heartPerTank);

        //boss
    }

    void ApplyHeartMax() {
        PlayerStats stats = Player.instance.stats;

        int curCount = mHearts.Length;
        int addCount = stats.heartMaxCount - curCount;
        if(addCount > 0) {
            System.Array.Resize<UIHeart>(ref mHearts, curCount + addCount);

            for(int i = 0; i < addCount; i++) {
                GameObject ngo = (GameObject)Object.Instantiate(heartTemplate);

                Transform nt = ngo.transform;
                nt.parent = heartHolder;
                nt.localPosition = Vector3.zero;
                nt.localRotation = Quaternion.identity;
                nt.localScale = Vector3.one;

                UIHeart heart = ngo.GetComponent<UIHeart>();

                mHearts[curCount + i] = heart;
            }
        }

        NGUILayoutBase.RefreshNow(heartHolder);
    }

    void ApplyHeartCurrent(bool hurt) {
        PlayerStats stats = Player.instance.stats;

        float heartVal = stats.curHP/PlayerStats.HitPerHeart;
        float heartFull = Mathf.Floor(heartVal);
        float remain = heartVal - heartFull;

        int curFullHeartCount = (int)heartFull;

        //blink?
        bool blinkCrit = curFullHeartCount <= lowHeartThreshold;
        bool blink = blinkCrit || hurt;
        Color blinkColor = blinkCrit ? heartBlinkCritColor : heartBlinkColor;

        int ind = 0;

        //full
        for(; ind < curFullHeartCount; ind++) {
            mHearts[ind].fillValue = 1.0f;

            if(blink)
                mHearts[ind].Blink(blinkCrit, blinkCrit ? 1 : 2, blinkColor);
            else
                mHearts[ind].BlinkStop();
        }


        //partial
        if(remain > 0.0f) {
            mHearts[ind].fillValue = remain;

            if(blink)
                mHearts[ind].Blink(blinkCrit, blinkCrit ? 1 : 2, blinkColor);
            else
                mHearts[ind].BlinkStop();

            ind++;
        }

        //empty
        for(; ind < mHearts.Length; ind++) {
            mHearts[ind].fillValue = 0.0f;

            if(blink)
                mHearts[ind].Blink(blinkCrit, blinkCrit ? 1 : 2, blinkColor);
            else
                mHearts[ind].BlinkStop();
        }
    }

    void OnBuddyUnlock(Player player, Buddy bud) {
        powers[bud.index].Init(bud.index, bud);
    }

    void OnHPChange(Stats stat, float delta) {
        ApplyHeartCurrent(delta < 0.0f);
    }

    void OnHeartMaxChange(Stats stat, int delta) {
        ApplyHeartMax();
    }

    void OnHeartReserveChange(Stats stat, int delta) {
        //TODO: blink to indicate change
        heartContainer.curHeartCount = Mathf.Clamp(Player.instance.stats.heartReserveCurrent, 0, PlayerStats.heartPerTank);
    }
}
