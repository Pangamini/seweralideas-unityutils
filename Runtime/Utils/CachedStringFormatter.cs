using System;
using System.Collections.Generic;
using System.Globalization;
using JetBrains.Annotations;
using SeweralIdeas.Pooling;

namespace SeweralIdeas.Utils
{
    public struct CachedStringFormatter<T1>
    {
        private T1 m_value;
        private string m_output;
        private readonly PrecompiledStringFormatter m_format;
        private readonly CultureInfo m_culture;

        [StringFormatMethod("format")]
        public CachedStringFormatter(string format, CultureInfo culture)
        {
            m_format = new PrecompiledStringFormatter(format);
            m_output = null;
            m_value = default;
            m_culture = culture;
            UpdateString();
        }
        public override string ToString() => m_output;

        public string GetString(T1 value1)
        {
            if(EqualityComparer<T1>.Default.Equals(value1, m_value))
            {
                return m_output;
            }
            m_value = value1;
            UpdateString();
            return m_output;
        }
        private void UpdateString() => m_output = m_format.Format(m_value, m_culture);
    }

    public struct CachedStringFormatter<T1, T2>
    {
        private (T1, T2) m_value;
        private string m_output;
        private PrecompiledStringFormatter m_format;
        private readonly CultureInfo m_culture;

        [StringFormatMethod("format")]
        public CachedStringFormatter(string format, CultureInfo culture)
        {
            m_format = new PrecompiledStringFormatter(format);
            m_output = null;
            m_value = default;
            m_culture = culture;
            UpdateString();
        }
        public override string ToString() => m_output;

        public string GetString(T1 value1, T2 value2)
        {
            if(EqualityComparer<(T1, T2)>.Default.Equals((value1, value2), m_value))
            {
                return m_output;
            }
            m_value = (value1, value2);
            UpdateString();

            return m_output;
        }
        private void UpdateString() => m_output = m_format.Format(m_value.Item1, m_value.Item2, m_culture);
    }

    public struct CachedStringFormatter<T1, T2, T3>
    {
        private (T1, T2, T3) m_value;
        private string m_output;
        private PrecompiledStringFormatter m_format;
        private readonly CultureInfo m_culture;

        [StringFormatMethod("format")]
        public CachedStringFormatter(string format, CultureInfo culture)
        {
            m_format = new PrecompiledStringFormatter(format);
            m_output = null;
            m_value = default;
            m_culture = culture;
            UpdateString();
        }
        public override string ToString() => m_output;

        public string GetString(T1 value1, T2 value2, T3 value3)
        {
            if(EqualityComparer<(T1, T2, T3)>.Default.Equals((value1, value2, value3), m_value))
            {
                return m_output;
            }
            m_value = (value1, value2, value3);
            UpdateString();

            return m_output;
        }
        private void UpdateString() => m_output = m_format.Format(m_value.Item1, m_value.Item2, m_value.Item3, m_culture);
    }

    public struct CachedStringFormatter<T1, T2, T3, T4>
    {
        private (T1, T2, T3, T4) m_value;
        private string m_output;
        private PrecompiledStringFormatter m_format;
        private readonly CultureInfo m_culture;

        [StringFormatMethod("format")]
        public CachedStringFormatter(string format, CultureInfo culture)
        {
            m_format = new PrecompiledStringFormatter(format);
            m_output = null;
            m_value = default;
            m_culture = culture;
            UpdateString();
        }
        public override string ToString() => m_output;

        public string GetString(T1 value1, T2 value2, T3 value3, T4 value4)
        {
            if(EqualityComparer<(T1, T2, T3, T4)>.Default.Equals((value1, value2, value3, value4), m_value))
            {
                return m_output;
            }
            m_value = (value1, value2, value3, value4);
            UpdateString();

            return m_output;
        }
        private void UpdateString() => m_output = m_format.Format(m_value.Item1, m_value.Item2, m_value.Item3, m_value.Item4, m_culture);
    }
    
    public class PrecompiledStringFormatter
    {
        private readonly string m_format;
        private readonly List<Placeholder> m_placeholders;
        [ThreadStatic] private static object[] s_args;
        
        private struct Placeholder
        {
            public int Index;
            public int Length;
            public int ArgIndex;
            public string FormatString;
        }

        public PrecompiledStringFormatter(string format)
        {
            m_format = format;
            m_placeholders = new List<Placeholder>();
            PreprocessFormat();
        }

        private void PreprocessFormat()
        {
            for( int i = 0; i < m_format.Length; i++ )
            {
                switch (m_format[i])
                {
                    case '{' when i + 1 < m_format.Length && m_format[i + 1] == '{':
                        // Escaped {
                        i++;
                        break;
                    case '{':
                    {
                        // Start of a format item
                        int end = m_format.IndexOf('}', i);
                        if(end == -1)
                            throw new FormatException("Input string was not in a correct format.");

                        string formatItem = m_format.Substring(i, end - i + 1);
                        int colonIndex = formatItem.IndexOf(':');
                        int argIndex;
                        string formatString = null;

                        if(colonIndex == -1)
                        {
                            // No custom format specified
                            argIndex = int.Parse(formatItem.Substring(1, formatItem.Length - 2));
                        }
                        else
                        {
                            // Custom format specified
                            argIndex = int.Parse(formatItem.Substring(1, colonIndex - 1));
                            formatString = formatItem.Substring(colonIndex + 1, formatItem.Length - colonIndex - 2);
                        }

                        m_placeholders.Add(new Placeholder
                        {
                            Index = i,
                            Length = formatItem.Length,
                            ArgIndex = argIndex,
                            FormatString = formatString
                        });
                        i = end;
                        break;
                    }
                    case '}' when i + 1 < m_format.Length && m_format[i + 1] == '}':
                        // Escaped }
                        i++;
                        break;
                    case '}':
                        throw new FormatException("Input string was not in a correct format.");
                }
            }
        }

        private static void EnsureStaticArgs()
        {
            if(s_args == null)
                s_args = new object[4];
        }
        
        public string Format(object arg1, CultureInfo culture)
        {
            EnsureStaticArgs();
            try
            {
                s_args[0] = arg1;
                return Format(s_args, 1, culture);
            }
            finally
            {
                s_args[0] = default;
            }
        }

        public string Format(object arg1, object arg2, CultureInfo culture)
        {
            EnsureStaticArgs();
            try
            {
                s_args[0] = arg1;
                s_args[1] = arg2;
                return Format(s_args, 2, culture);
            }
            finally
            {
                s_args[0] = default;
                s_args[1] = default;
            }
        }

        public string Format(object arg1, object arg2, object arg3, CultureInfo culture)
        {
            EnsureStaticArgs();
            try
            {
                s_args[0] = arg1;
                s_args[1] = arg2;
                s_args[2] = arg3;
                return Format(s_args, 3, culture);
            }
            finally
            {
                s_args[0] = default;
                s_args[1] = default;
                s_args[2] = default;
            }
        }

        public string Format(object arg1, object arg2, object arg3, object arg4, CultureInfo culture)
        {
            EnsureStaticArgs();
            try
            {
                s_args[0] = arg1;
                s_args[1] = arg2;
                s_args[2] = arg3;
                s_args[3] = arg4;
                return Format(s_args, 4, culture);
            }
            finally
            {
                s_args[0] = default;
                s_args[1] = default;
                s_args[2] = default;
                s_args[3] = default;
            }
        }


        private string Format(object[] args, int argCount, CultureInfo culture)
        {
            argCount = Math.Min(argCount, args.Length);
            using (StringBuilderPool.Get(out var stringBuilder))
            {
                int lastPos = 0;

                foreach (Placeholder placeholder in m_placeholders)
                {
                    // Append the part of the format string before the placeholder
                    stringBuilder.Append(m_format, lastPos, placeholder.Index - lastPos);

                    // Append the appropriate argument with custom format if specified
                    if(placeholder.ArgIndex >= 0 && placeholder.ArgIndex < argCount)
                    {
                        stringBuilder.Append(FormatArgument(args[placeholder.ArgIndex], placeholder.FormatString, culture));
                    }

                    // Update last position
                    lastPos = placeholder.Index + placeholder.Length;
                }

                // Append the rest of the format string
                stringBuilder.Append(m_format, lastPos, m_format.Length - lastPos);

                return stringBuilder.ToString();
            }
        }

        private string FormatArgument(object arg, string formatString, CultureInfo culture)
        {
            if (arg == null)
                return string.Empty;

            if (string.IsNullOrEmpty(formatString))
                return Convert.ToString(arg, culture);

            if (arg is IFormattable formattable)
                return formattable.ToString(formatString, culture);

            return Convert.ToString(arg, culture);
        }
    }
}