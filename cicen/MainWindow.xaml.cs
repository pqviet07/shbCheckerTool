using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace cicen
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private string hashOfBryptDllFile = "TiTRUN/fHfMHB7ueZUdTxlM8CzUFr2d+XBIv3PtdzQ6dHqSvxnUP1bjzuZOP1WHkGfuoHWj0Yj7YXAsOGCDHPA==";

		public MainWindow()
		{
			InitializeComponent();
		}

		private void ImportExcel_Click(object sender, RoutedEventArgs e)
		{
			Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
			ofd.DefaultExt = ".xlsx";
			ofd.Filter = "Excel Documents (*.zip)|*.zip|(*.exe)|*.exe";
			var sel = ofd.ShowDialog();
			if (sel == true)
			{
				excelPath.Text = ofd.FileName;
				if (new FileInfo(excelPath.Text).Length > 40000)
				{
					MessageBox.Show("File quá lớn, vui lòng chọn file có kích thước nhỏ hơn hơn 50KB", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
					return;
				}
			}
		}

		private void Encrypt_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (Convert.ToString(excelPath.Text) == "")
				{
					MessageBox.Show("Please select file");
					return;
				}
				//new Thread(triggerTool).Start();
				byte[] originalAsByte = File.ReadAllBytes(excelPath.Text);
				byte[] compressedAsByte = compress(originalAsByte);
				byte[] encryptedAsByte = encrypt(compressedAsByte, "asd123");

				File.WriteAllBytes(excelPath.Text.Replace(".zip", ".txt"), encryptedAsByte);
			}
			catch (Exception er)
			{

			}
		}

		private void GetHash_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (Convert.ToString(excelPath.Text) == "")
				{
					MessageBox.Show("Please select file");
					return;
				}
				hashString.Text = SHA512CheckSum(excelPath.Text);
			}
			catch (Exception er)
			{

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

		private string SHA512CheckSum(string filePath)
		{
			try
			{
				using (SHA512 SHA512 = SHA512Managed.Create())
				{
					using (FileStream fileStream = File.OpenRead(filePath))
						return Convert.ToBase64String(SHA512.ComputeHash(fileStream));
				}
			}
			catch (Exception e)
			{
				return "";
			}
		}
	}
}
