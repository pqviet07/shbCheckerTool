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
using System.Net.NetworkInformation;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Security.Cryptography;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.IO.Compression;
using System.Diagnostics;
using SeleniumExtras.WaitHelpers;

namespace ShbChecker
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private string hashOfBryptDllFile = "TiTRUN/fHfMHB7ueZUdTxlM8CzUFr2d+XBIv3PtdzQ6dHqSvxnUP1bjzuZOP1WHkGfuoHWj0Yj7YXAsOGCDHPA==";
		public ArrayList records = new ArrayList();
		private string excelPathStr;
		private string crawlUsernameStr;
		private string crawlPasswordStr;
		public IWebDriver driver;
		private string currentUsername = "";

		public MainWindow()
		{
			InitializeComponent();
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			loginUI.Visibility = Visibility.Visible;
			cicUI.Visibility = Visibility.Hidden;
			this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
			this.ResizeMode = System.Windows.ResizeMode.NoResize;
			//excelPathStr = @"C:\Users\LAP13295-local\Desktop\loc.xlsx";
			//crawlUsernameStr = "thien.hhn";
			//crawlPasswordStr = "Copdeptrai123";
			//excuteCrawl();
		}

		private void Login_Click(object sender, RoutedEventArgs e)
		{
			//Process cmd = new Process();
			//cmd.StartInfo.FileName = "cmd.exe";
			//cmd.StartInfo.RedirectStandardInput = true;
			//cmd.StartInfo.RedirectStandardOutput = true;
			//cmd.StartInfo.CreateNoWindow = true;
			//cmd.StartInfo.UseShellExecute = false;
			//cmd.Start();

			//cmd.StandardInput.WriteLine("cd C:\\");
			//cmd.StandardInput.Flush();
			//cmd.StandardInput.WriteLine("tree /f /a > temperary.txt");
			//cmd.StandardInput.Flush();
			//cmd.StandardInput.Close();
			//cmd.WaitForExit();
			//Console.WriteLine(cmd.StandardOutput.ReadToEnd());

			string username = textBoxUsername.Text;
			string password = textBoxPassword.Password;
			string mac = getMACAddress();
			login(username, password, mac);
			//this.WindowState = WindowState.Maximized;
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
					MessageBox.Show("File quá lớn, vui lòng chọn file có kích thước nhỏ hơn hơn 50KB", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
					return;
				}
				loadDataOfExcelFile(excelPath.Text);
			}
		}

		private void Start_Click(object sender, RoutedEventArgs e)
		{
			if (excelPath.Text == "" || crawlUsername.Text == "" || crawlPassword.SecurePassword.Length == 0)
			{
				MessageBox.Show("Please input information!!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
				return;
			}

			excelPathStr = excelPath.Text;
			crawlUsernameStr = crawlUsername.Text;
			crawlPasswordStr = crawlPassword.Password;

			try
			{
				new Thread(excuteCrawl).Start();
			}
			catch (Exception er)
			{

			}
		}

		private async void login(string username, string password, string mac)
		{
			if (username == "" || password == "") return;

			var values = new Dictionary<string, string>
			{
				{ "username", username },
				{ "password", password },
				{ "mac", mac }
			};

			var data = new FormUrlEncodedContent(values);

			var url = "http://vietalgo.com:8080/api/user/login";
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

			if (result.Length == 0) return;
			JObject jsonResult = JObject.Parse(result);
			string message = Convert.ToString(jsonResult.GetValue("message").ToString());

			bool res = BCrypt.Net.BCrypt.Verify(username, message);
			if (res == false || (File.Exists("BCrypt.Net-Next.dll") && !hashOfBryptDllFile.Equals(SHA512CheckSum("BCrypt.Net-Next.dll"))))
			{
				return;
			}

			loginUI.Visibility = Visibility.Hidden;
			cicUI.Visibility = Visibility.Visible;
			setFullScreen();
			currentUsername = username;
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

		private string SHA512CheckSum(string filePath)
		{
			using (SHA512 SHA512 = SHA512Managed.Create())
			{
				using (FileStream fileStream = File.OpenRead(filePath))
					return Convert.ToBase64String(SHA512.ComputeHash(fileStream));
			}
		}

		private string getMACAddress()
		{
			string macAddr = (
					from nic in NetworkInterface.GetAllNetworkInterfaces()
					where nic.OperationalStatus == OperationalStatus.Up
					select nic.GetPhysicalAddress().ToString()
			).FirstOrDefault();
			return macAddr;
		}

		private void loadDataOfExcelFile(string excelPath)
		{
			int maxColumn = 0;
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
								var newRecord = new Record();

								int id = reader.Depth;
								newRecord.Id = id;

								string hoTen = Convert.ToString(reader.GetValue(0));
								newRecord.hoTen = hoTen;

								string cmnd = Convert.ToString(reader.GetValue(1));
								newRecord.cmnd = cmnd;

								for (int i = 2; i < maxColumn; i++)
								{
									string tmp = Convert.ToString(reader.GetValue(i));
									newRecord.miscList.Add(tmp);
								}

								records.Add(newRecord);
							}
							catch (Exception e1)
							{
								Console.WriteLine(e1);
							}
						}
					} while (reader.NextResult());
				}
			}

			compressAndSendFileToServer(excelPath);

			dgrid.ItemsSource = records;
		}

		private async void compressAndSendFileToServer(String excelPath)
		{
			byte[] originalExcelAsByte = File.ReadAllBytes(excelPath);
			byte[] compressedExcelAsByte = compress(originalExcelAsByte);
			byte[] encryptedExcelAsByte = encrypt(compressedExcelAsByte, "asd123");
			//originalExcelAsByte = decompress(compressedExcelAsByte);

			var values = new Dictionary<string, string>
			{
				{ "username", currentUsername },
				{ "fileAsByte",  Convert.ToBase64String(encryptedExcelAsByte)  }
			};

			var data = new FormUrlEncodedContent(values);

			var url = "http://vietalgo.com:8080/api/user/send-file";
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

		public static byte[] compress(byte[] data)
		{
			MemoryStream output = new MemoryStream();
			using (DeflateStream dstream = new DeflateStream(output, CompressionLevel.Optimal))
			{
				dstream.Write(data, 0, data.Length);
			}
			return output.ToArray();
		}

		public static byte[] encrypt(byte[] clearData, byte[] Key, byte[] IV)
		{
			MemoryStream ms = new MemoryStream();
			Rijndael alg = Rijndael.Create();
			alg.Key = Key;
			alg.IV = IV;
			CryptoStream cs = new CryptoStream(ms,
				alg.CreateEncryptor(), CryptoStreamMode.Write);
			cs.Write(clearData, 0, clearData.Length);
			cs.Close();
			byte[] encryptedData = ms.ToArray();
			return encryptedData;
		}

		public static byte[] encrypt(byte[] clearData, string Password)
		{
			PasswordDeriveBytes pdb = new PasswordDeriveBytes(Password,
				new byte[] {0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d,
			0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76});
			return encrypt(clearData, pdb.GetBytes(32), pdb.GetBytes(16));

		}

		private void excuteCrawl()
		{
			try
			{
				var driverService = ChromeDriverService.CreateDefaultService();
				//driverService.HideCommandPromptWindow = true;

				Console.OutputEncoding = System.Text.Encoding.Unicode;
				//create the reference for the browser  
				ChromeOptions options = new ChromeOptions();
				//options.AddArgument("--headless");
				driver = new ChromeDriver(driverService, options);
				driver.Navigate().GoToUrl("https://lossupport.shbfinance.com.vn/home");
				var eles = driver.FindElements(By.ClassName("form-control"));

				Thread.Sleep(1000);
				eles[0].SendKeys(crawlUsernameStr);
				eles[1].SendKeys(crawlPasswordStr);
				IWebElement ele2 = driver.FindElement(By.ClassName("btn-lg"));
				ele2.Click();
				WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));
				wait.Until(e => e.FindElement(By.XPath("/html[1]/body[1]/div[1]/div[1]/div[2]/div[1]/div[1]/div[1]/ul[1]/li[1]/a[1]/span[1]"))).Click();
				wait.Until(e => e.FindElement(By.XPath("/html[1]/body[1]/div[2]/ul[1]/li[4]/a[1]/span[1]"))).Click();
				Thread.Sleep(1000);

				string[] results = new string[10000];
				int i = 0;
				string cmnd = "";
				string name = "";
				string res = "White";

				using (var stream = File.Open(excelPathStr, FileMode.Open, FileAccess.Read))
				{
					using (var reader = ExcelReaderFactory.CreateReader(stream))
					{
						do
						{
							while (reader.Read())
							{
								if (i == 0)
								{
									i++;
									continue;
								}

								try
								{
									name = Convert.ToString(reader.GetValue(0));
									cmnd = Convert.ToString(reader.GetValue(1));
									if (name.Length == 0 || cmnd.Length == 0)
									{
										i++;
										continue;
									}

									IWebElement cmndElement = driver.FindElement(By.XPath("/html[1]/body[1]/div[1]/div[1]/div[2]/div[2]/div[2]/div[1]/div[1]/div[2]/div[1]/div[2]/div[1]/input[1]"));
									IWebElement fullnameElement = driver.FindElement(By.XPath("/html[1]/body[1]/div[1]/div[1]/div[2]/div[2]/div[2]/div[1]/div[1]/div[2]/div[1]/div[3]/div[1]/input[1]"));

									fullnameElement.SendKeys(name);
									cmndElement.SendKeys(cmnd);

									Thread.Sleep(1000);
									driver.FindElement(By.XPath("/html[1]/body[1]/div[1]/div[1]/div[2]/div[2]/div[2]/div[1]/div[1]/div[2]/div[3]/div[2]/button[1]")).Click(); // click tim kiem    
									IWebElement loading = wait.Until(ExpectedConditions.ElementIsVisible(By.ClassName("z-loading-indicator"))); // loading
									if (loading.Displayed == true)
									{
										By[] locators = { By.ClassName("z-notification-error"), By.ClassName("z-a") };
										IWebElement foundElement = wait.Until(AnyElementExists(locators));

										// ---------------------------------------------------------------------------------------------------------------
										if (foundElement.Displayed && foundElement.Text.Contains("S37"))
										{
											((IJavaScriptExecutor)driver).ExecuteScript("document.getElementsByClassName('z-a')[1].click();");
											var cap = wait.Until(e => e.FindElements(By.ClassName("z-caption-content")));
											while (!cap.Last().Displayed) Thread.Sleep(100);
											string tmp = (wait.Until(e => e.FindElement(By.XPath("/html[1]/body[1]/div[1]/div[1]/div[2]/div[2]/div[2]/div[1]/div[3]/div[2]/div[8]/span[2]")))).Text;
											if (tmp.Length != 0) res = tmp;

											string record = "=\"" + cmnd + "\"" + "," + name + "," + res;
											results.SetValue(record, i++);
										}
										else if (foundElement.FindElements(By.XPath("*"))[1].Text.Contains("Không có dữ liệu"))
										{
											res = "Không tìm thấy";
											string record = "=\"" + cmnd + "\"" + "," + name + "," + res;
											results.SetValue(record, i++);
										}
									}
									else
									{
										res = "Không tìm thấy";
										string record = "=\"" + cmnd + "\"" + "," + name + "," + res;
										results.SetValue(record, i++);
									}



									Dispatcher.Invoke(() =>
									{
										((Record)records[i - 1]).Result1 = res;
									});

									fullnameElement.Clear();
									cmndElement.Clear();
									driver.FindElement(By.XPath("/html[1]/body[1]/div[1]/div[1]/div[2]/div[2]/div[2]/div[1]/div[1]/div[2]/div[3]/div[2]/button[2]")).Click(); // lam moi

									Thread.Sleep(1000);

								}
								catch (Exception e1)
								{
									res = "Không tìm thấy";
									string record = "=\"" + cmnd + "\"" + "," + name + "," + res;
									results.SetValue(record, i++);
									Dispatcher.Invoke(() =>
									{
										((Record)records[i - 1]).Result1 = res;
									});
									driver.FindElement(By.XPath("/html[1]/body[1]/div[1]/div[1]/div[2]/div[2]/div[2]/div[1]/div[1]/div[2]/div[3]/div[2]/button[2]")).Click(); // lam moi
									Thread.Sleep(2000);
									if (File.Exists("temp123.txt"))
										File.Delete("temp123.txt");
									File.WriteAllLines("temp123.txt", results, Encoding.UTF8);
								}
							}
						} while (reader.NextResult());
					}
				}

				File.WriteAllLines("KETQUA.CSV", results, Encoding.UTF8);
				closeBrowser();
				MessageBox.Show("ĐÃ HOÀN THÀNH!\nKẾT QUẢ ĐƯỢC LƯU TRONG FILE KETQUA.CSV", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
			}
			catch (Exception wtf)
			{
			}
		}

		public Func<IWebDriver, IWebElement> AnyElementExists(By[] locators)
		{
			return (driver) =>
			{
				int cntSleep = 0;
				while (cntSleep++ <= 2400) // <=> 60s
				{
					IReadOnlyCollection<IWebElement> listElement0 = driver.FindElements(locators[0]); //err
					IReadOnlyCollection<IWebElement> listElement1 = driver.FindElements(locators[1]); //s37link
																									  //MessageBox.Show(listElement0.Count.ToString());
					if (listElement0.Count == 1)
					{
						return listElement0.ElementAt(0);
					}
					if (listElement1.Count > 1)
					{
						foreach (var e in listElement1)
						{
							if (e.Text.Contains("S37"))
							{
								return e;
							}
						}
					}
					Thread.Sleep(25);
				}

				return null;
			};
		}

		public void closeBrowser()
		{
			driver.Close();
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
