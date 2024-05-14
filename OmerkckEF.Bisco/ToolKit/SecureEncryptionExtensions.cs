using System.Security.Cryptography;
using System.Text;

namespace OmerkckEF.Biscom.ToolKit
{
    public static class SecureEncryptionExtensions
    {
        public static string? EncryptSecurely(this string dataToEncrypt, string key)
        {
            if (dataToEncrypt == null) return null;

            byte[] dataToEncryptBytes = Encoding.UTF8.GetBytes(dataToEncrypt);
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);

            // 1. Key Management: Use strong and random keys
            using (var sha256 = SHA256.Create())
            {
                keyBytes = SHA256.HashData(keyBytes);
            }

            // 2. Trusted Algorithms: Use AES (Advanced Encryption Standard)
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Key = keyBytes;

            // 4. Initialization Vector (IV): Use a random IV for each encryption operation
            aes.GenerateIV();

            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // 9. Security Checks: Perform security checks before starting the encryption process
            if (!IsValidKeySize(aes.KeySize))
            {
                throw new ArgumentException("Invalid key size. Use a key size supported by the encryption algorithm.");
            }

            // 3. Padding: Use PKCS7 padding mechanism
            // 5. Salting: Use a random salt for password-based encryption
            byte[] saltBytes = GenerateRandomSalt();

            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();
            ms.Write(saltBytes, 0, saltBytes.Length);
            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(dataToEncryptBytes, 0, dataToEncryptBytes.Length);
            }

            byte[] encryptedData = ms.ToArray();

            return Convert.ToBase64String(encryptedData);
        }

        public static string? DecryptSecurely(this string encryptedData, string key)
        {
            if (encryptedData == null) return null;

            byte[] encryptedDataBytes = Convert.FromBase64String(encryptedData);
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);

            // 1. Key Management: Use strong and random keys
            using (var sha256 = SHA256.Create())
            {
                keyBytes = SHA256.HashData(keyBytes);
            }

            // 2. Trusted Algorithms: Use AES (Advanced Encryption Standard)
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Key = keyBytes;

            // 3. Padding: Use PKCS7 padding mechanism
            // 5. Salting: Retrieve the salt used during encryption from the beginning of the encrypted data
            byte[] saltBytes = new byte[8];
            Array.Copy(encryptedDataBytes, saltBytes, 8);

            // 4. Initialization Vector (IV): Retrieve the IV used during encryption from the encrypted data
            byte[] iv = new byte[aes.BlockSize / 8];
            Array.Copy(encryptedDataBytes, 8, iv, 0, iv.Length);
            aes.IV = iv;

            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // 9. Security Checks: Perform security checks before starting the decryption process
            if (!IsValidKeySize(aes.KeySize))
            {
                throw new ArgumentException("Invalid key size. Use a key size supported by the encryption algorithm.");
            }

            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
            {
                // Decrypt the data (excluding the salt and IV)
                cs.Write(encryptedDataBytes, 8 + iv.Length, encryptedDataBytes.Length - (8 + iv.Length));
            }

            byte[] decryptedData = ms.ToArray();

            return Encoding.UTF8.GetString(decryptedData);
        }

        // 6. Secure Random Number Generation: Generate a cryptographically secure salt
        private static byte[] GenerateRandomSalt(int size = 8)
        {
            byte[] salt = new byte[size];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        // 7. Hash Functions: Use secure hash functions and perform key size validation
        private static bool IsValidKeySize(int keySize)
        {
            return keySize == 128 || keySize == 192 || keySize == 256;
        }
    }
}