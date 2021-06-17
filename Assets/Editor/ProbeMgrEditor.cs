using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ProbeMgr))]
public class ProbeMgrEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        if (GUILayout.Button("Bake")) {
            var mgr = this.target as ProbeMgr;
            mgr.Bake();
        }
    }
}