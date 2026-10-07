using Project.Base.Commons.Formats;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Project.Core.Extentions
{
    public static class TextExtentions
    {
        private const string PatternForSplittingWords = @"(?<=[A-Z])(?=[A-Z][a-z])|(?<=[^A-Z])(?=[A-Z])|(?<=[A-Za-z])(?=[^A-Za-z])";
        private const string PatternGuid = @"^(\{){0,1}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\}){0,1}$";

        public static bool IsNotNullOrWhiteSpace(this string text)
        {
            return !string.IsNullOrWhiteSpace(text);
        }

        public static bool IsNullOrWhiteSpace(this string text)
        {
            return string.IsNullOrWhiteSpace(text);
        }

        public static bool IsGuid(this string candidate)
        {
            Regex isGuid = new Regex(PatternGuid, RegexOptions.Compiled);
            bool isValid = false;

            if (candidate != null)
            {
                if (isGuid.IsMatch(candidate))
                {
                    isValid = true;
                }
            }

            return isValid;
        }

        public static bool IsNumber(this string candidate)
        {
            bool isValid = false;

            if (candidate != null)
            {
                isValid = candidate.All(char.IsDigit);
            }

            return isValid;
        }

        public static bool IsNumeric(this string candidate)
        {
            bool isValid = false;

            if (candidate != null)
            {
                isValid = double.TryParse(Convert.ToString(candidate), NumberStyles.Any, NumberFormatInfo.InvariantInfo, out _);
            }

            return isValid;
        }

        public static bool IsIntegralTypes(this string candidate, Type type)
        {
            bool isValid = false;

            if (candidate != null)
            {
                if (type == typeof(sbyte) || type == typeof(sbyte?))
                {
                    isValid = sbyte.TryParse(candidate, out _);
                }
                else if (type == typeof(byte) || type == typeof(byte?))
                {
                    isValid = byte.TryParse(candidate, out _);
                }
                else if (type == typeof(short) || type == typeof(short?))
                {
                    isValid = short.TryParse(candidate, out _);
                }
                else if (type == typeof(ushort) || type == typeof(ushort?))
                {
                    isValid = ushort.TryParse(candidate, out _);
                }
                else if (type == typeof(int) || type == typeof(int?))
                {
                    isValid = int.TryParse(candidate, out _);
                }
                else if (type == typeof(long) || type == typeof(long?))
                {
                    isValid = long.TryParse(candidate, out _);
                }
                else if (type == typeof(ulong) || type == typeof(ulong?))
                {
                    isValid = ulong.TryParse(candidate, out _);
                }
            }

            return isValid;
        }

        public static bool IsDate(this string candidate)
        {
            bool isValid = false;

            if (candidate != null)
            {
                isValid = DateTime.TryParseExact(candidate, DateTimeFormats.yyyy_MM_dd, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
            }

            return isValid;
        }

        public static bool IsBase64(this string base64String)
        {
            if (string.IsNullOrEmpty(base64String) ||
                base64String.Length % 4 != 0 ||
                base64String.Contains(" ") ||
                base64String.Contains("\t") ||
                base64String.Contains("\r") ||
                base64String.Contains("\n"))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static string SplitWords(this string sentence)
        {
            var regexForSplittingWords = new Regex(PatternForSplittingWords, RegexOptions.Compiled);
            return regexForSplittingWords.Replace(sentence, " ");
        }

        public static string GetTwoAbbreviation(string value)
        {
            var data = value.Split(' ');
            if (data.Count() > 1)
            {
                return data[0].Substring(0, 1).ToUpper() + data[1].Substring(0, 1).ToUpper();
            }

            return data[0].Substring(0, 1).ToUpper();
        }

        public static string SplitPascalCase(this string str)
        {
            return Regex.Replace(
                Regex.Replace(
                    str,
                    @"(\P{Ll})(\P{Ll}\p{Ll})",
                    "$1 $2"
                ),
                @"(\p{Ll})(\P{Ll})",
                "$1 $2"
            );
        }

        public static string RemoveLastText(this string text, string character)
        {
            if (text.Length < 1)
            {
                return text;
            }

            if (text.Substring(text.Length - character.Length, character.Length) != character)
            {
                return text;
            }

            return text.Remove(text.ToString().LastIndexOf(character), character.Length);
        }

        public static string ToBase64EncodeWithKey(this string text, string key)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(key))
            {
                return text;
            }

            var keyValue = key.ToBase64Decode();
            return (text + keyValue).ToBase64Encode();
        }

        public static string ToBase64DecodeWithKey(this string text, string key)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(key))
            {
                return text;
            }

            var keyValue = key.ToBase64Decode();

            byte[] base64EncodedBytes = Convert.FromBase64String(text);
            var textDecode = Encoding.UTF8.GetString(base64EncodedBytes);
            int length = textDecode.Length - keyValue.Length;

            return textDecode.Substring(0, length);
        }

        public static string ToBase64Encode(this string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            byte[] textBytes = Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(textBytes);
        }

        public static string ToBase64Decode(this string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            byte[] base64EncodedBytes = Convert.FromBase64String(text);
            var textDecode = Encoding.UTF8.GetString(base64EncodedBytes);
            int length = textDecode.Length;

            return textDecode.Substring(0, length);
        }

        public static string Md5Encrypt(this string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            byte[] inputBytes = Encoding.UTF8.GetBytes(text);

            try
            {
                byte[] hashBytes = SHA256.HashData(inputBytes);
                return Convert.ToHexString(hashBytes).ToLowerInvariant();
            }
            finally
            {
                CryptographicOperations.ZeroMemory(inputBytes);
            }
        }

        public static string Md5Encrypt(this string text, string key)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            // kalau key kosong → fallback ke hash biasa
            if (string.IsNullOrEmpty(key))
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(text);

                try
                {
                    byte[] hashBytes = SHA256.HashData(inputBytes);
                    return Convert.ToHexString(hashBytes).ToLowerInvariant();
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(inputBytes);
                }
            }

            byte[] textBytes = Encoding.UTF8.GetBytes(text);
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);

            try
            {
                using var hmac = new HMACSHA256(keyBytes);
                byte[] hashBytes = hmac.ComputeHash(textBytes);

                return Convert.ToHexString(hashBytes).ToLowerInvariant();
            }
            finally
            {
                CryptographicOperations.ZeroMemory(textBytes);
                CryptographicOperations.ZeroMemory(keyBytes);
            }
        }

        public static string GetEncodeTextMd5(string textValue, string securityMD5EncryptKey)
        {
            if (string.IsNullOrEmpty(textValue))
            {
                return textValue;
            }

            var textEncrypt = textValue.Md5Encrypt();
            var textFormat = securityMD5EncryptKey + textEncrypt + securityMD5EncryptKey;
            var textFormatEncrypt = textFormat.Md5Encrypt();

            return textFormatEncrypt;
        }

        public static string DecryptText(string text, string key)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            const int aesKeySize = 32;       // 256-bit
            const int aesNonceSize = 12;     // 96-bit
            const int aesTagSize = 16;       // 128-bit
            const int pbkdf2SaltSize = 16;   // 128-bit
            const int pbkdf2Iterations = 100_000;
            const byte payloadVersion = 1;

            byte[] payload = Convert.FromBase64String(text);

            int minimumLength = 1 + pbkdf2SaltSize + aesNonceSize + aesTagSize + 1;
            if (payload.Length < minimumLength)
            {
                throw new CryptographicException("Invalid encrypted payload.");
            }

            int offset = 0;

            byte version = payload[offset++];
            if (version != payloadVersion)
            {
                throw new CryptographicException("Unsupported encrypted payload version.");
            }

            byte[] salt = payload[offset..(offset + pbkdf2SaltSize)];
            offset += pbkdf2SaltSize;

            byte[] nonce = payload[offset..(offset + aesNonceSize)];
            offset += aesNonceSize;

            byte[] tag = payload[offset..(offset + aesTagSize)];
            offset += aesTagSize;

            byte[] cipherBytes = payload[offset..];
            byte[] plainBytes = new byte[cipherBytes.Length];
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] derivedKey = new byte[aesKeySize];

            try
            {
                Rfc2898DeriveBytes.Pbkdf2(
                    password: keyBytes,
                    salt: salt,
                    iterations: pbkdf2Iterations,
                    hashAlgorithm: HashAlgorithmName.SHA256,
                    destination: derivedKey);

                using var aes = new AesGcm(derivedKey, aesTagSize);
                aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

                return Encoding.UTF8.GetString(plainBytes);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(keyBytes);
                CryptographicOperations.ZeroMemory(derivedKey);
                CryptographicOperations.ZeroMemory(plainBytes);
            }
        }

        public static string EncryptText(string text, string key)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            const int aesKeySize = 32;       // 256-bit
            const int aesNonceSize = 12;     // 96-bit (GCM standard)
            const int aesTagSize = 16;       // 128-bit
            const int pbkdf2SaltSize = 16;   // 128-bit
            const int pbkdf2Iterations = 100_000;
            const byte payloadVersion = 1;

            byte[] salt = RandomNumberGenerator.GetBytes(pbkdf2SaltSize);
            byte[] nonce = RandomNumberGenerator.GetBytes(aesNonceSize);
            byte[] plainBytes = Encoding.UTF8.GetBytes(text);
            byte[] cipherBytes = new byte[plainBytes.Length];
            byte[] tag = new byte[aesTagSize];
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] derivedKey = new byte[aesKeySize];

            try
            {
                // derive key securely
                Rfc2898DeriveBytes.Pbkdf2(
                    password: keyBytes,
                    salt: salt,
                    iterations: pbkdf2Iterations,
                    hashAlgorithm: HashAlgorithmName.SHA256,
                    destination: derivedKey);

                // AES-GCM encryption
                using var aes = new AesGcm(derivedKey, aesTagSize);
                aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

                // build payload: version | salt | nonce | tag | ciphertext
                byte[] payload = new byte[1 + salt.Length + nonce.Length + tag.Length + cipherBytes.Length];
                int offset = 0;

                payload[offset++] = payloadVersion;

                Buffer.BlockCopy(salt, 0, payload, offset, salt.Length);
                offset += salt.Length;

                Buffer.BlockCopy(nonce, 0, payload, offset, nonce.Length);
                offset += nonce.Length;

                Buffer.BlockCopy(tag, 0, payload, offset, tag.Length);
                offset += tag.Length;

                Buffer.BlockCopy(cipherBytes, 0, payload, offset, cipherBytes.Length);

                return Convert.ToBase64String(payload);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(keyBytes);
                CryptographicOperations.ZeroMemory(derivedKey);
                CryptographicOperations.ZeroMemory(plainBytes);
            }
        }
    }
}