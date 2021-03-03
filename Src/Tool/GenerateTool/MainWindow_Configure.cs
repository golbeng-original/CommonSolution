using GolbengFramework.GenerateTool.Utils;
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
		private List<string> _configFileNameList = new List<string>();

		private void InitalizeConfigureFileList()
		{
			_configFileNameList.Clear();

			if (Directory.Exists(ConfigurePath) == false)
			{
				MessageBox.Show($"{ConfigurePath}가 존재 하지 않습니다.");
				return;
			}

			var fileNames = Directory.GetFiles(ConfigurePath, "*.json");

			_configFileNameList = fileNames.Select(f => new FileInfo(f).Name).ToList();
			_configFileNameComboBox.ItemsSource = _configFileNameList;
		}

		private void SyncConfigure(Button sender)
		{
			StringBuilder errorBuilder = new StringBuilder();

			foreach(var configFile in _configFileNameList)
			{
				string destClientSrcDirectory = ToolConfigUtil.FindWildCardPath(ClientSrcConfigPath);

				string sourceConfigFilePath = System.IO.Path.Combine(ConfigurePath, configFile);
				string destClientSrcPath = System.IO.Path.Combine(destClientSrcDirectory, configFile);

				try
				{
					Directory.CreateDirectory(destClientSrcDirectory);

					File.Copy(sourceConfigFilePath, destClientSrcPath, true);
				}
				catch(Exception e)
				{
					errorBuilder.Append(e.Message);
				}
			}

			if(errorBuilder.Length > 0)
			{
				MessageBox.Show($"동기화 실패\n{errorBuilder.ToString()}");
				return;
			}

			MessageBox.Show("동기화 완료");
		}

		private void SaveConfigure(Button sender)
		{

		}
	}
}
