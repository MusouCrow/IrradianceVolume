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

    public Vector3Int size;
    public float interval;
    public ProbeData[] datas;
    public Texture3D[] textures;

    private Vector3 position;

    protected void Start() {
        this.AdjustPosition();
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

        var camera = this.transform.Find("Camera").GetComponent<Camera>();
        camera.gameObject.SetActive(true);
        camera.nearClipPlane = 0.001f;
        camera.farClipPlane = 100;
        camera.fieldOfView = 179;
        camera.backgroundColor = Color.white;
        camera.clearFlags = CameraClearFlags.SolidColor;

        for (int i = 0; i < this.datas.Length; i++) {
            this.CaptureProbe(this.datas[i], camera);
        }

        camera.gameObject.SetActive(false);

        this.textures = new Texture3D[6];

        for (int i = 0; i < this.textures.Length; i++) {
            var texture = new Texture3D(this.size.x * 2 + 1, this.size.y * 2 + 1, this.size.z * 2 + 1, DefaultFormat.HDR, 0);
            texture.wrapMode = TextureWrapMode.Clamp;

            foreach (var data in this.datas) {
                var pos = data.position;
                var color = data.colors[i];
                texture.SetPixel(pos.x, pos.y, pos.z, color);
            }
            
            texture.Apply();
            this.textures[i] = texture;
        }

        this.SetValue();
    }

    private Vector3 GetProbePosition(ProbeData data) {
        var position = this.position;

        for (int i = 0; i < 3; i++) {
            position[i] += (this.interval * data.position[i]) + (this.interval * 0.5f);
        }

        return position;
    }

    public Vector3Int GetPositionIndex(Vector3 position) {
        position -= this.position;
        position /= this.interval;
        
        return new Vector3Int((int)position.x, (int)position.y, (int)position.z);
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

        this.AdjustPosition();
    }

    private void CaptureProbe(ProbeData data, Camera camera) {
        var position = this.GetProbePosition(data);
        camera.transform.position = position;

        var cubemap = new Cubemap(SIZE, DefaultFormat.HDR, TextureCreationFlags.None);
        camera.RenderToCubemap(cubemap);
        cubemap.Apply();

        for (int i = 0; i < data.colors.Length; i++) {
            data.colors[i] = this.MixColor(cubemap, (CubemapFace)i);
        }
    }

    private Color MixColor(Cubemap cubemap, CubemapFace face) {
        var color = new Color();
        int max = cubemap.width * cubemap.height;

        for (int i = 0; i < cubemap.width; i++) {
            for (int j = 0; j < cubemap.height; j++) {
                var c = cubemap.GetPixel(face, i, j);
                color += c;
            }
        }

        return color / max;
    }

    private void SetValue() {
        if (this.datas == null) {
            return;
        }
        
        for (int i = 0; i < this.textures.Length; i++) {
            Shader.SetGlobalTexture("_VolumeTex" + i, this.textures[i]);
        }
        
        Shader.SetGlobalVector("_VolumeSize", (Vector3)this.size);
        Shader.SetGlobalVector("_VolumePosition", this.position);
        Shader.SetGlobalFloat("_VolumeInterval", this.interval);
    }

    private void AdjustPosition() {
        var interval = new Vector3(this.interval, this.interval, this.interval) * 0.5f;
        this.position = this.transform.position - ((Vector3)this.size * this.interval) - interval;
    }
}