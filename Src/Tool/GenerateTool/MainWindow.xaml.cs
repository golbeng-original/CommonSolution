using GolbengFramework.Serialize;
using GolbengFramework.GenerateTool.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GolbengFramework.GenerateTool
{
	/// <summary>
	/// MainWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		public enum FilterType
		{
			FileName,
			EnumName
		};

		public bool _isInitialize = false;

		private Lazy<EnumsDefines> _enumDefines;

		private ToolPathConfigInfo _toolPathConfigInfo = new ToolPathConfigInfo();

		private string _rootPath = "";
		private string _msBuildPath = "";

		public string RootPath { get => _rootPath; }

		public string MSBuildPath { get => _msBuildPath; }

		public string SchemaPath { get => RootPath.Length > 0 ? System.IO.Path.Combine(RootPath, _toolPathConfigInfo.SchemaPath) : ""; }

		public string TablePath { get => RootPath.Length > 0 ? System.IO.Path.Combine(RootPath, _toolPathConfigInfo.TablePath) : ""; }

		public string CommonPackageProjPath { get => RootPath.Length > 0 ? System.IO.Path.Combine(RootPath, _toolPathConfigInfo.CommonPackageProjPath) : ""; }

		public string SourcePath { get => RootPath.Length > 0 ? System.IO.Path.Combine(RootPath, _toolPathConfigInfo.SourcePath) : ""; }

		public string DllPath { get => RootPath.Length > 0 ? System.IO.Path.Combine(RootPath, _toolPathConfigInfo.CommonDllPath) : ""; }

		public string EnumPath { get => RootPath.Length > 0 ? System.IO.Path.Combine(RootPath, _toolPathConfigInfo.EnumPath) : ""; }

		public string ClientSrcDbPath { get => RootPath.Length > 0 ? System.IO.Path.Combine(RootPath, _toolPathConfigInfo.ClientSrcTablePath) : ""; }

		public string ClientBinDbPath { get => RootPath.Length > 0 ? System.IO.Path.Combine(RootPath, _toolPathConfigInfo.ClientBinTablePath) : ""; }

		public string ClientSrcConfigPath { get => RootPath.Length > 0 ? System.IO.Path.Combine(RootPath, _toolPathConfigInfo.ClientSrcConfigPath) : ""; }

		public string ClientBinConfigPath { get => RootPath.Length > 0 ? System.IO.Path.Combine(RootPath, _toolPathConfigInfo.ClientBinConfigPath) : ""; }

		public string ServerDbPath { get => RootPath.Length > 0 ? System.IO.Path.Combine(RootPath, _toolPathConfigInfo.ServerPath) : ""; }

		public string ConfigurePath { get => RootPath.Length > 0 ? System.IO.Path.Combine(RootPath, _toolPathConfigInfo.ConfigurePath) : ""; }

		public string FilterText { get => _filterTextBox?.Text; }

		public FilterType SelectedFIlterType
		{ 
			get => _filterComboBox.SelectedIndex == 0 ? FilterType.FileName : FilterType.EnumName;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public MainWindow()
		{
			InitializeComponent();

			this.DataContext = this;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			LoadPathConfig();
			LoadToolConfig();

			_isInitialize = true;
		}

		private void LoadToolConfig()
		{
			var configInfo = ToolConfigUtil.LoadToolConfig();
			
			_msBuildPath = configInfo.msbuildPath;
			
			if (string.IsNullOrEmpty(configInfo.rootPath) == false)
			{
				if (ApplyRootPath(configInfo.rootPath) == false)
				{
					SaveToolConfig();
				}
			}
		}

		private void LoadPathConfig()
		{
			var loadResult = ToolConfigUtil.LoadPathConfig();

			_toolPathConfigInfo = loadResult.pathConfig;

			if(loadResult.isNew == true)
				MessageBox.Show("PathConfig가 새로 생성 되었습니다.");
		}

		private void SaveToolConfig()
		{
			ToolConfigInfo configInfo = new ToolConfigInfo();
			configInfo.rootPath = _rootPath;
			configInfo.msbuildPath = _msBuildPath;

			ToolConfigUtil.SaveToolConfig(configInfo);
		}

		private void AddLog(string log)
		{
			if (string.IsNullOrEmpty(log) == true)
				return;

			_logListBox.Dispatcher.Invoke(() =>
			{
				_logListBox.Items.Add(log);
			});
		}

		private void ClearLog()
		{
			_logListBox.Dispatcher.Invoke(() =>
			{
				_logListBox.Items.Clear();
			});
		}

		public bool CheckRootPath(string rootPath)
		{
			var checkPaths = _toolPathConfigInfo.CheckPaths.Select(p => System.IO.Path.Combine(rootPath, p));
			foreach(var checkPath in checkPaths)
			{
				var fullPath = ToolConfigUtil.FindWildCardPath(checkPath);
				if(string.IsNullOrEmpty(fullPath) == true)
					return false;

				if (Directory.Exists(fullPath) == false)
					return false;
			}

			var initalizePaths = _toolPathConfigInfo.InitalizePaths.Select(p => System.IO.Path.Combine(rootPath, p));
			foreach (var initalizePath in initalizePaths)
			{
				var fullPath = ToolConfigUtil.FindWildCardPath(initalizePath);
				if (string.IsNullOrEmpty(fullPath) == true)
					continue;

				if (Directory.Exists(fullPath) == false)
					Directory.CreateDirectory(fullPath);
			}

			return true;
		}

		private bool ApplyRootPath(string rootPath)
		{
			if(CheckRootPath(rootPath) == false)
			{
				MessageBox.Show("Project 경로가 잘못 되었습니다.");
				return false;
			}

			_rootPath = rootPath;

			OnPropertyChanged("RootPath");
			OnPropertyChanged("MSBuildPath");
			OnPropertyChanged("TablePath");
			OnPropertyChanged("SourcePath");
			OnPropertyChanged("DllPath");
			OnPropertyChanged("EnumPath");
			OnPropertyChanged("ClientSrcDbPath");
			OnPropertyChanged("ClientBinDbPath");
			OnPropertyChanged("ServerDbPath");
			OnPropertyChanged("CommonPackageProjPath");
			OnPropertyChanged("ConfigurePath");

			InitializeSchmeList();
			InitializeDataList();

			_enumDefines = new Lazy<EnumsDefines>(() =>
			{
				return EnumSerialize.Serialize(System.IO.Path.Combine(TablePath, "enums.json"));
			});

			return true;
		}

		private void ApplyMsBuildPath(string path)
		{
			_msBuildPath = path;
			OnPropertyChanged("MSBuildPath");
			SaveToolConfig();
		}

		private void OnPathConfig(Button sender)
		{
			System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
			if(dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				if(ApplyRootPath(dialog.SelectedPath) == true)
				{
					SaveToolConfig();
				}
			}
		}

		private void OnMsBuildPathConfig(Button sender)
		{
			System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
			dialog.Filter = "MSBuild.exe|MSBuild.exe";
			if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				ApplyMsBuildPath(dialog.FileName);
			}
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Button button = sender as Button;
			if (button == null || button.Tag == null)
				return;

			ClearLog();

			switch (button.Tag)
			{
				case "PATH_CONFIG": OnPathConfig(button); break;
				case "MSBUILD_PATH_CONFIG": OnMsBuildPathConfig(button); break;
				case "TABLE_CONVERT": OnTableConvert(button); break;
				case "SOURCE_GENERATOR": OnSourceGenerator(button); break;
				case "ENUM_GENERATOR": OnEnumGenerator(button); break;
				case "BUILD": BuildCommonPackage(button); break;
				case "SYNC_CONFIGUER": SyncConfigure(button); break;
				case "SAVE_CONFIGUER": SaveConfigure(button); break;
			}
		}

		private void _filterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_isInitialize == false)
				return;

			OnFilterSchemaList();
			OnFilterDataList();
		}

		private void _filterTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (_isInitialize == false)
				return;

			OnFilterSchemaList();
			OnFilterDataList();
		}

		protected void OnPropertyChanged(string name)
		{
			var handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(name));
			}
		}

		private void MenuItem_Click(object sender, RoutedEventArgs e)
		{
			MenuItem menuItem = sender as MenuItem;
			if (menuItem == null)
				return;

			ClearLog();

			if(menuItem.Tag?.Equals("SCHEMA_TAB") == true)
			{
				InitializeSchmeList();
				InitializeDataList();
			}
			else if(menuItem.Tag?.Equals("TABLE_TAB") == true)
			{
				InitializeDataList();
			}

			AddLog("갱신 완료");
		}

		private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count == 0)
				return;

			var TabItem = e.AddedItems[0] as TabItem;
			var tag = TabItem?.Tag as string;
			if (tag == null)
				return;

			switch (tag)
			{
				case "TABLE":
					break;
				case "SOURCE":
					break;
				case "ENUM":
					break;
				case "CONFIG":
					InitalizeConfigureFileList();
					break;
			}
		}
	}
}
