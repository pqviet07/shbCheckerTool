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
        public static async void downloadNewVersion(string downloadedFileName, string appType)
        {
            try
            {
                string currentDirectory = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                string downloadedFileHash = SHA512CheckSum(currentDirectory + "\\" + downloadedFileName + ".exe");

                var values = new Dictionary<string, string>
                {
                    { "appType", appType },
                    { "versionHash",  downloadedFileHash  }
                };
                var data = new FormUrlEncodedContent(values);
                var url = "http://vietalgo.com:8080/api/user/download-file";
                var client = new HttpClient();
                string result = "";
                var response = await client.PostAsync(url, data);
                result = response.Content.ReadAsStringAsync().Result;
                if (result.Length == 0) return;
                JObject jsonResult = JObject.Parse(result);
                if (jsonResult.Count == 0) return;

                string downloadedFileAsString = Convert.ToString(jsonResult.GetValue("downloadedFile").ToString());
                byte[] downloadedFileAsByte = Convert.FromBase64String(downloadedFileAsString);
                byte[] decryptedContent = decrypt(downloadedFileAsByte, "asd123");
                byte[] decompressedContent = decompress(decryptedContent);

                File.WriteAllBytes(currentDirectory + "\\" + downloadedFileName + ".zip", decompressedContent);
                Thread.Sleep(1000);
                //ZipFile.ExtractToDirectory(currentDirectory + "\\shbChecker.zip", currentDirectory);
                extractToDirectoryWithOverwrite(currentDirectory + "\\" + downloadedFileName + ".zip", currentDirectory);
                FileInfo file = new FileInfo(currentDirectory + "\\" + downloadedFileName + ".zip");
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
    }
}
