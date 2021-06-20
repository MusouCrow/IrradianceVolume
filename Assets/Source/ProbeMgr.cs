using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[Serializable]
public class ProbeData {
    public int index;
    public Vector3Int position;
    public Color[] colors;
}

[ExecuteAlways]
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
    public Texture3D texture;
    
    private ComputeBuffer buffer;

    protected void Start() {
        this.SetValue();
    }

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

        this.texture = new Texture3D(this.size.x * 2 + 1, this.size.z * 2 + 1, this.size.y * 2 + 1, GraphicsFormat.R16_SFloat, 0);
        
        foreach (var data in this.datas) {
            var pos = data.position;
            var color = new Color(data.index / 255.0f, 0, 0);
            this.texture.SetPixel(pos.x, pos.z, pos.y, color);
        }

        this.texture.filterMode = FilterMode.Point;
        this.texture.Apply();

        this.SetValue();
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
                        position = new Vector3Int(i, j, k),
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

    private void SetValue() {
        Shader.SetGlobalTexture("_IndexVolumeTex", this.texture);
        Shader.SetGlobalVector("_VolumeSize", (Vector3)this.size);
        Shader.SetGlobalVector("_VolumePosition", this.transform.position);
        Shader.SetGlobalFloat("_VolumeInterval", this.interval);

        if (this.buffer != null) {
            this.buffer.Release();
        }

        this.buffer = new ComputeBuffer(this.datas.Length, sizeof(float) * 3 * 6);
        var datas = new Vector3[this.datas.Length * 6];
        int n = -1;

        foreach (var data in this.datas) {
            for (int i = 0; i < 6; i++) {
                n++;
                var c = data.colors[i];
                datas[n] = new Vector3(c.r, c.g, c.b);
            }
        }

        this.buffer.SetData(datas);
        Shader.SetGlobalBuffer("_VolumeColors", this.buffer);
    }
}