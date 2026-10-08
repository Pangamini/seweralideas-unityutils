using System;
using UnityEngine;
namespace SeweralIdeas.UnityUtils
{
    public static class SeweralGUILayout
    {
        public static string DelayedTextField(string currentValue)
        {
            var rect = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.textField);
            return SeweralGUI.DelayedTextField(rect, currentValue);
        }
    }
    
    public static class SeweralGUI
    {
        public static string DelayedTextField(Rect position, string currentValue)
        {
            return DelayedText(position, currentValue, GUI.TextField);
        }
        
        public static string DelayedTextArea(Rect position, string currentValue)
        {
            return DelayedText(position, currentValue, GUI.TextArea);
        }
        
        private static string DelayedText(Rect position, string currentValue, Func<Rect, string, string> guiFunc)
        {
            int controlID = GUIUtility.GetControlID(FocusType.Keyboard, position);
            GUI.SetNextControlName(controlID.ToString());

            // Retrieve the intermediate value from the GUI state
            TextFieldState state = (TextFieldState)GUIUtility.GetStateObject(typeof( TextFieldState ), controlID);

            if(Event.current.type == EventType.KeyDown && Event.current.keyCode is KeyCode.Escape)
            {
                state.HasFocus = false;
                ReleaseFocus();
                Event.current.Use();
            }
            
            // Initialize the state if necessary
            if(!state.HasFocus)
            {
                state.Text = currentValue;
            }
            else if(state.Text == null)
            {
                state.Text = currentValue;
            }

            bool enterPressed = (Event.current.type == EventType.KeyDown && Event.current.keyCode is KeyCode.Return or KeyCode.KeypadEnter && (Event.current.modifiers & EventModifiers.Shift) == 0);
            bool lostFocus = state.HasFocus && GUI.GetNameOfFocusedControl() != controlID.ToString();

            state.Text = guiFunc(position, state.Text);

            if(enterPressed || lostFocus)
            {
                currentValue = state.Text;
                if(enterPressed)
                {
                    ReleaseFocus();
                }
            }

            bool hasFocus = GUI.GetNameOfFocusedControl() == controlID.ToString();

            // Nothing repaints a window on a key press by itself: without this the committed value, and the focus that
            // is gone, show only when the mouse moves.
            if(hasFocus != state.HasFocus)
                RequestRepaint();
            state.HasFocus = hasFocus;

            return currentValue;
        }

        // Takes the keyboard focus from the field now (FocusControl alone takes effect on the next event), and has the
        // window show it.
        private static void ReleaseFocus()
        {
            GUI.FocusControl(null);
            GUIUtility.keyboardControl = 0;
            RequestRepaint();
        }

        private static void RequestRepaint()
        {
            #if UNITY_EDITOR
            UnityEditor.HandleUtility.Repaint();
            #endif
        }

        private class TextFieldState
        {
            public string Text;
            public bool HasFocus;
        }
    }
}
