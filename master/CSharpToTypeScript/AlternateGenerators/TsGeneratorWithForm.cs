﻿using System.Collections.Immutable;
using CSharpToTypeScript.Models;

namespace CSharpToTypeScript.AlternateGenerators
{
    public class TsGeneratorWithForm : TsGeneratorWithResolver
    {
        // Bootstrap column count max. is 12
        public const Int32 MaxColCount = 12;
        public const string TypeScriptXmlFileType = ".tsx";
        public const string BootstrapUtilsNamespace = "BootstrapUtils";

        public bool GenerateBootstrapUtils { get; private set; }

        public Int32 ColCount { get; init; }

        public readonly IReadOnlyList<Int32> ColumnWidths;

        public TsGeneratorWithForm(int colCount, bool enableNamespace)
            : base(enableNamespace)
        {
            if (colCount > MaxColCount)
            {
                throw new ArgumentOutOfRangeException(nameof(colCount), "Column count cannot exceed 12.");
            }

            ColCount = colCount;

            var columnWidths = new int[colCount];
            var averageColumnWidth = MaxColCount / colCount;
            for (var i = 0; i < colCount; ++i)
            {
                if (i < colCount - 1)
                {
                    columnWidths[i] = averageColumnWidth;
                }
                else
                {
                    var remainder = MaxColCount % colCount;
                    columnWidths[i] = remainder == 0 ? averageColumnWidth : remainder;
                }
            }

            ColumnWidths = columnWidths;
        }

        protected override IReadOnlyDictionary<string, IReadOnlyDictionary<string, Int32>> AppendImports(
            TsNamespace @namespace,
            ScriptBuilder sb,
            TsGeneratorOptions generatorOptions,
            IReadOnlyDictionary<string, IReadOnlyCollection<TsModuleMember>> dependencies)
        {
            if ((generatorOptions.HasFlag(TsGeneratorOptions.Properties) || generatorOptions.HasFlag(TsGeneratorOptions.Fields))
                && @namespace.Classes.Any(c => !c.IsIgnored))
            {
                sb.AppendLine("import { useId } from 'react';");
            }

            return base.AppendImports(@namespace, sb, generatorOptions, dependencies);
        }

        protected override void AppendAdditionalImports(
            TsNamespace @namespace,
            ScriptBuilder sb, 
            TsGeneratorOptions tsGeneratorOptions,
            IReadOnlyDictionary<string, IReadOnlyCollection<TsModuleMember>> dependencies,
            Dictionary<string, IReadOnlyDictionary<string, Int32>> importIndices)
        {
            base.AppendAdditionalImports(@namespace, sb, tsGeneratorOptions, dependencies, importIndices);

            bool hasClasses = (tsGeneratorOptions.HasFlag(TsGeneratorOptions.Properties) || tsGeneratorOptions.HasFlag(TsGeneratorOptions.Fields))
                && @namespace.Classes.Any(c => !c.IsIgnored);

            if (hasClasses)
            {
                bool hasCheckBoxes = (tsGeneratorOptions.HasFlag(TsGeneratorOptions.Properties) && @namespace.Classes
                        .Any(c => c.Properties.Any(p => p.PropertyType is TsSystemType systemType && systemType.Kind == SystemTypeKind.Bool)))
                    || (tsGeneratorOptions.HasFlag(TsGeneratorOptions.Fields) && @namespace.Classes
                        .Any(c => c.Fields.Any(p => p.PropertyType is TsSystemType systemType && systemType.Kind == SystemTypeKind.Bool)));
                if (hasCheckBoxes)
                {
                    sb.AppendLineIndented("import { getClassName, getCheckBoxClassName, getErrorMessage } from './" + BootstrapUtilsNamespace + "';");
                    importIndices.Add(BootstrapUtilsNamespace, new Dictionary<string, Int32>() 
                    { 
                        { "getClassName", 0 },
                        { "getErrorMessage", 0 },
                        { "getCheckBoxClassName", 0 }
                    });
                }
                else
                {
                    sb.AppendLineIndented("import { getClassName, getErrorMessage } from './" + BootstrapUtilsNamespace + "';");
                    importIndices.Add(BootstrapUtilsNamespace, new Dictionary<string, Int32>() { { "getClassName", 0 }, { "getErrorMessage", 0 } });
                }

                GenerateBootstrapUtils = true;
            }
        }

        protected override void AppendAdditionalDependencies(Dictionary<string, TsGeneratorOutput> results)
        {
            base.AppendAdditionalDependencies(results);

            if (GenerateBootstrapUtils)
            {
                ScriptBuilder sb = new(this.IndentationString);

                sb.AppendLine("import { FieldError } from 'react-hook-form';");
                sb.AppendLine();

                sb.AppendLine("export const getClassName = (isValidated: boolean | undefined, error: FieldError | undefined): string =>");
                using (sb.IncreaseIndentation())
                {
                    sb.AppendLineIndented("error ? \"form-control is-invalid\" : (isValidated ? \"form-control is-valid\" : \"form-control\");");
                }

                sb.AppendLine();

                sb.AppendLine("export const getCheckBoxClassName = (isValidated: boolean | undefined, error: FieldError | undefined): string =>");
                using (sb.IncreaseIndentation())
                {
                    sb.AppendLineIndented("error ? \"form-check-input is-invalid\" : (isValidated ? \"form-check-input is-valid\" : \"form-check-input\");");
                }

                sb.AppendLine();

                sb.AppendLine("export const getErrorMessage = (error: FieldError | undefined) =>");
                using (sb.IncreaseIndentation())
                {
                    sb.AppendLineIndented("error && <span className=\"invalid-feedback\">{error.message}</span>;");
                }

                results.Add(BootstrapUtilsNamespace, new TsGeneratorOutput(TypeScriptXmlFileType, sb.ToString()) { ExcludeFromResultToString = true });
            }
        }

        protected override string AppendNamespace(ScriptBuilder sb, TsGeneratorOptions generatorOutput,
            List<TsClass> moduleClasses, List<TsInterface> moduleInterfaces,
            List<TsEnum> moduleEnums, IReadOnlyDictionary<string, IReadOnlyDictionary<string, Int32>> importNames)
        {
            bool hasClasses = (generatorOutput.HasFlag(TsGeneratorOptions.Properties) || generatorOutput.HasFlag(TsGeneratorOptions.Fields))
                && moduleClasses.Any(c => !c.IsIgnored);

            var fileType = base.AppendNamespace(sb, generatorOutput, moduleClasses, moduleInterfaces,
                moduleEnums, importNames);

            if (hasClasses)
            {
                return TypeScriptXmlFileType;
            }
            else
            {
                return fileType;
            }
        }

        protected override IReadOnlyList<TsProperty> AppendClassDefinition(
            TsClass classModel,
            ScriptBuilder sb,
            TsGeneratorOptions generatorOutput,
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, Int32>> importNames)
        {
            var propertiesToExport = base.AppendClassDefinition(classModel, sb, generatorOutput, importNames);

            List<TsProperty> allProperties = classModel.GetBaseProperties(generatorOutput.HasFlag(TsGeneratorOptions.Properties),
                generatorOutput.HasFlag(TsGeneratorOptions.Fields));

            allProperties.AddRange(propertiesToExport);

            sb.AppendLine();

            var propertyList = allProperties.ToImmutableSortedDictionary(a => this.FormatPropertyName(a), a => a)
                .ToList();

            string typeName = this.GetTypeName(classModel) ?? string.Empty;

            string str = this.GetTypeVisibility(classModel, typeName) ? "export " : "";

            sb.AppendLineIndented(str + "type " + typeName + "FormData = {");
            var typeNameInCamelCase = ToCamelCase(typeName);
            var hasRequiredConstructorParams = propertiesToExport.Any(p => p.IsRequired && string.IsNullOrEmpty(p.GetDefaultValue()));
            using (sb.IncreaseIndentation())
            {
                sb.AppendLineIndented(typeNameInCamelCase + (hasRequiredConstructorParams ? ": " : "?: ") + typeName + ",");
                sb.AppendLineIndented("onSubmit: SubmitHandler<" + typeName + '>');
            }
            sb.AppendLineIndented("};");

            sb.AppendLine();

            sb.AppendLineIndented(str + "const " + typeName + "Form = (props: " + typeName + "FormData) => {");

            using (sb.IncreaseIndentation())
            {
                sb.AppendLineIndented("const formId = useId();");
                sb.AppendLineIndented("const { register, handleSubmit, formState: { errors, touchedFields, isSubmitting } } = useForm<" + typeName+ ">({");
                using (sb.IncreaseIndentation())
                {
                    sb.AppendLineIndented("mode: \"onTouched\",");
                    sb.AppendLineIndented("resolver: " + typeName + "Resolver,");
                    var defaultValues = "defaultValues: props." + typeNameInCamelCase;
                    if (!hasRequiredConstructorParams)
                    {
                        defaultValues += " ?? new " + typeName + "()";
                    }
                    sb.AppendLineIndented(defaultValues);
                }
                sb.AppendLineIndented("});");

                sb.AppendLine();

                sb.AppendLineIndented("return <form onSubmit={handleSubmit(props.onSubmit)}>");
                using (sb.IncreaseIndentation())
                {
                    var rowCount = (propertyList.Count + ColCount - 1) / ColCount;
                    for (var i = 0; i < rowCount; ++i)
                    {
                        var startIndex = i * ColCount;
                        AppendRow(sb, startIndex, Math.Min(ColCount, propertyList.Count - startIndex), propertyList);
                    }
                    AppendButtonRow(sb);
                }
                sb.AppendLineIndented("</form>;");
            }

            sb.AppendLineIndented("};");

            return propertiesToExport;
        }

        protected override IReadOnlyList<string> GetReactHookFormComponentNames(IEnumerable<TsClass> classes)
        {
            var reactHookFormComponentNames = new List<string>() { "useForm", "SubmitHandler", "FieldError" };

            reactHookFormComponentNames.AddRange(base.GetReactHookFormComponentNames(classes));

            return reactHookFormComponentNames;
        }

        protected override bool HasAdditionalImports(TsNamespace @namespace, TsGeneratorOptions generatorOptions)
        {
            return (generatorOptions.HasFlag(TsGeneratorOptions.Properties) || generatorOptions.HasFlag(TsGeneratorOptions.Fields))
                && @namespace.Classes.Any(c => !c.IsIgnored);
        }

        private static void AppendButtonRow(ScriptBuilder sb)
        {
            sb.AppendLineIndented("<div className=\"row\">");
            using (sb.IncreaseIndentation())
            {
                sb.AppendLineIndented("<div className=\"form-group col-md-12\">");
                using (sb.IncreaseIndentation())
                {
                    sb.AppendLineIndented("<button className=\"btn btn-primary\" type=\"submit\" disabled={isSubmitting}>Submit</button>");
                    sb.AppendLineIndented("<button className=\"btn btn-secondary mx-1\" type=\"reset\" disabled={isSubmitting}>Reset</button>");
                }
                sb.AppendLineIndented("</div>");
            }
            sb.AppendLineIndented("</div>");
        }

        private void AppendRow(ScriptBuilder sb, int startIndex, int count, IReadOnlyList<KeyValuePair<string, TsProperty>> properties)
        {
            sb.AppendLineIndented("<div className=\"row mb-3\">");
            using (sb.IncreaseIndentation())
            {
                for (var i = 0; i < count; ++i)
                {
                    var propertyValuePair = properties[i + startIndex];
                    var property = propertyValuePair.Value;
                    var propertyName = propertyValuePair.Key;
                    var columnWidth = ColumnWidths[i];
                    sb.AppendLineIndented("<div className=\"form-group col-md-" + columnWidth + "\">");
                    using (sb.IncreaseIndentation())
                    {
                        if (property.PropertyType is TsEnum tsEnum)
                        {
                            sb.AppendLineIndented("<label htmlFor={formId + \"-" + propertyName + "\"}>" + property.GetDisplayName() + ":</label>");
                            sb.AppendLineIndented("<select className={getClassName(touchedFields." + propertyName + ", errors." + propertyName + ")} id={formId + \"-" + propertyName + "\"} {...register('" + propertyName + "')}>");
                            using (sb.IncreaseIndentation())
                            {
                                if (!property.IsRequired)
                                {
                                    sb.AppendLineIndented("<option value=\"\">Select a " + property.GetDisplayName() + "</option>");
                                }
                                foreach (var enumValue in tsEnum.Values)
                                {
                                    sb.AppendLineIndented("<option value=\"" + enumValue.Value + "\">" + enumValue.GetDisplayName() + "</option>");
                                }
                            }
                            sb.AppendLineIndented("</select>");
                        }
                        else if (property.PropertyType is TsSystemType tsSystemType)
                        {
                            if (tsSystemType.Kind == SystemTypeKind.Bool)
                            {
                                sb.AppendLineIndented("<input type=\"" + GetInputType(property, tsSystemType.Kind)
                                    + "\" className={getCheckBoxClassName(touchedFields." + propertyName + ", errors." + propertyName + ")} {formId + \"-" 
                                    + propertyName + "\"} {...register(\"" + propertyName + "\")} />");
                                sb.AppendLineIndented("<label className=\"form-check-label\" htmlFor={formId + \"-" + propertyName + "\"}>" + property.GetDisplayName() + "</label>");
                            }
                            else if (tsSystemType.Kind == SystemTypeKind.Number)
                            {
                                sb.AppendLineIndented("<label htmlFor={formId + \"-" + propertyName + "\"}>" + property.GetDisplayName() + ":</label>");
                                sb.AppendLineIndented("<input type=\"" + GetInputType(property, tsSystemType.Kind)
                                    + "\" className={getClassName(touchedFields." + propertyName + ", errors." + propertyName + ")} id={formId + \"-" 
                                    + propertyName + "\"} {...register(\"" + propertyName + "\", { valueAsNumber: true })} />");
                            }
                            else
                            {
                                sb.AppendLineIndented("<label htmlFor={formId + \"-" + propertyName + "\"}>" + property.GetDisplayName() + ":</label>");
                                sb.AppendLineIndented("<input type=\"" + GetInputType(property, tsSystemType.Kind)
                                    + "\" className={getClassName(touchedFields." + propertyName + ", errors." + propertyName + ")} id={formId + \"-" 
                                    + propertyName + "\"} {...register(\"" + propertyName + "\")} />");
                            }
                        }
                        else
                        {
                            sb.AppendLineIndented("<label htmlFor=\"" + propertyName + "\">" + property.GetDisplayName() + ":</label>");
                            sb.AppendLineIndented("<input type=\"text\" className={getClassName(touchedFields." + propertyName + ", errors." + propertyName + ")} id=\"" + propertyName
                                + "\" {...register(\"" + propertyName + "\")} />");
                        }
                        sb.AppendLineIndented("{getErrorMessage(errors." + propertyName + ")}");
                    }
                    sb.AppendLineIndented("</div>");
                }
            }
            sb.AppendLineIndented("</div>");
        }

        private static string GetInputType(TsProperty property, SystemTypeKind kind)
        {
            if (!string.IsNullOrEmpty(property.UiHint?.UIHint))
            {
                return property.UiHint.UIHint;
            }

            switch (kind)
            {
                case SystemTypeKind.Number:
                    return "number";
                case SystemTypeKind.Bool:
                    return "checkbox";
                case SystemTypeKind.Date:
                    return "date";
                default:
                    return property.ValidationRules switch 
                        { 
                            _ when property.ValidationRules.Any(r => r is UrlRule) => "url",
                            _ when property.ValidationRules.Any(r => r is EmailAddressRule) => "email",
                            _ when property.ValidationRules.Any(r => r is PhoneNumberRule) => "tel",
                            _ => "text"
                        };
            }
        }
    }
}
