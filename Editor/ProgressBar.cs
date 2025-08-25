using System;
using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Editor
{

    public class ProgressBar : IDisposable
    {
        public class CancelException : Exception
        { }

        private readonly string _title;
        private          string _info;
        private          float  _progress;
        private readonly float  _progressTolerance;

        public ProgressBar(string title, float progressTolerance = 0.02f)
        {
            _progressTolerance = progressTolerance;
            _title = title;
        }

        public void Update(string info, float progress)
        {
            if (_info == info && Mathf.Abs(progress - _progress) < _progressTolerance)
                return;
            _info = info;
            _progress = progress;
            Update();
        }

        private void Update()
        {
            if (EditorUtility.DisplayCancelableProgressBar(_title, _info, _progress))
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