using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPackage.Tables
{
	public partial class GenerateTablesMeta
	{
		public static Dictionary<Type, TableMeta> TableMetaMapping = new Dictionary<Type, TableMeta>();

		static GenerateTablesMeta()
		{
			TableMetaMapping.Add(typeof(TblTable1), new TableMeta()
			{
				TableName = "TblTable1",
				DbName = "Table1.db",
				ClientDbName = "Table1.bytes"
			});

			TableMetaMapping.Add(typeof(TblTable2), new TableMeta()
			{
				TableName = "TblTable2",
				DbName = "Table2.db",
				ClientDbName = "Table2.bytes"
			});

			InitalizeGenerateTableMeta();
		}
	}
}
