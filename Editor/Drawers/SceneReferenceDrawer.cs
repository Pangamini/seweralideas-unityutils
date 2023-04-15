using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Drawers.Editor
{
    [CustomPropertyDrawer(typeof(SceneReference))]
    public class SceneReferenceDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            const int buttonWidth = 64;
            var firstLine = new Rect(position.x, position.y, position.width - buttonWidth, EditorGUIUtility.singleLineHeight);
            var remainingRect = new Rect(firstLine.xMax, position.y, position.xMax - firstLine.xMax, position.height);
            
            var sceneAssetProperty = property.FindPropertyRelative("m_sceneAsset");
            var scenePathProperty = property.FindPropertyRelative("m_scenePath");

            var sceneAssetProperties = Multiple(scenePathProperty);
            //var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            
            EditorGUI.PropertyField(firstLine, sceneAssetProperty, label);

            bool inBuild = true;
            EditorBuildSettingsScene[] oldScenes = EditorBuildSettings.scenes;
            
            foreach (var prop in sceneAssetProperties)
            {
                bool thisInBuild = false;
                foreach(var buildScene in oldScenes)
                {
                    if (buildScene.path != prop.stringValue)
                        continue;

                    if (buildScene.enabled)
                    {
                        thisInBuild = true;
                        break;
                    }
                }

                if (!thisInBuild)
                {
                    inBuild = false;
                    break;
                }
            }

            //GUI.enabled = sceneAsset != null;
            var inBuildNew = GUI.Toggle(remainingRect, inBuild, "In Build", EditorStyles.miniButton);
            //GUI.enabled = true;
            
            if (inBuildNew != inBuild)
            {
                var newScenes = new List<EditorBuildSettingsScene>(oldScenes);
                
                foreach (var scenePathProp in sceneAssetProperties)
                {
                    if (inBuildNew)
                    {
                        if (IndexOf(newScenes,scenePathProp.stringValue, out var index))
                        {
                            var scene = newScenes[index];
                            scene.enabled = true;
                            newScenes[index] = scene;
                        }
                        else
                        {
                            newScenes.Add(new(scenePathProp.stringValue, true));
                        }
                    }
                    else
                    {
                        if (IndexOf(newScenes, scenePathProp.stringValue, out var index))
                        {
                            var scene = newScenes[index];
                            scene.enabled = false;
                            newScenes[index] = scene;
                        }
                    }
                }

                EditorBuildSettings.scenes = newScenes.ToArray();
            }
        }

        private static bool IndexOf(IList<EditorBuildSettingsScene> scenes, string path, out int index)
        {
            for(int i = 0; i< scenes.Count; ++i)
            {
                var buildScene = scenes[i];
                
                if (buildScene.path != path)
                    continue;

                index = i;
                return true;
            }

            index = -1;
            return false;
        }
        
        public static SerializedProperty[] Multiple(SerializedProperty property)
        {
            if (property.hasMultipleDifferentValues)
            {
                Object[] targetObjects = property.serializedObject.targetObjects;
                var output = new SerializedProperty[targetObjects.Length];
                for (int i = 0; i < output.Length; ++i)
                {
                    output[i] = new SerializedObject(targetObjects[i]).FindProperty(property.propertyPath);
                }

                return output;
            }
            else
            {
                return new[] { property };
            }
        }
    }
}
