using System;
using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Editor
{

    public class ProgressBar : IDisposable
    {
        public class CancelException : Exception
        { }

        private string m_title;
        private string m_info;
        private float m_progress;
        private float m_progressTolerance;

        public ProgressBar(string title, float progressTolerance = 0.02f)
        {
            m_progressTolerance = progressTolerance;
            m_title = title;
        }

        public void Update(string info, float progress)
        {
            if (m_info == info && Mathf.Abs(progress - m_progress) < m_progressTolerance)
                return;
            m_info = info;
            m_progress = progress;
            Update();
        }

        private void Update()
        {

            if (EditorUtility.DisplayCancelableProgressBar("Generating light probes", m_info, m_progress))
            {
                throw new CancelException();
            }
        }

        public void Dispose()
        {
            EditorUtility.ClearProgressBar();
        }
    }
}