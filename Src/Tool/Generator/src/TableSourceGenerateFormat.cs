using GolbengFramework.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GolbengFramework.Generator
{
	internal class TableSourceGenerateFormat
	{
		public static string GetMetaTableSource(ExcelSchemaData excelSchemaData)
		{
			StringBuilder builder = new StringBuilder();

			builder.AppendLine($"\tTableMetaMapping.Add(typeof({excelSchemaData.TableName}), new TableMeta()");
			builder.AppendLine("\t{");
			builder.AppendLine($"\t\tTableName = \"{excelSchemaData.TableName}\",");
			builder.AppendLine($"\t\tDbName = \"{excelSchemaData.DbName}\",");
			builder.AppendLine($"\t\tClientDbName = \"{excelSchemaData.ClientUseDbName}\"");
			builder.AppendLine("\t});");

			return builder.ToString();
		}

		public static string GetTableSource(ExcelSchemaData excelSchemaData)
		{
			StringBuilder builder = new StringBuilder();
			builder.AppendLine($"public class {excelSchemaData.TableName} : TblBase");
			builder.AppendLine("{");

			string accesorString = "{ get; set; }";

			foreach (var field in excelSchemaData.SchemaFields)
			{
				var fieldTypeInfo = GetFieldTypeInfo(field);

				string type = fieldTypeInfo.typeStr;
				string defaultContext = fieldTypeInfo.defaultContext;
				string prefix = "";
				if (field.Name.Equals("PrimaryKey", StringComparison.OrdinalIgnoreCase) == true ||
					field.Name.Equals("SecondaryKey", StringComparison.OrdinalIgnoreCase) == true)
				{
					prefix = "override ";
				}

				if (string.IsNullOrEmpty(defaultContext) == false)
					defaultContext = $"= {defaultContext};";

				builder.AppendLine($"\tpublic {prefix}{type} {field.Name} {accesorString} {defaultContext}");
			}

			// PropertyMetaData
			builder.AppendLine($"\tpublic override int PropertyCount {{ get => {excelSchemaData.SchemaFields.Count}; }}");

			MakeGetPropertyInfoSource(builder, excelSchemaData);
			MakeSetPropertyValueSource(builder, excelSchemaData);

			//
			builder.AppendLine("}");

			return builder.ToString();
		}

		private static (string typeStr, string defaultContext) GetFieldTypeInfo(ExcelSchemaField field)
		{
			string type = "";
			string defaultContext = "";

			if (field.Name.Equals("PrimaryKey", StringComparison.OrdinalIgnoreCase) == true ||
				field.Name.Equals("SecondaryKey", StringComparison.OrdinalIgnoreCase) == true)
			{
				type = "uint";
				defaultContext = string.IsNullOrEmpty(field.Default) ? "0" : $"{field.Default}";
			}

			if (type.Length == 0)
			{
				switch (field.Type.ToLower())
				{
					case "string":
						type = "string";
						defaultContext = $"\"{field.Default}\"";
						break;
					case "int":
						type = "int";
						defaultContext = string.IsNullOrEmpty(field.Default) ? "0" : $"{field.Default}";
						break;
					case "uint":
						type = "uint";
						defaultContext = string.IsNullOrEmpty(field.Default) ? "0" : $"{field.Default}";
						break;
					case "float":
						type = "float";
						defaultContext = string.IsNullOrEmpty(field.Default) ? "0" : $"{field.Default}";
						break;
					case "bool":
						type = "bool";
						defaultContext = string.IsNullOrEmpty(field.Default) ? "false" : $"{field.Default}";
						defaultContext = defaultContext.ToLower();
						break;
					default:    // enum 종류
						type = field.Type;
						defaultContext = string.IsNullOrEmpty(field.Default) ? "" : $"{field.Type}.{field.Default}";
						break;
				}
			}

			return (type, defaultContext);
		}

		private static string MakeGetPropertyInfoSource(StringBuilder builder, ExcelSchemaData excelSchemaData)
		{
			builder.AppendLine("\tpublic override (string propertyName, Type type)? GetPropertyInfo(int index)");
			builder.AppendLine("\t{");
			builder.AppendLine("\t\tswitch (index)");
			builder.AppendLine("\t\t{");
			foreach (var field in excelSchemaData.SchemaFields)
			{
				var index = excelSchemaData.SchemaFields.IndexOf(field);

				builder.AppendLine($"\t\t\tcase {index}: return (nameof({field.Name}), {field.Name}.GetType());");
			}
			builder.AppendLine("\t\t}");
			builder.AppendLine("\t\treturn null;");
			builder.AppendLine("\t}");

			return "";
		}

		private static void MakeSetPropertyValueSource(StringBuilder builder, ExcelSchemaData excelSchemaData)
		{
			builder.AppendLine("\tpublic override bool SetPropertyValue(string propertyName, object value)");
			builder.AppendLine("\t{");
			builder.AppendLine("\t\tif (base.SetPropertyValue(propertyName, value) == true)");
			builder.AppendLine("\t\t\treturn true;");

			foreach (var field in excelSchemaData.SchemaFields)
			{
				MakeSetPropertyValueUnitSource(builder, field);
			}

			builder.AppendLine("\t\treturn false;");
			builder.AppendLine("\t}");
		}

		private static void MakeSetPropertyValueUnitSource(StringBuilder builder, ExcelSchemaField field)
		{
			string prefix = "\t\t";

			var fieldTypeInfo = GetFieldTypeInfo(field);

			builder.AppendLine($"{prefix}if (propertyName.Equals(\"{field.Name}\", StringComparison.OrdinalIgnoreCase))");
			builder.AppendLine($"{prefix}{{");
			builder.AppendLine($"{prefix}\tif (CheckProprtyType({field.Name}, ({fieldTypeInfo.typeStr})value) == false)");
			builder.AppendLine($"{prefix}\t\treturn false;");
			builder.AppendLine($"{prefix}\t{field.Name} = ({fieldTypeInfo.typeStr})value;");
			builder.AppendLine($"{prefix}\treturn true;");
			builder.AppendLine($"{prefix}}}");
		}
	}
}
