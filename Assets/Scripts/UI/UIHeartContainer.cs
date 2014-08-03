using UnityEngine;
using System.Collections;

public class UIHeartContainer : MonoBehaviour {
    public class HeartDat {
        private Transform mHeart;
        private IEnumerator mRun;

        public HeartDat(UIWidget parent, GameObject template) {
            GameObject ngo = (GameObject)Object.Instantiate(template);

            mHeart = ngo.transform;
            mHeart.parent = parent.transform;
            mHeart.localPosition = Vector3.zero;
            mHeart.localRotation = Quaternion.identity;
            mHeart.localScale = Vector3.one;

            UIWidget ui = mHeart.GetComponentInChildren<UIWidget>();
            ui.depth = parent.depth + 1;

            ngo.SetActive(false);
        }

        public void Run(MonoBehaviour b, Vector3 ofs, float radius, float moveDelay, float destMoveDelay) {
            mHeart.gameObject.SetActive(true);

            b.StartCoroutine(mRun = DoHeartMove(ofs, radius, moveDelay, destMoveDelay));
        }

        public void Stop(MonoBehaviour b) {
            mHeart.gameObject.SetActive(false);

            if(mRun != null) {
                b.StopCoroutine(mRun);
                mRun = null;
            }
        }

        private Vector3 Dest(float radius) {
            return (Quaternion.Euler(0, 0, Random.Range(0, 360))*Vector3.up*radius);
        }

        private IEnumerator DoHeartMove(Vector3 ofs, float radius, float moveDelay, float destMoveDelay) {
            WaitForFixedUpdate wait = new WaitForFixedUpdate();

            Vector3 dest = mHeart.parent.localToWorldMatrix.MultiplyPoint3x4(Dest(radius) + ofs);
            Vector3 nDest = mHeart.parent.localToWorldMatrix.MultiplyPoint3x4(Dest(radius) + ofs);
            Vector3 curVel = Vector3.zero;

            float curDestTime = 0.0f;

            while(true) {
                yield return wait;

                float dt = Time.fixedDeltaTime;

                curDestTime += dt;

                float destT = curDestTime/destMoveDelay;
                if(destT > 1.0f) {
                    destT = 0.0f;
                    curDestTime = 0.0f;
                    dest = nDest;
                    nDest = mHeart.parent.localToWorldMatrix.MultiplyPoint3x4(Dest(radius) + ofs);
                }

                mHeart.position = Vector3.SmoothDamp(mHeart.position, Vector3.Lerp(dest, nDest, destT), ref curVel, moveDelay, dt);
            }
        }
    }

    public GameObject heartTemplate;

    public UIWidget containerUI;
    public float containRadius = 1.0f;
    public Vector2 containOfs;

    public float heartMoveDelay = 1.0f;
    public float heartDestMoveDelay = 2.0f;

    private HeartDat[] mHearts;
    private int mCurHeartCount;

    private bool mStarted;

    public int curHeartCount {
        get { return mCurHeartCount; }
        set {
            if(mCurHeartCount < value) {
                for(; mCurHeartCount < value && mCurHeartCount < mHearts.Length; mCurHeartCount++) {
                    if(gameObject.activeInHierarchy)
                        mHearts[mCurHeartCount].Run(this, containOfs, containRadius, heartMoveDelay, heartDestMoveDelay);
                }
            }
            else if(mCurHeartCount > value) {
                for(; mCurHeartCount > value; mCurHeartCount--) {
                    if(gameObject.activeInHierarchy)
                        mHearts[mCurHeartCount-1].Stop(this);
                }
            }
        }
    }

    void OnEnable() {
        if(mStarted) {
            for(int i = 0; i < mCurHeartCount; i++)
                mHearts[i].Run(this, containOfs, containRadius, heartMoveDelay, heartDestMoveDelay);
        }
    }

    void OnDisable() {
        for(int i = 0; i < mCurHeartCount; i++)
            mHearts[i].Stop(this);
    }

    void Awake() {
        mHearts = new HeartDat[PlayerStats.heartPerTank];
        for(int i = 0; i < mHearts.Length; i++) {
            mHearts[i] = new HeartDat(containerUI, heartTemplate);
        }
    }

    void Start() {
        mStarted = true;
    }

    void OnDrawGizmosSelected() {
        if(containRadius > 0.0f && containerUI) {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(containerUI.transform.position + containerUI.transform.localToWorldMatrix.MultiplyVector(containOfs), containerUI.transform.lossyScale.x*containRadius);
        }
    }
}
