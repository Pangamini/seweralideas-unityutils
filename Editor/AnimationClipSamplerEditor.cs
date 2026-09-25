using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    [CustomEditor(typeof(AnimationClipSampler))]
    public class AnimationClipSamplerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var sampler = (AnimationClipSampler)target;
            var clip = sampler.Clip;

            using (new EditorGUI.DisabledScope(clip == null))
            {
                EditorGUI.BeginChangeCheck();
                float normalized = EditorGUILayout.Slider("Normalized Time", sampler.NormalizedTime, 0f, 1f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(sampler, "Sample Time");
                    sampler.NormalizedTime = normalized;
                    EditorUtility.SetDirty(sampler);

                    // The runtime PlayableGraph only exists in Play mode (the
                    // component isn't ExecuteAlways) - so while editing, drive
                    // the preview directly through AnimationMode instead.
                    if (!Application.isPlaying && clip != null)
                        PreviewInEditMode(sampler, clip);
                }
            }
        }

        private void OnDisable()
        {
            if (!Application.isPlaying && AnimationMode.InAnimationMode())
                AnimationMode.StopAnimationMode();
        }

        private static void PreviewInEditMode(AnimationClipSampler sampler, AnimationClip clip)
        {
            if (!AnimationMode.InAnimationMode())
                AnimationMode.StartAnimationMode();

            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(sampler.gameObject, clip, sampler.Time);
            AnimationMode.EndSampling();

            SceneView.RepaintAll();
        }
    }
}
