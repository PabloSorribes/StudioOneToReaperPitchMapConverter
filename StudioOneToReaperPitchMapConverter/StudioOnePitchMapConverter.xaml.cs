using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml;
using System.Xml.Linq;

namespace ReadXmlFileTest_WPF
{
	/// <summary>
	/// Interaction logic for OpenFileDialogSample.xaml
	/// </summary>
	public partial class StudioOnePitchMapConverter : Window
	{
		private List<string> filesToConvert = new List<string>();
		private string targetFolder = string.Empty;

		public StudioOnePitchMapConverter()
		{
			InitializeComponent();
		}

		private void BtnSelectFiles_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Multiselect = true;
			openFileDialog.Filter = "Studio One Pitch Map files (*.pitchlist)|*.pitchlist|Text files (*.txt)|*.txt|All files (*.*)|*.*";

			if (openFileDialog.ShowDialog() == true)
			{
				foreach (var fileName in openFileDialog.FileNames)
				{
					filesToConvert.Add(fileName);
					listBoxFiles.Items.Add(fileName);
				}
				//txtEditor.Text = File.ReadAllText(openFileDialog.FileName);
			}

			CheckAllConvertionConditions();
		}

		private void BtnClearList_Click(object sender, RoutedEventArgs e)
		{
			filesToConvert.Clear();
			listBoxFiles.Items.Clear();
		}

		private void BtnSelectDestinationFolder_Click(object sender, RoutedEventArgs e)
		{
			//Source: https://stackoverflow.com/questions/4007882/select-folder-dialog-wpf?rq=1
			Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog openFolderDialog = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog();
			openFolderDialog.Title = "Select Destination Folder for Reaper PitchMap-files";
			openFolderDialog.IsFolderPicker = true;
			//dlg.InitialDirectory = currentDirectory;

			openFolderDialog.AddToMostRecentlyUsedList = false;
			openFolderDialog.AllowNonFileSystemItems = false;
			//dlg.DefaultDirectory = currentDirectory;
			openFolderDialog.EnsureFileExists = true;
			openFolderDialog.EnsurePathExists = true;
			openFolderDialog.EnsureReadOnly = false;
			openFolderDialog.EnsureValidNames = true;
			openFolderDialog.Multiselect = false;
			openFolderDialog.ShowPlacesList = true;

			if (openFolderDialog.ShowDialog() == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
			{
				var folder = openFolderDialog.FileName;
				txtEditor.Text = folder;
				targetFolder = folder;
				// Do something with selected folder string
			}

			CheckAllConvertionConditions();
		}

		private void BtnConvertToReaperFormat_Click(object sender, RoutedEventArgs e)
		{
			foreach (var filePath in filesToConvert)
			{
				var xmlFile = XElement.Parse(File.ReadAllText(filePath));

				var pitchNamesList = xmlFile.Elements().Select(element => new PitchNameElement
				{
					pitch = int.Parse(element.Attribute("pitch").Value),
					name = element.Attribute("name").Value
				}).ToList();

				var fileName = $"{Path.GetFileNameWithoutExtension(filePath)}";

				try
				{
					var newFileName = $"{targetFolder}\\{fileName}.txt";
					if (File.Exists(newFileName))
					{
						File.Delete(newFileName);
					}

					// Create a new file     
					using (StreamWriter sw = File.CreateText(newFileName))
					{
						//Write header and an empty line.
						sw.WriteLine($"#Pitch Map: {fileName}");
						sw.WriteLine($"{string.Empty}");

						//Write Pitch items
						foreach (var pitchNameItem in pitchNamesList)
						{
							sw.WriteLine($"{pitchNameItem.pitch} {pitchNameItem.name}");
						}
					}

					//// Write file contents on console.     
					//using (StreamReader sr = File.OpenText(newFileName))
					//{
					//	string s = "";
					//	while ((s = sr.ReadLine()) != null)
					//	{
					//		Console.WriteLine(s);
					//	}
					//}
				}
				catch (Exception Ex)
				{
					Console.WriteLine(Ex.ToString());
				}
			}
			MessageBox.Show($"Converted {filesToConvert.Count} Studio One PitchMaps into Reaper PitchMaps! \n\n" +
				$"The new pitch maps have been saved at the following location: \n\n'{targetFolder}'");
		}

		private void BtnConvertToStudioOneFormat(object sender, RoutedEventArgs e)
		{
			foreach (var filePath in filesToConvert)
			{
				var currentFile = File.ReadLines(filePath);

				List<PitchNameElement> pitchNamesList = new List<PitchNameElement>();
				foreach (var line in currentFile)
				{
					//Check if line is an empty line or a comment.
					if (string.IsNullOrEmpty(line) || line[0] == '#')
					{
						continue;
					}

					var pitchNameElement = new PitchNameElement();

					char[] separator = " ".ToCharArray();
					var splitLine = line.Split(separator, 2);
					pitchNameElement.pitch = int.Parse(splitLine[0]);
					pitchNameElement.name = splitLine[1];

					pitchNamesList.Add(pitchNameElement);
				}

				//Console.WriteLine($"Elements to Write: {pitchNamesList.Count}");

				var fileName = $"{Path.GetFileNameWithoutExtension(filePath)}";

				try
				{
					var newFileName = $"{targetFolder}\\{fileName}.pitchlist";
					if (File.Exists(newFileName))
					{
						File.Delete(newFileName);
					}

					XmlWriterSettings writerSettings = new XmlWriterSettings();
					writerSettings.Indent = true;
					writerSettings.IndentChars = ("    ");
					writerSettings.CloseOutput = true;
					writerSettings.NewLineOnAttributes = false;

					using (XmlWriter writer = XmlWriter.Create(newFileName, writerSettings))
					{
						writer.WriteStartElement("Music.PitchNameList");

						foreach (var pitchElement in pitchNamesList)
						{
							writer.WriteStartElement("Music.PitchName");

							writer.WriteAttributeString("pitch", pitchElement.pitch.ToString());
							writer.WriteAttributeString("name", pitchElement.name);

							writer.WriteEndElement();
						}
						writer.WriteEndElement();
						writer.Flush();
					}

					//// Write file contents on console.     
					//using (StreamReader sr = File.OpenText(newFileName))
					//{
					//	string s = "";
					//	while ((s = sr.ReadLine()) != null)
					//	{
					//		Console.WriteLine(s);
					//	}
					//}
				}
				catch (Exception Ex)
				{
					Console.WriteLine(Ex.ToString());
				}
			}
			MessageBox.Show($"Converted {filesToConvert.Count} Reaper PitchMaps into Studio One PitchMaps! \n\n" +
				$"The new pitch maps have been saved at the following location: \n\n'{targetFolder}'");
		}


		private void CheckAllConvertionConditions()
		{
			if (Directory.Exists(txtEditor.Text) && filesToConvert.Count > 0)
			{
				ConvertButton.IsEnabled = true;
			}
			else
			{
				ConvertButton.IsEnabled = false;
			}
		}
	}

	public class PitchNameElement
	{
		public int pitch;
		public string name;
	}
}
