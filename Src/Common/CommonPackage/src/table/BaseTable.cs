using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPackage.Tables
{
	public class TableMeta
	{
		public string TableName { get; set; } = "";
		public string DbName { get; set; } = "";
	}

	public class TblBase
	{
		public virtual int PrimaryKey { get; set; } = 0;
		public virtual int SecondaryKey { get; set; } = 0;

		public override bool Equals(object obj)
		{
			return obj is TblBase @base &&
				   PrimaryKey == @base.PrimaryKey &&
				   SecondaryKey == @base.SecondaryKey;
		}

		public override int GetHashCode()
		{
			int hashCode = -281792184;
			hashCode = hashCode * -1521134295 + PrimaryKey.GetHashCode();
			hashCode = hashCode * -1521134295 + SecondaryKey.GetHashCode();
			return hashCode;
		}
	}


	/// Example Table
	public class TblTable1 : TblBase
	{
		public int Id { get; set; }
		public string Name { get; set; }

		public static TableMeta TableMeta { get; set; } = new TableMeta()
		{
			TableName = "TblTable1",
			DbName = "Table1.db"
		};
	}

	public class TblTable2 : TblBase
	{
		public int Id { get; set; }
		public string Desc { get; set; }

		public static TableMeta TableMeta { get; set; } = new TableMeta()
		{
			TableName = "TblTable2",
			DbName = "Table2.db"
		};
	}
}
