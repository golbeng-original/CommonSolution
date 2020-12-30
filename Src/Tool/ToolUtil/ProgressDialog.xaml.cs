using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
	/// ProgressDialog.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class ProgressDialog : Window
	{
		private BackgroundWorker _worker = new BackgroundWorker();

		public Exception Exception { get; private set; } = null;

		public DoWorkEventHandler DoWorkHandler { get; set; }

		public ProgressDialog()
		{
			InitializeComponent();

			_worker.WorkerReportsProgress = true;

			_progressLabel.Content = "";

			_progressBar.Minimum = 0.0f;
			_progressBar.Maximum = 100.0f;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			_worker.RunWorkerCompleted += (workerSender, workerArgs) => {
				
				if(workerArgs.Error != null)
				{
					Exception = workerArgs.Error;
					this.DialogResult = false;
					return;
				}

				_progressBar.Value = 100.0f;
				this.DialogResult = true;
			};

			_worker.ProgressChanged += (workerSender, workerArgs) => {
				_progressBar.Value = workerArgs.ProgressPercentage;
				_progressLabel.Content = workerArgs.UserState;
			};

			_worker.DoWork += this.DoWorkHandler;
			_worker.DoWork += (workderSender, workerArgs) =>
			{
				Thread.Sleep(500);
			};

			_worker.RunWorkerAsync();
		}

		public static int GetPercent(int curr, int total, int startSection = 0, int endSection = 100)
		{
			int sectionPerscent = (int)(((float)curr / (float)total) * 100.0f);

			int section = endSection - startSection;

			sectionPerscent = (int)(((float)sectionPerscent / (float)section) * (float)endSection);

			return startSection + sectionPerscent;
		}
	}
}
