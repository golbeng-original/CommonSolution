using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace GolbengFramework.Serialize
{
	public class EnumValue
	{
		public string Name { get; set; }
		public int Value { get; set; }
		public string Description { get; set; }
	}

	public class EnumsDefines
	{
		public bool IsContainEnumType(string enumTypeName)
		{
			return EnumPool.ContainsKey(enumTypeName);
		}

		public bool IsContainEnumValue(string enumTypeName, string enumValueName)
		{
			if (IsContainEnumType(enumTypeName) == false)
				return false;

			return EnumPool[enumTypeName].Where(e => e.Name.Equals(enumValueName)).Any();
		}

		public EnumValue ParseEnumvalue(string enumTypeName, string enumValueName)
		{
			if (IsContainEnumType(enumTypeName) == false)
				return null;

			return EnumPool[enumTypeName].Where(e => e.Name.Equals(enumValueName)).SingleOrDefault();
		}

		public Dictionary<string, List<EnumValue>> EnumPool { get; private set; } = new Dictionary<string, List<EnumValue>>();
	}

	public class EnumSerialize
	{
		public static EnumsDefines Serialize(string jsonFilePath)
		{
			if (File.Exists(jsonFilePath) == false)
				throw new FileNotFoundException($"{jsonFilePath}가 없습니다.");

			EnumsDefines defined = new EnumsDefines();

			using (StreamReader reader = new StreamReader(jsonFilePath))
			{
				
				var array = JArray.Parse(reader.ReadToEnd());

				foreach(var element in array)
				{
					var enumType = element["name"].Value<string>();

					var values = element["values"].Value<JArray>();

					List<EnumValue> serializeValues = new List<EnumValue>();
					foreach (var value in values)
					{
						EnumValue serializeValue = new EnumValue()
						{
							Name = value["name"].Value<string>(),
							Value = value["value"].Value<int>(),
							Description = value["description"].Value<string>()
						};

						serializeValues.Add(serializeValue);
					};

					defined.EnumPool.Add(enumType, serializeValues);
				}
			}

			return defined;

		}
	}
}
