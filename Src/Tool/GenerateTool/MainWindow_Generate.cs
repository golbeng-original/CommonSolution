using GolbengFramework.Generator;
using GolbengFramework.Serialize;
using GolbengFramework.ToolUtil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GolbengFramework.GenerateTool
{
	public partial class MainWindow : Window
	{
		private void OnSourceGenerator(Button sender)
		{
			_sourceGenerateTextBox.Text = "";

			if (Directory.Exists(SchemaPath) == false)
			{
				MessageBox.Show($"{SchemaPath} 스키마 폴더가 없습니다.");
				return;
			}

			if(File.Exists(SourcePath) == false)
			{
				MessageBox.Show($"{SourcePath} 소스파일이 없습니다.");
				return;
			}

			ProgressDialog dialog = new ProgressDialog();
			dialog.DoWorkHandler += (workerSender, e) =>
			{
				BackgroundWorker worker = workerSender as BackgroundWorker;

				List<string> generateResults = new List<string>();
				foreach (var schemaNameInfo in _schemaNameList)
				{
					FileInfo fileInfo = new FileInfo(schemaNameInfo.SchemaFilePath);
					if (fileInfo.Attributes.HasFlag(FileAttributes.Hidden) == true)
						continue;

					try
					{
						int percent = ProgressDialog.GetPercent(_schemaNameList.IndexOf(schemaNameInfo), _schemaNameList.Count);
						worker.ReportProgress(percent, $"{schemaNameInfo.SchemaName} SourceGenerating...");

						TableSourceGenerator generator = new TableSourceGenerator(fileInfo.FullName);
						var result = generator.Generate(_enumDefines.Value);
						generateResults.Add(result);
					}
					catch(Exception ex)
					{
						throw ex;
					}
				}

				StringBuilder sourceBuiler = new StringBuilder();
				sourceBuiler.AppendLine("using CommonPackage.Enums;");
				sourceBuiler.AppendLine("namespace CommonPackage.Tables");
				sourceBuiler.AppendLine("{");
				foreach (var generateResult in generateResults)
				{
					sourceBuiler.AppendLine(generateResult);
				}
				sourceBuiler.AppendLine("}");

				worker.ReportProgress(100, $"SourceGenerate Complete");

				using (StreamWriter stream = new StreamWriter(SourcePath))
				{
					stream.Write(sourceBuiler.ToString());
				}

				_sourceGenerateTextBox.Dispatcher.Invoke(() =>
				{
					_sourceGenerateTextBox.Text = sourceBuiler.ToString();
				});
			};

			dialog.ShowDialog();

			if(dialog.Exception != null)
			{
				AddLog($"Source Generate Exception : {dialog.Exception.Message}");
			}
			else
			{
				AddLog($"{SourcePath} sourceFile Generate Complete");
			}
		}

		private void OnEnumGenerator(Button sender)
		{
			_enumGenerateTextBox.Text = "";

			if (File.Exists(DllPath) == false)
			{
				AddLog($"{DllPath} 파일이 없습니다.");
				return;
			}

			FileInfo fileInfo = new FileInfo(EnumPath);
			if (fileInfo.Directory.Exists == false)
			{
				AddLog($"{EnumPath} 경로가 잘못 되었습니다.");
				return;
			}
			
			try
			{
				AddLog($"{EnumPath} enum Generate Start");

				EnumJsonGenerator generate = new EnumJsonGenerator(DllPath, "CommonPackage.Enums");
				var result = generate.Generate();

				using (StreamWriter stream = new StreamWriter(EnumPath, false, Encoding.UTF8))
				{
					stream.Write(result);
				}

				_enumGenerateTextBox.Text = result;

				AddLog($"{EnumPath} enum Generate Complete");

				_enumDefines = new Lazy<EnumsDefines>(() =>
				{
					return EnumSerialize.Serialize(System.IO.Path.Combine(TablePath, "enums.json"));
				});

				AddLog($"EnumsDefines reloading");
			}
			catch(Exception e)
			{
				AddLog($"enum Generate Exception : {e.Message}");
			}
		}
	
		private void BuildCommonPackage(Button sender)
		{
			if(File.Exists(CommonPackageProjPath) == false)
			{
				AddLog($"{CommonPackageProjPath} 경로가 잘못 되었습니다.");
				return;
			}

			string buildOption = "/p:Configuration=Release;VisualStudioVersion=19.0";

			ProcessStartInfo processStartInfo = new ProcessStartInfo();
			processStartInfo.UseShellExecute = false;
			processStartInfo.CreateNoWindow = true;
			processStartInfo.FileName = MSBuildPath;
			processStartInfo.Arguments = $"{CommonPackageProjPath} {buildOption}";
			processStartInfo.RedirectStandardOutput = true;
			processStartInfo.RedirectStandardError = true;

			try
			{
				using (var process = new Process())
				{
					process.StartInfo = processStartInfo;
					process.OutputDataReceived += (processSender, e) =>
					{
						AddLog(e.Data);
					};

					process.ErrorDataReceived += (processSender, e) =>
					{
						AddLog(e.Data);
					};

					process.Start();

					process.BeginOutputReadLine();
					process.BeginErrorReadLine();
				}
			}
			catch (Exception e)
			{
				AddLog($"{nameof(BuildCommonPackage)} Exception : {e.Message}");
			}
		}
	}
}
