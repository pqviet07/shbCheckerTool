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

namespace ShbChecker
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private string hashOfBryptDllFile = "TiTRUN/fHfMHB7ueZUdTxlM8CzUFr2d+XBIv3PtdzQ6dHqSvxnUP1bjzuZOP1WHkGfuoHWj0Yj7YXAsOGCDHPA==";
		public ArrayList records = new ArrayList();

		public MainWindow()
		{
			InitializeComponent();
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			loginUI.Visibility = Visibility.Visible;
			cicUI.Visibility = Visibility.Hidden;
			this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
			this.ResizeMode = System.Windows.ResizeMode.NoResize;
		}

		private void Login_Click(object sender, RoutedEventArgs e)
		{
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
				loadDataOfExcelFile(excelPath.Text);
			}
		}

		private void Start_Click(object sender, RoutedEventArgs e)
		{
			if (excelPath.Text == "" || crawlUsername.Text == "" || crawlPassword.Text == "")
			{
				MessageBox.Show("Please input information!!");
				return;
			}

			//Thread thread = new Thread(new ThreadStart(() =>{
			//	triggerTool(excelPath.Text, crawlUsername.Text, crawlPassword.Text);
			//}));
			//thread.Start();

			try
			{
				triggerTool(excelPath.Text, crawlUsername.Text, crawlPassword.Text);
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
		}

		private void setFullScreen()
		{
			this.Left = 0;
			this.Top = 0;
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
								if (reader.GetValue(0) == null) break;
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

			dgSimple.ItemsSource = records;
		}

		private void triggerTool(string excelPath, string crawlUsername, string crawlPassword)
		{
			try
			{
				Console.OutputEncoding = System.Text.Encoding.Unicode;
				//create the reference for the browser  
				ChromeOptions options = new ChromeOptions();
				options.AddArgument("--headless");
				IWebDriver driver = new ChromeDriver(options);
				driver.Navigate().GoToUrl("https://lossupport.shbfinance.com.vn/home");
				var eles = driver.FindElements(By.ClassName("form-control"));

				Thread.Sleep(1000);
				eles[0].SendKeys(crawlUsername);
				eles[1].SendKeys(crawlPassword);
				IWebElement ele2 = driver.FindElement(By.ClassName("btn-lg"));
				ele2.Click();
				WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));
				wait.Until(e => e.FindElement(By.XPath("/html[1]/body[1]/div[1]/div[1]/div[2]/div[1]/div[1]/div[1]/ul[1]/li[1]/a[1]/span[1]"))).Click();
				wait.Until(e => e.FindElement(By.XPath("/html[1]/body[1]/div[2]/ul[1]/li[4]/a[1]/span[1]"))).Click();
				Thread.Sleep(2000);
				//Console.Clear();
				string[] results = new string[10000];
				int i = 0;
				using (var stream = File.Open(excelPath, FileMode.Open, FileAccess.Read))
				{
					using (var reader = ExcelReaderFactory.CreateReader(stream))
					{
						do
						{
							while (reader.Read())
							{
								if (i == 0) { i++; continue; }
								try
								{
									string name = reader.GetString(0);
									string cmnd = reader.GetValue(1).ToString();
									string res = "White";

									IWebElement cmndEle = driver.FindElement(By.XPath("/html[1]/body[1]/div[1]/div[1]/div[2]/div[2]/div[2]/div[1]/div[1]/div[2]/div[1]/div[2]/div[1]/input[1]"));
									IWebElement fullnameEle = driver.FindElement(By.XPath("/html[1]/body[1]/div[1]/div[1]/div[2]/div[2]/div[2]/div[1]/div[1]/div[2]/div[1]/div[3]/div[1]/input[1]"));
									fullnameEle.SendKeys(name);
									cmndEle.SendKeys(cmnd);
									Thread.Sleep(100);
									driver.FindElement(By.XPath("/html[1]/body[1]/div[1]/div[1]/div[2]/div[2]/div[2]/div[1]/div[1]/div[2]/div[3]/div[2]/button[1]")).Click(); // click tim kiem    
									IWebElement loading = wait.Until(e => e.FindElement(By.ClassName("z-loading-indicator"))); // click tim kiem    
									if (loading.Displayed == true)
									{
										bool foundS37 = false;
										int cntWait = 0;
										while (cntWait <= 100)
										{
											IWebElement s37Link = driver.FindElements(By.ClassName("z-a"))[1];
											if (s37Link.Text.Contains("S37"))
											{
												((IJavaScriptExecutor)driver).ExecuteScript("document.getElementsByClassName('z-a')[1].click();");
												var cap = wait.Until(e => e.FindElements(By.ClassName("z-caption-content")));
												while (!cap.Last().Displayed) Thread.Sleep(100);
												string tmp = (wait.Until(e => e.FindElement(By.XPath("/html[1]/body[1]/div[1]/div[1]/div[2]/div[2]/div[2]/div[1]/div[3]/div[2]/div[8]/span[2]")))).Text;
												if (tmp.Length != 0) res = tmp;

												string record = "=\"" + cmnd + "\"" + "," + name + "," + res;
												results.SetValue(record, i++);
												foundS37 = true;
												break;
											}

											Thread.Sleep(250);
											cntWait++;
										}

										if (!foundS37)
										{
											//Console.WriteLine("WTF; " + cntWait * 250 / 1000);
											try
											{
												WebDriverWait wait2 = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
												IWebElement noti = wait2.Until(e => e.FindElement(By.ClassName("z-notification-content")));
												if (noti.Displayed && noti.Text.Contains("Không có dữ liệu"))
												{
													res = "Không tìm thấy";
													string record = "=\"" + cmnd + "\"" + "," + name + "," + res;
													results.SetValue(record, i++);
													Thread.Sleep(3000);
												}
											}
											catch (Exception e)
											{
												//Console.WriteLine(e);
											}
										}
									}

									//Console.WriteLine("DONE " + i);
									fullnameEle.Clear();
									cmndEle.Clear();
									driver.FindElement(By.XPath("/html[1]/body[1]/div[1]/div[1]/div[2]/div[2]/div[2]/div[1]/div[1]/div[2]/div[3]/div[2]/button[2]")).Click(); // lam moi

									Thread.Sleep(1000);

								}
								catch (Exception e1)
								{
									Console.WriteLine(e1);
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
				File.WriteAllLines("results.txt", results, Encoding.UTF8);
				driver.Close();
				Console.Write("test case ended ");
			}
			catch (Exception wtf) { }
		}
	}

	public class User
	{
		public string username { get; set; }
		public string password { get; set; }
		public string mac { get; set; }
	}

	public class Record
	{
		public int Id { get; set; }
		public string hoTen { get; set; }
		public string cmnd { get; set; }
		public List<string> miscList = new List<string>();
		public string result1 { get; set; }
		public string result2 { get; set; }
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
