using UnityEngine;
using UnityEditor;

namespace SeweralIdeas.UnityUtils.Editor
{
    [CustomEditor(typeof(Detector<,>), true)]
    public class DetectorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var trigger = (IDetector)target;
            foreach (object targ in trigger.GetObjectsInside())
            {
                if(targ is UnityEngine.Object unityObject)
                {
                    EditorGUILayout.ObjectField(GUIContent.none, unityObject, typeof( Object ), true);
                }
                else
                {
                    EditorGUILayout.LabelField(targ.ToString());
                }
            }
                
        }

        public override bool RequiresConstantRepaint() => true;
    }
}
