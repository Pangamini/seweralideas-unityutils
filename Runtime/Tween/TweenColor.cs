using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    public class TweenColor : TweenValue<Color>
    {
        private static MaterialPropertyBlock _propertyBlock;
        
        [SerializeField] 
        private string m_materialProperty = "_Color";
        
        [SerializeField]
        private Renderer[] m_renderers;
        
        private int m_materialPropertyId;

        protected void OnValidate() => UpdateProperty();
        private void UpdateProperty() => m_materialPropertyId = Shader.PropertyToID(m_materialProperty);

        protected override void Start()
        {
            _propertyBlock ??= new MaterialPropertyBlock();
            UpdateProperty();
            base.Start();
        }

        protected override Color Interpolate(float t) => Color.Lerp(OffValue, OnValue, t);
        protected override void OnValueInterpolated(float value, Color newValue)
        {
            foreach (var rend in m_renderers)
            {
                rend.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(m_materialPropertyId, newValue);
                rend.SetPropertyBlock(_propertyBlock);
            }
        }

    }
}
