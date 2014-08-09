using UnityEditor;
using UnityEngine;
using System.Collections;

[CustomEditor(typeof(CameraField))]
public class CameraFieldEdit : Editor {
    void OnSceneGUI() {
        if(Event.current.shift) {
            CameraField dat = target as CameraField;

            Handles.color = dat.boundColor;

            Vector3 pos = dat.transform.position + dat.bounds.center;
            Vector3 min = dat.bounds.min;
            Vector3 max = dat.bounds.max;

            min.x = Handles.ScaleSlider(min.x,
                            new Vector3(pos.x-dat.bounds.extents.x, pos.y, pos.z),
                            Vector3.left,
                            Quaternion.identity,
                            HandleUtility.GetHandleSize(pos),
                            0);

            max.x = Handles.ScaleSlider(max.x,
                            new Vector3(pos.x+dat.bounds.extents.x, pos.y, pos.z),
                            Vector3.right,
                            Quaternion.identity,
                            HandleUtility.GetHandleSize(pos),
                            0);

            min.y = Handles.ScaleSlider(min.y,
                            new Vector3(pos.x, pos.y-dat.bounds.extents.y, pos.z),
                            Vector3.down,
                            Quaternion.identity,
                            HandleUtility.GetHandleSize(pos),
                            0);

            max.y = Handles.ScaleSlider(max.y,
                            new Vector3(pos.x, pos.y+dat.bounds.extents.y, pos.z),
                            Vector3.up,
                            Quaternion.identity,
                            HandleUtility.GetHandleSize(pos),
                            0);

            dat.bounds.size = new Vector3(Mathf.Abs(max.x-min.x), Mathf.Abs(max.y-min.y), Mathf.Abs(max.z-min.z));
            dat.bounds.center = Vector3.Lerp(min, max, 0.5f);
        }

        //dat.bounds.Expand
    }
}
