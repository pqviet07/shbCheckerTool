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
        private string hashOfBryptDllFile = "sRSOd8eN8zwkS6v2fQKCMMek78blctg1s19X/mNzNWDiYABJMt/By6+v6suyx3EyuzG8iG+AWa3F/1HB35OPQw==";
        private string downloadedFileName = "processing";
        private string appPassword = "asd123";

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
            
            try
            {
                var data = new FormUrlEncodedContent(values);

                var url = CicUtil.decrypt("jaSlWs0q7Agq9XPZ+f9rwLGbxYCaf3D0o/nu4s4zvO/trAoQ0B/T01UYEf747XyIYJ+cO3389olBW+RFNHjbyk9sW8+iyGg6l+b+LHqGWlQ=", appPassword);
                var client = new HttpClient();
                string result = "";
                var response = await client.PostAsync(url, data);
                result = response.Content.ReadAsStringAsync().Result;

                if (result.Length == 0) return;
                JObject jsonResult = JObject.Parse(result);
                string message = Convert.ToString(jsonResult.GetValue("message").ToString());

                bool res = BCrypt.Net.BCrypt.Verify(username, message);
                if (res == false || (File.Exists("BCrypt.Net-Core.dll") && !hashOfBryptDllFile.Equals(CicUtil.SHA512CheckSum("BCrypt.Net-Core.dll"))))
                {
                    return;
                }

                string checkedFileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".txt";

                File.Create(checkedFileName).Close();

                var process = new Process();

                process.StartInfo = new ProcessStartInfo(CicUtil.getCurrentDirectory() + "\\" + downloadedFileName + ".exe");
                process.StartInfo.Arguments = "*" + checkedFileName + "*" + username;
                process.Start();

                this.Close();
            }
            catch (Exception e)
            {
                return;
            }
        }
    }
}
