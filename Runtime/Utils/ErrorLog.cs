#nullable enable
using System.Collections.Generic;
using System.Text;
namespace SeweralIdeas.Utils
{
    public struct ErrorLog
    {
        private const string Indent = "  ";

        private static void AppendIndent(StringBuilder sb, int level)
        {
            for( int i = 0; i < level; ++i )
                sb.Append(Indent);
        }
        
        public void Clear()
        {
            m_string = string.Empty;
            m_errors?.Clear();
        }
        
        private List<Error>? m_errors;
        private string?      m_string;
        
        public bool IsEmpty => m_errors == null || m_errors.Count == 0;

        public void AddError(string error)
        {
            AddError(new Error(error));
        }

        public void AddError(string error, ErrorLog sceneLog)
        {
            if(sceneLog.IsEmpty)
                return;
            AddError(new Error(error, sceneLog));
        }

        private void AddError(Error error)
        {
            if(m_errors == null)
                m_errors = new();

            m_errors.Add(error);
            m_string = null;
        }

        public struct Error
        {
            public readonly string Text;
            public readonly ErrorLog ChildLog;

            public Error(string text)
            {
                Text = text;
                ChildLog = default;
            }
            public Error(string text, ErrorLog childLog)
            {
                Text = text;
                ChildLog = childLog;
            }
        }

        public override string ToString()
        {
            if(m_string == null)
            {
                var sb = new StringBuilder();
                ToString(sb, 0);
                m_string = sb.ToString();
            }
            
            return m_string;
        }

        public void ToString(StringBuilder sb, int indentLevel)
        {
            if(m_errors == null)
            {
                AppendIndent(sb, indentLevel);
                sb.AppendLine("No errors");
            }

            else
            {
                for( int i = 0; i < m_errors.Count; i++ )
                {
                    Error error = m_errors[i];
                    AppendIndent(sb, indentLevel);
                    sb.AppendLine($"[{i}]: {error.Text}");
                    if(!error.ChildLog.IsEmpty)
                    {
                        error.ChildLog.ToString(sb, indentLevel + 1);
                    }
                }
            }
        }

    }
}