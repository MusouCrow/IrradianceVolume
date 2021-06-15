using UnityEngine;

public class ProbeMgr : MonoBehaviour {
    public Vector3Int size;
    public float interval;

    protected void OnDrawGizmosSelected() {
        var pos = this.transform.position;
        var size = new Vector3(this.interval, this.interval, this.interval);

        Gizmos.color = Color.black;
        
        for (int i = -this.size.x; i <= this.size.x; i++) {
            for (int j = -this.size.y; j <= this.size.y; j++) {
                for (int k = -this.size.z; k <= this.size.z; k++) {
                    var p = pos;
                    p.x += this.interval * i;
                    p.y += this.interval * j;
                    p.z += this.interval * k;
                    Gizmos.DrawWireCube(p, size);
                }
            }
        }
        
        // Gizmos.DrawWireCube()
    }
}