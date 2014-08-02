using UnityEngine;
using System.Collections;

public class HUD : MonoBehaviour {
    public UIPower[] powers;

    void Start() {
        Player player = Player.instance;

        player.buddyUnlockCallback += OnBuddyUnlock;

        //initialize powers
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

        //boss
    }

    void OnBuddyUnlock(Player player, Buddy bud) {
        powers[bud.index].Init(bud.index, bud);
    }
}
