using GolbengFramework.Generator;
using GolbengFramework.Serialize;

using GolbengFramework.Parser;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Generator
{
	class Program
	{
		static void Main(string[] args)
		{
			/*
			GenerateEnums(
				@"D:\_Projects_Ing\CookieProject\src\Common\CommonPackage\bin\\Release\netstandard2.0\CommonPackage.dll",
				"CommonPackage.Enums",
				@"D:\_Projects_Ing\CookieProject\table\enums.json"
			);
			*/

			/*
			GenerateTables(
				@"D:\_Projects_Ing\CookieProject\table\schema", 
				@"D:\_Projects_Ing\CookieProject\src\Common\CommonPackage\CommonPackage\GenerateTables.cs"
			);
			*/

			var enumsDefines = EnumSerialize.Serialize(@"D:\_Projects_Ing\CookieProject\table\enums.json");

			GenerateTables(
				@"D:\_Projects_Ing\CookieProject\table\schema",
				@"D:\_Projects_Ing\CookieProject\src\Common\CommonPackage\src\table\GenerateTables.cs",
				@"D:\_Projects_Ing\CookieProject\src\Common\CommonPackage\src\table\GenerateTableMeta.cs",
				enumsDefines
			);


		}
		static void GenerateTables(string rootDirectory, string targetSourceFile, string targetMetasourceFile, EnumsDefines enumDefines)
		{
			if (Directory.Exists(rootDirectory) == false)
			{
				Console.WriteLine($"{rootDirectory} 스키마 폴더 없습니다.");
				return;
			}

			if(File.Exists(targetSourceFile) == false)
			{
				Console.WriteLine($"{targetSourceFile} 소스 파일이 없습니다.");
				return;
			}

			List<string> generateResults = new List<string>();
			List<string> gnerateMetaResults = new List<string>();

			var files = Directory.GetFiles(rootDirectory, "*.schema.xlsx");
			foreach(var file in files)
			{
				FileInfo fileInfo = new FileInfo(file);
				if (fileInfo.Attributes.HasFlag(FileAttributes.Hidden) == true)
					continue;

				try
				{
					TableSourceGenerator generator = new TableSourceGenerator(fileInfo.FullName);
					var result = generator.Generate(enumDefines);
					generateResults.Add(result.tableSource);
					gnerateMetaResults.Add(result.metaSource);
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
					throw new AggregateException($"{fileInfo.FullName} Generate 중 예외 발생", e);
				}
			}

			StringBuilder sourceBuiler = new StringBuilder();
			sourceBuiler.AppendLine("using CommonPackage.Enums;");
			sourceBuiler.AppendLine("namespace CommonPackage.Tables");
			sourceBuiler.AppendLine("{");
			foreach(var generateResult in generateResults)
			{
				sourceBuiler.AppendLine(generateResult);
			}
			sourceBuiler.AppendLine("}");
			
			using (StreamWriter stream = new StreamWriter(targetSourceFile))
			{
				stream.Write(sourceBuiler.ToString());
			}

			StringBuilder metaSourceBuilder = new StringBuilder();
			metaSourceBuilder.AppendLine("using CommonPackage.Enums;");
			metaSourceBuilder.AppendLine("namespace CommonPackage.Tables");
			metaSourceBuilder.AppendLine("{");

			metaSourceBuilder.AppendLine("public partial class GenerateTableMeta");
			metaSourceBuilder.AppendLine("{");
			metaSourceBuilder.AppendLine("private static void InitalizeGenerateTableMeta()");
			metaSourceBuilder.AppendLine("{");

			foreach (var generateMetaResult in gnerateMetaResults)
			{
				metaSourceBuilder.AppendLine(generateMetaResult);
			}

			metaSourceBuilder.AppendLine("}");
			metaSourceBuilder.AppendLine("}");
			metaSourceBuilder.AppendLine("}");

			using (StreamWriter stream = new StreamWriter(targetMetasourceFile))
			{
				stream.Write(metaSourceBuilder.ToString());
			}
		}
	
		static void GenerateEnums(string targetDllPath, string targetNamespace, string targetExtractFile)
		{
			if (File.Exists(targetDllPath) == false)
			{
				Console.WriteLine($"{targetDllPath} 파일이 없습니다.");
				return;
			}

			FileInfo fileInfo = new FileInfo(targetExtractFile);
			if(fileInfo.Directory.Exists == false)
			{
				Console.WriteLine($"{targetExtractFile} 경로가 잘못 되었습니다.");
				return;
			}

			EnumJsonGenerator generate = new EnumJsonGenerator(@"D:\_Projects_Ing\CookieProject\src\Common\CommonPackage\bin\\Release\netstandard2.0\CommonPackage.dll", "CommonPackage.Enums");
			var result = generate.Generate();

			using (StreamWriter stream = new StreamWriter(targetExtractFile, false, Encoding.UTF8))
			{
				stream.Write(result);
			}
		}
	}
}
