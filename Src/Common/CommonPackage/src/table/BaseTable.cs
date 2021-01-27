using CommonPackage.Enums;
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
		public virtual uint primarykey { get; set; } = 0;
		public virtual uint secondarykey { get; set; } = 0;

		public override bool Equals(object obj)
		{
			return obj is TblBase @base &&
				   primarykey == @base.primarykey &&
				   secondarykey == @base.secondarykey;
		}

		public override int GetHashCode()
		{
			int hashCode = -281792184;
			hashCode = hashCode * -1521134295 + primarykey.GetHashCode();
			hashCode = hashCode * -1521134295 + secondarykey.GetHashCode();
			return hashCode;
		}
	}


	/// Example Table
	public class TblTable1 : TblBase
	{
		public override uint primarykey { get; set; }
		public override uint secondarykey { get; set; }

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
		public override uint primarykey { get; set; } = 0;
		public override uint secondarykey { get; set; } = 0;
		public int IntField1 { get; set; } = 0;
		public string StringField2 { get; set; } = "";
		public float FloatField3 { get; set; } = 0;
		public TestEnum EnumField4 { get; set; } = TestEnum.None;
		public static TableMeta TableMeta { get; set; } = new TableMeta()
		{
			TableName = "TblExample_table",
			DbName = "Example_table.db"
		};
	}

}
