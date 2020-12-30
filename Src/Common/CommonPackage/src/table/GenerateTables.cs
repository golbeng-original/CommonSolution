using CommonPackage.Enums;
namespace CommonPackage.Tables
{
public class TblExample_table : TblBase
{
	public override int PrimaryKey { get; set; } = 0;
	public override int SecondaryKey { get; set; } = 0;
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
