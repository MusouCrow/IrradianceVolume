using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class ProbeMgr : MonoBehaviour {
    public Vector3Int size;
    public float interval;

    private List<AmbientProbe> probes;
    private Vector3Int _size;
    private float _interval;
    private Vector3 _position;

    protected void OnRenderObject() {
        this.FlushProbes();
    }

    protected void OnDrawGizmosSelected() {
        if (this.probes == null) {
            return;
        }

        Gizmos.color = Color.black;

        foreach (var probe in this.probes) {
            probe.Draw();
        }
    }

    private void FlushProbes() {
        bool tick = this.size != this._size || this.interval != this._interval || this.transform.position != this._position;

        if (!tick) {
            return;
        }
        
        this._size = this.size;
        this._interval = this.interval;
        this._position = this.transform.position;

        this.probes = new List<AmbientProbe>();

        var camera = this.GetComponentInChildren<Camera>();
        var pos = this.transform.position;
        int n = 0;
        
        for (int i = -this.size.x; i <= this.size.x; i++) {
            for (int j = -this.size.y; j <= this.size.y; j++) {
                for (int k = -this.size.z; k <= this.size.z; k++) {
                    var p = pos;
                    p.x += this.interval * i;
                    p.y += this.interval * j;
                    p.z += this.interval * k;
                    n++;

                    var probe = new AmbientProbe(p, this.interval, n, camera);
                    this.probes.Add(probe);
                }
            }
        }

        this.probes[0].Capture();
    }
}