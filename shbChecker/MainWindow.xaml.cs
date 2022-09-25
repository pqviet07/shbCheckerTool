using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Net.Http;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Threading;
using Org.BouncyCastle.Crypto.Generators;
using processing;

namespace shbChecker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string hashOfBryptDllFile = "TiTRUN/fHfMHB7ueZUdTxlM8CzUFr2d+XBIv3PtdzQ6dHqSvxnUP1bjzuZOP1WHkGfuoHWj0Yj7YXAsOGCDHPA==";
        private string downloadedFileName = "processing";

        public MainWindow()
        {
            InitializeComponent();
            loginUI.Visibility = Visibility.Visible;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.ResizeMode = System.Windows.ResizeMode.NoResize;
            new Thread(() => CicUtil.downloadNewVersion(downloadedFileName, "cic-core")).Start();
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
            string mac = CicUtil.getMACAddress();
            login(username, password, mac);
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
            if (res == false || (File.Exists("BCrypt.Net-Next.dll") && !hashOfBryptDllFile.Equals(CicUtil.SHA512CheckSum("BCrypt.Net-Next.dll"))))
            {
                return;
            }

            string checkedFileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".txt";

            File.Create(checkedFileName).Close();

            var process = new Process();

            string a = CicUtil.getCurrentDirectory() + "\\" + downloadedFileName + ".exe";

            process.StartInfo = new ProcessStartInfo(CicUtil.getCurrentDirectory() + "\\" + downloadedFileName + ".exe");
            process.StartInfo.Arguments = "*" + checkedFileName + "*" + username;
            process.Start();

            this.Close();
        }
    }
}
