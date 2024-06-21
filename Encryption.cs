using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Encryption
{
    public class StringEncryptionService
    {
        private static byte[] GenerateRandomKey(int keySizeInBits)
        {
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = keySizeInBits;
                aes.GenerateKey();
                return aes.Key;
            }
        }

        public static byte[] GenerateRandomIV()
        {
            using (Aes aes = Aes.Create())
            {
                aes.GenerateIV();
                return aes.IV;
            }
        }

        private const int KeySizeInBits = 256; // Choose 128, 192, or 256 bits
        private byte[] SecKey = GenerateRandomKey(KeySizeInBits); // Secret key for encryption
        private byte[] SecIV = GenerateRandomIV();
        private Aes AesAlg = Aes.Create();

        public string Encrypt(string plainText)
        {
            AesAlg.Key = SecKey;
            AesAlg.IV = SecIV;
            using var encryptor = AesAlg.CreateEncryptor();

            byte[] encryptedBytes;
            using (var msEncrypt = new MemoryStream())
            {
                using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                using (var swEncrypt = new StreamWriter(csEncrypt))
                {
                    swEncrypt.Write(plainText);
                }
                encryptedBytes = msEncrypt.ToArray();
            }

            return Convert.ToBase64String(encryptedBytes);
        }

        public string Decrypt(string encryptedText)
        {
            AesAlg.Key = SecKey;
            AesAlg.IV = SecIV;
            var decryptor = AesAlg.CreateDecryptor();

            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
            using var msDecrypt = new MemoryStream(encryptedBytes);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);

            return srDecrypt.ReadToEnd();
        }

        private static byte[] HexStringToBytes(string hexString)
        {
            // Remove any dashes that might be present from previous conversions
            hexString = hexString.Replace("-", "");

            // Allocate byte array based on half of hex string length
            byte[] bytes = new byte[hexString.Length / 2];

            // Convert each pair of hex digits to a byte
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }

            // Convert byte array to string using UTF8 encoding
            return bytes;
        }

        public void SetKey(string Input)
        {
            SecKey = HexStringToBytes(Input);
        }

        public void SetIV(string Input)
        {
            SecIV = HexStringToBytes(Input);
        }

        public string ShowKey()
        {
            return Convert.ToHexString(SecKey);
        }

        public string ShowIV()
        {
            return Convert.ToHexString(SecIV);
        }
    }
}
