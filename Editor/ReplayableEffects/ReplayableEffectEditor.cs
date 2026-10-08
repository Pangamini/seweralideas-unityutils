using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SeweralIdeas.ReplayableEffects;

namespace SeweralIdeas.ReplayableEffects.Editor
{
    [CustomEditor(typeof(ReplayableEffect))]
    [CanEditMultipleObjects]
    public class ReplayableEffectEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Play"))
            {
                foreach (Object targ in targets)
                {
                    var effect = (ReplayableEffect)targ;
                    effect.Play();
                }
            }
            base.OnInspectorGUI();
        }
    }
}