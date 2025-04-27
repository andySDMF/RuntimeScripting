using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using WebP;
using WebP.Experiment.Animation;

namespace BrandLab360
{
    public static class CoreUtilities
    {
        public enum RenderPipelineType { Standard, URP, HDRP }
        public static RenderPipelineType RenderPipeline;
        public static string ShaderName;

        //function to read transparent pixels on texture and return bounds

        /// <summary>
        /// Function called to find file extension - replaces System.IO.Path.GetExtension as this wont work on WebGL
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string GetExtension(string source)
        {
            int n = source.Length - 1;
            string extension = "";

            for (int i = n; i > 0; i--)
            {
                if (source[i].Equals('.'))
                {
                    extension += source[i];
                    break;
                }
                else
                {
                    extension += source[i];
                }
            }

            char[] output = extension.ToCharArray();
            System.Array.Reverse(output);

            return new string(output);
        }

        public static string GetFilename(string source)
        {
            int n = source.Length - 1;
            string file = "";
            bool hasStarted = false;

            for (int i = n; i > 1; i--)
            {
                if (source[i].Equals('.'))
                {
                    hasStarted = true;
                }
                else if(source[i].Equals('/'))
                {
                    break;
                }
                else if(hasStarted)
                {
                    file += source[i];
                }
            }

            char[] output = file.ToCharArray();
            System.Array.Reverse(output);

            return new string(output);
        }

        public static List<T> GetInterfaces<T>(this GameObject gObj)
        {
            if (!typeof(T).IsInterface) throw new SystemException("Specified type is not an interface!");
            var mObjs = gObj.GetComponentsInChildren<MonoBehaviour>();

            return (from a in mObjs where a.GetType().GetInterfaces().Any(k => k == typeof(T)) select (T)(object)a).ToList();
        }

        public static bool IsRectTransformCulled(RectTransform elem)
        {
            Vector3[] v = new Vector3[4];
            elem.GetWorldCorners(v);

            float maxY = Mathf.Max(v[0].y, v[1].y, v[2].y, v[3].y);
            float minY = Mathf.Min(v[0].y, v[1].y, v[2].y, v[3].y);

            float maxX = Mathf.Max(v[0].x, v[1].x, v[2].x, v[3].x);
            float minX = Mathf.Min(v[0].x, v[1].x, v[2].x, v[3].x);

            if (maxY < 0 || minY > Screen.height || maxX < 0 || minX > Screen.width)
            {
                return false;
            }

            return true;
        }


        public static Bounds CreateNewBounds(BoxCollider[] colliders)
        {
            //create new bounds based on the collders sum
            Bounds temp = new Bounds();
            Vector3 center = new Vector3();
            float top = 0;
            float bottom = 0;
            float left = 0;
            float right = 0;
            float front = 0;
            float back = 0;

            foreach (BoxCollider col in colliders)
            {
                center += col.center;
            }

            foreach (BoxCollider col in colliders)
            {
                if (center.y + col.bounds.extents.y >= top)
                {
                    top = center.y + col.bounds.extents.y;
                }

                if (center.y - col.bounds.extents.y <= bottom)
                {
                    bottom = center.y - col.bounds.extents.y;
                }

                if (center.x + col.bounds.extents.x >= right)
                {
                    right = center.x + col.bounds.extents.x;
                }

                if (center.x - col.bounds.extents.x <= left)
                {
                    left = center.x - col.bounds.extents.x;
                }

                if (center.z + col.bounds.extents.z >= front)
                {
                    front = center.z + col.bounds.extents.z;
                }

                if (center.z - col.bounds.extents.z <= back)
                {
                    back = center.z - col.bounds.extents.z;
                }
            }

            temp.center = center;
            temp.extents = new Vector3(left, top, front);

            return temp;
        }

        public static async Task<OpaqueBounds> GetOpaqueBoundsOfTexture(Texture tex, TranparencyPixelSampling pixelSampling = TranparencyPixelSampling.x4, float scale = 1)
        {
            OpaqueBounds oBounds = new OpaqueBounds();

            if (tex != null)
            {
                Texture2D rTexture = CreateReadableTexture((Texture2D)tex);

                if(rTexture != null)
                {
                    Vector4[] transparentBounds = GetTransparencyBounds(pixelSampling, rTexture);

                    float opaqueWidth = transparentBounds[0].z - transparentBounds[0].y;
                    float opaqueHeight = transparentBounds[1].z - transparentBounds[1].y;

                    oBounds.extents.x = (opaqueWidth / 2) * scale;
                    oBounds.extents.y = (opaqueHeight / 2) * scale;

                    float offsetx = transparentBounds[0].y + (opaqueWidth / 2);
                    float offsety = transparentBounds[1].y + (opaqueHeight / 2);
                    float scaledOffsetX = offsetx / tex.width;
                    float scaledOffsetY = offsety / tex.height;

                    if (scaledOffsetX > 0.5f)
                    {
                        oBounds.center.x = scaledOffsetX - 0.5f;
                    }
                    else if (scaledOffsetX < 0.5f)
                    {
                        oBounds.center.x = (0.5f - scaledOffsetX) * -1.0f;
                    }

                    if (scaledOffsetY > 0.5f)
                    {
                        oBounds.center.y = scaledOffsetY - 0.5f;
                    }
                    else if (scaledOffsetY < 0.5f)
                    {
                        oBounds.center.y = (0.5f - scaledOffsetY) * -1.0f;
                    }
                }
            }

            await Task.Delay(10);

            return oBounds;
        }

        public static IEnumerator AttainTexture(string url, Material mat)
        {
            if (GetExtension(url).Equals(".webp"))
            {
                //Load pictures in static webp format
                LoadWebpTexture2D(url, mat);

                while (mat.mainTexture == null)
                {
                    yield return null;
                }
            }
            else
            {
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                UnityWebRequest request = UnityWebRequestTexture.GetTexture(url, true);

                var operation = request.SendWebRequest();

                while (!operation.isDone)
                {
                    yield return null;
                }

                if (request.result != UnityWebRequest.Result.ConnectionError || !string.IsNullOrEmpty(request.error))
                {
                    tex = DownloadHandlerTexture.GetContent(request);
                }

                //dispose the request as not needed anymore
                request.Dispose();

                mat.mainTexture = tex;
            }

            //Enable alpha clipping and doublesided rendering on the material
            if (RenderPipeline == RenderPipelineType.HDRP)
            {
                mat.SetFloat("_AlphaCutoffEnable", 1.0f);
                mat.SetFloat("_DoubleSidedEnable", 1.0f);
            }
            else if (RenderPipeline == RenderPipelineType.URP)
            {
                mat.SetFloat("_AlphaClip", 1.0f);
                mat.SetFloat("_Cull", 0);
            }
        }

        private static async void LoadWebpTexture2D(string path, Material mat)
        {
            byte[] bytes = await LoadAsyncBytes(path);
            Error lError;

            mat.mainTexture = Texture2DExt.CreateTexture2DFromWebP(bytes, lMipmaps: true, lLinear: false, lError: out lError);

            if (lError != Error.Success)
            {
                Debug.LogError("Webp Load Error : " + lError.ToString());
                return;
            }
        }

        private static async Task<byte[]> LoadAsyncBytes(string url)
        {
            var getRequest = UnityWebRequest.Get(url);
            await getRequest.SendWebRequest();
            return getRequest.downloadHandler.data; 
        }


        /// <summary>
        /// Get the default shader name based on renderpipeline
        /// </summary>
        public static void GetShaderName()
        {
#if !UNITY_2019
            if (GraphicsSettings.currentRenderPipeline)
            {
                if (GraphicsSettings.currentRenderPipeline.GetType().ToString().Contains("HighDefinition"))
                {
                    RenderPipeline = RenderPipelineType.HDRP;
                    ShaderName = "HDRP/Unlit";
                }
                else
                {
                    RenderPipeline = RenderPipelineType.URP;
                    ShaderName = "Universal Render Pipeline/Unlit";
                }
            }
            else
            {
                RenderPipeline = RenderPipelineType.Standard;
                ShaderName = "Unlit/Transparent Cutout";
            }
#else
        RenderPipeline = RenderPipelineType.Standard;
        ShaderName = "Unlit/Transparent Cutout";
#endif
        }

        private static Texture2D CreateReadableTexture(Texture2D source)
        {
            if(source == null)
            {
                return null;
            }

            RenderTexture renderTex = RenderTexture.GetTemporary(
                        source.width,
                        source.height,
                        0,
                        RenderTextureFormat.Default,
                        RenderTextureReadWrite.Linear);

            Graphics.Blit(source, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            Texture2D readableText = new Texture2D(source.width, source.height);
            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);

            return readableText;
        }

        private static Vector4[] GetTransparencyBounds(TranparencyPixelSampling pixelSampling, Texture2D tex)
        {
            Vector4 xArea = new Vector4();
            Vector4 yArea = new Vector4();

            OpaqueArea left = new OpaqueArea();
            OpaqueArea right = new OpaqueArea();
            right.End = tex.width;

            OpaqueArea bottom = new OpaqueArea();
            OpaqueArea top = new OpaqueArea();
            top.End = tex.height;

            int pixelCount = int.Parse(pixelSampling.ToString().Replace("x", ""));

            for (int x = 0; x < tex.width;)
            {
                for (int y = 0; y < tex.height;)
                {
                    if (tex.GetPixel(x, y).a != 0)
                    {
                        if (!left.foundEnd)
                        {
                            left.foundEnd = true;
                            left.End = x;
                        }

                        if (right.foundStart)
                        {
                            if (x > right.start)
                            {
                                right.foundStart = false;
                            }
                        }

                        if (!bottom.foundEnd)
                        {
                            bottom.foundEnd = true;
                            bottom.End = y;
                        }

                        if (top.foundStart)
                        {
                            if (y > top.start)
                            {
                                top.foundStart = false;
                            }
                        }
                    }
                    else
                    {
                        if (!left.foundStart)
                        {
                            left.foundStart = true;
                            left.start = x;
                        }
                        else
                        {
                            if (!right.foundStart)
                            {
                                right.foundStart = true;
                                right.start = x;
                            }
                        }

                        if (!bottom.foundStart)
                        {
                            bottom.foundStart = true;
                            bottom.start = y;
                        }
                        else
                        {
                            if (bottom.foundEnd)
                            {
                                if (y < bottom.End)
                                {
                                    bottom.End = y;
                                }

                                if (!top.foundStart)
                                {
                                    top.foundStart = true;
                                    top.start = y;
                                }
                            }
                        }
                    }

                    y += pixelCount;
                }

                x += pixelCount;
            }

            xArea.x = left.start;
            xArea.y = left.End;
            xArea.z = right.start;
            xArea.w = right.End;

            yArea.x = bottom.start;
            yArea.y = bottom.End;
            yArea.z = top.start;
            yArea.w = top.End;

            Vector4[] temp = new Vector4[2];
            temp[0] = xArea;
            temp[1] = yArea;

            return temp;
        }

        public enum TranparencyPixelSampling { x4, x8, x16, x32 }

        public class OpaqueBounds
        {
            public Vector3 center = Vector3.zero;
            public Vector3 extents = new Vector3(1, 1, 0.05f);
        }

        private class OpaqueArea
        {
            public bool foundStart = false;
            public float start;

            public bool foundEnd = false;
            public float End;
        }

#if UNITY_EDITOR
        public static UnityEngine.Object GetAsset<T>(string path)
        {
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(path, typeof(T));

            if (obj == null)
            {
                obj = GetPackageAsset<T>(path);
            }

            return obj;
        }

        private static UnityEngine.Object GetPackageAsset<T>(string path)
        {
            return AssetDatabase.LoadAssetAtPath(path.Replace("Assets", "Packages"), typeof(T));
        }
#endif
    }
}
