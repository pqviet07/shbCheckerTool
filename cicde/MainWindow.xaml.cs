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

namespace cicde
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private void ImportExcel_Click(object sender, RoutedEventArgs e)
		{
			Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
			ofd.DefaultExt = ".xlsx";
			ofd.Filter = "Excel Documents (*.txt)|*.txt";
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

		private void Start_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (Convert.ToString(excelPath.Text) == "")
				{
					MessageBox.Show("Please select file");
					return;
				}
				//new Thread(triggerTool).Start();
				byte[] content = File.ReadAllBytes(excelPath.Text);
				byte[] decryptedContent = decrypt(content, "asd123");
				byte[] compressedContent = decompress(decryptedContent);
				File.WriteAllBytes(excelPath.Text.Substring(0, excelPath.Text.LastIndexOf('\\') + 1) + "message.xlsx", compressedContent);
			}
			catch (Exception er)
			{

			}
		}

		public static byte[] decompress(byte[] data)
		{
			MemoryStream input = new MemoryStream(data);
			MemoryStream output = new MemoryStream();
			using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
			{
				dstream.CopyTo(output);
			}
			return output.ToArray();
		}

		public static byte[] decrypt(byte[] cipherData,
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
	}


}
