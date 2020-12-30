using GolbengFramework.Parser;
using GolbengFramework.Serialize;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


namespace GolbengFramework.Generator
{
	public class TableSourceGenerator
	{
		private string _schemaFilePath = "";

		public TableSourceGenerator(string schemaFilePath)
		{
			_schemaFilePath = schemaFilePath;
		}

		public string Generate(EnumsDefines enumDefines)
		{
			using (SchemaTableParser schemaParser = new SchemaTableParser(_schemaFilePath))
			{
				var excelSchemaData = schemaParser.Parsing();

				foreach(var field in excelSchemaData.SchemaFields)
				{
					if (Validation(field, enumDefines) == false)
						throw new Exception($"{field.Name} Type={field.Type}, Defailt={field.Default} 유효성 통과를 못했습니다.");
				}

				GenerateTableSource(excelSchemaData);
			}

			return GenerateResult;
		}

		private bool Validation(ExcelSchemaField field, EnumsDefines enumDefines)
		{
			if (field.Type.Equals("string", StringComparison.OrdinalIgnoreCase))
			{
				//
			}
			else if (field.Type.Equals("int", StringComparison.OrdinalIgnoreCase))
			{
				int temp;
				if (string.IsNullOrEmpty(field.Default) == false)
					return Int32.TryParse(field.Default, out temp);

				return true;
			}
			else if (field.Type.Equals("uint", StringComparison.OrdinalIgnoreCase))
			{
				int temp;
				if (string.IsNullOrEmpty(field.Default) == false)
					return Int32.TryParse(field.Default, out temp);

				return true;
			}
			else if (field.Type.Equals("float", StringComparison.OrdinalIgnoreCase))
			{
				float temp;
				if (string.IsNullOrEmpty(field.Default) == false)
					return Single.TryParse(field.Default, out temp);

				return true;
			}
			else if (field.Type.Equals("bool", StringComparison.OrdinalIgnoreCase))
			{
				bool temp;
				if (string.IsNullOrEmpty(field.Default) == false)
					return Boolean.TryParse(field.Default, out temp);

				return true;
			}
			// Enum
			else
			{
				if (enumDefines.IsContainEnumType(field.Type) == false)
					return false;

				if (enumDefines.IsContainEnumValue(field.Type, field.Default) == false)
					return false;

				return true;
			}

			return true;
		}

		private void GenerateTableSource(ExcelSchemaData excelSchemaData)
		{
			StringBuilder builder = new StringBuilder();
			builder.AppendLine($"public class {excelSchemaData.TableName} : TblBase");
			builder.AppendLine("{");

			string accesorString = "{ get; set; }";

			foreach(var field in excelSchemaData.SchemaFields)
			{
				string type = "";
				string defaultContext = "";
				string prefix = "";
				if(field.Name.Equals("PrimaryKey", StringComparison.OrdinalIgnoreCase) == true ||
					field.Name.Equals("SecondaryKey", StringComparison.OrdinalIgnoreCase) == true)
				{
					prefix = "override ";
					type = "uint";
					defaultContext = string.IsNullOrEmpty(field.Default) ? "0" : $"{field.Default}";
				}

				if(type.Length == 0)
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

				if (string.IsNullOrEmpty(defaultContext) == false)
					defaultContext = $"= {defaultContext};";

				builder.AppendLine($"\tpublic {prefix}{type} {field.Name} {accesorString} {defaultContext}");
			}

			// MetaTable
			builder.AppendLine("\tpublic static TableMeta TableMeta { get; set; } = new TableMeta()");
			builder.AppendLine("\t{");
			builder.AppendLine($"\t\tTableName = \"{excelSchemaData.TableName}\",");
			builder.AppendLine($"\t\tDbName = \"{excelSchemaData.DbName}\"");
			builder.AppendLine("\t};");

			builder.AppendLine("}");

			GenerateResult = builder.ToString();
		}

		public string GenerateResult { get; private set; } = "";
	}
}
