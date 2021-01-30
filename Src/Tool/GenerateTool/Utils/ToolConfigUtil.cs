using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GolbengFramework.GenerateTool.Utils
{
	public struct ToolConfigInfo
	{
		public string rootPath;
		public string msbuildPath;
	}

	public class ToolPathConfigInfo
	{
		public string[] CheckPaths { get; private set; } = new string[]
		{
			@"Src\Client\**\Assets",
		};

		public string[] InitalizePaths { get; private set; } = new string[]
		{
			@"Data\Table",
			@"Data\Table\schema",
			@"Src\Client\**\Assets\StreamingAssets\Data\Table",
			@"Bin\Data\Table"
		};

		public string SchemaPath { get; set; } = @"Data\Table\schema";
		public string TablePath { get; set; } = @"Data\Table";
		public string CommonPackageProjPath { get; set; } = @"Common\CommonPackage\CommonPackage.csproj";
		public string SourcePath { get; set; } = @"Common\CommonPackage\src\table\GenerateTables.cs";
		public string ClientSrcTablePath { get; set; } = @"Src\Client\**\Assets\StreamingAssets\Data\Table";
		public string ClientBinTablePath { get; set; } = @"Bin\Client\**\StreamingAssets\Data\Table";

		public string ClientSrcConfigPath { get; set; } = @"Src\Client\**\Assets\StreamingAssets\Data\Config";
		public string ClientBinConfigPath { get; set; } = @"Bin\Client\**\StreamingAssets\Data\Config";

		public string ServerPath { get; set; } = @"Bin\Data\Table";
		public string CommonDllPath { get; set; } = @"Bin\Lib\CommonPackage.dll";
		public string EnumPath { get; set; } = @"Data\Table\enums.json";
		public string ConfigurePath { get; set; } = @"Data\Config";

		public void Serialize(string path)
		{
			var jsonConfigContent = Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
			using (StreamWriter streamReader = new StreamWriter(path))
			{
				streamReader.Write(jsonConfigContent);
			}
		}

		public bool Deserialize(string path)
		{
			try
			{
				using (StreamReader streamReader = new StreamReader(path))
				{
					string jsonConfigContent = streamReader.ReadToEnd();

					var deserialize = Newtonsoft.Json.JsonConvert.DeserializeObject<ToolPathConfigInfo>(jsonConfigContent);

					this.SchemaPath = deserialize.SchemaPath;
					this.TablePath = deserialize.TablePath;
					this.CommonPackageProjPath = deserialize.CommonPackageProjPath;
					this.SourcePath = deserialize.SourcePath;
					this.ClientSrcTablePath = deserialize.ClientSrcTablePath;
					this.ClientBinTablePath = deserialize.ClientBinTablePath;

					this.ClientSrcConfigPath = deserialize.ClientSrcConfigPath;
					this.ClientBinConfigPath = deserialize.ClientBinConfigPath;

					this.ServerPath = deserialize.ServerPath;
					this.CommonDllPath = deserialize.CommonDllPath;
					this.EnumPath = deserialize.EnumPath;
					this.ConfigurePath = deserialize.ConfigurePath;
				}

				return true;
			}
			catch(Exception e)
			{
				Debug.WriteLine(e.Message);
				return false;
			}
		}
	}


	public partial class ToolConfigUtil
	{
		private static readonly string _staticConfigFileName = "config.json";


		public static ToolConfigInfo LoadToolConfig()
		{
			ToolConfigInfo configInfo = new ToolConfigInfo();

			if (File.Exists(ConfigFilePath) == false)
				return configInfo;

			using (StreamReader streamReader = new StreamReader(ConfigFilePath))
			{
				string jsonConfigContent = streamReader.ReadToEnd();

				configInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<ToolConfigInfo>(jsonConfigContent);
			}

			return configInfo;
		}

		public static void SaveToolConfig(ToolConfigInfo toolConfigInfo)
		{
			var jsonConfigContent = Newtonsoft.Json.JsonConvert.SerializeObject(toolConfigInfo);
			using (StreamWriter streamReader = new StreamWriter(ConfigFilePath))
			{
				streamReader.Write(jsonConfigContent);
			}
		}

		private static string ConfigFilePath
		{
			get => System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _staticConfigFileName);
		}
	}

	public partial class ToolConfigUtil
	{
		private static readonly string _staticPathConfigFileName = "pathconfig.json";

		public static (ToolPathConfigInfo pathConfig, bool isNew) LoadPathConfig()
		{
			string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _staticPathConfigFileName);
			bool isNew = false;

			ToolPathConfigInfo toolPathConfigInfo = new ToolPathConfigInfo();
			if(toolPathConfigInfo.Deserialize(fullPath) == false)
			{
				toolPathConfigInfo.Serialize(fullPath);
				isNew = true;
			}

			return (toolPathConfigInfo, isNew);
		}
	}

	public partial class ToolConfigUtil
	{
		public static string FindWildCardPath(string wildCardPath)
		{
			string fullPath = "";

			var paths = wildCardPath.Split(@"\".ToArray());

			bool needFindPostFix = false;
			foreach(var path in paths)
			{
				if(path.Equals("**") == true)
				{
					needFindPostFix = true;
					continue;
				}

				if(needFindPostFix == true)
				{
					fullPath = FindDoubleStartPath(fullPath, path);
					needFindPostFix = false;
					continue;
				}

				if (System.IO.Path.IsPathRooted(path) == true)
				{
					fullPath = path + System.IO.Path.DirectorySeparatorChar;
				}
				else
				{
					fullPath = System.IO.Path.Combine(fullPath, path);
				}
			}

			return fullPath;
		}

		private static string FindDoubleStartPath(string prefixPath, string postfixPath)
		{
			DirectoryInfo directorInfo = new DirectoryInfo(prefixPath);
			foreach(var subDir in directorInfo.GetDirectories())
			{
				var searchPath = System.IO.Path.Combine(subDir.FullName, postfixPath);
				if(Directory.Exists(searchPath) == true)
				{
					return searchPath;
				}

				searchPath = FindDoubleStartPath(subDir.FullName, postfixPath);
				if (string.IsNullOrEmpty(searchPath) == false)
					return searchPath;
			}

			return "";
		}
	}
}
