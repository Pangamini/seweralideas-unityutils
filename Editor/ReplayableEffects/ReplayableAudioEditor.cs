using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SeweralIdeas.ReplayableEffects;

namespace SeweralIdeas.ReplayableEffects.Editor
{
    [CustomEditor(typeof(ReplayableAudio))]
    [CanEditMultipleObjects]
    public class ReplayableAudioEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var clipCollection = serializedObject.FindProperty("m_clipCollection");
            var loop = serializedObject.FindProperty("m_loop");
            var volumeOverTime = serializedObject.FindProperty("m_volumeOverTime");
            var duration = serializedObject.FindProperty("m_duration");
            var playAsNew = serializedObject.FindProperty("m_playAsNew");

            EditorGUILayout.PropertyField(clipCollection);

            EditorGUILayout.PropertyField(loop);

            if (!loop.hasMultipleDifferentValues)
            {
                if (loop.boolValue)
                {
                    EditorGUILayout.PropertyField(volumeOverTime);
                    EditorGUILayout.PropertyField(duration);
                }
                else
                {
                    EditorGUILayout.PropertyField(playAsNew);
                }
            }
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}