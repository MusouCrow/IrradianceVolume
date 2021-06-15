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

    private Camera camera;
    private Color[] colors;
    private RenderTexture renderTexture;

    public AmbientProbe(Vector3 position, float size, int index, Camera camera) {
        this.Position = position;
        this.Size = size;
        this.Index = index;
        this.camera = camera;
        this.renderTexture = new RenderTexture(1, 1, 0);
        this.colors = new Color[6];
    }

    public void Capture() {
        this.SetColor(0);
    }

    public Color GetColor(int index) {
        return this.colors[index];
    }

    private void SetColor(int index) {
        RenderTexture.active = this.renderTexture;
        this.camera.targetTexture = this.renderTexture;
        this.camera.Render();
        
        var texture = new Texture2D(1, 1);
        texture.ReadPixels(new Rect(0, 0, 1, 1), 0, 0);
        texture.Apply();

        this.colors[index] = texture.GetPixel(0, 0);
    }
}