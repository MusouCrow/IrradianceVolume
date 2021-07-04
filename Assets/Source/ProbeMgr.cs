using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

[Serializable]
public class ProbeData {
    public int index;
    public Vector3Int position;
    public Color[] colors;
}

[ExecuteAlways]
public class ProbeMgr : MonoBehaviour {
    private const int SIZE = 128;

    private static Vector3[] Directions = new Vector3[] {
        new Vector3(-1, 0, 0),
        new Vector3(1, 0, 0),
        new Vector3(0, -1, 0),
        new Vector3(0, 1, 0),
        new Vector3(0, 0, -1),
        new Vector3(0, 0, 1),
    };

    public ComputeShader shader;
    public Vector3Int size;
    public float interval;
    public ProbeData[] datas;
    public Texture3D[] textures;

    private Vector3 position;
    private int progress;

    public bool IsBaking {
        get;
        private set;
    }

    protected void Start() {
        this.AdjustPosition();
        this.SetValue();
    }

#if UNITY_EDITOR
    protected void Update() {
        if (this.IsBaking) {
            EditorUtility.SetDirty(this);
        }
    }
#endif

    protected void OnDrawGizmosSelected() {
        if (this.datas == null) {
            return;
        }

        Gizmos.color = Color.black;
        var size = new Vector3(this.interval, this.interval, this.interval);
        var position = this.transform.position;

        for (int x = -this.size.x; x <= this.size.x; x++) {
            for (int y = -this.size.y; y <= this.size.y; y++) {
                for (int z = -this.size.z; z <= this.size.z; z++) {
                    var pos = new Vector3(x, y, z) * this.interval;
                    Gizmos.DrawWireCube(position + pos, size);
                }
            }
        }

        foreach (var data in this.datas) {
            var pos = this.GetProbePosition(data);
            
            for (int i = 0; i < data.colors.Length; i++) {
                Gizmos.color = data.colors[i];
                Gizmos.DrawSphere(pos + Directions[i] * this.interval * 0.3f, this.interval * 0.1f);
            }
        }
    }

    public async void Bake() {
        print("Baking...");

        this.IsBaking = true;
        Shader.EnableKeyword("_BAKING");
        this.progress = 0;

        this.FlushProbe();
        this.textures = new Texture3D[6];

        for (int i = 0; i < this.textures.Length; i++) {
            var texture = new Texture3D(this.size.x * 2 + 1, this.size.y * 2 + 1, this.size.z * 2 + 1, DefaultFormat.HDR, 0);
            texture.wrapMode = TextureWrapMode.Clamp;
            this.textures[i] = texture;
        }
        
        for (int i = 0; i < this.datas.Length; i++) {
            this.CaptureProbe(this.datas[i]);
        }
        
        while (this.progress < this.datas.Length) {
            EditorUtility.SetDirty(this);
            await Task.Yield();
        }

        foreach (var texture in this.textures) {
            texture.Apply();
        }

        this.SetValue();
        Shader.DisableKeyword("_BAKING");
        this.IsBaking = false;

        print("Bake Finish");
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

    private async void CaptureProbe(ProbeData data) {
        var go = new GameObject("Reflect");
        var reflect = go.AddComponent<ReflectionProbe>();
        
        reflect.nearClipPlane = 0.001f;
        reflect.farClipPlane = 100;
        reflect.hdr = true;
        reflect.backgroundColor = Color.white;
        reflect.clearFlags = ReflectionProbeClearFlags.SolidColor;
        reflect.resolution = 128;

        var position = this.GetProbePosition(data);
        go.transform.SetParent(this.transform);
        go.transform.position = position;

        var rt = RenderTexture.GetTemporary(SIZE, SIZE, 32, RenderTextureFormat.ARGBFloat);
        rt.dimension = TextureDimension.Cube;

        var id = reflect.RenderProbe(rt);

        while (!reflect.IsFinishedRendering(id)) {
            EditorUtility.SetDirty(this);
            await Task.Yield();
        }

        var colorBuffer = new ComputeBuffer(6, sizeof(float) * 4);
        int kernel = this.shader.FindKernel("CSMain");

        this.shader.SetTexture(kernel, "_CubeMap", rt);
        this.shader.SetBuffer(kernel, "_Colors", colorBuffer);
        this.shader.SetFloat("_Size", SIZE);
        this.shader.Dispatch(kernel, 6, 1, 1);

        colorBuffer.GetData(data.colors);

        var pos = data.position;

        for (int i = 0; i < data.colors.Length; i++) {
            var color = data.colors[i];
            this.textures[i].SetPixel(pos.x, pos.y, pos.z, color);
        }

        colorBuffer.Release();
        RenderTexture.ReleaseTemporary(rt);
        DestroyImmediate(go);

        this.progress++;
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