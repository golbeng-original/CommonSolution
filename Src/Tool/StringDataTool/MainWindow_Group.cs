using CommonPackage.String;
using GolbengFramework.ToolUtil;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GolbengFramework.StringDataTool
{
	public class FileComboItem : INotifyPropertyChanged
	{
		private FileInfo _fileInfo;

		public event PropertyChangedEventHandler PropertyChanged;

		public string Name
		{
			get
			{
				if (IsAddItem == true)
					return "Conatiner 추가";

				return _fileInfo?.Name;
			}
		}

		public string FilePath { get => _fileInfo?.FullName; }

		public bool IsAddItem { get; private set; } = false;

		private bool _isDirty = false;
		public bool IsDirty
		{
			get => _isDirty;
			set
			{
				_isDirty = value;
				OnPropertyChanged("State");
			}
		}

		public string State
		{
			get
			{
				if (IsAddItem == true)
					return "#00000000";

				if(IsDirty == true)
					return "#FFFF0000";

				if (Container == null)
					return "#FF000000";

				return "#FF00FF00";
			}
		}

		public StringDataContainer Container { get; set; }

		public FileComboItem(FileInfo fileInfo)
		{
			_fileInfo = fileInfo;
		}

		public FileComboItem(bool isAddItem)
		{
			IsAddItem = isAddItem;
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

	public class GroupComboItem
	{
		public string GroupName { get; set; }

		public bool IsAll { get; set; } = false;
	}

	// Category
	public partial class MainWindow : Window
	{
		//private List<FileComboItem> _fileComboItemList = new List<FileComboItem>();
		private ObservableCollection<FileComboItem> _fileComboItemList = new ObservableCollection<FileComboItem>();

		private StringDataContainer _currentStringDataContainer = null;

		private FileComboItem SelectedFileComboItem { get => _fileComboBox.SelectedItem as FileComboItem; }

		public void InitlaizeCategory()
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(SourceStringDataPath);
			if (directoryInfo.Exists == false)
				return;

			var fileInfos = directoryInfo.GetFiles("*.json");
			foreach(var fileInfo in fileInfos)
			{
				_fileComboItemList.Add(new FileComboItem(fileInfo));
			}

			_fileComboItemList.Add(new FileComboItem(true));

			SortContainer();
			
			_fileComboBox.ItemsSource = _fileComboItemList;
		}

		private void AddContainer(string containerName)
		{
			StringDataContainer container = new StringDataContainer();
			container.FileName = containerName + ".json";

			var resultFileInfo = StringDataContainer.Serialize(SourceStringDataPath, container);

			FileComboItem newfileComboItem = new FileComboItem(resultFileInfo);

			_fileComboItemList.Add(newfileComboItem);

			SortContainer();

			_fileComboBox.ItemsSource = _fileComboItemList;
		}

		private void SortContainer()
		{
			_fileComboItemList = new ObservableCollection<FileComboItem>(_fileComboItemList.OrderBy(f => f.IsAddItem).ThenBy(f => f.Name));
		}

		private void OnDirty()
		{
			if (SelectedFileComboItem == null)
				return;

			SelectedFileComboItem.IsDirty = true;
			_fileComboBox.Items.Refresh();
		}

		private void _fileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count == 0)
			{
				_currentStringDataContainer = null;
				return;
			}

			FileComboItem selectedItem = e.AddedItems[0] as FileComboItem;
			if(selectedItem.IsAddItem == true)
			{
				FileComboItem preSelectedItem = null;
				if (e.RemovedItems.Count > 0)
					preSelectedItem = e.RemovedItems[0] as FileComboItem;

				TextInputDialog dialog = new TextInputDialog();
				dialog.Title = "Container 이름 입력";
				if(dialog.ShowDialog() == true)
				{
					AddContainer(dialog.InputText);
				}

				_fileComboBox.SelectedItem = preSelectedItem;
				return;
			}

			if(selectedItem.Container == null)
			{
				ProgressDialog dialog = new ProgressDialog();
				dialog.DoWorkHandler += (workderSender, args) =>
				{
					BackgroundWorker worker = workderSender as BackgroundWorker;
					worker.ReportProgress(0, $"{selectedItem.Name} 불러오는 중..");

					var result = StringDataContainer.Deserialize(selectedItem.FilePath);
					selectedItem.Container = result.Container;

					worker.ReportProgress(100, $"{selectedItem.Name} 불러오기 완료");
				};

				dialog.ShowDialog();

				_fileComboBox.Items.Refresh();
			}

			_currentStringDataContainer = selectedItem.Container;

			InitlizeGroup();
		}
	}

	// StringDataViewer
	public partial class MainWindow : Window
	{
		private List<GroupComboItem> _groupList = null;

		private IEnumerable<StringData> _groupStringDataList = null;

		private string _selectedGroup = null;

		private void InitlizeGroup()
		{
			var stringDataSet = _currentStringDataContainer.StringDataSet;

			_groupList = stringDataSet.OrderBy(g => g.Group).Select(g => new GroupComboItem()
			{
				GroupName = g.Group.Length == 0 ? "미분류" : g.Group,
			}).ToList();

			_groupList.Insert(0, new GroupComboItem()
			{
				GroupName = "전체",
				IsAll = true
			}); ;

			_groupComboBox.ItemsSource = _groupList;

			_groupComboBox.SelectedIndex = 0;
		}

		private void _groupComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count == 0)
				return;

			GroupComboItem groupComboItem = e.AddedItems[0] as GroupComboItem;
			if (groupComboItem == null)
				return;

			_selectedGroup = null;
			if (groupComboItem.IsAll == false)
				_selectedGroup = groupComboItem.GroupName;

			InitlizeDataGrid(_selectedGroup);
		}

		private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			UpdateFilterDataGrid();
		}

		private void SaveStringDataConainer(FileComboItem comboItem)
		{
			var saveComboItem = comboItem;

			ProgressDialog dialog = new ProgressDialog();
			dialog.DoWorkHandler += (workderSender, args) =>
			{
				BackgroundWorker worker = workderSender as BackgroundWorker;
				worker.ReportProgress(0, $"{saveComboItem.Name} 저장 중..");

				StringDataContainer.Serialize(SourceStringDataPath, saveComboItem.Container);

				worker.ReportProgress(100, $"{saveComboItem.Name} 저장 완료");
			};

			dialog.ShowDialog();
		}

		private void OnSave(Button sender)
		{
			ClearLog();

			var selectedFileComboItem = SelectedFileComboItem;

			if (selectedFileComboItem == null || selectedFileComboItem.IsDirty == false)
				return;

			SaveStringDataConainer(selectedFileComboItem);

			selectedFileComboItem.IsDirty = false;
			_fileComboBox.Items.Refresh();
		}
	
		private void OnPublish(Button sender)
		{
			ClearLog();

			if (_fileComboItemList.Count == 0)
				return;

			// 저장 체크
			foreach (var fileComboItems in _fileComboItemList)
			{
				if(fileComboItems.IsDirty == true)
				{
					var result = MessageBox.Show($"{fileComboItems.Name}이 저장 전입니다.\n 저장하시겠습니까?", "", MessageBoxButton.OKCancel);
					if(result == MessageBoxResult.OK)
						SaveStringDataConainer(fileComboItems);
				}
			}

			StringDataContainer publishContainer = new StringDataContainer();
			bool isDuplicateExists = false;

			ProgressDialog dialog = new ProgressDialog();
			dialog.DoWorkHandler += (workderSender, args) =>
			{
				BackgroundWorker worker = workderSender as BackgroundWorker;

				int percentUnit = 100 / _fileComboItemList.Count;
				int currPercent = 0;
				foreach (var fileComboItems in _fileComboItemList)
				{
					if (fileComboItems.IsAddItem == true)
						continue;

					currPercent += percentUnit;
					worker.ReportProgress(currPercent, $"{fileComboItems.Name} 불러오는 중..");

					var deserializeResult = StringDataContainer.Deserialize(fileComboItems.FilePath);

					worker.ReportProgress(currPercent, $"{fileComboItems.Name} Publish 중..");
					foreach (var stringData in deserializeResult.Container.StringDataSet)
					{
						bool success = publishContainer.StringDataSet.Add(stringData);
						if(success == false)
						{
							AddLog($"{deserializeResult.Container.FileName} - {stringData.Key} 중복 발생");
							isDuplicateExists = true;
						}
					}
				}
			};

			dialog.ShowDialog();

			if(isDuplicateExists == true)
			{
				MessageBox.Show("Key 중복이 발생하여, Publish를 중단합니다.");
				return;
			}

			publishContainer.FileName = "StringDataBundle.json";

			ProgressDialog publishDialog = new ProgressDialog();
			publishDialog.DoWorkHandler += (workderSender, args) =>
			{
				BackgroundWorker worker = workderSender as BackgroundWorker;
				worker.ReportProgress(0, $"{publishContainer.FileName} 저장 중..");

				StringDataContainer.Serialize(ClientStringDataPath, publishContainer);

				worker.ReportProgress(100, $"{publishContainer.FileName} 저장 완료");
			};

			publishDialog.ShowDialog();
		}
	}
}
