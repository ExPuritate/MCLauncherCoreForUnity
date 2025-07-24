using System.IO;
using UnityEngine;

public class MCSkinBinder : MonoBehaviour
{
    public MinecraftLauncher launcherScript;
    public GameObject head, body, leftArm, rightArm, leftLeg, rightLeg;

    void Start()
    {
        RefreshSkin();
    }

    public void RefreshSkin()
    {
        string username = launcherScript.usernameInput.text;
        string skinPath = Path.Combine(Application.dataPath, "../Xenos/.minecraft/versions/1.12.2-Forge_14.23.5.2847-OptiFine_G5/CustomSkinLoader/LocalSkin/skins", $"{username}.png");

        if (File.Exists(skinPath))
        {
            Texture2D skin = LoadSkinTexture(skinPath);
            ApplySkinToModel(skin);
        }
    }

    Texture2D LoadSkinTexture(string path)
    {
        byte[] imageData = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
        tex.LoadImage(imageData);
        return tex;
    }

    void ApplySkinToModel(Texture2D skin)
    {
        bool isOldFormat = skin.height == 32;
        int unit = skin.width / 64; // scale factor

        ApplyCubeTexture(head, skin, new Vector2Int(8 * unit, 8 * unit));        // Head: front (8,8)
        ApplyCubeTexture(body, skin, new Vector2Int(20 * unit, 20 * unit));      // Body: front (20,20)
        ApplyCubeTexture(rightArm, skin, new Vector2Int(44 * unit, 20 * unit));  // Right arm: front (44,20)
        ApplyCubeTexture(leftArm, skin, isOldFormat ? new Vector2Int(44 * unit, 20 * unit) : new Vector2Int(36 * unit, 52 * unit)); // Left arm: front
        ApplyCubeTexture(rightLeg, skin, new Vector2Int(4 * unit, 20 * unit));   // Right leg: front (4,20)
        ApplyCubeTexture(leftLeg, skin, isOldFormat ? new Vector2Int(4 * unit, 20 * unit) : new Vector2Int(20 * unit, 52 * unit));  // Left leg: front
    }

    void ApplyCubeTexture(GameObject target, Texture2D skin, Vector2Int frontStart)
    {
        int size = skin.width / 64 * 4; // face size (4x4, scaled)
        Texture2D front = ExtractFace(skin, frontStart.x, frontStart.y, size, size);
        Texture2D back = ExtractFace(skin, frontStart.x + size * 1, frontStart.y, size, size);
        Texture2D top = ExtractFace(skin, frontStart.x, frontStart.y + size * 1, size, size);
        Texture2D bottom = ExtractFace(skin, frontStart.x + size * 1, frontStart.y + size * 1, size, size);
        Texture2D left = ExtractFace(skin, frontStart.x - size * 1, frontStart.y, size, size);
        Texture2D right = ExtractFace(skin, frontStart.x + size * 2, frontStart.y, size, size);

        Texture2D combined = new Texture2D(size * 4, size * 3);
        combined.SetPixels(0, size, size, size, left.GetPixels());
        combined.SetPixels(size, size, size, size, front.GetPixels());
        combined.SetPixels(size * 2, size, size, size, right.GetPixels());
        combined.SetPixels(size * 3, size, size, size, back.GetPixels());
        combined.SetPixels(size, size * 2, size, size, top.GetPixels());
        combined.SetPixels(size, 0, size, size, bottom.GetPixels());
        combined.Apply();

        Material mat = new Material(Shader.Find("Standard"));
        mat.mainTexture = combined;
        target.GetComponent<Renderer>().material = mat;
    }

    Texture2D ExtractFace(Texture2D source, int x, int y, int w, int h)
    {
        Texture2D part = new Texture2D(w, h);
        Color[] pixels = source.GetPixels(x, source.height - y - h, w, h);
        part.SetPixels(pixels);
        part.Apply();
        return part;
    }
}
