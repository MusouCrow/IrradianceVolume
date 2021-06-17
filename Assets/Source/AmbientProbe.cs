using UnityEngine;

public class AmbientProbe {
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
        RenderTexture.active = this.renderTexture;
        this.camera.targetTexture = this.renderTexture;

        var transform = this.camera.transform;

        for (int i = 0; i < this.colors.Length; i++) {
            transform.position = this.Position + Directions[i];
            transform.rotation = Quaternion.Euler(Rotation[i]);

            this.camera.Render();
            this.texture.ReadPixels(new Rect(0, 0, 1, 1), 0, 0, false);
            this.colors[i] = this.texture.GetPixel(0, 0);
        }

        RenderTexture.active = null;
    }

    public void Draw() {
        Gizmos.color = Color.black;
        var size = new Vector3(this.Size, this.Size, this.Size);
        Gizmos.DrawWireCube(this.Position, size);

        for (int i = 0; i < this.colors.Length; i++) {
            Gizmos.color = this.colors[i];
            Gizmos.DrawSphere(this.Position + Directions[i] * this.Size * 0.5f, this.Size * 0.1f);
        }
    }

    public Color GetColor(int index) {
        return this.colors[index];
    }
}