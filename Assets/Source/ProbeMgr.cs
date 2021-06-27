using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

[Serializable]
public class ProbeData {
    public int index;
    public Vector3Int position;
    public Vector3[] cofficients;
}

[ExecuteAlways]
public class ProbeMgr : MonoBehaviour {
    public Vector3Int size;
    public float interval;
    public ProbeData[] datas;
    public Texture3D[] textures;
    public ComputeShader shader;

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

        this.textures = new Texture3D[4];

        for (int i = 0; i < this.textures.Length; i++) {
            var texture = new Texture3D(this.size.x * 2 + 1, this.size.y * 2 + 1, this.size.z * 2 + 1, TextureFormat.RGBAFloat, false);
            texture.wrapMode = TextureWrapMode.Clamp;

            foreach (var data in this.datas) {
                var pos = data.position;
                var cf = data.cofficients[i];
                var color = new Color(cf.x, cf.y, cf.z);
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
                        position = new Vector3Int(i, j, k)
                    };
                }
            }
        }

        this.AdjustPosition();
    }

    private void CaptureProbe(ProbeData data, Camera camera) {
        var texture = new RenderTexture(128, 128, 32, RenderTextureFormat.ARGBFloat);
        texture.dimension = TextureDimension.Cube;

        var position = this.GetProbePosition(data);

        camera.transform.position = position;
        camera.RenderToCubemap(texture);

        int kernel = this.shader.FindKernel("CSMain");
        this.shader.SetTexture(kernel, "_CubeMap", texture);

        var buffer = new ComputeBuffer(9216, sizeof(float) * 3);
        this.shader.SetBuffer(kernel, "_Cofficients", buffer);
        this.shader.Dispatch(kernel, 32, 32, 1);

        var cofficients = new Vector3[4096];
        buffer.GetData(cofficients);

        buffer.Release();
        texture.Release();

        data.cofficients = new Vector3[4];

        for (int i = 0; i < data.cofficients.Length; i++) {
            for (int j = 0; j < 1024; j++) {
                data.cofficients[i] += cofficients[4 * j + i];
            }

            data.cofficients[i] *= 1 / 1024.0f * 4 * Mathf.PI;
        }
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