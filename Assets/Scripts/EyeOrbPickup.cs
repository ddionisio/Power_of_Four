using UnityEngine;
using System.Collections;

public class EyeOrbPickup : MonoBehaviour {
    public int index = 0;

    private EntityActivator mActivator;

    void OnTriggerEnter(Collider col) {
        LevelController.instance.eyeOrbSetState(index, LevelController.EyeOrbState.Collected, true);
        EyeOrbPlayer.instance.Add(transform.position, index);
        gameObject.SetActive(false);
    }

    void Awake() {
        mActivator = GetComponent<EntityActivator>();
    }

    void Start() {
        LevelController lvlCtrl = LevelController.instance;

        //check if already picked up
        if(lvlCtrl.eyeOrbGetState(index) != LevelController.EyeOrbState.Available) {
            gameObject.SetActive(false);

            if(mActivator)
                mActivator.Release(false);
        }
    }
}
