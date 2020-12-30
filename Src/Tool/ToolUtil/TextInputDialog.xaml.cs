using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace GolbengFramework.ToolUtil
{
	/// <summary>
	/// TextInputDialog.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class TextInputDialog : Window
	{
		public string InputText { get => _textInput.Text; }

		public TextInputDialog()
		{
			InitializeComponent();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			if(InputText.Length == 0)
			{
				MessageBox.Show("입력 된 값이 없습니다.");
				return;
			}

			this.DialogResult = true;
		}
	}
}
