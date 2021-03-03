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
		struct GenerateFieldInfo
		{
			public string typeStr;
			public string defaultContext;
			public bool isPrimaryKey;
			public bool isSecondaryKey;

			public bool IsPkFiled()
			{
				return isPrimaryKey == true || isSecondaryKey == true ? true : false;
			}
		}


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

			foreach (var field in excelSchemaData.SchemaFields)
			{
				var fieldTypeInfo = GetFieldTypeInfo(field);
				if (fieldTypeInfo.IsPkFiled())
				{
					MakePkKeySource(builder, field.Name, fieldTypeInfo);
				}
				else
				{
					MakeNonPkKeySource(builder, field.Name, fieldTypeInfo);
				}
			}

			// PropertyMetaData
			builder.AppendLine($"\tpublic override int PropertyCount {{ get => {excelSchemaData.SchemaFields.Count}; }}");

			MakeGetPropertyInfoSource(builder, excelSchemaData);
			MakeSetPropertyValueSource(builder, excelSchemaData);

			//
			builder.AppendLine("}");

			return builder.ToString();
		}

		private static void MakePkKeySource(StringBuilder builder, string fieldName, GenerateFieldInfo generateFieldInfo)
		{
			string defaultContext = generateFieldInfo.defaultContext;
			if (string.IsNullOrEmpty(defaultContext) == false)
				defaultContext = $" = {defaultContext}";

			string privateFieldName = $"_{fieldName}";
			string isPrimryKeyString = generateFieldInfo.isPrimaryKey ? "true" : " false"; 

			builder.AppendLine($"\tprivate {generateFieldInfo.typeStr} {privateFieldName}{defaultContext};");

			builder.AppendLine($"\tpublic {generateFieldInfo.typeStr} {fieldName}");
			builder.AppendLine($"\t{{");
			builder.AppendLine($"\t\tget => {privateFieldName};");
			builder.AppendLine($"\t\tset");
			builder.AppendLine($"\t\t{{");
			builder.AppendLine($"\t\t\t{privateFieldName} = value;");
			builder.AppendLine($"\t\t\tConvertKey({privateFieldName}, {isPrimryKeyString});");
			builder.AppendLine($"\t\t}}");
			builder.AppendLine($"\t}}");
		}

		private static void MakeNonPkKeySource(StringBuilder builder, string fieldName, GenerateFieldInfo generateFieldInfo)
		{
			string accesorString = "{ get; set; }";

			string defaultContext = generateFieldInfo.defaultContext;
			if (string.IsNullOrEmpty(defaultContext) == false)
				defaultContext = $"= {defaultContext};";

			builder.AppendLine($"\tpublic {generateFieldInfo.typeStr} {fieldName} {accesorString} {defaultContext}");
		}

		private static GenerateFieldInfo GetFieldTypeInfo(ExcelSchemaField field)
		{
			string type = "";
			string defaultContext = "";
			bool isPrimaryKey = false;
			bool isSecondaryKey = false;

			if (field.Name.Equals("PrimaryKey", StringComparison.OrdinalIgnoreCase) == true)
			{
				isPrimaryKey = true;
			}
			if(field.Name.Equals("SecondaryKey", StringComparison.OrdinalIgnoreCase) == true)
			{
				isSecondaryKey = true;
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

			return new GenerateFieldInfo()
			{
				typeStr = type,
				defaultContext = defaultContext,
				isPrimaryKey = isPrimaryKey,
				isSecondaryKey = isSecondaryKey
			};
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
