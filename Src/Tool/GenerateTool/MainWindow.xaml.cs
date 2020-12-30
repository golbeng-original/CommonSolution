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
		private static readonly string _staticSchemaReleativePath = @"Data\Table\schema";
		private static readonly string _staticTableReleativePath = @"Data\Table";
		private static readonly string _staticCommonPackageProjReleativePath = @"Src\Common\CommonPackage\CommonPackage.csproj";
		private static readonly string _staticSourceReleativePath = @"Src\Common\CommonPackage\src\table\GenerateTables.cs";
		private static readonly string _staticClientReleativePath = @"Src\Client\CookieGame\Assets\StreamingAssets\Data\Table";
		private static readonly string _staticServerReleativePath = @"Bin\Data\Table";

		private static readonly string _staticCommonDllReleativePath = @"Bin\Lib\CommonPackage.dll";
		private static readonly string _staticEnumReleativePath = @"Data\Table\enums.json";

		public enum FilterType
		{
			FileName,
			EnumName
		};

		public bool _isInitialize = false;

		private Lazy<EnumsDefines> _enumDefines;

		private string _rootPath = "";
		private string _msBuildPath = "";

		public string RootPath { get => _rootPath; }

		public string MSBuildPath { get => _msBuildPath; }

		public string SchemaPath { get => RootPath.Length > 0 ? System.IO.Path.Combine(RootPath, _staticSchemaReleativePath) : ""; }

		public string TablePath { get => RootPath.Length > 0 ? System.IO.Path.Combine(RootPath, _staticTableReleativePath) : ""; }

		public string CommonPackageProjPath { get => RootPath.Length > 0 ? System.IO.Path.Combine(RootPath, _staticCommonPackageProjReleativePath) : ""; }

		public string SourcePath { get => RootPath.Length > 0 ? System.IO.Path.Combine(RootPath, _staticSourceReleativePath) : ""; }

		public string DllPath { get => RootPath.Length > 0 ? System.IO.Path.Combine(RootPath, _staticCommonDllReleativePath) : ""; }

		public string EnumPath { get => RootPath.Length > 0 ? System.IO.Path.Combine(RootPath, _staticEnumReleativePath) : ""; }

		public string ClientDbPath { get => RootPath.Length > 0 ? System.IO.Path.Combine(RootPath, _staticClientReleativePath) : ""; }

		public string ServerDbPath { get => RootPath.Length > 0 ? System.IO.Path.Combine(RootPath, _staticServerReleativePath) : ""; }

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
			string tablePath = System.IO.Path.Combine(rootPath, _staticTableReleativePath);
			if (Directory.Exists(tablePath) == false)
				return false;

			string clientPath = System.IO.Path.Combine(rootPath, _staticClientReleativePath);
			if (Directory.Exists(clientPath) == false)
				return false;

			string serverPath = System.IO.Path.Combine(rootPath, _staticServerReleativePath);
			if (Directory.Exists(serverPath) == false)
				return false;

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
			OnPropertyChanged("ClientDbPath");
			OnPropertyChanged("ServerDbPath");
			OnPropertyChanged("CommonPackageProjPath");

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
	}
}
