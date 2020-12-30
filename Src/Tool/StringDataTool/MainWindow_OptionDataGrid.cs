using CommonPackage.String;
using System;
using System.Collections;
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
	public class OptionGridItem
	{
		private string _backupKey;
		private string _backupValue;

		public StringData StringData { get; private set; }

		public OptionGridItem() {}

		public OptionGridItem(StringData stringData, string key, string value)
		{
			StringData = stringData;
			_key = key;
			_value = value;
		}

		public void Backup()
		{
			_backupKey = _key;
			_backupValue = _value;
		}

		public void Rollback()
		{
			_key = _backupKey;
			_value = _backupValue;
		}

		public void KeyCommit()
		{
			if (_backupKey != null)
				StringData?.Options.Remove(_backupKey);

			if (_key != null)
				StringData?.Options.Add(_key, _value);
		}

		public bool NewCommit(StringData stringData)
		{
			StringData = stringData;
			if(StringData.Options.ContainsKey(_key) == true)
				return false;

			StringData.Options.Add(_key, _value);
			return true;
		}

		public void DeleteCommit()
		{
			if (StringData == null)
				return;

			StringData.Options.Remove(_key);
		}

		public bool IsEmpty
		{ 
			get
			{
				return string.IsNullOrEmpty(_key) && string.IsNullOrEmpty(_value);
			}
		}

		public bool IsDuplicateKey
		{
			get
			{
				if (_backupKey == _key)
					return false;

				return StringData?.Options.ContainsKey(_key) ?? false;
			}
		}

		public bool IsDirty
		{
			get
			{
				return (_key == _backupKey) && (_value == _backupValue) ? false : true;
			}
		}

		private string _key;
		public string Key
		{
			get => _key;
			set
			{
				_key = value;

			}
		}

		private string _value;

		public string Value
		{
			get => _value;
			set
			{
				_value = value;
				if(_key != null && StringData?.Options.ContainsKey(_key) == true)
				{
					StringData.Options[_key] = _value;
				}
			}
		}
	}

	public partial class MainWindow : Window
	{
		private bool _isNewOptionState = false;

		private StringDataGridItem _selectedStringGridItem = null;

		private void UpdateOptionDataGrid(StringDataGridItem gridItem)
		{
			_selectedStringGridItem = gridItem;

			var optionEnumable = _selectedStringGridItem.Options.Select(kv => new OptionGridItem(gridItem.StringData, kv.Key, kv.Value));

			_optionDataGrid.ItemsSource = new ObservableCollection<OptionGridItem>(optionEnumable);
		}

		private void _optionDataGrid_InitializingNewItem(object sender, InitializingNewItemEventArgs e)
		{
			OptionGridItem optionGridItem = e.NewItem as OptionGridItem;
			if (optionGridItem == null)
				return;

			_isNewOptionState = true;
		}

		private void _optionDataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
		{
			OptionGridItem optionGridItem = e.Row.DataContext as OptionGridItem;
			if (optionGridItem == null)
				return;

			optionGridItem.Backup();

		}

		private void _optionDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
		{
			OptionGridItem optionGridItem = e.Row.DataContext as OptionGridItem;
			if (optionGridItem == null)
				return;

			if (e.EditAction == DataGridEditAction.Cancel)
			{
				optionGridItem.Rollback();
				return;
			}

			if (e.Column.SortMemberPath.Equals(nameof(OptionGridItem.Key)) == false)
				return;

			if (optionGridItem.IsEmpty == true)
			{
				_optionDataGrid.CancelEdit(DataGridEditingUnit.Row);
				return;
			}

			if (string.IsNullOrEmpty(optionGridItem.Key))
			{
				MessageBox.Show("OptionKey는 필수 입력입니다.");
				optionGridItem.Rollback();
				_optionDataGrid.CancelEdit(DataGridEditingUnit.Row);
				return;
			}

			if(optionGridItem.IsDuplicateKey == true)
			{
				MessageBox.Show("중복 된 OptionKey 입니다.");
				optionGridItem.Rollback();
				_optionDataGrid.CancelEdit(DataGridEditingUnit.Row);
				return;
			}

			optionGridItem.KeyCommit();
		}

		private void _optionDataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
		{
			OptionGridItem optionGridItem = e.Row.DataContext as OptionGridItem;
			if (optionGridItem == null)
				return;

			if (e.EditAction == DataGridEditAction.Cancel)
				return;

			if (optionGridItem.IsEmpty == true)
			{
				_isNewOptionState = false;
				_optionDataGrid.CancelEdit(DataGridEditingUnit.Row);
				return;
			}

			if (_isNewOptionState == true)
			{
				_isNewOptionState = false;
				if (string.IsNullOrEmpty(optionGridItem.Key) == true)
				{
					MessageBox.Show("OptionKey는 필수 입력입니다.");
					_optionDataGrid.CancelEdit(DataGridEditingUnit.Row);
					return;
				}

				if (optionGridItem.IsDuplicateKey == true)
				{
					MessageBox.Show("중복 된 OptionKey 입니다.");
					optionGridItem.Rollback();
					_optionDataGrid.CancelEdit(DataGridEditingUnit.Row);
					return;
				}

				if(optionGridItem.NewCommit(_selectedStringGridItem.StringData) == false)
				{
					MessageBox.Show("중복 된 OptionKey 입니다.");
					_optionDataGrid.CancelEdit(DataGridEditingUnit.Row);
					return;
				}

				OnDirty();
				return;
			}

			if(optionGridItem.IsDirty == true)
				OnDirty();
		}

		private void _optionDataGrid_PreviewCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			DataGrid dataGrid = sender as DataGrid;
			if (dataGrid == null)
				return;

			if(e.Command == DataGrid.DeleteCommand)
			{

				OptionGridItem deleteGridItem = dataGrid.SelectedItem as OptionGridItem;
				if (deleteGridItem == null)
					return;

				var result = MessageBox.Show("삭제 하시겠습니까?", "경고", MessageBoxButton.OKCancel);
				if (result != MessageBoxResult.OK)
				{
					e.Handled = true;
					return;
				}

				deleteGridItem.DeleteCommit();
				OnDirty();
			}
		}
	}
}
