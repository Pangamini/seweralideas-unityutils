using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    [CreateAssetMenu(menuName="SeweralIdeas/"+nameof(GradientTexture))]
    public class GradientTexture : ScriptableObject
    {
        public const int MaxSize = 4096;

        /// <summary> Uncompressed formats that Texture2D.SetPixels(Color[]) can write. Compressed formats are not supported. </summary>
        public static readonly TextureFormat[] SupportedFormats =
        {
            TextureFormat.ARGB32,
            TextureFormat.RGBA32,
            TextureFormat.BGRA32,
            TextureFormat.RGB24,
            TextureFormat.RGBA4444,
            TextureFormat.ARGB4444,
            TextureFormat.RGB565,
            TextureFormat.Alpha8,
            TextureFormat.R8,
            TextureFormat.RG16,
            TextureFormat.R16,
            TextureFormat.RHalf,
            TextureFormat.RGHalf,
            TextureFormat.RGBAHalf,
            TextureFormat.RFloat,
            TextureFormat.RGFloat,
            TextureFormat.RGBAFloat,
        };

        public enum CombineMode
        {
            Multiply,
            Add,
            Average,
            Min,
            Max,
            Screen,
            Overlay,
            Subtract,
            Difference,
            /// <summary> Second gradient is drawn over the first, using its alpha. </summary>
            Over,
        }

        [SerializeField] public Gradient gradient;
        [SerializeField, Min(1)] public int size = 256;
        [Tooltip("Switch axes: the first gradient runs along Y instead of X (and the second along X, in 2D).")]
        [SerializeField] public bool vertical = false;
        [Tooltip("Add a second axis: 'gradient' is the first axis, 'gradient2' the second.")]
        [SerializeField] public bool is2D = false;
        [SerializeField] public Gradient gradient2;
        [SerializeField, Min(1)] public int size2 = 256;
        [SerializeField] public CombineMode combineMode = CombineMode.Multiply;
        [SerializeField] public TextureFormat format = TextureFormat.ARGB32;
        [Tooltip("Store the gradient values raw instead of as sRGB colors. Formats without sRGB support (float, half, ...) always behave as sRGB-equivalent unless this is on.")]
        [SerializeField] public bool linear = false;
        [SerializeField] public bool mipmaps = false;
        [SerializeField] public FilterMode filterMode = FilterMode.Bilinear;
        [SerializeField] public TextureWrapMode wrapMode = TextureWrapMode.Clamp;
        [SerializeField] public TextureWrapMode wrapMode2 = TextureWrapMode.Clamp;
        [SerializeField, HideInInspector] private Texture2D m_texture;

        [System.NonSerialized] private int m_hash;

        public Texture2D Texture => m_texture;

#if UNITY_EDITOR
        private bool m_attachQueued;

        protected void OnValidate()
        {
            size = Mathf.Clamp(size, 1, MaxSize);
            size2 = Mathf.Clamp(size2, 1, MaxSize);
            if (System.Array.IndexOf(SupportedFormats, format) < 0)
                format = TextureFormat.ARGB32;
            RefreshTexture(force: false);

            // On creation (CreateAssetMenu) OnValidate runs before the asset exists on disk, so the
            // texture can't be attached yet. Defer until the asset has a path, then attach.
            if (m_attachQueued)
                return;
            m_attachQueued = true;
            UnityEditor.EditorApplication.delayCall += AttachTexture;
        }

        private void AttachTexture()
        {
            m_attachQueued = false;
            if (this == null)
                return;

            var assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrEmpty(assetPath))
                return;

            if (m_texture == null)
                RefreshTexture();

            if (UnityEditor.AssetDatabase.GetAssetPath(m_texture) == assetPath)
                return;

            UnityEditor.AssetDatabase.AddObjectToAsset(m_texture, this);
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
        }
#endif

        /// <summary> Regenerates the texture. Unless forced, pixels are only rebuilt if an input changed. </summary>
        public void RefreshTexture(bool force = true)
        {
            EnsureGradients();

            int sizeX = Mathf.Clamp(size, 1, MaxSize);
            int sizeY = Mathf.Clamp(size2, 1, MaxSize);

            // size is the first axis' resolution, size2 the second's. 'vertical' puts the first axis on Y.
            int width;
            int height;
            if (is2D)
            {
                width = vertical ? sizeY : sizeX;
                height = vertical ? sizeX : sizeY;
            }
            else if (vertical)
            {
                width = 1;
                height = sizeX;
            }
            else
            {
                width = sizeX;
                height = 1;
            }

            // wrapMode belongs to the first axis (X, or Y when switched); the unused axis of a 1D texture is clamped.
            var wrapFirst = wrapMode;
            var wrapSecond = is2D ? wrapMode2 : TextureWrapMode.Clamp;
            var wrapX = vertical ? wrapSecond : wrapFirst;
            var wrapY = vertical ? wrapFirst : wrapSecond;

            int hash = ComputeHash(width, height);
            if (force || m_texture == null || hash != m_hash)
            {
                m_hash = hash;
                Rebuild(width, height, wrapX, wrapY);
            }

            m_texture.name = name;
            m_texture.wrapModeU = wrapX;
            m_texture.wrapModeV = wrapY;
            m_texture.filterMode = filterMode;
        }

        private void Rebuild(int width, int height, TextureWrapMode wrapX, TextureWrapMode wrapY)
        {
            // Float/half and other non-sRGB formats ignore the sRGB flag and always sample as raw values.
            bool srgb = !linear && UnityEngine.Experimental.Rendering.GraphicsFormatUtility.IsSRGBFormat(
                UnityEngine.Experimental.Rendering.GraphicsFormatUtility.GetGraphicsFormat(format, true));

            if (m_texture == null)
            {
                m_texture = new Texture2D(width, height, format, mipmaps, !srgb);
            }
            else if (m_texture.isDataSRGB != srgb)
            {
                // Color space can't be changed by Reinitialize, so build a fresh texture.
                var fresh = new Texture2D(width, height, format, mipmaps, !srgb);
#if UNITY_EDITOR
                // Copy over the existing object so references to the sub-asset stay valid.
                UnityEditor.EditorUtility.SetDirty(m_texture);
                UnityEditor.EditorUtility.CopySerialized(fresh, m_texture);
                DestroyImmediate(fresh);
#else
                Destroy(m_texture);
                m_texture = fresh;
#endif
            }
            else
            {
                m_texture.Reinitialize(width, height, format, mipmaps);
            }

            var colors = new Color[width * height];

            if (is2D)
            {
                // Precompute both axes, then combine: first = 'gradient', second = 'gradient2'.
                var colorsX = new Color[width];
                var colorsY = new Color[height];
                var gradientX = vertical ? gradient2 : gradient;
                var gradientY = vertical ? gradient : gradient2;
                for (int x = 0; x < width; ++x)
                    colorsX[x] = gradientX.Evaluate(PixelT(x, width, wrapX));
                for (int y = 0; y < height; ++y)
                    colorsY[y] = gradientY.Evaluate(PixelT(y, height, wrapY));

                for (int y = 0; y < height; ++y)
                for (int x = 0; x < width; ++x)
                {
                    colors[y * width + x] = vertical
                        ? Combine(colorsY[y], colorsX[x], combineMode)
                        : Combine(colorsX[x], colorsY[y], combineMode);
                }
            }
            else
            {
                int count = width * height;
                for (int i = 0; i < count; ++i)
                    colors[i] = gradient.Evaluate(PixelT(i, count, wrapMode));
            }

            // Gradient colors are gamma values. An sRGB texture gets decoded to linear on sampling, so formats
            // that can't be sRGB must store the decoded values to look the same (linear color space only).
            if (!linear && !srgb && QualitySettings.activeColorSpace == ColorSpace.Linear)
            {
                for (int i = 0; i < colors.Length; ++i)
                    colors[i] = colors[i].linear;
            }

            m_texture.SetPixels(colors);
            m_texture.Apply();
        }

        /// <summary>
        /// Gradient position of pixel i. Repeating textures sample [0,1) so tiling has no doubled edge pixel,
        /// everything else samples [0,1] so both end colors are hit exactly.
        /// </summary>
        private static float PixelT(int i, int count, TextureWrapMode wrap)
        {
            if (count <= 1)
                return 0f;
            return wrap == TextureWrapMode.Repeat ? (float)i / count : (float)i / (count - 1);
        }

        /// <summary>
        /// Samples the texture analytically (without the pixel grid). u is horizontal, v vertical, like texture UVs.
        /// Respects the wrap modes. v is ignored for 1D textures.
        /// </summary>
        public Color Evaluate(float u, float v = 0f)
        {
            EnsureGradients();

            float t1 = ApplyWrap(vertical ? v : u, wrapMode);
            var first = gradient.Evaluate(t1);
            if (!is2D)
                return first;

            float t2 = ApplyWrap(vertical ? u : v, wrapMode2);
            return Combine(first, gradient2.Evaluate(t2), combineMode);
        }

        private static float ApplyWrap(float t, TextureWrapMode wrap)
        {
            switch (wrap)
            {
                case TextureWrapMode.Repeat: return t - Mathf.Floor(t);
                case TextureWrapMode.Mirror: return Mathf.PingPong(t, 1f);
                case TextureWrapMode.MirrorOnce: return Mathf.Clamp01(Mathf.Abs(t));
                default: return Mathf.Clamp01(t);
            }
        }

        private void EnsureGradients()
        {
            if (gradient == null)
                gradient = new Gradient();
            if (gradient2 == null)
                gradient2 = new Gradient();
        }

        private int ComputeHash(int width, int height)
        {
            var hash = new System.HashCode();
            hash.Add(width);
            hash.Add(height);
            hash.Add(is2D);
            hash.Add(vertical);
            hash.Add(combineMode);
            hash.Add(format);
            hash.Add(linear);
            hash.Add(QualitySettings.activeColorSpace);
            hash.Add(mipmaps);
            hash.Add(wrapMode);
            hash.Add(wrapMode2);
            AddGradient(ref hash, gradient);
            if (is2D)
                AddGradient(ref hash, gradient2);
            return hash.ToHashCode();
        }

        private static void AddGradient(ref System.HashCode hash, Gradient g)
        {
            hash.Add(g.mode);
            hash.Add(g.colorSpace);
            foreach (var key in g.colorKeys)
            {
                hash.Add(key.color);
                hash.Add(key.time);
            }
            foreach (var key in g.alphaKeys)
            {
                hash.Add(key.alpha);
                hash.Add(key.time);
            }
        }

        public static Color Combine(Color a, Color b, CombineMode mode)
        {
            switch (mode)
            {
                case CombineMode.Add: return a + b;
                case CombineMode.Average: return (a + b) * 0.5f;
                case CombineMode.Min: return new Color(Mathf.Min(a.r, b.r), Mathf.Min(a.g, b.g), Mathf.Min(a.b, b.b), Mathf.Min(a.a, b.a));
                case CombineMode.Max: return new Color(Mathf.Max(a.r, b.r), Mathf.Max(a.g, b.g), Mathf.Max(a.b, b.b), Mathf.Max(a.a, b.a));
                case CombineMode.Screen: return Color.white - (Color.white - a) * (Color.white - b);
                case CombineMode.Overlay: return new Color(Overlay(a.r, b.r), Overlay(a.g, b.g), Overlay(a.b, b.b), a.a * b.a);
                case CombineMode.Subtract: return new Color(a.r - b.r, a.g - b.g, a.b - b.b, a.a * b.a);
                case CombineMode.Difference: return new Color(Mathf.Abs(a.r - b.r), Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b), a.a * b.a);
                case CombineMode.Over:
                {
                    float outA = b.a + a.a * (1f - b.a);
                    if (outA <= 0f)
                        return Color.clear;
                    var rgb = (b * b.a + a * a.a * (1f - b.a)) / outA;
                    return new Color(rgb.r, rgb.g, rgb.b, outA);
                }
                default: return a * b;
            }
        }

        private static float Overlay(float a, float b) => a < 0.5f ? 2f * a * b : 1f - 2f * (1f - a) * (1f - b);
    }
}
