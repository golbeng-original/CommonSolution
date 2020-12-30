using GolbengFramework.Converter;
using GolbengFramework.Parser;
using GolbengFramework.ToolUtil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GolbengFramework.GenerateTool
{
	public class TableDataInfo : INotifyPropertyChanged
	{
		public string TableName { get; set; } = "";
		public string DependencySchemaName { get; set; } = "";

		private bool _isChecked = false;
		public bool IsChecked 
		{
			get => _isChecked;
			set
			{
				_isChecked = value;
				OnPropertyChanged("IsChecked");
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string name)
		{
			var handler = PropertyChanged;
			if(handler != null)
			{
				handler(this, new PropertyChangedEventArgs(name));
			}
		}
	}

	public partial class MainWindow : Window
	{
		private List<TableDataInfo> _tableDataInfoList = new List<TableDataInfo>();

		private void InitializeDataList()
		{
			_tableDataInfoList.Clear();

			foreach(var schemaName in _schemaNameList)
			{
				var dataTableFiles = FindDataTableFiles(schemaName.SchemaName);

				dataTableFiles.ForEach((file)=>
				{
					_tableDataInfoList.Add(new TableDataInfo()
					{
						TableName = file.Replace(".xlsx", ""),
						DependencySchemaName = schemaName.SchemaName
					});
				});
			}

			_dataListBox.ItemsSource = _tableDataInfoList;
		}

		private void OnFilterDataList()
		{
			var filterList = _tableDataInfoList.Select(d => d);

			if (string.IsNullOrEmpty(FilterText) == false)
			{
				filterList = filterList.Where(d =>
				{
					if (SelectedFIlterType == FilterType.FileName)
					{
						return d.TableName.ToLower().Contains(FilterText.ToLower());
					}
					else if (SelectedFIlterType == FilterType.EnumName)
					{
						var schemaDataInfo = FindSchemaDataInfo(d.DependencySchemaName);
						if (schemaDataInfo == null)
							return false;

						return schemaDataInfo.EnumTypes.Where(e => e.ToLower().Contains(FilterText.ToLower())).Any();
					}

					return false;
				});
			}

			_dataListBox.ItemsSource = filterList;
		}

		private List<string> FindDataTableFiles(string schemaName)
		{
			List<string> dataTables = new List<string>();

			DirectoryInfo directoryInfo = new DirectoryInfo(TablePath);
			var files = directoryInfo.GetFiles("*.xlsx");
			foreach(var file in files)
			{
				int firstDotIndex = file.Name.IndexOf('.');
				if (firstDotIndex == -1)
					continue;

				string fureFileName = file.Name.Substring(0, firstDotIndex);
				if (fureFileName.Equals(schemaName) == false)
					continue;

				dataTables.Add(file.Name);
			}

			dataTables.Sort((lhs, rhs) =>
			{
				return lhs.Length < rhs.Length ? -1 : 1;
			});

			return dataTables;
		}

		private void OnTableConvert(Button sender)
		{
			ClearLog();

			AddLog($"TableConvert 시작");

			var checkedDataInfoList = _tableDataInfoList.Where(d => d.IsChecked == true);
			if(checkedDataInfoList.Count() == 0)
			{
				MessageBox.Show("선택 된 데이터 테이블이 없습니다.");
				return;
			}

			// schema기준으로 묶기
			Dictionary<string, List<TableDataInfo>> schemaBasisDic = new Dictionary<string, List<TableDataInfo>>();
			foreach(var tableDataInfo in checkedDataInfoList)
			{
				if (schemaBasisDic.ContainsKey(tableDataInfo.DependencySchemaName) == false)
					schemaBasisDic.Add(tableDataInfo.DependencySchemaName, new List<TableDataInfo>());

				schemaBasisDic[tableDataInfo.DependencySchemaName].Add(tableDataInfo);
			}

			foreach(var bundle in schemaBasisDic)
			{
				TableConvertProcess(bundle.Key, bundle.Value);
			}
		}

		private void TableConvertProcess(string schemaName, List<TableDataInfo> tableDataInfos)
		{
			string schemaPath = System.IO.Path.Combine(SchemaPath, $"{schemaName}.schema.xlsx");

			ProgressDialog dialog = new ProgressDialog();
			dialog.DoWorkHandler += (sender, e) =>
			{
				BackgroundWorker worker = sender as BackgroundWorker;

				int currPercent = 0;

				List<ExcelDataTable> excelDataTables = new List<ExcelDataTable>();
				foreach (var tableDataInfo in tableDataInfos)
				{
					string dataPath = System.IO.Path.Combine(TablePath, $"{tableDataInfo.TableName}.xlsx");

					worker.ReportProgress(currPercent, $"{tableDataInfo.TableName} Parsing..");

					using (DataTableParser parser = new DataTableParser(schemaPath, dataPath))
					{
						var excelDataTable = parser.Parsing();
						excelDataTables.Add(excelDataTable);
					}

					currPercent = ProgressDialog.GetPercent(tableDataInfos.IndexOf(tableDataInfo), tableDataInfos.Count, 0, 50);
					worker.ReportProgress(currPercent, $"{tableDataInfo.TableName} Parsing Complete");
				}

				if (excelDataTables.Count == 0)
				{
					AddLog($"[{schemaName}] schema 데이터 테이블 parsing 갯수 = 0 Convert 생략");
					return;
				}

				worker.ReportProgress(currPercent, $"{schemaName} 중복 PirmaryKey 검사 중...");

				if (IsDuplicatePrimary(excelDataTables) == true)
				{
					throw new Exception("PrimaryKey 중복 발생");
				}

				worker.ReportProgress(75, $"{schemaName} 테이블 생성 중");

				try
				{
					ExcelTableConverter clientConverter = new ExcelTableConverter(ClientDbPath, excelDataTables, _enumDefines.Value);
					clientConverter.Convter();
				}
				catch(Exception ex)
				{
					AddLog($"clientConverter Exception : {ex.Message}");
				}

				try
				{
					ExcelTableConverter serverConverter = new ExcelTableConverter(ServerDbPath, excelDataTables, _enumDefines.Value);
					serverConverter.Convter();
				}
				catch (Exception ex)
				{
					AddLog($"serverConverter Exception : {ex.Message}");
				}
			};

			if(dialog.ShowDialog() == false)
			{
				AddLog($"[{schemaName}] schema Exception : {dialog.Exception.Message}");
				return;
			}

			AddLog($"[{schemaName}] TableConvert 완료");
		}

		private bool IsDuplicatePrimary(List<ExcelDataTable> excelDataTables)
		{
			if (excelDataTables.Count == 0)
				return false;

			Dictionary<string, int> primaryCount = new Dictionary<string, int>();

			foreach (var excelDataTable in excelDataTables)
			{
				foreach(var descriptionRow in excelDataTable.PrimaryDescriptionRows())
				{
					if (primaryCount.ContainsKey(descriptionRow) == false)
					{
						primaryCount.Add(descriptionRow, 0);
						continue;
					}

					primaryCount[descriptionRow]++;
				}
			}

			var duplicateList = primaryCount.Where(keyValue =>
			{
				return keyValue.Value > 0;
			})
			.ToList();

			foreach(var duplicate in duplicateList)
			{
				AddLog($"Primary Duplicate 감지 : {duplicate.Key}  (중복 갯수 : {duplicate.Value + 1})");
			}

			return duplicateList.Count > 0 ? true : false;
		}

		private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
		{
			CheckBox checkBox = sender as CheckBox;
			if (checkBox == null)
				return;

			string schemaName = checkBox.Tag as string;
			if (schemaName == null)
				return;

			var findList = _tableDataInfoList.Where(d => d.DependencySchemaName.Equals(schemaName));

			foreach (var dataInfo in findList)
			{
				dataInfo.IsChecked = false;
			}
		}

		private void CheckBox_Checked(object sender, RoutedEventArgs e)
		{
			CheckBox checkBox = sender as CheckBox;
			if (checkBox == null)
				return;

			string schemaName = checkBox.Tag as string;
			if (schemaName == null)
				return;

			var findList = _tableDataInfoList.Where(d => d.DependencySchemaName.Equals(schemaName));

			foreach(var dataInfo in findList)
			{
				dataInfo.IsChecked = true;
			}
		}
	}
}
