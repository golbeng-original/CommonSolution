using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CommonPackage.Tables
{
	public class TableUtils
	{
		public static bool IsTableType<T>()
		{
			return GetTableMeta<T>() != null ? true : false;
		}

		public static TableMeta GetTableMeta<T>()
		{
			Type type = typeof(T);

			if (GenerateTablesMeta.TableMetaMapping.ContainsKey(type) == false)
				return null;

			TableMeta metaData = GenerateTablesMeta.TableMetaMapping[type];
			return metaData;
		}
	}
}
