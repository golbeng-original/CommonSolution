using CommonPackage.String;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GolbengFramework.StringDataTool
{
	public class StringDataGridItem : INotifyPropertyChanged
	{
		private StringData _backupStringData;

		public StringDataGridItem()
		{
			StringData = new StringData();
		}

		public StringDataGridItem(StringData stringData)
		{
			StringData = stringData;
		}

		public string Key
		{
			get => StringData.Key;
			set
			{
				StringData.Key = value;
				OnPropertyChanged("Key");
			}
		}

		public string Data
		{
			get => StringData.Data;
			set
			{
				StringData.Data = value;
				OnPropertyChanged("Data");
			}
		}

		public string Group
		{
			get => StringData.Group;
			set
			{
				StringData.Group = value;
				OnPropertyChanged("Group");
			}
		}

		public Dictionary<string, string> Options
		{
			get
			{
				return StringData.Options;
			}
		}

		public StringData StringData { get; private set; }

		public event PropertyChangedEventHandler PropertyChanged;

		public bool IsEmpty
		{
			get
			{
				if (string.IsNullOrEmpty(Key) == true &&
					string.IsNullOrEmpty(Data) == true &&
					string.IsNullOrEmpty(Group) == true)
					return true;

				return false;
			}

		}

		public bool IsDirty
		{
			get
			{
				return StringData.IsDeepEquals(_backupStringData) == false ? true : false;
			}
		}

		public void Backup()
		{
			_backupStringData = new StringData();

			_backupStringData.Key = StringData.Key;
			_backupStringData.Data = StringData.Data;
			_backupStringData.Group = StringData.Group;

			foreach (var option in StringData.Options)
			{
				_backupStringData.Options[option.Key] = option.Value;
			}
		}

		public void Rollback()
		{
			if (_backupStringData == null)
				return;

			StringData.Key = _backupStringData.Key;
			StringData.Data = _backupStringData.Data;
			StringData.Group = _backupStringData.Group;

			foreach (var option in _backupStringData.Options)
			{
				StringData.Options[option.Key] = option.Value;
			}
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


	public partial class MainWindow : Window
	{
		private bool _isNewState = false;

		public string FilterKey { get; set; }

		public string FilterData { get; set; }

		private void InitlizeDataGrid(string group = null)
		{
			_groupStringDataList = _currentStringDataContainer.StringDataSet.Select(s => s);

			if (group != null)
				_groupStringDataList = _groupStringDataList.Where(s => s.Group.Equals(group));

			UpdateFilterDataGrid();
		}

		private void UpdateFilterDataGrid()
		{
			if (_groupStringDataList == null)
				return;

			if (string.IsNullOrEmpty(FilterKey) == false)
				_groupStringDataList = _groupStringDataList.Where(s => s.Key.Contains(FilterKey));

			if (string.IsNullOrEmpty(FilterData) == false)
				_groupStringDataList = _groupStringDataList.Where(s => s.Data.Contains(FilterData));


			var observableCollection = new ObservableCollection<StringDataGridItem>(
				_groupStringDataList.Select(s => new StringDataGridItem(s))
			);

			_dataGrid.ItemsSource = observableCollection;
		}


		private void _dataGrid_InitializingNewItem(object sender, InitializingNewItemEventArgs e)
		{
			StringDataGridItem stringDataGridItem = e.NewItem as StringDataGridItem;
			if (stringDataGridItem == null)
				return;

			stringDataGridItem.Group = _selectedGroup != null ? _selectedGroup : "";
			_isNewState = true;
		}

		private void _dataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
		{
			var stringDataGridItem = e.Row.DataContext as StringDataGridItem;
			if (stringDataGridItem == null)
				return;

			stringDataGridItem.Backup();
		}

		private void _dataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
		{
			StringDataGridItem gridItem = e.Row.DataContext as StringDataGridItem;
			if (gridItem == null)
				return;

			if (e.EditAction == DataGridEditAction.Cancel)
			{
				gridItem.Rollback();
				return;
			}

			if (e.Column.SortMemberPath.Equals(nameof(StringDataGridItem.Key)) == false)
				return;

			if (gridItem.IsEmpty == true)
			{
				e.Cancel = true;
				return;
			}

			if (string.IsNullOrEmpty(gridItem.Key))
			{
				MessageBox.Show("Key는 필수 입력입니다.");
				gridItem.Rollback();
				_dataGrid.CancelEdit(DataGridEditingUnit.Row);
			}

			var stringDataSet = _currentStringDataContainer.StringDataSet;
			var isExists = stringDataSet.Where(s =>
			{
				if (ReferenceEquals(s, gridItem.StringData) == false &&
					s.Key.Equals(gridItem.Key, StringComparison.OrdinalIgnoreCase) == true)
				{
					return true;
				}

				return false;
			}).Any();

			if (isExists == true)
			{
				MessageBox.Show("중복 된 Key 입니다.");
				gridItem.Rollback();
				e.Cancel = true;
				_dataGrid.CancelEdit(DataGridEditingUnit.Row);
			}
		}

		private void _dataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
		{
			StringDataGridItem gridItem = e.Row.DataContext as StringDataGridItem;
			if (gridItem == null)
				return;

			if (e.EditAction == DataGridEditAction.Cancel)
				return;

			if (gridItem.IsEmpty == true)
			{
				_isNewState = false;
				_dataGrid.CancelEdit(DataGridEditingUnit.Row);
				return;
			}

			if (_isNewState == true)
			{
				_isNewState = false;

				if (string.IsNullOrEmpty(gridItem.Key) == true)
				{
					MessageBox.Show("Key는 필수 입력입니다.");
					_dataGrid.CancelEdit(DataGridEditingUnit.Row);
					return;
				}

				_currentStringDataContainer.StringDataSet.Add(gridItem.StringData);

				OnDirty();
				return;
			}

			if(gridItem.IsDirty == true)
				OnDirty();
		}

		private void _dataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count == 0)
				return;

			StringDataGridItem item = e.AddedItems[0] as StringDataGridItem;
			if (item == null)
				return;

			UpdateOptionDataGrid(item);
		}

		private void _dataGrid_PreviewCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			DataGrid datagrid = sender as DataGrid;
			if (datagrid == null)
				return;
			
			if(e.Command == DataGrid.DeleteCommand)
			{
				StringDataGridItem deleteGridItem = _dataGrid.SelectedItem as StringDataGridItem;
				if (deleteGridItem == null)
					return;

				var result = MessageBox.Show("삭제 하시겠습니까?", "경고", MessageBoxButton.OKCancel);
				if (result != MessageBoxResult.OK)
				{
					e.Handled = true;
					return;
				}

				var stringDataSet = _currentStringDataContainer.StringDataSet;
				stringDataSet.Remove(deleteGridItem.StringData);
				OnDirty();
			}
		}
	}
}
