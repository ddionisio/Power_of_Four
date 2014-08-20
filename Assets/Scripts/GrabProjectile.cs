using UnityEngine;
using System.Collections;

public class GrabProjectile : Projectile {
    public Transform surroundHolder; //holder of items surrounding the contained
    public float surroundBoundScale = 1.0f; //scale of highest bound of contain to determine surface position

    public string impactProjGrp = "playerProj"; //pool group of projectile spawn
    public string impactProjType; //projectiles to spawn upon impact, amount and dir based on surrounds

    private Grab mContained;
    private Transform[] mSurroundItems;

    public Grab grab {
        get { return mContained; }
        set {
            mContained = value;

            Bounds containedBounds = mContained.collider.bounds;
            Transform containedT = mContained.transform;

            containedT.parent = transform;
            containedT.localPosition = -containedT.worldToLocalMatrix.MultiplyPoint3x4(containedBounds.center);
            containedT.localRotation = Quaternion.identity;
                        
            //determine surround stuff
            if(mSurroundItems.Length > 0) {
                Vector3 dir = Vector3.up;
                Quaternion rot = Quaternion.Euler(0, 0, 360.0f/mSurroundItems.Length);
                float dist = Mathf.Max(containedBounds.extents.x, containedBounds.extents.y)*surroundBoundScale;
                for(int i = 0; i < mSurroundItems.Length; i++) {
                    mSurroundItems[i].localPosition = dir*dist;
                    dir = rot*dir;
                }
            }

            //determine collision size
            Collider coll = collider;
            SphereCollider scoll = coll as SphereCollider;
            if(scoll) {
                scoll.radius = Mathf.Max(containedBounds.extents.x, containedBounds.extents.y);
            }
            //TODO: other shapes
        }
    }

    protected override void StateChanged() {
        if(state == (int)State.Dying) {
            //set Grab to Impact action
            if(mContained) {
                mContained.transform.parent = null;
                mContained.Impact(lastHit.point, lastHit.normal);
                mContained = null;

                //release impacts
                if(mSurroundItems.Length > 0) {
                    Vector3 pos = transform.position; pos.z = 0.0f;

                    for(int i = 0; i < mSurroundItems.Length; i++) {
                        Vector3 dir = mSurroundItems[i].position - pos; dir.z = 0.0f; dir.Normalize();
                        Projectile.Create(impactProjGrp, impactProjType, pos, dir, null);
                    }
                }
            }
        }

        base.StateChanged();
    }

    protected override void Awake() {
        base.Awake();

        mSurroundItems = new Transform[surroundHolder.childCount];
        for(int i = 0; i < mSurroundItems.Length; i++)
            mSurroundItems[i] = surroundHolder.GetChild(i);
    }
}
