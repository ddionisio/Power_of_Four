using UnityEngine;
using System.Collections;

public class TriggerPlayerWarpToLastGround : MonoBehaviour {
    public string spawnGrp; //for particle effect
    public string spawnType;
    public Vector3 spawnOfs;

    public float delay = 0.5f;

    private IEnumerator mAction;

    void OnTriggerEnter(Collider col) {
        if(mAction == null) {
            Player player = col.GetComponent<Player>();
            if(player)
                StartCoroutine(mAction = DoAction(player));
        }
    }

    void OnDisable() {
        mAction = null;
    }

    IEnumerator DoAction(Player player) {
        yield return new WaitForSeconds(delay);

        if(!string.IsNullOrEmpty(spawnGrp) && !string.IsNullOrEmpty(spawnType)) {
            PoolController.Spawn(spawnGrp, spawnType, null, null, player.transform.localToWorldMatrix.MultiplyPoint3x4(spawnOfs));
        }

        player.WarpToLastGroundPosition();

        mAction = null;
    }
}
