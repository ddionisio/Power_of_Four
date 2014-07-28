using UnityEngine;
using System.Collections;

public class MaterialFloatPropertyControl : MonoBehaviour {
    public Material material;
    public string modProperty = "_ColorOverlay";

    public Renderer[] targets; //manually select targets, leave empty to grab recursively

    private Material[] mRendererDefaultMats;

    private int mModID;
    private Material mMatInstance;
    private float mValue;
    private bool mActive;

    public float val {
        get { return mValue; }
        set {
            if(mValue != value) {
                if(!mActive && mMatInstance) {
                    for(int i = 0, max = targets.Length; i < max; i++) {
                        targets[i].sharedMaterial = mMatInstance;
                    }

                    mActive = true;
                }

                mValue = value;

                if(mMatInstance)
                    mMatInstance.SetFloat(mModID, mValue);
            }
        }
    }

    public void Revert() {
        if(mActive) {
            for(int i = 0, max = targets.Length; i < max; i++) {
                targets[i].sharedMaterial = mRendererDefaultMats[i];
            }

            mActive = false;
        }
    }

    void OnDestroy() {
        Revert();

        if(mMatInstance)
            DestroyImmediate(mMatInstance);
    }

    void Awake() {
        mMatInstance = new Material(material);
        mModID = Shader.PropertyToID(modProperty);

        if(targets == null || targets.Length == 0)
            targets = GetComponentsInChildren<Renderer>(true);

        mRendererDefaultMats = new Material[targets.Length];

        for(int i = 0, max = targets.Length; i < max; i++) {
            mRendererDefaultMats[i] = targets[i].sharedMaterial;
        }
    }
}
