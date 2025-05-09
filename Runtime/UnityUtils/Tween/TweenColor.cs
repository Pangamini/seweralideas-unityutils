using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    public class TweenColor : TweenValue<Color>
    {
        [SerializeField] 
        private string m_materialProperty = "_Color";
        
        [SerializeField]
        private Renderer[] m_renderers;
        
        private int m_materialPropertyId;

        protected void OnValidate() => UpdateProperty();
        private void UpdateProperty() => m_materialPropertyId = Shader.PropertyToID(m_materialProperty);

        protected override void Start()
        {
            UpdateProperty();
            base.Start();
        }

        protected override Color Interpolate(float t) => Color.Lerp(OffValue, OnValue, t);
        protected override void OnValueInterpolated(float value, Color newValue)
        {
            using (MaterialPropertyPool.Get(out var block))
            {
                foreach (var rend in m_renderers)
                {
                    rend.GetPropertyBlock(block);
                    block.SetColor(m_materialPropertyId, newValue);
                    rend.SetPropertyBlock(block);
                }
            }
        }

    }
}
