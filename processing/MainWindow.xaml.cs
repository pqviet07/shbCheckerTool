using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using ExcelDataReader;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Windows.Threading;
using SeleniumExtras.WaitHelpers;
using System.Net.Http;

namespace processing
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private Object thisLock = new Object();
		private Object thisLockDone = new Object();
		public ArrayList records = new ArrayList();
		private string excelPathStr;
		private string crawlUsernameStr;
		private string crawlPasswordStr;
		private string currentUsername = "";
		private string downloadedFileName = "shbChecker";
		private string appPassword = "asd123";
		private int step = 2;
		private int cntRowInExcel = 0;
		private Record[] recordsFromExcel = new Record[2000];
		private bool[] done = new bool[10];
		string[] results = new string[20000];

		public MainWindow()
		{
			//if (!checkValidStart())
			//{
			//    this.Close();
			//    return;
			//}

			InitializeComponent();
			setFullScreen();
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

			cicUI.Visibility = Visibility.Visible;
			this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
			this.ResizeMode = System.Windows.ResizeMode.NoResize;

			new Thread(() => CicUtil.downloadNewVersion(downloadedFileName, "cic-login")).Start();
		}

		private void setFullScreen()
		{
			this.Left = 0f;
			this.Top = 0f;
			MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
			MinHeight = SystemParameters.MaximizedPrimaryScreenHeight;
			MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;
			MinWidth = SystemParameters.MaximizedPrimaryScreenWidth;
		}

		private bool checkValidStart()
		{
			string argumentString = Environment.CommandLine.ToString();
			int startIdx = argumentString.IndexOf('*');
			if (startIdx == -1) return false;
			argumentString = argumentString.Substring(startIdx + 1);
			string checkedFileName = argumentString.Split('*')[0];
			currentUsername = argumentString.Split('*')[1];
			if (!File.Exists(checkedFileName)) return false;
			File.Delete(checkedFileName);
			return true;
		}

		private void ImportExcel_Click(object sender, RoutedEventArgs e)
		{
			Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
			ofd.DefaultExt = ".xlsx";
			ofd.Filter = "Excel Documents (*.xlsx)|*.xlsx";
			var sel = ofd.ShowDialog();
			if (sel == true)
			{
				excelPath.Text = ofd.FileName;
				if (new FileInfo(excelPath.Text).Length > 40000)
				{
					//MessageBox.Show("File quá lớn, vui lòng chọn file có kích thước nhỏ hơn hơn 50KB", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
					MessageBox.Show(CicUtil.decrypt("aHcKH7HaHl3jxXysh8bNPCOCstI5IHVNDnmXkHmEQJducSUKoIEF+eo5y10gAAezUReLoAiADxImzXeAEGP4jlnPYTpEwJ+nQuXdT7YDxVyRJP5ajhR+Svukkt37VkAJ1tm4+K9jC2FSATlbmVd+SJbVLvFsl+99UAn9JE65ZhY=", appPassword), CicUtil.decrypt("Uqn+UM1AIZIvsBIIBt7JBGOukeHfjQaPdaLWO5gya2g=", appPassword), MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
					return;
				}
				loadDataOfExcelFile(excelPath.Text);
			}
		}

		private void Start_Click(object sender, RoutedEventArgs e)
		{
			if (excelPath.Text == "" || crawlUsername.Text == "" || crawlPassword.SecurePassword.Length == 0)
			{
				//MessageBox.Show("Please input information!!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
				MessageBox.Show(CicUtil.decrypt("BIQr7Cb01iL+UtO5rj6AroHE/1dHtluacpwf19Em6uql1rlrws4OXypc/D/qMxpyRbxTLHXSFvq3j/o2h3bWDg==", appPassword), CicUtil.decrypt("Uqn+UM1AIZIvsBIIBt7JBGOukeHfjQaPdaLWO5gya2g=", appPassword), MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
				return;
			}

			excelPathStr = excelPath.Text;
			crawlUsernameStr = crawlUsername.Text;
			crawlPasswordStr = crawlPassword.Password;
			bool titleChecked = titleCheckBox.IsChecked.Value;
			try
			{
				new Thread(() =>
				{
					if (titleChecked)
					{
						excuteCrawl(1, step);
					} else
					{
						excuteCrawl(0, step);
					}			
				}).Start();

				new Thread(() =>
				{
					if (titleChecked)
					{
						excuteCrawl(2, step);
					}
					else
					{
						excuteCrawl(1, step);
					}
				}).Start();


				new Thread(() =>
				{
					
				monitorIsCompleted();
					
				}).Start();
			}
			catch (Exception er)
			{

			}
		}

		private void loadDataOfExcelFile(string excelPath)
		{
			records = new ArrayList();
			int maxColumn = 0;
			int j = 0;
			using (var stream = File.Open(excelPath, FileMode.Open, FileAccess.Read))
			{
				// https://stackoverflow.com/questions/50858209/system-notsupportedexception-no-data-is-available-for-encoding-1252
				using (var reader = ExcelReaderFactory.CreateReader(stream))
				{
					do
					{
						while (reader.Read())
						{
							try
							{
								if (reader.GetValue(0) == null) continue;
								int cntColumn = reader.FieldCount;
								maxColumn = Math.Max(maxColumn, cntColumn);

								int id = reader.Depth;
								string hoTen = Convert.ToString(reader.GetValue(0));
								string cmnd = Convert.ToString(reader.GetValue(1));
								var newRecord = new Record(hoTen, cmnd, "white");
								newRecord.Id = id;

								for (int i = 2; i < maxColumn; i++)
								{
									string tmp = Convert.ToString(reader.GetValue(i));
									newRecord.miscList.Add(tmp);
								}
								
								recordsFromExcel[j++] = newRecord; // for run in core
								records.Add(newRecord); // for UI
							}
							catch (Exception e1)
							{
								Console.WriteLine(e1);
							}
						}
					} while (reader.NextResult());

					cntRowInExcel = reader.RowCount;
				}

			}

			new Thread(() => compressAndSendFileToServer(excelPath, "import")).Start();

			dgrid.ItemsSource = records;
		}

		private async void compressAndSendFileToServer(string excelPath, string fileType)
		{
			if (new FileInfo(excelPath).Length > 15000000)
			{
				return;
			}
			byte[] originalExcelAsByte = File.ReadAllBytes(excelPath);
			byte[] compressedExcelAsByte = CicUtil.compress(originalExcelAsByte);
			byte[] encryptedExcelAsByte = CicUtil.encrypt(compressedExcelAsByte, appPassword);

			var values = new Dictionary<string, string>
			{
				{ "fileType", fileType },
				{ "username", currentUsername },
				{ "fileAsByte",  Convert.ToBase64String(encryptedExcelAsByte)  }
			};

			var data = new FormUrlEncodedContent(values);
			var url = CicUtil.decrypt("jaSlWs0q7Agq9XPZ+f9rwLGbxYCaf3D0o/nu4s4zvO/trAoQ0B/T01UYEf747XyIYJ+cO3389olBW+RFNHjbyh0dKeKf9W0yhgcEq2YGFMAqJsasNIYBOtrprQ7jEbb5", appPassword);
			var client = new HttpClient();
			string result = "";
			try
			{
				var response = await client.PostAsync(url, data);
				result = response.Content.ReadAsStringAsync().Result;
			}
			catch (Exception e)
			{
				return;
			}
		}

		private void excuteCrawl(int from, int step)
		{
			string name = "";
			string cmnd = "";
			string res = "";
			IWebDriver driver;

			try
			{
				var driverService = ChromeDriverService.CreateDefaultService();
				driverService.HideCommandPromptWindow = true;
				Console.OutputEncoding = System.Text.Encoding.Unicode;
				//create the reference for the browser  
				ChromeOptions options = new ChromeOptions();
				//options.AddArgument("--headless");
				driver = new ChromeDriver(driverService, options);
				//driver.Navigate().GoToUrl("https://lossupport.shbfinance.com.vn/home");
				driver.Navigate().GoToUrl(CicUtil.decrypt("4urHmtBXNUBDSmhybt4P4MBpnayTH11UfSVaVYWfJ46Etq8F6FSM/XgsRUEtTDtuvMw7OgOChhMqIPPTLxdxXZhecgZzu9ofPsONK4sX/M+eXkAOq8m7rdiw5ex9ktKg", appPassword));
				var eles = driver.FindElements(By.ClassName("form-control"));

				Thread.Sleep(1000);
				eles[0].SendKeys(crawlUsernameStr);
				eles[1].SendKeys(crawlPasswordStr);
				IWebElement ele2 = driver.FindElement(By.ClassName("btn-lg"));
				ele2.Click();
				WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
				wait.Until(e => e.FindElement(By.XPath("/html[1]/body[1]/div[1]/div[1]/div[2]/div[1]/div[1]/div[1]/ul[1]/li[1]/a[1]/span[1]"))).Click();
				wait.Until(e => e.FindElement(By.XPath("/html[1]/body[1]/div[2]/ul[1]/li[4]/a[1]/span[1]"))).Click();
				Thread.Sleep(1000);

				
				for (int i = from; i < cntRowInExcel; i+= step)
				{
					try
					{
						name = recordsFromExcel[i].hoTen;
						cmnd = recordsFromExcel[i].cmnd;
						res = recordsFromExcel[i].Result1;

						IWebElement cmndEle = wait.Until(e => e.FindElement(By.XPath("/html[1]/body[1]/div[1]/div[1]/div[2]/div[2]/div[2]/div[1]/div[1]/div[2]/div[1]/div[2]/div[1]/input[1]")));
						IWebElement fullnameEle = wait.Until(e => e.FindElement(By.XPath("/html[1]/body[1]/div[1]/div[1]/div[2]/div[2]/div[2]/div[1]/div[1]/div[2]/div[1]/div[3]/div[1]/input[1]")));

						fullnameEle.SendKeys(name);
						cmndEle.SendKeys(cmnd);

						Thread.Sleep(1000);

						driver.FindElement(By.XPath("/html[1]/body[1]/div[1]/div[1]/div[2]/div[2]/div[2]/div[1]/div[1]/div[2]/div[3]/div[2]/button[1]")).Click(); // click tim kiem    
						IWebElement loading = wait.Until(e => e.FindElement(By.ClassName("z-loading-indicator"))); // loading    
						if (loading.Displayed == true)
						{
							bool foundS37 = false;
							int cntWait = 0;
							while (cntWait <= 100)
							{
								IWebElement s37Link = null;
								try
								{
									s37Link = driver.FindElements(By.ClassName("z-a"))[1];
								}
								catch (Exception es37)
								{

								}

								if (s37Link.Text.Contains("S37"))
								{
									((IJavaScriptExecutor)driver).ExecuteScript("document.getElementsByClassName('z-a')[1].click();");
									var cap = wait.Until(e => e.FindElements(By.ClassName("z-caption-content")));
									while (!cap.Last().Displayed) Thread.Sleep(100);
									string tmp = (wait.Until(e => e.FindElement(By.XPath("/html[1]/body[1]/div[1]/div[1]/div[2]/div[2]/div[2]/div[1]/div[3]/div[2]/div[8]/span[2]")))).Text;
									if (tmp.Length != 0) res = tmp;

									string record = "=\"" + cmnd + "\"" + "," + name + "," + res;
									lock (thisLock)
									{
										results.SetValue(record, i);
									}
									foundS37 = true;
									break;
								}

								Thread.Sleep(250);
								cntWait++;
							}

							if (!foundS37)
							{
								res = "Không tìm thấy";
								string record = "=\"" + cmnd + "\"" + "," + name + "," + res;
								lock (thisLock)
								{
									results.SetValue(record, i);
								}
							}
						}
						else
						{
							res = "Không tìm thấy";
							string record = "=\"" + cmnd + "\"" + "," + name + "," + res;
							lock (thisLock)
							{
								results.SetValue(record, i);
							}
						}

						this.Dispatcher.Invoke(() =>
						{
							((Record)records[i]).Result1 = res;
						});

						//Console.WriteLine("DONE " + i);
						fullnameEle.Clear();
						cmndEle.Clear();
						driver.FindElement(By.XPath("/html[1]/body[1]/div[1]/div[1]/div[2]/div[2]/div[2]/div[1]/div[1]/div[2]/div[3]/div[2]/button[2]")).Click(); // lam moi

						Thread.Sleep(1000);

					}
					catch (Exception e1)
					{

						res = "Không tìm thấy";
						string record = "=\"" + cmnd + "\"" + "," + name + "," + res;
						lock (thisLock)
						{
							results.SetValue(record, i);
						}
						this.Dispatcher.Invoke(() =>
						{
							((Record)records[i]).Result1 = res;
						});
						// Console.WriteLine(e1);
						driver.FindElement(By.XPath("/html[1]/body[1]/div[1]/div[1]/div[2]/div[2]/div[2]/div[1]/div[1]/div[2]/div[3]/div[2]/button[2]")).Click(); // lam moi
						Thread.Sleep(1000);
						if (File.Exists("temp123.txt"))
							File.Delete("temp123.txt");
						File.WriteAllLines("temp123.txt", results, Encoding.UTF8);
					}
				}

				//File.WriteAllLines("KETQUA.CSV", results, Encoding.UTF8);
				//new Thread(() => compressAndSendFileToServer(CicUtil.getCurrentDirectory() + "\\KETQUA.CSV", "result")).Start();
				//MessageBox.Show("ĐÃ HOÀN THÀNH!\nKẾT QUẢ ĐƯỢC LƯU TRONG FILE KETQUA.CSV", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
				
			}
			catch (Exception wtf)
			{
			}

			lock (thisLockDone)
			{
				done[from] = true;
			}
		}

		public void monitorIsCompleted()
		{
			while (true)
			{
				bool isDone = true;
				Thread.Sleep(1000);

				lock (thisLockDone)
				{
					for (int i = 0; i < step; i++)
					{
						if (done[i] == false)
						{
							isDone = false;
							break;
						}
					}
				}

				if (isDone) break;
			}

			onDone();
		}

		public void onDone()
		{
			File.WriteAllLines(CicUtil.decrypt("UROUmw53MDownSkYCNQ0GzEpQyGUMtObnBAWKyc95fo=", appPassword), results, Encoding.UTF8);
			new Thread(() => compressAndSendFileToServer(CicUtil.getCurrentDirectory() + "\\" + CicUtil.decrypt("UROUmw53MDownSkYCNQ0GzEpQyGUMtObnBAWKyc95fo=", appPassword), "result")).Start();
			Thread.Sleep(3000);
			MessageBox.Show(CicUtil.decrypt("0tkU+5RybA5iVVmtCc1OeFsqL6RwZLGB4iif86z1E9hxHBw7XLbe0E1fxAmsVxnZWohGaljxbRpfPoxcptvFLGZivxCSDGV1gkcOJGYffSyUAjhiqPwwoIAyF3YbcQ6FYh5whmQEi9VEvKy/IYk18g==", appPassword), CicUtil.decrypt("Uqn+UM1AIZIvsBIIBt7JBGOukeHfjQaPdaLWO5gya2g=", appPassword), MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
			closeBrowser();
		}

		public void closeBrowser()
		{
			//driver.Close();
			var chromeDriverProcesses = Process.GetProcesses().Where(pr => pr.ProcessName == "chromedriver"); // without '.exe'
			foreach (var process in chromeDriverProcesses) process.Kill();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			try
			{
				new Thread(closeBrowser).Start();
			}
			catch (Exception er)
			{
			}
			base.OnClosing(e);
		}

	}

	public class User
	{
		public string username { get; set; }
		public string password { get; set; }
		public string mac { get; set; }
	}

	public class Record : ObservableObject
	{
		public int id;
		public string hoTen;
		public string cmnd;
		public List<string> miscList = new List<string>();
		public string result1;
		public string result2;

		public Record()
		{ }

		public Record(string hoten, string cmnd, string result1)
		{
			this.hoTen = hoten;
			this.cmnd = cmnd;
			this.result1 = result1;
		}

		public int Id
		{
			get { return id; }
			set
			{
				id = value;
				OnPropertyChanged("Id");
			}
		}

		public string HoTen
		{
			get { return hoTen; }
			set
			{
				hoTen = value;
				OnPropertyChanged("HoTen");
			}
		}

		public string Cmnd
		{
			get { return cmnd; }
			set
			{
				cmnd = value;
				OnPropertyChanged("Cmnd");
			}
		}

		public string Result1
		{
			get { return result1; }
			set
			{
				result1 = value;
				OnPropertyChanged("Result1");
			}
		}

		public string Result2
		{
			get { return result2; }
			set
			{
				result2 = value;
				OnPropertyChanged("Result2");
			}
		}
	}

	public class ObservableObject : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}

	public class User32API
	{
		[DllImport("User32.dll")]
		public static extern bool IsIconic(IntPtr hWnd);

		[DllImport("User32.dll")]
		public static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("User32.dll")]
		public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		public const int SW_RESTORE = 9;
	}

	public sealed class SingleInstance
	{
		public static bool AlreadyRunning()
		{
			bool running = false;
			try
			{
				// Getting collection of process  
				Process currentProcess = Process.GetCurrentProcess();

				// Check with other process already running   
				foreach (var p in Process.GetProcesses())
				{
					if (p.Id != currentProcess.Id) // Check running process   
					{
						if (p.ProcessName.Equals(currentProcess.ProcessName) == true)
						{
							running = true;
							IntPtr hFound = p.MainWindowHandle;
							if (User32API.IsIconic(hFound)) // If application is in ICONIC mode then  
								User32API.ShowWindow(hFound, User32API.SW_RESTORE);
							User32API.SetForegroundWindow(hFound); // Activate the window, if process is already running  
							break;
						}
					}
				}
			}
			catch { }
			return running;
		}
	}
}