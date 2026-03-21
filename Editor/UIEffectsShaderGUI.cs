using UnityEngine;
using UnityEditor;

public class UIEffectsShaderGUI : ShaderGUI
{
    private bool _showRendering = false;
    private bool _showStencil = false;

    public override void OnGUI(MaterialEditor editor, MaterialProperty[] props)
    {
        var mainTex        = FindProperty("_MainTex", props);
        var color          = FindProperty("_Color", props);
        var useSSAA        = FindProperty("_UseSSAA", props);
        var bias           = FindProperty("_Bias", props);
        var useSaturation  = FindProperty("_UseSaturation", props);
        var saturation     = FindProperty("_Saturation", props);
        var useDither      = FindProperty("_UseDither", props);
        var ditherBitDepth = FindProperty("_DitherBitDepth", props);
        var ditherSRGB     = FindProperty("_DitherSRGB", props);
        var srcBlend       = FindProperty("_SrcBlend", props);
        var dstBlend       = FindProperty("_DstBlend", props);
        var colorMask      = FindProperty("_ColorMask", props);
        var alphaClip      = FindProperty("_UseUIAlphaClip", props);
        var stencil        = FindProperty("_Stencil", props);
        var stencilComp    = FindProperty("_StencilComp", props);
        var stencilOp      = FindProperty("_StencilOp", props);
        var stencilRead    = FindProperty("_StencilReadMask", props);
        var stencilWrite   = FindProperty("_StencilWriteMask", props);

        // Base
        editor.ShaderProperty(color, "Tint");
        EditorGUILayout.Space();

        // Supersampling
        DrawFeatureToggle(editor, useSSAA, "Supersampling", "UI_SSAA", () =>
        {
            editor.ShaderProperty(bias, "Bias");
        });

        // Saturation
        DrawFeatureToggle(editor, useSaturation, "Saturation", "UI_SATURATION", () =>
        {
            editor.ShaderProperty(saturation, "Amount");
        });

        // Dithering
        DrawFeatureToggle(editor, useDither, "Dithering", "UI_DITHER", () =>
        {
            editor.ShaderProperty(ditherBitDepth, "Bit Depth");
            BoolShaderProperty(ditherSRGB, "sRGB Target");
            EditorGUILayout.HelpBox(
                "Frame index for temporal dithering: Shader.SetGlobalFloat(\"_DitherFrameIndex\", Time.frameCount)",
                MessageType.None);
        });

        EditorGUILayout.Space();

        // Rendering
        _showRendering = EditorGUILayout.Foldout(_showRendering, "Rendering", true);
        if (_showRendering)
        {
            EditorGUI.indentLevel++;
            editor.ShaderProperty(srcBlend, "Source Blend");
            editor.ShaderProperty(dstBlend, "Destination Blend");
            ColorMaskProperty(colorMask, "Color Mask");
            editor.ShaderProperty(alphaClip, "Use Alpha Clip");
            EditorGUI.indentLevel--;
        }

        // Stencil
        _showStencil = EditorGUILayout.Foldout(_showStencil, "Stencil", true);
        if (_showStencil)
        {
            EditorGUI.indentLevel++;
            editor.ShaderProperty(stencil, "ID");
            editor.ShaderProperty(stencilComp, "Comparison");
            editor.ShaderProperty(stencilOp, "Operation");
            editor.ShaderProperty(stencilRead, "Read Mask");
            editor.ShaderProperty(stencilWrite, "Write Mask");
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        editor.RenderQueueField();
    }

    private static void DrawFeatureToggle(
        MaterialEditor editor,
        MaterialProperty toggleProp,
        string label,
        string keyword,
        System.Action drawBody)
    {
        EditorGUI.BeginChangeCheck();
        bool enabled = EditorGUILayout.Toggle(label, toggleProp.floatValue > 0.5f);
        if (EditorGUI.EndChangeCheck())
        {
            toggleProp.floatValue = enabled ? 1f : 0f;
            foreach (Object target in editor.targets)
            {
                var mat = (Material)target;
                if (enabled) mat.EnableKeyword(keyword);
                else         mat.DisableKeyword(keyword);
            }
        }

        if (toggleProp.floatValue > 0.5f)
        {
            EditorGUI.indentLevel++;
            drawBody();
            EditorGUI.indentLevel--;
        }
    }

    private static void ColorMaskProperty(MaterialProperty prop, string label)
    {
        int mask = (int)prop.floatValue;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(label);

        bool r = (mask & 8) != 0;
        bool g = (mask & 4) != 0;
        bool b = (mask & 2) != 0;
        bool a = (mask & 1) != 0;

        EditorGUI.BeginChangeCheck();
        r = GUILayout.Toggle(r, "R", "buttonLeft");
        g = GUILayout.Toggle(g, "G", "buttonMid");
        b = GUILayout.Toggle(b, "B", "buttonMid");
        a = GUILayout.Toggle(a, "A", "buttonRight");
        if (EditorGUI.EndChangeCheck())
        {
            int newMask = 0;
            if (r) newMask |= 8;
            if (g) newMask |= 4;
            if (b) newMask |= 2;
            if (a) newMask |= 1;
            prop.floatValue = newMask;
        }

        EditorGUILayout.EndHorizontal();
    }

    private static void BoolShaderProperty(MaterialProperty prop, string label)
    {
        EditorGUI.BeginChangeCheck();
        bool val = EditorGUILayout.Toggle(label, prop.floatValue > 0.5f);
        if (EditorGUI.EndChangeCheck())
            prop.floatValue = val ? 1f : 0f;
    }
}
