using UnityEngine;

public class AmbientProbe {
    public Vector3 Position {
        get;
        private set;
    }

    public float Size {
        get;
        private set;
    }

    public int Index {
        get;
        private set;
    }

    private Color[] colors;
    private RenderTexture renderTexture;
    private Texture2D texture;
    private Camera camera;

    public AmbientProbe(Vector3 position, float size, int index, Camera camera) {
        this.Position = position;
        this.Size = size;
        this.Index = index;
        this.camera = camera;

        this.renderTexture = new RenderTexture(1, 1, 24, RenderTextureFormat.Default);
        this.texture = new Texture2D(1, 1, TextureFormat.RGB24, false);
        this.colors = new Color[6];
    }

    public void Capture() {
        this.SetColor(0);
    }

    public void Draw() {
        Gizmos.color = Color.black;
        var size = new Vector3(this.Size, this.Size, this.Size);
        Gizmos.DrawWireCube(this.Position, size);

        Gizmos.color = this.colors[0];
        Gizmos.DrawSphere(this.Position, this.Size * 0.5f);
    }

    public Color GetColor(int index) {
        return this.colors[index];
    }

    private void SetColor(int index) {
        this.camera.targetTexture = this.renderTexture;
        this.camera.Render();
        this.camera.targetTexture = null;
        
        RenderTexture.active = this.renderTexture;
        this.texture.ReadPixels(new Rect(0, 0, 1, 1), 0, 0, false);
        this.colors[index] = this.texture.GetPixel(0, 0);
        RenderTexture.active = null;
    }
}