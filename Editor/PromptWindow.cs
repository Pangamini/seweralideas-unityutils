#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Editor
{
    public class PromptWindow : EditorWindow
    {
        private       object?               _value;
        private       string?               _promptText;
        private       bool                  _accepted   = false;
        private       Func<object, object>  _onFieldGui = null;
        private       Func<object, bool>?   _validator;
        private       bool                  _focusNextFrame  = true;
        private       Action<bool, object>? _onComplete;
        private const string                FieldControlName = "ModalInputField";

        public static bool ShowModal<T>(
            string title,
            string? promptText,
            T? defaultValue,
            Func<T, T> fieldGui, out T value,
            Func<T, bool>? validator = null,
            Vector2 size = default
            )
        {
            var window = Create(title, promptText, defaultValue, fieldGui, validator, size);
            window.ShowModalUtility();

            value = (T)window._value;
            return window._accepted;
        }

        public static Task<(bool accepted, T value)> ShowAsync<T>(
            string title,
            string? promptText,
            T? defaultValue,
            Func<T, T> fieldGui,
            Func<T, bool>? validator = null,
            Vector2 size = default
            )
        {
            var tcs = new TaskCompletionSource<(bool, T)>();
            var window = Create(title, promptText, defaultValue, fieldGui, validator, size);
            window._onComplete = (accepted, val) => tcs.TrySetResult((accepted, (T)val));
            window.ShowUtility();
            return tcs.Task;
        }

        private static PromptWindow Create<T>(
            string title,
            string promptText,
            T defaultValue,
            Func<T, T> fieldGui,
            Func<T, bool> validator,
            Vector2 size
            )
        {
            if (size == Vector2.zero)
                size = new Vector2(250, 80);

            var window = CreateInstance<PromptWindow>();
            window._promptText = promptText;
            window._onFieldGui = objVal => fieldGui((T)objVal);
            window._validator = validator == null ? null : objVal => validator((T)objVal);
            window.titleContent = new GUIContent(title);
            window._value = defaultValue;
            window.position = new Rect(Screen.width / 2f, Screen.height / 2f, size.x, size.y);
            return window;
        }

        protected void OnDestroy()
        {
            // Complete with cancellation if closed via the X button
            _onComplete?.Invoke(false, _value);
            _onComplete = null;
        }

        protected void OnGUI()
        {
            // Handle keys for confirm / cancel
            var e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                switch (e.keyCode)
                {
                    case KeyCode.Return or KeyCode.KeypadEnter:
                        if (TryAccept())
                            GUIUtility.ExitGUI();
                        break;
                    case KeyCode.Escape:
                        _accepted = false;
                        Close();
                        GUIUtility.ExitGUI();
                        break;
                }
            }

            if (!string.IsNullOrEmpty(_promptText))
                EditorGUILayout.LabelField(_promptText);

            GUI.SetNextControlName(FieldControlName);
            _value = _onFieldGui(_value);

            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();

            GUI.enabled = _validator == null || _validator(_value);
            if (GUILayout.Button("OK"))
            {
                TryAccept();
            }
            GUI.enabled = true;
            if (GUILayout.Button("Cancel"))
            {
                _accepted = false;
                _onComplete?.Invoke(false, _value);
                _onComplete = null;
                Close();
            }
            GUILayout.EndHorizontal();

            if (_focusNextFrame && (Event.current.type == EventType.Layout || Event.current.type == EventType.Repaint))
            {
                GUI.FocusControl(FieldControlName);
                _focusNextFrame = false; // only do this once
            }
        }

        private bool TryAccept()
        {
            if (_validator != null && !_validator(_value))
                return false;

            _accepted = true;
            _onComplete?.Invoke(true, _value);
            _onComplete = null;
            Close();
            return true;
        }
    }
}
