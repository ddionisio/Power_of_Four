using UnityEngine;
using System.Collections;

public class MaterialZSetPlayerListener : MonoBehaviour {
    private MaterialZSet mMatZSet;
    private bool mStarted;

    void OnEnable() {
        if(mStarted) {
            if(Player.instance) {
                Player.instance.layerMoveUpdateCallback += OnPlayerLayerMove;
                mMatZSet.ApplyZOfs(Player.instance.transform.position.z);
            }
        }
    }

    void OnDisable() {
        if(Player.instance)
            Player.instance.layerMoveUpdateCallback -= OnPlayerLayerMove;
    }

    void Awake() {
        mMatZSet = GetComponent<MaterialZSet>();
    }

    void Start() {
        mStarted = true;
        OnEnable();
    }

    void OnPlayerLayerMove(Player p, float z) {
        mMatZSet.ApplyZOfs(z);
    }
}
