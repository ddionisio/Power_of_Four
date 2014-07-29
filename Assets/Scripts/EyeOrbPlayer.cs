using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EyeOrbPlayer : MonoBehaviour {
    public struct OrbData {
        public Transform orbT;
        public IEnumerator routine;
    }

    public GameObject template;
    public int count = 4;

    public float attachOfsChangeDelay = 1.0f;
    public float attachOfsRadius = 1.0f;
    public float orbMoveDelay = 0.5f;

    private static EyeOrbPlayer mInstance;
        
    private Queue<Transform> mOrbAvailables;
    private Queue<OrbData> mOrbActives;

    public static EyeOrbPlayer instance { get { return mInstance; } }

    public void Add(Vector3 spawnPoint) {
        if(mOrbAvailables.Count > 0) {
            Transform orb = mOrbAvailables.Dequeue();
            orb.position = spawnPoint;
            orb.gameObject.SetActive(true);

            IEnumerator r = DoOrbUpdate(orb);

            mOrbActives.Enqueue(new OrbData() { orbT=orb, routine=r });

            StartCoroutine(r);
        }
    }

    //Make sure to deactive the transform once you are done
    public Transform Remove() {
        if(mOrbActives.Count > 0) {
            OrbData dat = mOrbActives.Dequeue();
            mOrbAvailables.Enqueue(dat.orbT);
            StopCoroutine(dat.routine);
            return dat.orbT;
        }

        return null;
    }

    void OnDestroy() {
        mInstance = null;
    }

    void Awake() {
        mInstance = this;

        mOrbAvailables = new Queue<Transform>(count);
        mOrbActives = new Queue<OrbData>(count);

        for(int i = 0; i < count; i++) {
            GameObject ng = (GameObject)Object.Instantiate(template);
            Transform nt = ng.transform;
            nt.parent = transform;
            ng.SetActive(false);
            mOrbAvailables.Enqueue(nt);
        }
    }

    IEnumerator DoOrbUpdate(Transform orb) {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();

        float curAttachPointT = attachOfsChangeDelay;
        Vector3 attachPointOfs = Vector3.zero;
        Vector3 curVel = Vector3.zero;

        Transform attachTrans = Player.instance.eyeOrbPoint;

        while(true) {
            yield return wait;

            float dt = Time.fixedDeltaTime;

            curAttachPointT += dt;
            if(curAttachPointT >= attachOfsChangeDelay) {
                Vector2 r = Random.insideUnitCircle;
                attachPointOfs.x = r.x*attachOfsRadius;
                attachPointOfs.y = r.y*attachOfsRadius;

                curAttachPointT = 0.0f;
            }

            orb.position = Vector3.SmoothDamp(orb.position, attachTrans.position + attachPointOfs, ref curVel, orbMoveDelay, Mathf.Infinity, dt);
        }
    }

}
