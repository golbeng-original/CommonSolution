using GolbengFramework.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GolbengFramework.Generator
{
	public class EnumJsonGenerator
	{
		private string _targetDllPath = "";
		private string _targetNamespace = "";

		public EnumJsonGenerator(string targetDllPath, string targetNamespace)
		{
			_targetDllPath = targetDllPath;
			_targetNamespace = targetNamespace;
		}

		public string Generate()
		{
			EnumSourceParser parser = new EnumSourceParser(_targetDllPath, _targetNamespace);

			var enumMetaList = parser.Parsing();
			GenerateEnumList(enumMetaList);

			return GenerateResult;
		}

		private void GenerateEnumList(IList<EnumMetaInfo> enumMetaList)
		{
			StringBuilder builder = new StringBuilder();
			builder.AppendLine("[");

			foreach (var enumMeta in enumMetaList)
			{
				builder.AppendLine("\t{");
				builder.AppendLine($"\t\t\"name\": \"{enumMeta.EnumName}\",");
				builder.AppendLine("\t\t\"values\": [");

				foreach(var field in enumMeta.EnumFileds)
				{
					string serialize = $"\"name\": \"{field.Name}\", \"value\": {field.Value}, \"description\": \"{field.Description}\"";
					builder.AppendLine($"\t\t\t{{{serialize}}},");
				}

				builder.AppendLine("\t\t]");
				builder.AppendLine("\t},");
			}

			builder.AppendLine("]");

			GenerateResult = builder.ToString();
		}

		public string GenerateResult { get; private set; } = "";
	}
}
