using Mocaccino.Log;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Mocaccino.Security
{
    class Crypter
    {
        private static int _blockSize = 128;
        private static int _keySize = 256;
        private static int _iterations = 50000;
        private static int _bufferLength = 1048576;
        private static int _saltLength = 32;
        private static string _fileExtension = ".mocaccino";

        public static PaddingMode _paddingMode = PaddingMode.PKCS7;
        public static CipherMode _cipherMode = CipherMode.CBC;

        ///Call this function to remove the key from memory after use for security
        [DllImport("KERNEL32.DLL", EntryPoint = "RtlZeroMemory")]
        public static extern bool ZeroMemory(IntPtr destination, int length);
        ///public unsafe static extern bool ZeroMemory(byte* destination, int length);

        /// <summary>
        /// Creates a random salt that will be used to encrypt your file. This method is required on FileEncrypt.
        /// </summary>
        /// <returns></returns>
        public static byte[] GenerateRandomSalt()
        {
            byte[] data = new byte[_saltLength];

            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                for (int i = 0; i < _saltLength / 2; i++)
                {
                    //Fill the buffer with the generated data.
                    rng.GetBytes(data);
                }
            }
            return data;
        }

        /// <summary>
        /// Encrypts a file from its path and a plain password.
        /// </summary>
        /// <param name="inputFile">The file to encrypt.</param>
        /// <param name="password">The key used to encrypt the file.</param>
        public static bool FileEncrypt(string inputFile, string password)
        {
            //Generate random salt.
            byte[] salt = GenerateRandomSalt();

            //Create output file name.
            string outputFile = $"{inputFile}{_fileExtension}";
            FileStream fsOut = new FileStream(outputFile, FileMode.Create);

            //Convert password string to byte arrray.
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            //Set Rijndael symmetric encryption algorithm.
            RijndaelManaged AES = new RijndaelManaged
            {
                KeySize = _keySize,
                BlockSize = _blockSize,
                Padding = _paddingMode
            };

            //"What it does is repeatedly hash the user password along with the salt." High iteration counts.
            Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(passwordBytes, salt, _iterations);
            AES.Key = key.GetBytes(AES.KeySize / 8);
            AES.IV = key.GetBytes(AES.BlockSize / 8);

            //Cipher modes.
            AES.Mode = _cipherMode;

            //Write salt to the begining of the output file, so in this case can be random every time.
            fsOut.Write(salt, 0, salt.Length);

            CryptoStream cs = new CryptoStream(fsOut, AES.CreateEncryptor(), CryptoStreamMode.Write);

            //If file attributes include Read-only, this can't create new filestream with FileStream(string path, FileMode mode);
            //Another way is change file attributes to Normal by SetAttributes(string path, FileAttributes fileAttributes);
            FileStream fsIn = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.None);

            //Create a buffer (1mb) so only this amount will allocate in the memory and not the whole file.
            byte[] buffer = new byte[_bufferLength];

            int read;
            try
            {
                while ((read = fsIn.Read(buffer, 0, _bufferLength)) > 0)
                {
                    cs.Write(buffer, 0, read);
                }

                //Close up.
                fsIn.Close();
                cs.Close();
                fsOut.Close();

                if (File.GetAttributes(inputFile).HasFlag(FileAttributes.ReadOnly))
                {
                    File.SetAttributes(inputFile, FileAttributes.Normal);
                }

                File.Delete(inputFile);
                File.Move(outputFile, inputFile);

                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"[Error] {ex.Message} Error file: {inputFile}");
                return false;
            }
        }

        /// <summary>
        /// Decrypts an encrypted file with the FileEncrypt method through its path and the plain password.
        /// </summary>
        /// <param name="inputFile"></param>
        /// <param name="password"></param>
        public static bool FileDecrypt(string inputFile, string password)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] salt = new byte[_saltLength];

            FileStream fsIn = new FileStream(inputFile, FileMode.Open);
            fsIn.Read(salt, 0, salt.Length);

            RijndaelManaged AES = new RijndaelManaged
            {
                KeySize = _keySize,
                BlockSize = _blockSize
            };

            Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(passwordBytes, salt, _iterations);
            AES.Key = key.GetBytes(AES.KeySize / 8);
            AES.IV = key.GetBytes(AES.BlockSize / 8);
            AES.Padding = _paddingMode;
            AES.Mode = _cipherMode;

            CryptoStream cs = new CryptoStream(fsIn, AES.CreateDecryptor(), CryptoStreamMode.Read);

            string outputFile = $"{inputFile}{_fileExtension}";
            FileStream fsOut = new FileStream(outputFile, FileMode.Create);

            int read;
            byte[] buffer = new byte[_bufferLength];

            try
            {
                while ((read = cs.Read(buffer, 0, _bufferLength)) > 0)
                {
                    fsOut.Write(buffer, 0, read);
                }
                cs.Close();
                fsIn.Close();
                fsOut.Close();

                File.Delete(inputFile);
                File.Move(outputFile, inputFile);

                return true;
            }
            catch (CryptographicException cryptographicException)
            {
                Logger.WriteLine($"[Cryptographic Exception Error] {cryptographicException.Message}\nError file: {inputFile}");
            }
            catch (Exception exception)
            {
                Logger.WriteLine($"[Error] {exception.Message} Error file:{inputFile}");
            }
            return false;
        }
    }
}
