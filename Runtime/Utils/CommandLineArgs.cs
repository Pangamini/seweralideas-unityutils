using System;
using System.Collections.Generic;

namespace SeweralIdeas.Utils
{
    public class CommandLineArgs
    {
        private Dictionary<string, Parameter> m_parameters = new Dictionary<string, Parameter>();
        public bool hasMissingArgs { get; private set; }

        public struct ParameterDefinition
        {
            public string identifier;
            public bool hasArgument;
            public bool mandatory;
        }

        private class Parameter
        {
            public readonly ParameterDefinition definition;
            public string value;

            public Parameter(ParameterDefinition definition)
            {
                this.definition = definition;
                this.value = null;
            }
        }

        public CommandLineArgs(string[] args, params ParameterDefinition[] parameters)
        {
            foreach (var parameter in parameters)
            {
                m_parameters.Add(parameter.identifier, new Parameter(parameter));
            }

            Parse(args);

            hasMissingArgs = false;

            foreach (var pair in m_parameters)
            {
                var parameter = pair.Value;
                if (parameter.definition.mandatory && pair.Value.value == null)
                {
                    Console.Error.WriteLine($"Missing argument: {parameter.definition.identifier}");
                    hasMissingArgs = true;
                }
            }
        }

        public bool TryGetValue(string identifier, out string value)
        {
            if (m_parameters.TryGetValue(identifier, out Parameter parameter) && parameter.value != null)
            {
                value = parameter.value;
                return true;
            }

            value = default;
            return false;
        }

        private void Parse(string[] args)
        {
            for (int i = 1; i < args.Length; ++i)
            {
                var arg = args[i];

                if (m_parameters.TryGetValue(arg, out Parameter parameter))
                {
                    if (parameter.definition.hasArgument)
                        parameter.value = args[++i];
                    else
                        parameter.value = string.Empty;
                }
                else
                {
                    Console.Error.WriteLine($"Unknown argument: {arg}");
                }

            }
        }
    }
}