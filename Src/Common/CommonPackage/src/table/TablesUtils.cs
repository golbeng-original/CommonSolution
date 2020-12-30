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

			var property = type.GetProperty("TableMeta", BindingFlags.Public | BindingFlags.Static);
			if (property == null)
				return null;

			var methodInfo = property.GetGetMethod();

			return methodInfo?.Invoke(null, null) as TableMeta;
		}
	}
}
