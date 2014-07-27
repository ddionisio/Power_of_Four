using UnityEngine;
using System.Collections;

public class Damage : MonoBehaviour {
    public const string DamageMessage = "OnDamage";

    public enum Type {
        Physical,
        Fire,
        Water,
        Earth,
        Wind,

        NumType
    }

    public float amount;
    public Type type = Type.Physical;

    public string noDamageSpawnGroup;
    public string noDamageSpawnType;
    public string noDamageSfx;

    public bool CallDamageTo(Stats stat, Vector3 hitPos, Vector3 hitNorm) {
        bool ret = stat.ApplyDamage(this, hitPos, hitNorm);

        if(!ret) {
            if(!string.IsNullOrEmpty(noDamageSfx))
                SoundPlayerGlobal.instance.Play(noDamageSfx);

            if(!string.IsNullOrEmpty(noDamageSpawnGroup) && !string.IsNullOrEmpty(noDamageSpawnType)) {
                PoolController.Spawn(noDamageSpawnGroup, noDamageSpawnType, null, null, new Vector2(hitPos.x, hitPos.y));
            }
        }

        return ret;
    }

    public bool CallDamageTo(GameObject target, Vector3 hitPos, Vector3 hitNorm) {
        bool ret;
        //target.SendMessage(DamageMessage, this, SendMessageOptions.DontRequireReceiver);
        Stats stat = target.GetComponent<Stats>();
        if(stat)
            ret = CallDamageTo(stat, hitPos, hitNorm);
        else
            ret = false;

        return ret;
    }
}
