using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    public interface IErrorCheck
    {
        void CheckForErrors(ref ErrorCheckTool.GameObjectError error);
    }

    public static class ErrorCheckTool
    {
        public struct GameObjectError
        {
            public GameObject gameObject;
            public string error;
            public string warning;
            public bool hasError;
            public bool childHasError;
            public bool hasWarning;
            public bool childHasWarning;

            public void AddError(string newError)
            {
                error += newError;
                hasError = true;
            }

            public void AddWarning(string newWarning)
            {
                warning += newWarning+"\n";
                hasWarning = true;
            }
        }
        
    }
}