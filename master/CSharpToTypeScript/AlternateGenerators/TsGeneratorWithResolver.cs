﻿#nullable enable
using System.Collections.Generic;
using System.Collections.Immutable;
using CSharpToTypeScript.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CSharpToTypeScript.AlternateGenerators
{
    public class TsGeneratorWithResolver : TsGenerator
    {
        public TsGeneratorWithResolver(bool enableNamespace)
            : base(enableNamespace)
        {
        }

        protected override void AppendModule(
          TsModule module,
          ScriptBuilder sb,
          TsGeneratorOutput generatorOutput)
        {
            sb.AppendLine("import { Resolver, FieldError } from 'react-hook-form';");
            sb.AppendLine();
            base.AppendModule(module, sb, generatorOutput);
        }

        protected override void AppendClassDefinition(
          TsClass classModel,
          ScriptBuilder sb,
          TsGeneratorOutput generatorOutput)
        {
            base.AppendClassDefinition(classModel, sb, generatorOutput);
            sb.AppendLine();

            List<TsProperty> source = new();
            if ((generatorOutput & TsGeneratorOutput.Properties) == TsGeneratorOutput.Properties)
                source.AddRange(classModel.Properties);
            if ((generatorOutput & TsGeneratorOutput.Fields) == TsGeneratorOutput.Fields)
                source.AddRange(classModel.Fields);
            source.RemoveAll(p => p.JsonIgnore != null);
            var sortedSourceList = source.ToImmutableSortedDictionary(a => this.FormatPropertyName(a), a => a);

            string? typeName = this.GetTypeName(classModel);
            string str = this.GetTypeVisibility(classModel, typeName) ? "export " : "";
            sb.AppendLineIndented(str + "const " + typeName + "Resolver: Resolver<" + typeName  + "> = async (values) => {");

            using (sb.IncreaseIndentation())
            {
                sb.AppendLineIndented("const errorBuffer = {");
                using (sb.IncreaseIndentation())
                {
                    foreach (var property in sortedSourceList.Where(p => p.Value.ValidationRules.Count > 0))
                    {
                        string propertyName = this._memberFormatter(property.Value);
                        sb.AppendLineIndented(propertyName + ": FieldError[]");
                    }
                }
                sb.AppendLineIndented("};");
                sb.AppendLine();

                foreach (var property in sortedSourceList)
                {
                    foreach (var validationRule in property.Value.ValidationRules)
                    {
                        validationRule.BuildRule(sb, property.Key, property.Value, sortedSourceList);
                    }
                }

                sb.AppendLine();
                sb.AppendLineIndented("let returnValues:" + typeName + " = {};");
                sb.AppendLineIndented("let returnErrors = {};");
                sb.AppendLine();

                foreach (var property in sortedSourceList)
                {
                    string propertyName = this._memberFormatter(property.Value);
                    if (property.Value.ValidationRules.Count == 0)
                    {
                        sb.AppendLineIndented("returnValues." + propertyName + " = values." + propertyName + ';');
                    }
                    else
                    {
                        sb.AppendLineIndented("if (errorBuffer." + propertyName + ".length == 0) {");
                        using (sb.IncreaseIndentation())
                        {
                            sb.AppendLineIndented("returnValues." + propertyName + " = values." + propertyName + ';');
                        }
                        sb.AppendLineIndented("} else {");
                        using (sb.IncreaseIndentation())
                        {
                            sb.AppendLineIndented("returnErrors." + propertyName + " = errorBuffer." + propertyName + ';');
                        }
                        sb.AppendLineIndented("};");
                    }
                }
                sb.AppendLine();

                sb.AppendLineIndented("return {");
                using (sb.IncreaseIndentation())
                {
                    sb.AppendLineIndented("values: returnValues,");
                    sb.AppendLineIndented("errors: returnErrors");
                }
                sb.AppendLineIndented("};");
            }

            sb.AppendLineIndented("};");

            sb.AppendLine();
        }

        protected override void AppendInterfaceDefinition(
          TsInterface interfaceModel,
          ScriptBuilder sb,
          TsGeneratorOutput generatorOutput)
        {
            base.AppendInterfaceDefinition(interfaceModel, sb, generatorOutput);
        }
    }
}