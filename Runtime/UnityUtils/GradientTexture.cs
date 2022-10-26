using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    [CreateAssetMenu]
    public class GradientTexture : ScriptableObject
    {
        [SerializeField] public Gradient gradient;
        [SerializeField] public int size = 256;
        [SerializeField] public bool vertical = false;
        [SerializeField] public TextureFormat format = TextureFormat.ARGB32;
        [SerializeField] public TextureWrapMode wrapMode = TextureWrapMode.Repeat;
        [SerializeField, HideInInspector] private Texture2D m_texture;

#if UNITY_EDITOR
    void OnValidate()
    {
        RefreshTexture();

        var assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
        if (!string.IsNullOrEmpty(assetPath))
        {
            var texturePath = UnityEditor.AssetDatabase.GetAssetPath(m_texture);
            if (texturePath != assetPath)
            {
                UnityEditor.AssetDatabase.AddObjectToAsset(m_texture, assetPath);
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
            }
        }
    }
#endif

        public void RefreshTexture()
        {
            int width;
            int height;
            if (vertical)
            {
                width = 1;
                height = size;
            }
            else
            {
                width = size;
                height = 1;
            }

            if (m_texture == null)
                m_texture = new Texture2D(width, height, format, true);
            else
                m_texture.Reinitialize(width, height, format, true);
            m_texture.name = name;
            m_texture.wrapMode = wrapMode;

            var colors = new Color[size];

            for (int i = 0; i < size; ++i)
            {
                float f = (float)i / size;
                var color = gradient.Evaluate(f);
                colors[i] = color;
            }
            m_texture.SetPixels(colors);
            m_texture.Apply();
        }
    }
}