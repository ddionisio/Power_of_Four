using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Set all materials of children with given variable name to this object's Z value.
/// The materials are replicated.
/// </summary>
public class MaterialZSet : MonoBehaviour {
    public string varName = "_ZWorld";
    public string varOfsName = "_ZOfs";

    private int mVarId;
    private int mVarOfsId;
    private float mZOfs;
    private Renderer[] mRenders;
    private MaterialPropertyBlock mMatProp;

    public void ApplyZWorld() {
        mMatProp.Clear();
        if(mVarId != -1) mMatProp.AddFloat(mVarId, transform.position.z);
        if(mVarOfsId != -1) mMatProp.AddFloat(mVarOfsId, mZOfs);

        for(int i = 0; i < mRenders.Length; i++) {
            mRenders[i].SetPropertyBlock(mMatProp);
        }
    }

    public void ApplyZOfs(float zofs) {
        mZOfs = zofs;
        ApplyZWorld();
    }

    void Awake() {
        mVarId = string.IsNullOrEmpty(varName) ? -1 : Shader.PropertyToID(varName);
        mVarOfsId = string.IsNullOrEmpty(varOfsName) ? -1 : Shader.PropertyToID(varOfsName);
    }

	// Use this for initialization
	void Start () {
        List<Renderer> renderList = new List<Renderer>();

        Renderer[] renders = GetComponentsInChildren<Renderer>(true);
        foreach(Renderer render in renders) {
            //render.SetPropertyBlock
            bool hasProp = false;
            Material[] sharedMats = render.sharedMaterials;
            for(int i = 0; i < sharedMats.Length; i++) {
                if(sharedMats[i].HasProperty(varName) || sharedMats[i].HasProperty(varOfsName)) {
                    hasProp = true;
                    break;
                }
            }

            if(hasProp) {
                renderList.Add(render);
            }
        }

        mMatProp = new MaterialPropertyBlock();
        mRenders = renderList.ToArray();

        ApplyZWorld();
	}
}
