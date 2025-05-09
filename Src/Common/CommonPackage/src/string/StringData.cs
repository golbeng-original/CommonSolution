﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CommonPackage.String
{
	public class StringData : IEquatable<StringData>
	{
		public StringData() { }

		public string Key { get; set; } = "";

		public string Group { get; set; } = "";

		public string Data { get; set; } = "";

		public Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>();

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(this, obj) == true)
				return true;

			return Equals(obj as StringData);
		}

		public bool Equals(StringData other)
		{
			if (other == null)
				return false;

			return Key == other.Key;
		}

		public override int GetHashCode()
		{
			return Key.GetHashCode();
		}

		public override string ToString()
		{
			return Key;
		}
	}


	public partial class StringDataContainer
	{
		public string FileName { get; set; }

		public HashSet<StringData> StringDataSet { get; private set; } = new HashSet<StringData>();
	}

	public class StringDataSerializeResult
	{
		public StringDataContainer Container { get; set; } = new StringDataContainer();

		public Dictionary<StringData, List<StringData>> DuplicateList { get; private set; } = new Dictionary<StringData, List<StringData>>();
	}

	public partial class StringDataContainer
	{
		public static StringDataSerializeResult Deserialize(string filePath)
		{
			FileInfo fileInfo = new FileInfo(filePath);
			if (fileInfo.Exists == false)
				throw new FileNotFoundException($"{filePath} not found");

			using (StreamReader streamReader = new StreamReader(filePath))
			{
				StringDataSerializeResult result = DeserializeContent(streamReader);
				result.Container.FileName = fileInfo.Name;

				return result;
			}
		}

		public static StringDataSerializeResult Deserialize(Stream stream)
		{
			using (StreamReader streamReader = new StreamReader(stream))
			{
				return DeserializeContent(streamReader);
			}
		}

		private static StringDataSerializeResult DeserializeContent(StreamReader streamReader)
		{
			StringDataSerializeResult result = new StringDataSerializeResult();
			var container = result.Container;

			try
			{
				string content = streamReader.ReadToEnd();

				var root = JArray.Parse(content);
				foreach (JObject element in root)
				{
					var stringData = DeserializeStringData(element);
					if (stringData == null)
						continue;

					bool exists = container.StringDataSet.Add(stringData) ? false : true;
					if (exists == false)
						continue;

					var alreadyStringData = container.StringDataSet.Where(s => s.Equals(stringData)).FirstOrDefault();
					if (alreadyStringData == null)
						continue;

					if (result.DuplicateList.ContainsKey(alreadyStringData) == false)
						result.DuplicateList.Add(alreadyStringData, new List<StringData>() { alreadyStringData });

					result.DuplicateList[alreadyStringData].Add(stringData);

				}

				return result;
			}
			catch (Exception e)
			{
				throw e;
			}
		}

		private static StringData DeserializeStringData(JObject jObject)
		{
			StringData stringData = new StringData();

			foreach (var property in jObject)
			{
				if (property.Key.Equals("Key", StringComparison.OrdinalIgnoreCase) == true)
				{
					stringData.Key = property.Value.ToString();
				}
				else if (property.Key.Equals("Group", StringComparison.OrdinalIgnoreCase) == true)
				{
					stringData.Group = property.Value.ToString();
				}
				else if (property.Key.Equals("Data", StringComparison.OrdinalIgnoreCase) == true)
				{
					stringData.Data = property.Value.ToString();
				}
				else if (property.Key.Equals("Options", StringComparison.OrdinalIgnoreCase) == true &&
					property.Value.Type == JTokenType.Object)
				{
					foreach (JProperty optionProperty in property.Value)
					{
						stringData.Options.Add(optionProperty.Name, optionProperty.Value.ToString());
					}
				}
			}

			return stringData;
		}

		public static FileInfo Serialize(string rootDirectory, StringDataContainer conatiner)
		{
			string fullPath = Path.Combine(rootDirectory, conatiner.FileName);

			FileInfo fileInfo = new FileInfo(fullPath);
			if (fileInfo.Directory.Exists == false)
				throw new DirectoryNotFoundException($"{fileInfo.Directory} not found");

			var serializeContent = JsonConvert.SerializeObject(conatiner.StringDataSet.ToList(), Formatting.Indented);

			using (StreamWriter sw = new StreamWriter(fullPath, false, Encoding.UTF8))
			{
				sw.Write(serializeContent);
				sw.Flush();
			}

			return fileInfo;
		}
	}
}