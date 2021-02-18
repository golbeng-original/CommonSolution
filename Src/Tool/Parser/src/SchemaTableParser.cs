using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Excel = Microsoft.Office.Interop.Excel;

namespace GolbengFramework.Parser
{
	public class ExcelSchemaField
	{
		public string Name { get; set; } = "";
		public string Type { get; set; } = "";
		public string Default { get; set; } = "";
		public string Title { get; set; } = "";
		public string Comment { get; set; } = "";
		public bool Primary { get; set; } = false;

		public Type GetNativeType()
		{
			if (string.IsNullOrEmpty(Type) == true)
				return null;

			if (Type.Equals("uint", StringComparison.OrdinalIgnoreCase))
			{
				return typeof(uint);
			}
			else if (Type.Equals("int", StringComparison.OrdinalIgnoreCase))
			{
				return typeof(int);
			}
			else if (Type.Equals("bool", StringComparison.OrdinalIgnoreCase))
			{
				return typeof(bool);
			}
			else if (Type.Equals("float", StringComparison.OrdinalIgnoreCase))
			{
				return typeof(float);
			}
			else if (Type.Equals("string", StringComparison.OrdinalIgnoreCase))
			{
				return typeof(string);
			}

			return typeof(Enum);
		}

		public string GetSqliteType()
		{
			if (string.IsNullOrEmpty(Type) == true)
				return null;

			if (GetNativeType() == typeof(uint) ||
				GetNativeType() == typeof(int) ||
				GetNativeType() == typeof(bool) ||
				GetNativeType() == typeof(Enum))
			{
				return "Integer";
			}
			else if (GetNativeType() == typeof(float))
			{
				return "Real";
			}
			else if (GetNativeType() == typeof(string))
			{
				return "Text";
			}

			return null;
		}

		public object GetNativeDefault()
		{
			if (GetNativeType() == typeof(uint))
			{
				return UInt32.Parse(Default);
			}
			else if (GetNativeType() == typeof(int))
			{
				return Int32.Parse(Default);
			}
			else if (GetNativeType() == typeof(float))
			{
				return Single.Parse(Default);
			}
			else if (GetNativeType() == typeof(bool))
			{
				return Boolean.Parse(Default);
			}

			return Default;
		}
	}

	public class ExcelSchemaData
	{
		public string SchemaName { get; set; } = "";

		public string TableName { get => $"Tbl{SchemaName.First().ToString().ToUpper()}{SchemaName.Substring(1)}"; }

		public string DbName { get => $"{SchemaName.First().ToString().ToUpper()}{SchemaName.Substring(1)}.db"; }

		public string ClientUseDbName { get => $"{SchemaName.First().ToString().ToUpper()}{SchemaName.Substring(1)}.bytes"; }

		public ExcelSchemaField FindExcelSchemaField(string fieldName)
		{
			return SchemaFields.Where(s => s.Name.Equals(fieldName)).SingleOrDefault();
		}

		public bool IsPrimary(string FieldName)
		{
			var field = FindExcelSchemaField(FieldName);
			if (field == null)
				return false;

			return field.Primary;
		}

		public IList<ExcelSchemaField> SchemaFields { get; set; }
	}

	public class SchemaTableParser : IDisposable
	{
		private bool _disposed = false;

		private Excel.Application _application = null;
		private Excel.Workbooks _workbooks = null;
		private Excel.Workbook _workBook = null;
		private Excel.Sheets _worksheets = null;
		private Excel.Worksheet _schemaWorkSheet = null;

		private string _schemaFilePath = "";
		private string _schemaName = "";

		private void Dispose(bool disposing)
		{
			if (_disposed == true)
				return;

			// 여기선 dispose 객체가 없다.
			if (disposing == true) { }

			_workBook?.Close();
			_workbooks?.Close();
			_application?.Quit();

			if (_schemaWorkSheet != null)
			{
				Marshal.ReleaseComObject(_schemaWorkSheet);
				_schemaWorkSheet = null;
			}

			if(_worksheets != null)
			{
				Marshal.ReleaseComObject(_worksheets);
				_worksheets = null;
			}

			if (_workBook != null)
			{
				Marshal.ReleaseComObject(_workBook);
				_workBook = null;
			}

			if(_workbooks != null)
			{
				Marshal.ReleaseComObject(_workbooks);
				_workbooks = null;
			}

			if (_application != null)
			{
				Marshal.ReleaseComObject(_application);
				_application = null;
			}

			GC.Collect();

			_disposed = true;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public SchemaTableParser(string schemaExcelFilePath)
		{
			FileInfo fileInfo = new FileInfo(schemaExcelFilePath);
			if (fileInfo.Exists == false)
				throw new FileNotFoundException($"{schemaExcelFilePath}가 없습니다.");

			_schemaFilePath = schemaExcelFilePath;

			string schemaFileName = fileInfo.Name;
			if(schemaFileName.EndsWith(".schema.xlsx") == false)
				throw new ArgumentException($"{_schemaFilePath}은 스키마파일이 아닙니다.");

			int findIndex = schemaFileName.IndexOf(".schema.xlsx");
			_schemaName = schemaFileName.Substring(0, findIndex);
		}

		~SchemaTableParser()
		{
			Dispose(false);
		}

		public ExcelSchemaData Parsing()
		{
			_application = new Excel.Application();

			try
			{
				_workbooks = _application.Workbooks;

				_workBook = _workbooks.Open(_schemaFilePath);
				_worksheets = _workBook.Worksheets;
				_schemaWorkSheet = _worksheets["SCHEMA"];
				_schemaWorkSheet.Activate();

				ExcelSchemaData schemaData = new ExcelSchemaData();
				schemaData.SchemaName = _schemaName;

				var range = _schemaWorkSheet.UsedRange.Value2 as object[,];
				var fields = GetFields(range);

				schemaData.SchemaFields = fields;

				return schemaData;
			}
			catch (Exception e)
			{
				Dispose(true);
				throw new AggregateException("schema worksheet open failed", e);
			}
		}

		private IList<ExcelSchemaField> GetFields(object[,] range)
		{
			int columnCount = range.GetLength(1);

			Dictionary<string, int> columnOrdinal = new Dictionary<string, int>();
			for (int column = 1; column <= columnCount; column++)
			{
				string cellValue = range[1, column] as string;
				if (cellValue == null)
					continue;

				columnOrdinal.Add(cellValue.ToLower(), column);
			}

			if (CheckSchemaFields<ExcelSchemaField>(columnOrdinal.Keys) == false)
				throw new Exception("schema 구성이 잘못 되었습니다.");

			List<ExcelSchemaField> fileds = new List<ExcelSchemaField>();

			int rowCount = range.GetLength(0);
			for (int row = 2; row <= rowCount; row++)
			{
				ExcelSchemaField field = new ExcelSchemaField();

				var propertyArr = typeof(ExcelSchemaField).GetProperties(BindingFlags.Public | BindingFlags.Instance);
				var poroperties = propertyArr.Where(property => !property.Name.Equals("Default", StringComparison.OrdinalIgnoreCase));

				// Default는 마지막에..
				foreach (var property in poroperties)
				{
					if (columnOrdinal.ContainsKey(property.Name.ToLower()) == false)
						continue;

					int ordinal = columnOrdinal[property.Name.ToLower()];
					object cellValue = range[row, ordinal];

					// Name, Type 프로퍼티는 무조건 있어야 한다..
					if (property.Name.Equals(nameof(ExcelSchemaField.Name), StringComparison.OrdinalIgnoreCase) == true ||
						property.Name.Equals(nameof(ExcelSchemaField.Type), StringComparison.OrdinalIgnoreCase) == true)
					{
						if (cellValue == null)
							throw new Exception($"{property.Name} 값이 정의 되어있지 않습니다.");
					}
					else
					{
						if (cellValue == null)
						{
							if (property.PropertyType == typeof(string))
								cellValue = "";
							else if (property.PropertyType == typeof(bool))
								cellValue = false;
						}
					}

					property.SetValue(field, cellValue);
				}

				poroperties = propertyArr.Where(property => property.Name.Equals("Default", StringComparison.OrdinalIgnoreCase));
				// Default만 체크
				foreach (var property in poroperties)
				{
					if (columnOrdinal.ContainsKey(property.Name.ToLower()) == false)
						continue;

					int ordinal = columnOrdinal[property.Name.ToLower()];
					object cellValue = range[row, ordinal];

					if (cellValue == null)
					{
						switch (field.Type.ToLower())
						{
							case "int": cellValue = 0; break;
							case "uint": cellValue = 0; break;
							case "float": cellValue = 0; break;
							case "bool": cellValue = "FALSE"; break;
							default: cellValue = ""; break;
						}
					}

					property.SetValue(field, $"{cellValue}");
				}

				fileds.Add(field);
			}

			return fileds;

		}

		private bool CheckSchemaFields<T>(IEnumerable<string> schemaFieldNames)
		{
			Type type = typeof(T);

			var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

			foreach (var property in properties)
			{
				bool any = schemaFieldNames.Where(name => name.Equals(property.Name, StringComparison.OrdinalIgnoreCase)).Any();

				if (any == false)
					return false;
			}

			return true;
		}
	}
}
