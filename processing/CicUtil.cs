using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Net.NetworkInformation;
using System.Net.Http;
using System.Security.Cryptography;
using System.IO.Compression;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace processing
{
	class CicUtil
	{
		private static string appPassword = "asd123";
		public static async void downloadNewVersion(string downloadedFileName, string appType)
		{
			try
			{
				string currentDirectory = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
				//string downloadedFileHash = SHA512CheckSum(currentDirectory + "\\" + downloadedFileName + ".exe");
				string downloadedFileHash = SHA512CheckSum(currentDirectory + "\\" + downloadedFileName + CicUtil.decrypt("/ukeDogQHvIGQL1ACvunQg==", appPassword));
				var values = new Dictionary<string, string>
				{
					{ "appType", appType },
					{ "versionHash",  downloadedFileHash  }
				};
				var data = new FormUrlEncodedContent(values);
				var url = CicUtil.decrypt("jaSlWs0q7Agq9XPZ+f9rwLGbxYCaf3D0o/nu4s4zvO/trAoQ0B/T01UYEf747XyIYJ+cO3389olBW+RFNHjbykvhWRY6p3T1p46obxVDWThDSPa0R3gfoYJgiHkibb+b", appPassword);
				var client = new HttpClient();
				string result = "";
				var response = await client.PostAsync(url, data);
				result = response.Content.ReadAsStringAsync().Result;
				if (result.Length == 0) return;
				JObject jsonResult = JObject.Parse(result);
				if (jsonResult.Count == 0) return;

				string downloadedFileAsString = Convert.ToString(jsonResult.GetValue("downloadedFile").ToString());
				byte[] downloadedFileAsByte = Convert.FromBase64String(downloadedFileAsString);
				byte[] decryptedContent = decrypt(downloadedFileAsByte, appPassword);
				byte[] decompressedContent = decompress(decryptedContent);

				//File.WriteAllBytes(System.IO.Path.GetTempPath() + "\\" + downloadedFileName + ".zip", decompressedContent);
				File.WriteAllBytes(System.IO.Path.GetTempPath() + "\\" + downloadedFileName + CicUtil.decrypt("/gfCKDXXeXFVnvDaKaVbFw==", appPassword), decompressedContent);
				Thread.Sleep(1000);

				////ZipFile.ExtractToDirectory(currentDirectory + "\\shbChecker.zip", currentDirectory);

				//extractToDirectoryWithOverwrite(System.IO.Path.GetTempPath() + "\\" + downloadedFileName + ".zip", currentDirectory);
				extractToDirectoryWithOverwrite(System.IO.Path.GetTempPath() + "\\" + downloadedFileName + CicUtil.decrypt("/gfCKDXXeXFVnvDaKaVbFw==", appPassword), currentDirectory);
				//FileInfo file = new FileInfo(System.IO.Path.GetTempPath() + "\\" + downloadedFileName + ".zip");
				FileInfo file = new FileInfo(System.IO.Path.GetTempPath() + "\\" + downloadedFileName + CicUtil.decrypt("/gfCKDXXeXFVnvDaKaVbFw==", appPassword));

				while (isFileLocked(file))
					Thread.Sleep(1000);
				file.Delete();
				//File.Delete(currentDirectory + "\\shbChecker.zip");
			}
			catch (Exception e)
			{
				return;
			}
		}

		private static void extractToDirectoryWithOverwrite(string zipPath, string extractPath)
		{
			ZipArchive archive = ZipFile.OpenRead(zipPath);
			foreach (var entry in archive.Entries)
			{
				entry.ExtractToFile(Path.Combine(extractPath, entry.FullName), true);
			}
		}

		private static bool isFileLocked(FileInfo file)
		{
			FileStream stream = null;
			try
			{
				stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
			}
			catch (IOException)
			{
				return true;
			}
			finally
			{
				if (stream != null)
					stream.Close();
			}
			return false;
		}

		public static string SHA512CheckSum(string filePath)
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

		public static string getMACAddress()
		{
			string macAddr = (
					from nic in NetworkInterface.GetAllNetworkInterfaces()
					where nic.OperationalStatus == OperationalStatus.Up
					select nic.GetPhysicalAddress().ToString()
			).FirstOrDefault();
			return macAddr;
		}

		public static string getCurrentDirectory()
		{
			return System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
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

		private static byte[] encrypt(byte[] clearData, byte[] Key, byte[] IV)
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
	}
}
