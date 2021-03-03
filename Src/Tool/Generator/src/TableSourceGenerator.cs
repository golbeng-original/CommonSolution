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

		public (string tableSource, string metaSource) Generate(EnumsDefines enumDefines)
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

			return (GenerateResult, GenerateMetaResult);
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
			// Table Body Source
			GenerateResult = TableSourceGenerateFormat.GetTableSource(excelSchemaData);

			// Table Meta Source
			GenerateMetaResult = TableSourceGenerateFormat.GetMetaTableSource(excelSchemaData);
		}

		public string GenerateResult { get; private set; } = "";

		public string GenerateMetaResult { get; private set; } = "";
	}
}
