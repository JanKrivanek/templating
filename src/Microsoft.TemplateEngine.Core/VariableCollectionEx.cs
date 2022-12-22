// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Parameters;
using Microsoft.TemplateEngine.Core.Contracts;

namespace Microsoft.TemplateEngine.Core
{
    public class VariableCollectionEx : VariableCollection, IVariableCollectionEx
    {
        public VariableCollectionEx()
            : this(Abstractions.Parameters.ParameterSetData.Empty)
        { }

        public VariableCollectionEx(IParameterSetData parameterSetData)
            : base()
        {
            ParameterSetData = parameterSetData;
        }

        public VariableCollectionEx(
            IVariableCollection? parent,
            IDictionary<string, object> values,
            IParameterSetData parameterSetData)
            : base(parent, values)
            => ParameterSetData = parameterSetData;

        public IParameterSetData ParameterSetData { get; private set; }

        public static VariableCollectionEx Root(IParameterSetData parameterSetData) =>
            new(null, new Dictionary<string, object>(), parameterSetData);

        public static IVariableCollectionEx SetupVariables(IParameterSetData parameters, IVariableConfig variableConfig)
        {
            IVariableCollectionEx variables = Root(parameters);

            Dictionary<string, VariableCollectionEx> collections = new Dictionary<string, VariableCollectionEx>();

            foreach (KeyValuePair<string, string> source in variableConfig.Sources)
            {
                VariableCollectionEx? variablesForSource = null;
                string format = source.Value;

                switch (source.Key)
                {
                    //may be extended for other categories in future if needed.
                    case "user":
                        variablesForSource = VariableCollectionFromParameters(parameters, format);

                        if (variableConfig.FallbackFormat != null)
                        {
                            VariableCollectionEx variablesFallback = VariableCollectionFromParameters(parameters, variableConfig.FallbackFormat);
                            variablesFallback.Parent = variablesForSource;
                            variablesForSource = variablesFallback;
                        }
                        break;
                }
                if (variablesForSource != null)
                {
                    collections[source.Key] = variablesForSource;
                }
            }

            foreach (string order in variableConfig.Order)
            {
                IVariableCollectionEx current = collections[order.ToString()];

                IVariableCollection tmp = current;
                while (tmp.Parent != null)
                {
                    tmp = tmp.Parent;
                }

                tmp.Parent = variables;
                variables = current;
            }

            return variables;
        }

        private static VariableCollectionEx VariableCollectionFromParameters(IParameterSetData parameters, string format)
        {
            VariableCollectionEx vc = new VariableCollectionEx(parameters);
            foreach (ITemplateParameter param in parameters.ParametersDefinition)
            {
                string key = string.Format(format ?? "{0}", param.Name);

                if (parameters.TryGetValue(param, out ParameterData value) &&
                    value.IsEnabled && value.Value != null)
                {
                    vc[key] = value.Value;
                }
            }

            return vc;
        }
    }
}
