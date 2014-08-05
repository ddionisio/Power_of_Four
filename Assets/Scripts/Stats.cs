using UnityEngine;
using System.Collections;

public class Stats : MonoBehaviour {
    public delegate void ChangeCallback(Stats stat, float delta);
    public delegate void ChangeCallbackInt(Stats stat, int delta);
    public delegate void ApplyDamageCallback(Damage damage);

    [System.Serializable]
    public class DamageMod {
        public Damage.Type type;
        public float val;
    }

    public float maxHP;

    public float damageAmp = 0.0f; //general amplification
    public float damageReduction = 0.0f;

    public DamageMod[] damageTypeAmp;
    public DamageMod[] damageTypeReduction;

    public string deathTag = "Death";

    public event ChangeCallback changeHPCallback;
    public event ApplyDamageCallback applyDamageCallback;

    protected Damage mLastDamage;
    protected Vector3 mLastDamagePos;
    protected Vector3 mLastDamageNorm;

    protected float mCurHP;
    private bool mIsInvul;

    public float curHP {
        get { return mCurHP; }

        set {
            float v = Mathf.Clamp(value, 0, maxHP);
            if(mCurHP != v) {
                float prev = mCurHP;
                mCurHP = v;

                if(changeHPCallback != null)
                    changeHPCallback(this, mCurHP - prev);
            }
        }
    }

    public virtual bool isInvul { get { return mIsInvul; } set { mIsInvul = value; } }

    public Damage lastDamageSource { get { return mLastDamage; } }

    /// <summary>
    /// This is the latest damage hit position when hp was reduced, set during ApplyDamage
    /// </summary>
    public Vector3 lastDamagePosition { get { return mLastDamagePos; } }

    /// <summary>
    /// This is the latest damage hit normal when hp was reduced, set during ApplyDamage
    /// </summary>
    public Vector3 lastDamageNormal { get { return mLastDamageNorm; } }

    public void AddDamageReduce(float amt) {
        for(int i = 0; i < damageTypeReduction.Length; i++) {
            damageTypeReduction[i].val += amt;
        }
    }

    public DamageMod GetDamageMod(DamageMod[] dat, Damage.Type type) {
        if(dat != null) {
            for(int i = 0, max = dat.Length; i < max; i++) {
                if(dat[i].type == type) {
                    return dat[i];
                }
            }
        }
        return null;
    }

    public bool CanDamage(Damage damage) {
        if(!isInvul) {
            float amt = damage.amount;

            if(damageAmp > 0.0f) {
                amt += amt * damageAmp;
            }

            if(damageReduction > 0.0f) {
                amt -= amt * damageReduction;
            }

            DamageMod damageAmpByType = GetDamageMod(damageTypeAmp, damage.type);
            if(damageAmpByType != null) {
                amt += damage.amount * damageAmpByType.val;
            }
            else {
                DamageMod damageReduceByType = GetDamageMod(damageTypeReduction, damage.type);
                if(damageReduceByType != null)
                    amt -= amt * damageReduceByType.val;
            }

            return amt > 0.0f;
        }
        return false;
    }

    protected float CalculateDamageAmount(Damage damage) {
        float amt = damage.amount;

        if(damageAmp > 0.0f) {
            amt += amt * damageAmp;
        }

        if(damageReduction > 0.0f) {
            amt -= amt * damageReduction;
        }

        DamageMod damageAmpByType = GetDamageMod(damageTypeAmp, damage.type);
        if(damageAmpByType != null) {
            amt += damage.amount * damageAmpByType.val;
        }
        else {
            DamageMod damageReduceByType = GetDamageMod(damageTypeReduction, damage.type);
            if(damageReduceByType != null)
                amt -= amt * damageReduceByType.val;
        }

        return amt;
    }

    protected void ApplyDamageEvent(Damage damage) {
        if(applyDamageCallback != null)
            applyDamageCallback(damage);
    }

    public virtual bool ApplyDamage(Damage damage, Vector3 hitPos, Vector3 hitNorm) {
        mLastDamage = damage;
        mLastDamagePos = hitPos;
        mLastDamageNorm = hitNorm;

        if(!isInvul && mCurHP > 0.0f) {
            float amt = CalculateDamageAmount(damage);

            if(amt > 0.0f) {
                curHP -= amt;

                ApplyDamageEvent(damage);

                return true;
            }
        }

        return false;
    }

    public virtual void Reset() {
        curHP = maxHP;
        mIsInvul = false;
        mLastDamage = null;
    }

    protected virtual void OnDestroy() {
        changeHPCallback = null;
        applyDamageCallback = null;
    }

    protected virtual void Awake() {
        mCurHP = maxHP;
    }

    void OnCollisionEnter(Collision col) {
        if(col.gameObject.CompareTag(deathTag)) {
            SendMessage("OnSuddenDeath", null, SendMessageOptions.DontRequireReceiver);
            //curHP = 0;
        }
    }

    void OnTriggerEnter(Collider col) {
        if(col.gameObject.CompareTag(deathTag)) {
            SendMessage("OnSuddenDeath", null, SendMessageOptions.DontRequireReceiver);
            //curHP = 0;
        }
    }
}
