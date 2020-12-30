using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GolbengFramework.Parser
{
	class Program
	{
		static void Main(string[] args)
		{
			// 테스트 코드..

			using (var parser = new SchemaTableParser(@"D:\_Projects_Ing\CookieProject\table\schema\example_table.schema.xlsx"))
			{
				var result = parser.Parsing();
			}

			using (var parser = new DataTableParser(@"D:\_Projects_Ing\CookieProject\table\schema\example_table.schema.xlsx", @"D:\_Projects_Ing\CookieProject\table\example_table.xlsx"))
			{
				var result = parser.Parsing();
			}
		}
	}
}
