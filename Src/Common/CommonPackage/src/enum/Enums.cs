using System;
using System.ComponentModel;

namespace CommonPackage.Enums
{
	public enum TestEnum
	{
		[Description("None")]
		None,
		[Description("Value_1")]
		Value_1 = 4,
		[Description("Value_2")]
		Value_2,
		[Description("Max")]
		Max,
	}

	public enum TestEnum2
	{
		[Description("None")]
		None,
		[Description("None")]
		XXX = 5,
		[Description("None")]
		YYY,
		[Description("Max")]
		Max
	}
}
