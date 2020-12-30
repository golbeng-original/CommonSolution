using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;

namespace GolbengFramework.Parser
{
	public class ExcelDataTable
	{
		private List<List<object>> _rows = new List<List<object>>();

		public ExcelDataTable()
		{
			_primaryKeyIndex = new Lazy<int>(() =>
			{
				return DataColumns.FindIndex(column => column.Equals("PrimaryKey", StringComparison.OrdinalIgnoreCase));
			});
		}

		public bool AddRow(List<object> row)
		{
			if (DataColumns.Count != row.Count)
				return false;

			_rows.Add(row);
			return true;
		}

		public int GetColumnIndex(string column)
		{
			return DataColumns.FindIndex(c => c.Equals(column));
		}

		public IEnumerable<List<object>> GetRawRows()
		{
			foreach(var row in _rows)
			{
				yield return row;
			}
		}

		public IEnumerable<List<(string ColumnName, object Value)>> GetMappingRows()
		{
			foreach (var row in _rows)
			{
				List<(string ColumnName, object Value)> mappingRow = new List<(string ColumnName, object Value)>();
				foreach(var schema in SchemaData.SchemaFields)
				{
					int index = DataColumns.IndexOf(schema.Name);
					if(index == -1)
					{
						mappingRow.Add((schema.Name, schema.GetNativeDefault()));
						continue;
					}

					mappingRow.Add((schema.Name, row[index]));
				}

				yield return mappingRow;
			}
		}

		public IEnumerable<string> PrimaryDescriptionRows()
		{
			foreach(var rows in GetMappingRows())
			{
				List<string> primaryColumns = new List<string>();
				foreach (var row in rows)
				{
					if (SchemaData.IsPrimary(row.ColumnName) == false)
						continue;

					primaryColumns.Add($"{row.ColumnName}:{row.Value.ToString()}");
				}

				yield return string.Join(",", primaryColumns);
			}
		}

		public ExcelSchemaData SchemaData { get; set; }

		public List<string> DataColumns { get; private set; } = new List<string>();

		public int RowCount { get => _rows.Count; }

		public List<object> this[int rowIndex] { get => _rows[rowIndex]; }

		private Lazy<int> _primaryKeyIndex = null;
		public int PrimaryKeyIndex { get => _primaryKeyIndex?.Value ?? -1; }
	}

	public class DataTableParser : IDisposable
	{
		private bool _disposed = false;

		private Excel.Application _application = null;
		private Excel.Workbook _workBook = null;
		private Excel.Worksheet _dataWorkSheet = null;

		private string _schemaExcelFilePath = "";
		private string _tableExcelFilePath = "";

		private void Dispose(bool disposing)
		{
			if (_disposed == true)
				return;

			// 여기선 dispose 객체가 없다.
			if (disposing == true) { }

			_workBook?.Close();
			_application?.Quit();

			if (_dataWorkSheet != null)
			{
				Marshal.ReleaseComObject(_dataWorkSheet);
				_dataWorkSheet = null;
			}

			if (_workBook != null)
			{
				Marshal.ReleaseComObject(_workBook);
				_workBook = null;
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

		public DataTableParser(string schemaExcelFilePath, string tableExcelFilePath)
		{
			_schemaExcelFilePath = schemaExcelFilePath;
			_tableExcelFilePath = tableExcelFilePath;
		}

		~DataTableParser()
		{
			Dispose(false);
		}

		public ExcelDataTable Parsing()
		{
			ExcelSchemaData excelSchemaData = null;
			using (SchemaTableParser schemaTableParser = new SchemaTableParser(_schemaExcelFilePath))
			{
				try
				{
					excelSchemaData = schemaTableParser.Parsing();
				}
				catch (Exception e)
				{
					throw new AggregateException("schema Parsing Exception", e);
				}
			}

			_application = new Excel.Application();

			try
			{
				_workBook = _application.Workbooks.Open(_tableExcelFilePath);
				_dataWorkSheet = _workBook.Worksheets["DATA"];
				_dataWorkSheet.Activate();

				return ParsingDataRows(excelSchemaData, _dataWorkSheet);
			}
			catch(Exception e)
			{
				Dispose(true);
				throw new AggregateException("data worksheet open failed", e);
			}
		}

		private List<string> GetDataColumns(Excel.Worksheet dataWorksheet)
		{
			int columnCount = dataWorksheet.UsedRange.Columns.Count;

			Excel.Range columnStartCell = dataWorksheet.Cells[2, 2];
			Excel.Range columnEndCell = dataWorksheet.Cells[2, columnCount];

			var columnCells = dataWorksheet.Range[columnStartCell, columnEndCell].Value2 as object[,];

			List<string> columnNames = new List<string>();
			foreach (var columnCell in columnCells)
			{
				string columnStr = columnCell as string;
				if (columnStr == null)
					continue;

				// 컬럼 무시 체크
				columnStr = columnStr.Trim();
				if (columnStr.StartsWith("//"))
					columnStr = null;

				columnNames.Add(columnStr);
			}

			return columnNames;
		}

		private ExcelDataTable ParsingDataRows(ExcelSchemaData excelSchemaData, Excel.Worksheet dataWorksheet)
		{
			try
			{
				List<string> dataColumns = GetDataColumns(dataWorksheet);

				ExcelDataTable dataTableResult = new ExcelDataTable();
				dataTableResult.SchemaData = excelSchemaData;

				var schemaFieldDic = excelSchemaData.SchemaFields.ToDictionary<ExcelSchemaField, string>((field) =>
				{
					return field.Name;
				});

				foreach(var dataColumn in dataColumns)
				{
					if (string.IsNullOrEmpty(dataColumn) == true)
						continue;

					dataTableResult.DataColumns.Add(dataColumn);
				}

				//
				int columnCount = dataWorksheet.UsedRange.Columns.Count;
				int rowCount = dataWorksheet.Rows.Count;

				Excel.Range dataStartCell = dataWorksheet.Cells[7, 1];
				Excel.Range dataEndCell = dataWorksheet.Cells[rowCount, columnCount];

				var dataRange = dataWorksheet.Range[dataStartCell, dataEndCell].Value2 as object[,];

				for(int rowIndex = 1; rowIndex <= dataRange.GetLength(0); rowIndex++)
				{
					// 무시 체크
					var ignoreValue = dataRange[rowIndex, 1] as string;
					if (ignoreValue?.Trim().StartsWith("//") == true)
						continue;

					var primaryValue = dataRange[rowIndex, dataTableResult.PrimaryKeyIndex + 2];
					if (primaryValue == null)
						continue;

					List<object> row = new List<object>();
					for (int columnIndex = 2; columnIndex <= dataRange.GetLength(1); columnIndex++)
					{
						var columnName = dataColumns[columnIndex - 2];
						if (columnName == null)
							continue;

						object value = dataRange[rowIndex, columnIndex];
						var schemaField = schemaFieldDic[columnName];

						if(value == null)
							value = schemaField.GetNativeDefault();

						row.Add(value);
					}

					dataTableResult.AddRow(row);
				}

				return dataTableResult;
			}
			catch(Exception e)
			{
				throw new AggregateException("ParsingDataRows Exception", e);
			}
		}
	}
}
