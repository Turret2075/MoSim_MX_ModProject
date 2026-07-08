using System.IO;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class TexturePacker : MonoBehaviour
    {
        [MenuItem("Tools/Pack Metallic Map")]
        static void PackMetallicMap()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Packed Texture", "PackedMetallic", "png", "Save packed texture");

            if (string.IsNullOrEmpty(path)) return;

            Texture2D metallic = null; // usually solid black
            Texture2D ao = Selection.objects[0] as Texture2D;
            Texture2D roughness = Selection.objects[1] as Texture2D;

            int size = ao.width;
            Texture2D result = new Texture2D(size, size, TextureFormat.RGBA32, false);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float metallicVal = 0f; // always black for non-metal
                    float aoVal = ao.GetPixel(x, y).r;
                    float bVal = 0f;
                    float roughnessVal = roughness.GetPixel(x, y).r;
                    float smoothnessVal = 1f - roughnessVal;

                    result.SetPixel(x, y, new Color(metallicVal, aoVal, bVal, smoothnessVal));
                }
            }

            result.Apply();

            byte[] bytes = result.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();

            Debug.Log("Packed texture saved to: " + path);
        }
    }
}
