using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GolbengFramework.Parser
{
	public class EnumField
	{
		public string Name { get; set; }
		public int Value { get; set; }
		public string Description { get; set; } = "";
	}

	public class EnumMetaInfo
	{
		public EnumMetaInfo(string enumName)
		{
			EnumName = enumName;
		}

		public string EnumName { get; set; }

		public IList<EnumField> EnumFileds { get; private set; } = new List<EnumField>();
	}

	public class EnumSourceParser
	{
		private string _enumDllPath = "";
		private string _targetNamespace = "";
		public EnumSourceParser(string enumDllPath, string targetNamespace)
		{
			_enumDllPath = enumDllPath;
			_targetNamespace = targetNamespace;
		}

		public IList<EnumMetaInfo> Parsing()
		{
			var dll = Assembly.LoadFrom(_enumDllPath);
			var types = dll.GetExportedTypes();

			var enumTypes = from type in types
							where type.Namespace.Equals(_targetNamespace, StringComparison.OrdinalIgnoreCase) == true &&
									type.IsEnum == true
							select type;

			List<EnumMetaInfo> enumMetaInfo = new List<EnumMetaInfo>();

			foreach(var enumType in enumTypes)
			{
				EnumMetaInfo newEnumMetaInfo = new EnumMetaInfo(enumType.Name);

				//Type enumUnderlyingType = Enum.GetUnderlyingType(enumType);

				foreach(var enumValues in Enum.GetValues(enumType))
				{
					string name = enumValues.ToString();
					int? value = System.Convert.ChangeType(enumValues, typeof(int)) as int?;
					var description = enumType.GetField(name).GetCustomAttribute<DescriptionAttribute>();

					//var description = enumValues.GetType().GetCustomAttribute<DescriptionAttribute>();

					EnumField field = new EnumField()
					{
						Name = name,
						Value = value ?? 0,
						Description = description?.Description ?? ""
					};

					newEnumMetaInfo.EnumFileds.Add(field);
				}

				enumMetaInfo.Add(newEnumMetaInfo);
			}

			return enumMetaInfo;
		}
	}
}
