﻿using UnityEngine;
using System.Collections;

/// <summary>
/// Ensure that there is a Camera2D to the MainCamera tagged Game Object.
/// </summary>
public class Camera2DActivator : EntityActivator {
    public Transform targetRef; //if not null, determine visibility based on this position
    public Rect extents;
    public float checkDelay = 0.1f;

    public override bool isVisible {
        get {
            return _CheckContain();
        }
    }

    private bool mStarted;
    private bool mCheckActive;

    bool _CheckContain() {
        Camera2D cam = Camera2D.main;
        Vector2 camPos = cam.transform.position;
        Rect screen = cam.screenExtent;
        screen.center = new Vector2(camPos.x, camPos.y);

        bool isContained = false;

        Vector2 pos = targetRef ? targetRef.position : transform.position;
        Rect wExtends = extents;
        wExtends.center = new Vector2(pos.x + (wExtends.center.x - wExtends.width * 0.5f), pos.y + (wExtends.center.y - wExtends.height * 0.5f));

        if(screen.Contains(new Vector2(wExtends.xMin, wExtends.yMin)))
            isContained = true;
        else if(screen.Contains(new Vector2(wExtends.xMin, wExtends.yMax)))
            isContained = true;
        else if(screen.Contains(new Vector2(wExtends.xMax, wExtends.yMax)))
            isContained = true;
        else if(screen.Contains(new Vector2(wExtends.xMax, wExtends.yMin)))
            isContained = true;

        return isContained;
    }

    void DoCheck() {
        if(isActive) {
            if(!_CheckContain()) {
                if(deactivateDelay > 0.0f) {
                    if(!IsInvoking(InActiveDelayInvoke))
                        Invoke(InActiveDelayInvoke, deactivateDelay);
                }
                else {
                    DoInActive(true);
                }
            }
            else
                CancelInvoke(InActiveDelayInvoke);
        }
        else {
            if(_CheckContain())
                DoActive();
        }
    }

    void OnEnable() {
        if(mStarted && !mCheckActive) {
            mCheckActive = true;
            InvokeRepeating("DoCheck", checkDelay, checkDelay);
        }
    }

    void OnDisable() {
        mCheckActive = false;
        CancelInvoke("DoCheck");
    }

    public override void Start() {
        base.Start();

        mStarted = true;
        OnEnable();
    }

    void OnDrawGizmosSelected() {
        if(extents.width > 0 && extents.height > 0) {
            Gizmos.color = Color.cyan;
            Vector3 c = targetRef ? targetRef.position : transform.position;
            c.x += extents.center.x - extents.width * 0.5f;
            c.y += extents.center.y - extents.height * 0.5f;
            Gizmos.DrawWireCube(c, new Vector3(extents.width, extents.height, 1.0f));
        }
    }
}
