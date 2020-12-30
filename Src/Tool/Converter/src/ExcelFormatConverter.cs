using GolbengFramework.Parser;
using GolbengFramework.Serialize;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using Excel = Microsoft.Office.Interop.Excel;

namespace GolbengFramework.Converter
{
	public class ExcelFormatConverter : IDisposable
	{
		private bool _isDisposed = false;

		private string _schemaPath = "";
		private string _tablePath = "";

		private EnumsDefines _enumDefines = null;

		private Dictionary<string, (string startIndex, string endIndex)> _enumWriteCellRange = new Dictionary<string, (string startIndex, string endIndex)>();

		private Excel.Application	_application = null;
		private Excel.Workbooks		_workbooks = null;
		private Excel.Workbook		_workbook = null;
		private Excel.Sheets		_worksheets = null;
		private Excel.Worksheet		_dataWorkSheet = null;
		private Excel.Worksheet		_enumWorkSheet = null;

		public ExcelFormatConverter(string schemaFilePath, string tableFilePath, EnumsDefines enumDefines)
		{
			_schemaPath = schemaFilePath;
			_tablePath = tableFilePath;

			_enumDefines = enumDefines;

			_application = new Excel.Application();
			_application.DisplayAlerts = false;
			_workbooks = _application.Workbooks;
		}

		~ExcelFormatConverter()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (_isDisposed == true)
				return;

			if (disposing) { }

			_workbook?.Close(false);
			_workbooks?.Close();
			_application?.Quit();

			if (_enumWorkSheet != null)
			{
				Marshal.ReleaseComObject(_enumWorkSheet);
				_enumWorkSheet = null;
			}

			if (_dataWorkSheet != null)
			{
				Marshal.ReleaseComObject(_dataWorkSheet);
				_dataWorkSheet = null;
			}

			if(_worksheets != null)
			{
				Marshal.ReleaseComObject(_worksheets);
			}

			if (_workbook != null)
			{
				Marshal.ReleaseComObject(_workbook);
				_workbook = null;
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
			_isDisposed = true;
		}

		public void CreateFormatDataTable()
		{
			if (File.Exists(_schemaPath) == false)
				throw new FileNotFoundException($"{_schemaPath}가 없습니다.");

			try 
			{
				_workbook = _workbooks.Add();
				_workbook.SaveAs(_tablePath);
			}
			catch(Exception e)
			{
				Dispose(true);
				throw new AggregateException("CreateFormatDataTable Excpetion", e);
			}
			finally
			{
				_workbook?.Close();

				if(_workbook != null)
				{
					Marshal.ReleaseComObject(_workbook);
					_workbook = null;
				}
			}
		}

		public void FormatConvert()
		{
			if(File.Exists(_schemaPath) == false)
				throw new FileNotFoundException($"{_schemaPath}가 없습니다.");

			if (File.Exists(_tablePath) == false)
				throw new FileNotFoundException($"{_tablePath}가 없습니다.");

			try
			{
				_workbook = _workbooks.Open(_tablePath);
				_worksheets = _workbook.Worksheets;

				// schema 정보 가져오기
				ExcelSchemaData excelSchemaData = null;
				using (SchemaTableParser schemaTableParser = new SchemaTableParser(_schemaPath))
				{
					excelSchemaData = schemaTableParser.Parsing();
				}

				// 사용 Enum목록 가져오기
				var enumTypeList = GetEnumTypeList(excelSchemaData.SchemaFields);
				foreach (var enumType in enumTypeList)
				{
					if (_enumDefines.IsContainEnumType(enumType) == false)
					{
						throw new Exception($"{enumType} EnumType이 정의 되어있지 않습니다.");
					}
				}

				WriteEnumWorkSheet(_workbook, enumTypeList);
				WriteFormatDataWorkSheet(_workbook, excelSchemaData.SchemaFields);

				_workbook.Save();
			}
			catch (Exception e)
			{
				Dispose(true);
				throw new AggregateException("FormatConverter Exception", e);
			}
		}

		private IList<string> GetEnumTypeList(IList<ExcelSchemaField> tableSchemaFields)
		{
			List<string> enumList = new List<string>();
			foreach (var schemaField in tableSchemaFields)
			{
				if(schemaField.GetNativeType() == typeof(Enum))
				{
					enumList.Add(schemaField.Type);
				}
			}

			return enumList;
		}

		private void WriteEnumWorkSheet(Excel.Workbook workbook, IList<string> enumTypeList)
		{
			foreach (Excel.Worksheet workSheet in workbook.Worksheets)
			{
				if (workSheet.Name.Equals("ENUM"))
				{
					_enumWorkSheet = workSheet;
					break;
				}
			}

			if (_enumWorkSheet == null)
			{
				_enumWorkSheet = (Excel.Worksheet)workbook.Worksheets.Add();
				_enumWorkSheet.Name = "ENUM";
			}

			_enumWorkSheet.Visible = Excel.XlSheetVisibility.xlSheetVeryHidden;

			Excel.Range range = _enumWorkSheet.UsedRange;
			range.Delete();

			for(int i = 0; i < enumTypeList.Count; i++)
			{
				string enumType = enumTypeList[i];

				Excel.Range cell = _enumWorkSheet.Cells[1, 1+i] as Excel.Range;
				cell.Value = enumType;

				List<EnumValue> enumValueList = _enumDefines.EnumPool[enumType];
				for (int j = 0; j < enumValueList.Count; j++)
				{
					cell = _enumWorkSheet.Cells[2+j, 1+i] as Excel.Range;
					cell.Value = enumValueList[j].Name;
				}

				string startIndex = string.Format("${0}${1}", (char)('A' + i), 2);
				string endIndex = string.Format("${0}${1}", (char)('A' + i), 2 + enumValueList.Count);

				_enumWriteCellRange.Add(enumType, (startIndex, endIndex));
			}
		}
	
		private void WriteFormatDataWorkSheet(Excel.Workbook workbook, IList<ExcelSchemaField> schemaFields)
		{
			foreach (Excel.Worksheet workSheet in workbook.Worksheets)
			{
				if (workSheet.Name.Equals("DATA"))
				{
					_dataWorkSheet = workSheet;
					break;
				}
			}

			if (_dataWorkSheet == null)
			{
				_dataWorkSheet = (Excel.Worksheet)workbook.Worksheets.Add();
				_dataWorkSheet.Name = "DATA";

				NewFormatDataWorkSheet(_dataWorkSheet, schemaFields);
			}

			try
			{
				// 작성되어 있는 Schema 체크
				int sheetRowCount = _dataWorkSheet.Rows.Count;
				int sheetColumnCount = _dataWorkSheet.UsedRange.Columns.Count;

				// name Columns
				Excel.Range startColumnCell = _dataWorkSheet.Cells[2, 2] as Excel.Range;
				Excel.Range endColumnCell = _dataWorkSheet.Cells[2, sheetColumnCount] as Excel.Range;

				var columnCells = _dataWorkSheet.Range[startColumnCell, endColumnCell].Value2 as object[,];
				for (int column = 1; column <= columnCells.GetLength(1); column++)
				{
					string dataFieldName = columnCells[1, column] as string;

					// 정상적인 필드 이름 체크
					var schemaFiled = GetValidFieldName(dataFieldName, schemaFields);
					if (schemaFiled == null)
						continue;

					// comment 처리
					Excel.Range typeCell = _dataWorkSheet.Cells[3, 1 + column] as Excel.Range;
					typeCell.Comment?.Delete();

					if (schemaFiled.GetNativeType() == typeof(Enum))
					{
						StringBuilder commentBuilder = new StringBuilder();
						var enumValues = _enumDefines.EnumPool[schemaFiled.Type];
						foreach(var enumValue in enumValues)
						{
							commentBuilder.AppendLine($"{enumValue.Name} = {enumValue.Value} // {enumValue.Description}");
						}

						Excel.Comment comment = typeCell.AddComment(commentBuilder.ToString());
						comment.Shape.TextFrame.AutoSize = true;
						
					}

					// data Range
					Excel.Range startCell = _dataWorkSheet.Cells[7, 1 + column] as Excel.Range;
					Excel.Range endCell = _dataWorkSheet.Cells[sheetRowCount, 1 + column] as Excel.Range;

					Excel.Range columnRowsCell = _dataWorkSheet.Range[startCell, endCell];

					SetValidationFiled(columnRowsCell, schemaFiled);
					CheckValidationFiled(columnRowsCell, schemaFiled);
				}
			}
			catch(Exception e)
			{
				throw new AggregateException("WriteFormatDataWorkSheet exception", e);
			}
		}

		private void NewFormatDataWorkSheet(Excel.Worksheet dataWorkSheet, IList<ExcelSchemaField> schemaFields)
		{
			List<string> generateFirstColumns = new List<string>()
			{
				nameof(ExcelSchemaField.Name).ToLower(),
				nameof(ExcelSchemaField.Type).ToLower(),
				nameof(ExcelSchemaField.Default).ToLower(),
				nameof(ExcelSchemaField.Title).ToLower(),
				nameof(ExcelSchemaField.Comment).ToLower(),
			};

			int row = 2;
			foreach(var name in generateFirstColumns)
			{
				Excel.Range range = dataWorkSheet.Cells[row, 1] as Excel.Range;
				range.Value = name;
				row++;
			}

			row = 2;
			int column = 2;
			foreach (var field in schemaFields)
			{
				Excel.Range range = dataWorkSheet.Cells[row, column] as Excel.Range;
				range.Value = field.Name;

				range = dataWorkSheet.Cells[row + 1, column] as Excel.Range;
				range.Value = field.Type;

				range = dataWorkSheet.Cells[row + 2, column] as Excel.Range;
				range.Value = field.Default;

				range = dataWorkSheet.Cells[row + 3, column] as Excel.Range;
				range.Value = field.Title;

				range = dataWorkSheet.Cells[row + 4, column] as Excel.Range;
				range.Value = field.Comment;

				column++;
			}
		}

		private ExcelSchemaField GetValidFieldName(string dataFieldName, IList<ExcelSchemaField> schemaFields)
		{
			if (dataFieldName.Trim().StartsWith("//") == true)
				return null;

			// schema에 정의 되어 있지 않은 값
			ExcelSchemaField findSchemaField = schemaFields.Where(schemaField => schemaField.Name.Equals(dataFieldName)).SingleOrDefault();
			return findSchemaField;
		}

		private void SetValidationFiled(Excel.Range range, ExcelSchemaField tableSchemaField)
		{
			range.Validation.Delete();

			if (tableSchemaField.GetNativeType() == typeof(int))
			{
				range.Validation.Add(Excel.XlDVType.xlValidateDecimal,
					Excel.XlDVAlertStyle.xlValidAlertWarning,
					Excel.XlFormatConditionOperator.xlBetween,
					Int32.MinValue,
					Int32.MaxValue);
			}
			else if(tableSchemaField.GetNativeType() == typeof(uint))
			{
				range.Validation.Add(Excel.XlDVType.xlValidateDecimal,
					Excel.XlDVAlertStyle.xlValidAlertWarning,
					Excel.XlFormatConditionOperator.xlBetween,
					UInt32.MinValue,
					UInt32.MaxValue);
			}
			else if(tableSchemaField.GetNativeType() == typeof(float))
			{
				range.Validation.Add(Excel.XlDVType.xlValidateDecimal,
					Excel.XlDVAlertStyle.xlValidAlertWarning,
					Excel.XlFormatConditionOperator.xlBetween,
					Single.MinValue,
					Single.MaxValue);
			}
			else if(tableSchemaField.GetNativeType() == typeof(string))
			{
				range.Validation.Add(Excel.XlDVType.xlValidateTextLength,
								Excel.XlDVAlertStyle.xlValidAlertWarning,
								Excel.XlFormatConditionOperator.xlLess,
								1024);
			}
			else if(tableSchemaField.GetNativeType() == typeof(bool))
			{
				range.Validation.Add(Excel.XlDVType.xlValidateList,
									Excel.XlDVAlertStyle.xlValidAlertWarning,
									Excel.XlFormatConditionOperator.xlBetween,
									"TRUE, FALSE");

				range.Validation.InCellDropdown = true;
			}
			else if(tableSchemaField.GetNativeType() == typeof(Enum))
			{
				if (_enumWriteCellRange.ContainsKey(tableSchemaField.Type) == false)
					throw new Exception($"Validation 생성 실패, EnumType = {tableSchemaField.Type}이 잘못되었습니다.");

				var enumRange = _enumWriteCellRange[tableSchemaField.Type];
				string formular = $"=ENUM!{enumRange.startIndex}:{enumRange.endIndex}";

				range.Validation.Add(Excel.XlDVType.xlValidateList,
					Excel.XlDVAlertStyle.xlValidAlertWarning,
					Excel.XlFormatConditionOperator.xlBetween,
					formular);

				range.Validation.InCellDropdown = true;
			}
		}
		private void CheckValidationFiled(Excel.Range range, ExcelSchemaField tableSchemaField)
		{
			var rangeValues = range.Value2 as object[,];


			if (tableSchemaField.GetNativeType() == typeof(int))
			{
				foreach(var rowIndex in Enumerable.Range(1, rangeValues.GetLength(0)))
				{
					var value = rangeValues[rowIndex, 1];
					if (value == null)
						continue;

					if(value is int intValue)
					{
						if(intValue <= Int32.MinValue || intValue >= Int32.MaxValue)
						{
							range.Cells[rowIndex, 1] = null;
						}
					}
				}
			}
			else if (tableSchemaField.GetNativeType() == typeof(uint))
			{
				foreach (var rowIndex in Enumerable.Range(1, rangeValues.GetLength(0)))
				{
					var value = rangeValues[rowIndex, 1];
					if (value == null)
						continue;

					if (value is uint uintValue)
					{
						if (uintValue <= UInt32.MinValue || uintValue >= UInt32.MaxValue)
						{
							range.Cells[rowIndex, 1] = null;
						}
					}
				}
			}
			else if (tableSchemaField.GetNativeType() == typeof(float))
			{
				foreach (var rowIndex in Enumerable.Range(1, rangeValues.GetLength(0)))
				{
					var value = rangeValues[rowIndex, 1];
					if (value == null)
						continue;

					if (value is float floatValue)
					{
						if (floatValue <= Single.MinValue || floatValue >= Single.MaxValue)
						{
							range.Cells[rowIndex, 1] = null;
						}
					}
				}
			}
			else if (tableSchemaField.GetNativeType() == typeof(string))
			{
				// string 은 어떤 값도 올수 있다..
			}
			else if (tableSchemaField.GetNativeType() == typeof(bool))
			{
				foreach (var rowIndex in Enumerable.Range(1, rangeValues.GetLength(0)))
				{
					var value = rangeValues[rowIndex, 1];
					if (value == null)
						continue;

					if(value is string strValue)
					{
						if (strValue.Equals("true", StringComparison.OrdinalIgnoreCase) == false &&
							strValue.Equals("false", StringComparison.OrdinalIgnoreCase) == false)
							range.Cells[rowIndex, 1] = null;
					}
				}
			}
			else if (tableSchemaField.GetNativeType() == typeof(Enum))
			{
				var enumValues = _enumDefines.EnumPool[tableSchemaField.Type];

				foreach (var rowIndex in Enumerable.Range(1, rangeValues.GetLength(0)))
				{
					var value = rangeValues[rowIndex, 1];
					if (value == null)
						continue;

					if (value is string enumValue)
					{
						if (enumValues.Where(v => v.Name.Equals(enumValue, StringComparison.OrdinalIgnoreCase)).Any() == false)
							range.Cells[rowIndex, 1] = null;
					}
				}
			}
		}
	
	}
}
