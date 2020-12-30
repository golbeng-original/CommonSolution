using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GolbengFramework.StringDataTool.Utils
{
	public struct ToolConfigInfo
	{
		public string rootPath;
	}

	public class ToolConfigUtil
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
}
