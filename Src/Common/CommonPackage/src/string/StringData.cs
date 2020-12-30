using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

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

			StringDataSerializeResult result = new StringDataSerializeResult();

			var container = result.Container;
			container.FileName = fileInfo.Name;

			using (StreamReader sr = new StreamReader(filePath))
			{
				var content = sr.ReadToEnd();

				try
				{
					var stringDataList = JsonConvert.DeserializeObject<List<StringData>>(content);

					foreach(var stringData in stringDataList)
					{
						bool exists = container.StringDataSet.Add(stringData) ? false : true;
						if (exists == false)
							continue;

						var alreadyStringData = container.StringDataSet.Where(s => s.Equals(stringData)).FirstOrDefault();
						if (alreadyStringData == null)
							continue;

						if(result.DuplicateList.ContainsKey(alreadyStringData) == false)
							result.DuplicateList.Add(alreadyStringData, new List<StringData>() { alreadyStringData });

						result.DuplicateList[alreadyStringData].Add(stringData);
					}

				}
				catch(Exception e)
				{
					throw new AggregateException($"{filePath} Deserialize Error", e);
				}
			}

			return result;
		}
	
		public static FileInfo Serialize(string rootDirectory, StringDataContainer conatiner)
		{
			string fullPath = Path.Combine(rootDirectory, conatiner.FileName);

			FileInfo fileInfo = new FileInfo(fullPath);
			if (fileInfo.Directory.Exists == false)
				throw new DirectoryNotFoundException($"{fileInfo.Directory} not found");

			var serializeContent = JsonConvert.SerializeObject(conatiner.StringDataSet.ToList(), Formatting.Indented);

			using(StreamWriter sw = new StreamWriter(fullPath, false, Encoding.UTF8))
			{
				sw.Write(serializeContent);
				sw.Flush();
			}

			return fileInfo;
		}
	}
}