using System;
using UnityEngine;

[Serializable]
public class ProbeData {
    public int index;
    public Vector3 position;
    public Color[] colors;
}

public class ProbeMgr : MonoBehaviour {
    private const int SIZE = 4;

    private static Vector3[] Directions = new Vector3[] {
        new Vector3(-1, 0, 0),
        new Vector3(1, 0, 0),
        new Vector3(0, -1, 0),
        new Vector3(0, 1, 0),
        new Vector3(0, 0, -1),
        new Vector3(0, 0, 1),
    };

    private static Vector3[] Rotation = new Vector3[] {
        new Vector3(0, 90, 0),
        new Vector3(0, -90, 0),
        new Vector3(-90, 0, 0),
        new Vector3(90, 0, 0),
        new Vector3(0, 0, 0),
        new Vector3(0, 180, 0),
    };

    public Vector3Int size;
    public float interval;
    public ProbeData[] datas;

    protected void OnDrawGizmosSelected() {
        if (this.datas == null) {
            return;
        }

        foreach (var data in this.datas) {
            var position = this.GetProbePosition(data);
            
            Gizmos.color = Color.black;
            var size = new Vector3(this.interval, this.interval, this.interval);
            Gizmos.DrawWireCube(position, size);
            
            for (int i = 0; i < data.colors.Length; i++) {
                Gizmos.color = data.colors[i];
                Gizmos.DrawSphere(position + Directions[i] * this.interval * 0.3f, this.interval * 0.1f);
            }
        }
    }

    public void Bake() {
        this.FlushProbe();

        var renderTexture = new RenderTexture(SIZE, SIZE, 24, RenderTextureFormat.Default);
        var texture = new Texture2D(SIZE, SIZE, TextureFormat.RGB24, false);

        var camera = this.transform.Find("Camera").GetComponent<Camera>();
        camera.gameObject.SetActive(true);
        camera.targetTexture = renderTexture;
        camera.farClipPlane = this.interval + 0.01f;

        RenderTexture.active = renderTexture;

        for (int i = 0; i < this.datas.Length; i++) {
            this.CaptureProbe(this.datas[i], camera, texture);
        }
        
        RenderTexture.active = null;
        camera.gameObject.SetActive(false);
    }

    private Vector3 GetProbePosition(ProbeData data) {
        var position = this.transform.position;

        for (int i = 0; i < 3; i++) {
            position[i] += this.interval * (data.position[i] - this.size[i]);
        }

        return position;
    }

    private Vector3 GetPositionIndex(Vector3 position) {
        position -= this.transform.position;
        position /= this.interval;
        position += this.size;

        return position;
    }

    private void FlushProbe() {
        int max = (this.size.x * 2 + 1) * (this.size.y * 2 + 1) * (this.size.z * 2 + 1);
        this.datas = new ProbeData[max];
        int n = -1;

        for (int i = 0; i <= this.size.x * 2; i++) {
            for (int j = 0; j <= this.size.y * 2; j++) {
                for (int k = 0; k <= this.size.z * 2; k++) {
                    n++;
                    this.datas[n] = new ProbeData() {
                        index = n,
                        position = new Vector3(i, j, k),
                        colors = new Color[6]
                    };
                }
            }
        }
    }

    private void CaptureProbe(ProbeData data, Camera camera, Texture2D texture) {
        var transform = camera.transform;
        var position = this.GetProbePosition(data);

        for (int i = 0; i < data.colors.Length; i++) {
            transform.position = position + Directions[i];
            transform.rotation = Quaternion.Euler(Rotation[i]);
            
            camera.Render();
            texture.ReadPixels(new Rect(0, 0, SIZE, SIZE), 0, 0, false);
            data.colors[i] = this.MixColor(texture);
        }
    }

    private Color MixColor(Texture2D texture) {
        var color = new Color();
        int max = texture.width * texture.height;

        for (int i = 0; i < texture.width; i++) {
            for (int j = 0; j < texture.height; j++) {
                var c = texture.GetPixel(i, j);
                color += c;
            }
        }

        return color / max;
    }
}