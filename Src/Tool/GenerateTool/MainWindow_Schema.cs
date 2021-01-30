using GolbengFramework.Converter;
using GolbengFramework.Generator;
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
	public class SchemaDataInfo
	{
		public SchemaDataInfo(string schemaFilePath)
		{
			SchemaFilePath = schemaFilePath;

			FileInfo fileInfo = new FileInfo(schemaFilePath);
			SchemaName = fileInfo.Name.Replace(".schema.xlsx", "");
		}

		public string SchemaFilePath { get; private set; } = "";

		public string SchemaName { get; private set; } = "";

		public List<string> EnumTypes { get; private set; } = new List<string>();
	}

	public partial class MainWindow : Window
	{
		private List<SchemaDataInfo> _schemaNameList = new List<SchemaDataInfo>();

		private void InitializeSchmeList()
		{
			_schemaNameList.Clear();

			DirectoryInfo directoryInfo = new DirectoryInfo(SchemaPath);
			var files = directoryInfo.GetFiles("*.schema.xlsx");

			var fileList = files.Where(f => f.Name.StartsWith("~$") == false).ToList();

			ProgressDialog dialog = new ProgressDialog();
			dialog.DoWorkHandler += (sender, e) =>
			{
				BackgroundWorker worker = sender as BackgroundWorker;

				int currPercent = 0;
				for(int i = 0; i < fileList.Count; i++)
				{
					worker.ReportProgress(currPercent, $"{files[i].Name} schema 내용 검사 중..");

					var schemaDataInfo = new SchemaDataInfo(files[i].FullName);

					try
					{
						using (var parser = new SchemaTableParser(files[i].FullName))
						{
							var schemaData = parser.Parsing();
							foreach (var field in schemaData.SchemaFields)
							{
								if (field.GetNativeType() == typeof(Enum))
									schemaDataInfo.EnumTypes.Add(field.Type);
							}
						}

						_schemaNameList.Add(schemaDataInfo);
					}
					catch(Exception ex)
					{
						AddLog($"[{schemaDataInfo.SchemaName}] [{ex.Message}] [inner: {ex.InnerException?.Message}]");
					}

					currPercent = ProgressDialog.GetPercent(i + 1, files.Length);
					worker.ReportProgress(currPercent, $"{files[i].Name} schema 내용 검사 완료");
				}
			};

			dialog.ShowDialog();

			_schemaListBox.ItemsSource = _schemaNameList;
			_schemaListBox.Items.Refresh();
		}

		private void OnFilterSchemaList()
		{
			var filterList = _schemaNameList.Select(s => s);

			if (string.IsNullOrEmpty(FilterText) == false)
			{
				filterList = filterList.Where(s =>
				{
					if (SelectedFIlterType == FilterType.FileName)
					{
						return s.SchemaName.ToLower().Contains(FilterText.ToLower());
					}
					else if (SelectedFIlterType == FilterType.EnumName)
					{
						return s.EnumTypes.Where(e => e.ToLower().Contains(FilterText.ToLower())).Any();
					}

					return false;
				});
			}

			_schemaListBox.ItemsSource = filterList;
		}

		private void OnFormatSync(string schemaName, string tableName, bool IsNew = false)
		{
			try
			{
				ClearLog();

				AddLog($"{schemaName} Formay Sync Start");

				string schemaPath = System.IO.Path.Combine(SchemaPath, $"{schemaName}.schema.xlsx");
				string tablePath = System.IO.Path.Combine(TablePath, $"{tableName}.xlsx");

				using (ExcelFormatConverter convert = new ExcelFormatConverter(schemaPath, tablePath, _enumDefines.Value))
				{
					if (IsNew == true)
						convert.CreateFormatDataTable();

					convert.FormatConvert();
				}

				AddLog($"{schemaName} Formay Sync Complete");
			}
			catch(AggregateException e)
			{
				AddLog(e.Message);
				AddLog(e.InnerException?.Message);
			}
			catch(Exception e)
			{
				AddLog(e.Message);
			}
		}
		
		private void OnClickSchemaTableFormatSync(object sender, RoutedEventArgs e)
		{
			Button button = sender as Button;
			if (button == null || button.Tag is string == false)
				return;

			string schemaName = button.Tag as string;
			var findDataTableFiles = FindDataTableFiles(schemaName);

			ProgressDialog dialog = new ProgressDialog();

			// 생성 되어있는 DataTable 파일이 없다.. 새로 생성
			if (findDataTableFiles.Count == 0)
			{
				dialog.DoWorkHandler += (workerSender, workerArgs) =>
				{
					BackgroundWorker worker = workerSender as BackgroundWorker;

					worker.ReportProgress(0, $"{schemaName} 데이터 엑셀 생성 중..");

					OnFormatSync(schemaName, schemaName, true);

					worker.ReportProgress(100, $"{schemaName} 데이터 엑셀 생성 완료");
				};
			}
			else
			{
				dialog.DoWorkHandler += (workerSender, workerArgs) =>
				{
					BackgroundWorker worker = workerSender as BackgroundWorker;

					int currPercent = 0;
					for (int i = 0; i < findDataTableFiles.Count; i++)
					{
						string dataFileName = findDataTableFiles[i];
						dataFileName = dataFileName.Replace(".xlsx", "");

						worker.ReportProgress(currPercent, $"{dataFileName} 데이터 엑셀 format sync 중..");
						
						OnFormatSync(schemaName, dataFileName);

						currPercent = (int)(((float)(i+1) / (float)findDataTableFiles.Count) * 100.0f);
						worker.ReportProgress(currPercent, $"{dataFileName} 데이터 엑셀 format sync 완료");
					}
				};
			}

			dialog.ShowDialog();
		}

		private SchemaDataInfo FindSchemaDataInfo(string schemaName)
		{
			return _schemaNameList.Where(s => s.SchemaName.Equals(schemaName)).SingleOrDefault();
		}
	}
}
