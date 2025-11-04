#nullable  enable
using System;
using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Editor
{
    public abstract class ModalProgressWindow<TProgress> : EditorWindow
    {
        protected abstract bool MoveNext(out TProgress progress);
        protected abstract void OnCancel();
        protected abstract bool DrawGUI(TProgress progress);

        private bool _canceled = false;
        
        private void OnGUI()
        {
            if(!MoveNext(out TProgress progress))
                Close();

            if(!DrawGUI(progress))
                Cancel();
            
            Repaint();
        }
        
        private void Cancel()
        {
            if(_canceled)
                return;
            
            _canceled = true;
            OnCancel();
            Close();
        }

        private void OnDestroy()
        {
            if(_canceled)
                return;
            _canceled = true;
            OnCancel();
        }

        protected static TWindow Init<TWindow>(string title, Vector2 size, Action<TWindow>? init = null ) where TWindow : ModalProgressWindow<TProgress>
        {
            Rect mainWindowRect = EditorGUIUtility.GetMainWindowPosition();

            // center window
            var position = new Rect(
                mainWindowRect.x + (mainWindowRect.width - size.x) * 0.5f,
                mainWindowRect.y + (mainWindowRect.height - size.y) * 0.5f,
                size.x,
                size.y
            );
            
            var window = CreateWindow<TWindow>(title);
            window.maximized = false;
            window.maxSize = size;
            window.minSize = size;
            window.position = position;
            init?.Invoke(window);
            window.ShowModalUtility();
            return window;
        }
    }

    public class ProgressDemo : ModalProgressWindow<(float, string)>
    {
        private DateTime _startTime;
        
        [MenuItem("Framework/"+nameof(TestProgressWindow))]
        public static void TestProgressWindow()
        {
            Init<ProgressDemo>(nameof(TestProgressWindow), new Vector2(300,50), w => w._startTime = DateTime.Now);
        }
        
        protected override bool MoveNext(out (float, string) progress)
        {
            const float duration = 5f;
            TimeSpan elapsed = DateTime.Now - _startTime;
            float relativeValue = (float)(elapsed.TotalSeconds / duration);
            string progressText = relativeValue.ToString("P0");
            progress = (relativeValue, progressText);
            return relativeValue < 1;
        }
        
        protected override bool DrawGUI((float, string) progress)
        {
            Rect progressBarRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.textField); 
            EditorGUI.ProgressBar(progressBarRect, progress.Item1, progress.Item2);
            if(GUILayout.Button("Cancel"))
                return false;

            return true;
        }
        
        protected override void OnCancel()
        {
        }
    }
}
