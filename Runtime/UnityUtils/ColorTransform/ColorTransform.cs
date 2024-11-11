using UnityEngine;

namespace SeweralIdeas.UnityUtils
{

    [RequireComponent(typeof(Renderer))]
    [ExecuteAlways]
    public class ColorTransform : MonoBehaviour
    {
        private Renderer m_renderer;

        [SerializeField] private float  m_hue            = 0f;
        [SerializeField] private float  m_saturation     = 1f;
        [SerializeField] private float  m_lightness      = 1f;
        [SerializeField] private string m_shaderProperty = "_HSLTransform";

        private                 int       m_propId;
        private static readonly Matrix4x4 s_rgb2Yiq;
        private static readonly Matrix4x4 s_yiq2Rgb;

        public void SetHsl(float hue, float saturation, float lightness)
        {
            m_hue = hue;
            m_saturation = saturation;
            m_lightness = lightness;
            Apply();
        }

        static ColorTransform()
        {
            s_rgb2Yiq = new Matrix4x4(
                new Vector4(0.299f, 0.587f, 0.114f, 0),
                new Vector4(0.5959f, -0.2746f, -0.3213f, 0),
                new Vector4(0.2115f, -0.5227f, 0.3112f, 0),
                new Vector4(0, 0, 0, 1));

            s_yiq2Rgb = s_rgb2Yiq.inverse;
        }

        protected void Awake()
        {
            m_renderer = GetComponent<Renderer>();
            Apply();
        }

        protected void OnValidate()
        {
            m_saturation = Mathf.Max(m_saturation, 0f);
            m_propId = Shader.PropertyToID(m_shaderProperty);
            Apply();
        }

        private void Apply()
        {
            Matrix4x4 matrix = HslMatrix(m_hue, m_saturation, m_lightness);
            Apply(ref matrix);
        }

        public static Matrix4x4 HslMatrix(float hue, float saturation, float lightness)
        {
            float cos = Mathf.Cos(hue);
            float sin = Mathf.Sin(hue);

            Matrix4x4 lightnessMatrix = new(
                new Vector4(lightness, 0, 0, 0),
                new Vector4(0, lightness, 0, 0),
                new Vector4(0, 0, lightness, 0),
                new Vector4(0, 0, 0, 1));

            Matrix4x4 saturationMatrix = new(
                new Vector4(1, 0, 0, 0),
                new Vector4(0, saturation, 0, 0),
                new Vector4(0, 0, saturation, 0),
                new Vector4(0, 0, 0, 1));

            Matrix4x4 hueMatrix = new(
                new Vector4(1, 0, 0, 0),
                new Vector4(0, cos, -sin, 0),
                new Vector4(0, sin, cos, 0),
                new Vector4(0, 0, 0, 1));

            var matrix = s_rgb2Yiq * hueMatrix * saturationMatrix * lightnessMatrix * s_yiq2Rgb;
            return matrix;
        }

        private void Apply(ref Matrix4x4 matrix)
        {
            using (MaterialPropertyPool.Get(out var block))
            {
                m_renderer.GetPropertyBlock(block);
                block.SetMatrix(m_propId, matrix);
                m_renderer.SetPropertyBlock(block);
            }
        }

        protected void OnDidApplyAnimationProperties() => Apply();

    }
}