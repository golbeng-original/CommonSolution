using CommonPackage.String;
using GolbengFramework.StringDataTool.Utils;
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

namespace GolbengFramework.StringDataTool
{
	/// <summary>
	/// MainWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		private static readonly string _staticStringReleativePath = @"Data\String";
		private static readonly string _staticClientReleativePath = @"Src\Client\CookieGame\Assets\StreamingAssets\Data\String";
		private static readonly string _staticServerReleativePath = @"Bin\Data\String";

		private bool _isInitialize = false;

		public event PropertyChangedEventHandler PropertyChanged;

		public string RootPath { get; private set; } = "";
		public string SourceStringDataPath { get => System.IO.Path.Combine(RootPath, _staticStringReleativePath); }
		public string ClientStringDataPath { get => System.IO.Path.Combine(RootPath, _staticClientReleativePath); }
		public string ServerStringDataPath { get => System.IO.Path.Combine(RootPath, _staticServerReleativePath); }

		public MainWindow()
		{
			InitializeComponent();
			this.DataContext = this;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			if (_isInitialize == true)
				return;

			LoadToolConfig();

			_isInitialize = true;
		}

		public bool CheckRootPath(string rootPath)
		{
			string tablePath = System.IO.Path.Combine(rootPath, _staticStringReleativePath);
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
			if (CheckRootPath(rootPath) == false)
			{
				MessageBox.Show("Project 경로가 잘못 되었습니다.");
				return false;
			}

			RootPath = rootPath;
			OnPropertyChanged("RootPath");

			InitlaizeCategory();

			return true;
		}

		private void LoadToolConfig()
		{
			var configInfo = ToolConfigUtil.LoadToolConfig();
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
			configInfo.rootPath = RootPath;

			ToolConfigUtil.SaveToolConfig(configInfo);
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Button button = sender as Button;
			if (button == null || button.Tag == null)
				return;

			string tag = button.Tag as string;
			switch (tag)
			{
				case "PATH_CONFIG":
					OnPathConfig(button);
					break;
				case "SAVE":
					OnSave(button);
					break;
				case "PUBLISH":
					OnPublish(button);
					break;
			}
		}

		private void OnPathConfig(Button sender)
		{
			System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
			if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				if(ApplyRootPath(dialog.SelectedPath) == true)
				{
					SaveToolConfig();
				}
			}
		}

		private void AddLog(string message)
		{
			_logListBox.Dispatcher.Invoke(() =>
			{
				_logListBox.Items.Add(message);
			});
			
		}

		private void ClearLog()
		{
			_logListBox.Dispatcher.Invoke(() =>
			{
				_logListBox.Items.Clear();
			});
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
