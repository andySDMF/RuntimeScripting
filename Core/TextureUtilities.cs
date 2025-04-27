using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Networking;

#if !UNITY_WEBGL || UNITY_EDITOR
using System.Threading.Tasks;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public static class TextureUtilities
    {
#if UNITY_EDITOR
        public static bool SaveToRelativePath(Texture2D tex, string path)
        {
            if (tex == null)
            {
                Debug.Log("Failed to save Texture. Texture cannot be null");
                return false;
            }

            bool success = false;

            AssetDatabase.CreateAsset(tex, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));

            if (obj != null)
            {
                success = true;
            }

            return success;
        }
#endif

        public static bool SaveToAbsolutePath(Texture2D tex, string path, TextureExtension ext, bool revealInFinder = false)
        {
            if(tex == null)
            {
                Debug.Log("Failed to save Texture. Texture cannot be null");
                return false;
            }

            bool success = false;

            byte[] bytes = ext.Equals(TextureExtension.PNG) ? tex.EncodeToPNG() : tex.EncodeToJPG();
            File.WriteAllBytes(path, bytes);

            if (File.Exists(path))
            {
                success = true;
                Debug.Log("Saved Texture [" + tex.name + "] to path [" + path + "]");

            }
            else
            {
                Debug.Log("Failed to save Texture [" + tex.name + "] to path [" + path + "]");
            }

            if(success && revealInFinder)
            {
#if UNITY_EDITOR
                EditorUtility.RevealInFinder(path);
#endif
            }

            return success;
        }

#if UNITY_EDITOR
        public static Texture2D LoadFromRelativePath(string path)
        {
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));

            if (obj != null)
            {
                return (Texture2D)obj;
            }

            return null;
        }
#endif

        public static Texture2D LoadFromAbsolutePath(string path)
        {
            if(string.IsNullOrEmpty(path) || !Path.HasExtension(path))
            {
                Debug.Log("Failed to load Texture from path [" + path + "]");
                return null;
            }

            Texture2D tex = Create(File.ReadAllBytes(path), Path.GetFileName(path));

            return tex;
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        public static async Task<Texture2D> AsyncLoadFromAbsolutePath(string path)
        {
            Texture2D temp = null;

            if (!string.IsNullOrEmpty(path))
            {
                using var www = UnityWebRequestTexture.GetTexture(path);
                await www.SendWebRequest();

                while (!www.isDone)
                {
                    await Task.Yield();
                }

                try
                {
                    temp = DownloadHandlerTexture.GetContent(www);
                }
                catch
                {
                    temp = null;
                }
                
            }

            return temp;
        }
#endif

        public static bool Delete(string path)
        {
            bool success = false;

            if(File.Exists(path))
            {
                File.Delete(path);
                success = true;
                Debug.Log("Deleted Texture from path [" + path + "]");
            }
            else
            {
                Debug.Log("Failed to delete Texture from path [" + path + "]");
            }

#if UNITY_EDITOR
            if (success)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
#endif

            return success;
        }

        public static Texture2D Create(byte[] bytes, string name)
        {
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
            tex.name = name;

            if (tex.LoadImage(bytes))
            {
                Debug.Log("Texture [" + name + "] created using byte[]");
            }
            else
            {
                Debug.Log("Failed to create new texure [" + name + "]");
            }

            return tex;
        }

        public static Texture2D Create(RenderTexture rTex, string name)
        {
            if(rTex == null)
            {
                Debug.Log("Failed to create new texure [" + name + "]. Rendered Texture cannot be null");
                return null;
            }

            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
            tex.name = name;
            tex.Reinitialize(rTex.width, rTex.height);

            RenderTexture.active = rTex;

            tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
            tex.Apply();

            RenderTexture.active = null;

            Debug.Log("Texture [" + name + "] created from RenderedTexture");

            return tex;
        }

        public static Texture2D ScaleUp(Texture2D tex, int scale)
        {
            if (tex == null)
            {
                Debug.Log("Failed to save Texture. Texture cannot be null");
                return null;
            }

            Texture2D temp = null;
            TextureScale tScale = new TextureScale();

            float tempWidth = tex.width * scale;
            float tempHeight = tex.height * scale;

            temp = new Texture2D(tex.width, tex.height, tex.format, false);
            temp.name = tex.name;
            temp.Apply();
            Graphics.CopyTexture(tex, temp);

            tScale.Scale(temp, Mathf.FloorToInt(tempWidth), Mathf.FloorToInt(tempHeight));

            return temp;
        }

        public static Texture2D ScaleDown(Texture2D tex, int scale)
        {
            if (tex == null)
            {
                Debug.Log("Failed to save Texture. Texture cannot be null");
                return null;
            }

            Texture2D temp = null;
            TextureScale tScale = new TextureScale();

            float tempWidth = tex.width / scale;
            float tempHeight = tex.height / scale;

            temp = new Texture2D(tex.width, tex.height, tex.format, false);
            temp.name = tex.name;
            temp.Apply();
            Graphics.CopyTexture(tex, temp);

            tScale.Scale(temp, Mathf.FloorToInt(tempWidth), Mathf.FloorToInt(tempHeight));

            return temp;
        }

        private class TextureScale
        {
            public Texture2D Scaled(Texture2D src, int width, int height, FilterMode mode = FilterMode.Trilinear)
            {
                Rect texR = new(0, 0, width, height);
                GPUScale(src, width, height, mode);

                //Get rendered data back to a new texture
                Texture2D result = new(width, height, TextureFormat.ARGB32, true);
                result.Reinitialize(width, height);
                result.ReadPixels(texR, 0, 0, true);
                return result;
            }

            public void Scale(Texture2D tex, int width, int height, FilterMode mode = FilterMode.Bilinear)
            {
                Rect texR = new Rect(0, 0, width, height);
                GPUScale(tex, width, height, mode);

                tex.Reinitialize(width, height);
                tex.ReadPixels(texR, 0, 0, true);
                tex.Apply(true);
            }

            private void GPUScale(Texture2D src, int width, int height, FilterMode fmode)
            {
                src.filterMode = fmode;
                src.Apply(true);

                RenderTexture rtt = new RenderTexture(width, height, 32);

                Graphics.SetRenderTarget(rtt);

                GL.LoadPixelMatrix(0, 1, 1, 0);

                GL.Clear(true, true, new Color(0, 0, 0, 0));
                Graphics.DrawTexture(new Rect(0, 0, 1, 1), src);
            }
        }
    }

    public enum TextureExtension { PNG, JPG }
}
