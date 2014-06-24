using UnityEngine;
using System.Collections;

public class MaterialZSetSpriteShared : MonoBehaviour {
    public string varOfsName = "_ZOfs";

    void Awake() {
        SpriteRenderer[] sprs = Resources.FindObjectsOfTypeAll<SpriteRenderer>();
        for(int i = 0; i < sprs.Length; i++) {
            GameObject go = sprs[i].gameObject;
            if(go.hideFlags == HideFlags.NotEditable || go.hideFlags == HideFlags.HideAndDontSave)
                continue;
        }
    }

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
