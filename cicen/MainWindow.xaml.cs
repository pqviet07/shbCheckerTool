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
			ofd.Filter = "Excel Documents (*.zip)|*.zip|(*.exe)|*.exe|(*.dll)|*.dll";
			var sel = ofd.ShowDialog();
			if (sel == true)
			{
				excelPath.Text = ofd.FileName;
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

		private void EncryptString_Click(object sender, RoutedEventArgs e)
		{
			if (Convert.ToString(endecryptString.Text) == "")
			{
				return;
			}
			endecryptString.Text = encrypt(endecryptString.Text, "asd123");
		}

		private void DecryptString_Click(object sender, RoutedEventArgs e)
		{
			if (Convert.ToString(endecryptString.Text) == "")
			{
				return;
			}
			endecryptString.Text = decrypt(endecryptString.Text, "asd123");
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

		public static string encrypt(string clearText, string Password)
		{
			byte[] clearBytes =
			  System.Text.Encoding.Unicode.GetBytes(clearText);
			PasswordDeriveBytes pdb = new PasswordDeriveBytes(Password,
				new byte[] {0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d,
			0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76});

			byte[] encryptedData = encrypt(clearBytes,
					 pdb.GetBytes(32), pdb.GetBytes(16));
			return Convert.ToBase64String(encryptedData);
		}

		private static byte[] decrypt(byte[] cipherData,
									byte[] Key, byte[] IV)
		{
			MemoryStream ms = new MemoryStream();
			Rijndael alg = Rijndael.Create();
			alg.Key = Key;
			alg.IV = IV;
			CryptoStream cs = new CryptoStream(ms,
				alg.CreateDecryptor(), CryptoStreamMode.Write);
			cs.Write(cipherData, 0, cipherData.Length);
			cs.Close();
			byte[] decryptedData = ms.ToArray();

			return decryptedData;
		}

		public static byte[] decrypt(byte[] cipherData, string Password)
		{
			PasswordDeriveBytes pdb = new PasswordDeriveBytes(Password,
				new byte[] {0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d,
			0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76});
			return decrypt(cipherData, pdb.GetBytes(32), pdb.GetBytes(16));
		}

		public static string decrypt(string cipherText, string Password)
		{
			byte[] cipherBytes = Convert.FromBase64String(cipherText);
			PasswordDeriveBytes pdb = new PasswordDeriveBytes(Password,
				new byte[] {0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65,
			0x64, 0x76, 0x65, 0x64, 0x65, 0x76});
			byte[] decryptedData = decrypt(cipherBytes,
				pdb.GetBytes(32), pdb.GetBytes(16));
			return System.Text.Encoding.Unicode.GetString(decryptedData);
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
