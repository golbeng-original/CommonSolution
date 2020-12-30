using System.Collections.Generic;
using System.Linq;

namespace CommonPackage.String
{
	public class StringDataFilterOption
	{
		public string FilterKey { get; set; }

		public string FilterData { get; set; }
	}


	public static class StringDataExtenstion
	{
		public static IEnumerable<StringData> FilterStringDatas(this StringDataContainer conatiner, StringDataFilterOption option)
		{
			var datas = conatiner.StringDataSet.Select(s => s);

			if (option != null && string.IsNullOrEmpty(option.FilterKey) == false)
			{
				datas = datas.Where(s => s.Key.Contains(option.FilterKey));
			}

			if (option != null && string.IsNullOrEmpty(option.FilterData) == false)
			{
				datas = datas.Where(s => s.Data.Contains(option.FilterData));
			}

			return datas;
		}

		public static bool AddStringData(this StringDataContainer conatiner, StringData stringData)
		{
			if( conatiner.StringDataSet.Contains(stringData) == true)
				return false;

			conatiner.StringDataSet.Add(stringData);
			return true;
		}

		public static void RemoveStringData(this StringDataContainer container, StringData stringData)
		{
			container.StringDataSet.Remove(stringData);
		}

		public static bool IsDeepEquals(this StringData stringData, StringData rhs)
		{
			if (stringData.Key.Equals(rhs.Key) == false)
				return false;

			if (stringData.Data.Equals(rhs.Data) == false)
				return false;

			if (stringData.Group.Equals(rhs.Group) == false)
				return false;

			if (stringData.Options.Keys.Count != rhs.Options.Keys.Count)
				return false;

			foreach(var key in stringData.Options.Keys)
			{
				if (rhs.Options.ContainsKey(key) == false)
					return false;

				if (stringData.Options[key].Equals(rhs.Options[key]) == false)
					return false;
			}

			return true;
		}
	}
}
