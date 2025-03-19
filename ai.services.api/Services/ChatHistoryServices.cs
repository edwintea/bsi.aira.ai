using ai.services.api.Models;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ai.services.api.Services
{
    public class ChatHistoryServices
    {
        private readonly BlobContainerClient _containerClient;

        private string connectionStringAzureStorage = Environment.GetEnvironmentVariable("blobStorageConnectionString");
        private string containerNameChat = Environment.GetEnvironmentVariable("containerNameChat");
        private string aesKey = Environment.GetEnvironmentVariable("aesKey");
        private string aesIV = Environment.GetEnvironmentVariable("aesIV");

        public ChatHistoryServices(HttpClient http)
        {

            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionStringAzureStorage);
            _containerClient = blobServiceClient.GetBlobContainerClient(containerNameChat);
        }

        // Save Chat Group History
        public async Task<ResponseEntity> SaveChatGroupHistoryAsync(string fileName, object chatHistory, string eid)
        {
            ResponseEntity _responseEntity = new ResponseEntity();
            try
            {
                var _fileName = RemoveSymbols(CapitalizeFirstLetter(fileName));
                var _chatHistory = System.Text.Json.JsonSerializer.Serialize(chatHistory);
                var encryptedChatHistory = EncryptChatHistory(_chatHistory);
                string blobFileName = $"{eid}/chatgroup/{_fileName}.json";

                BlobClient blobClient = _containerClient.GetBlobClient(blobFileName);

                // Upload the encrypted chat history to the blob
                using (var stream = new MemoryStream(encryptedChatHistory))
                {
                    await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = "application/json" });
                }

                _responseEntity.isSuccess = true;
                _responseEntity.Message = $"Chat history saved as {blobFileName}";
            }
            catch (Exception ex)
            {
                _responseEntity.isSuccess = false;
                _responseEntity.Message = $"Error: {ex.Message}";
            }

            return _responseEntity;
        }

        // Save Chat Item History
        public async Task<ResponseEntity> SaveChatItemHistoryAsync(string fileName, object chatHistory, string eid)
        {
            ResponseEntity _responseEntity = new ResponseEntity();
            try
            {
                var _fileName = RemoveSymbols(CapitalizeFirstLetter(fileName));
                var _chatHistory = System.Text.Json.JsonSerializer.Serialize(chatHistory);
                var encryptedChatHistory = EncryptChatHistory(_chatHistory);
                string blobFileName = $"{eid}/chatitem/{_fileName}.json";

                BlobClient blobClient = _containerClient.GetBlobClient(blobFileName);

                // Upload the encrypted chat history to the blob
                using (var stream = new MemoryStream(encryptedChatHistory))
                {
                    await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = "application/json" });
                }

                _responseEntity.isSuccess = true;
                _responseEntity.Message = $"Chat history saved as {blobFileName}";
            }
            catch (Exception ex)
            {
                _responseEntity.isSuccess = false;
                _responseEntity.Message = $"Error: {ex.Message}";
            }

            return _responseEntity;
        }

        // Delete Chat Group History
        public async Task<ResponseEntity> DeleteChatGroupHistoryAsync(string fileName, string eid)
        {
            ResponseEntity _responseEntity = new ResponseEntity();
            try
            {
                var _fileName = RemoveSymbols(CapitalizeFirstLetter(fileName));
                string blobFileName = $"{eid}/chatgroup/{_fileName}.json";
                BlobClient blobClient = _containerClient.GetBlobClient(blobFileName);
                await blobClient.DeleteIfExistsAsync();

                _responseEntity.isSuccess = true;
                _responseEntity.Message = $"Chat history deleted as {blobFileName}";
            }
            catch (Exception ex)
            {
                _responseEntity.isSuccess = false;
                _responseEntity.Message = $"Error: {ex.Message}";
            }

            return _responseEntity;
        }

        // Delete Chat Item History
        public async Task<ResponseEntity> DeleteChatItemHistoryAsync(string fileName, string eid)
        {
            ResponseEntity _responseEntity = new ResponseEntity();
            try
            {
                var _fileName = RemoveSymbols(CapitalizeFirstLetter(fileName));
                string blobFileName = $"{eid}/chatitem/{_fileName}.json";
                BlobClient blobClient = _containerClient.GetBlobClient(blobFileName);
                await blobClient.DeleteIfExistsAsync();

                _responseEntity.isSuccess = true;
                _responseEntity.Message = $"Chat history deleted as {blobFileName}";
            }
            catch (Exception ex)
            {
                _responseEntity.isSuccess = false;
                _responseEntity.Message = $"Error: {ex.Message}";
            }

            return _responseEntity;
        }

        public async Task<ResponseEntity> GetChatGroupHistoryListAsync(string eid, string serviceType, int take, int page)
        {
            ResponseEntity _responseEntity = new ResponseEntity();
            var blobFiles = new List<object>();
            try
            {
                string prefix = $"{eid}/chatgroup";

                foreach (var blobItem in _containerClient.GetBlobs(BlobTraits.None, BlobStates.None, prefix)
                    .OrderByDescending(T => T.Properties.LastModified)
                    .Skip((page - 1) * take)
                    .Take(take))
                {
                    var temp = await GetChatGroupHistoryAsync(blobItem.Name, serviceType);
                    if (temp != null)
                    {
                        blobFiles.Add(temp);
                    }
                }

                _responseEntity.isSuccess = true;
                _responseEntity.Message = $"Chat history get";
                _responseEntity.ChatGroup = blobFiles;
            }
            catch (Exception ex)
            {
                _responseEntity.isSuccess = false;
                _responseEntity.Message = $"Error: {ex.Message}";
            }

            return _responseEntity;
        }

        public async Task<ChatGroupEntity> GetChatGroupHistoryAsync(string blobFileName, string serviceType)
        {
            ResponseEntity _responseEntity = new ResponseEntity();
            try
            {
                BlobClient blobClient = _containerClient.GetBlobClient(blobFileName);
                if (await blobClient.ExistsAsync())
                {
                    var downloadResponse = await blobClient.DownloadAsync();

                    // Download the blob's content
                    var downloadInfo = await blobClient.DownloadAsync();

                    using (var ms = new MemoryStream())
                    {
                        await downloadInfo.Value.Content.CopyToAsync(ms);
                        byte[] fileContent = ms.ToArray();

                        string fileContentString = System.Text.Encoding.UTF8.GetString(fileContent);

                        // Check if the file is encrypted by looking for the "ENC:" prefix
                        string prefix = "ENC:";
                        if (fileContentString.StartsWith(prefix))
                        {
                            // Remove the prefix and decrypt the rest of the content
                            byte[] encryptedContent = fileContent.Skip(prefix.Length).ToArray();
                            var tempChatHistory = DecryptChatHistory(encryptedContent);

                            // Deserialize the decrypted chat history
                            var data = JsonConvert.DeserializeObject<object>(tempChatHistory).ToString();
                            string tempin = data.Replace("\r\n", "");
                            ChatGroupEntity chatInfo = JsonConvert.DeserializeObject<ChatGroupEntity>(tempin);

                            if (chatInfo.IsActive)
                            {
                                if (serviceType != null)
                                {
                                    if (serviceType.Contains(chatInfo.ServiceType))
                                    {
                                        return chatInfo;
                                    }
                                }
                                else
                                {
                                    return chatInfo;
                                }
                            }
                            else
                            {
                                return null;
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException("Deserialized data is failed.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }

            return null;
        }

        // Get Chat Item History (for testing or use case purposes)
        public async Task<ResponseEntity> GetChatItemHistoryAsync(string fileName, string eid)
        {
            ResponseEntity _responseEntity = new ResponseEntity();
            try
            {
                var _fileName = RemoveSymbols(CapitalizeFirstLetter(fileName));
                string blobFileName = $"{eid}/chatitem/{_fileName}.json";
                BlobClient blobClient = _containerClient.GetBlobClient(blobFileName);
                if (await blobClient.ExistsAsync())
                {
                    var downloadResponse = await blobClient.DownloadAsync();

                    // Download the blob's content
                    var downloadInfo = await blobClient.DownloadAsync();

                    using (var ms = new MemoryStream())
                    {
                        await downloadInfo.Value.Content.CopyToAsync(ms);
                        byte[] fileContent = ms.ToArray();

                        string fileContentString = System.Text.Encoding.UTF8.GetString(fileContent);

                        // Check if the file is encrypted by looking for the "ENC:" prefix
                        string prefix = "ENC:";
                        if (fileContentString.StartsWith(prefix))
                        {
                            // Remove the prefix and decrypt the rest of the content
                            byte[] encryptedContent = fileContent.Skip(prefix.Length).ToArray();
                            var tempChatHistory = DecryptChatHistory(encryptedContent);

                            // Deserialize the decrypted chat history
                            var data = JsonConvert.DeserializeObject<object>(tempChatHistory).ToString();
                            string tempin = data.Replace("\r\n", "");
                            List<ChatItemEntity.ChatItem> chatInfo = JsonConvert.DeserializeObject<List<ChatItemEntity.ChatItem>>(tempin);

                            _responseEntity.ChatItem = chatInfo;
                        }
                        else
                        {
                            throw new InvalidOperationException("Deserialized data is failed.");
                        }
                    }
                }

                _responseEntity.isSuccess = true;
                _responseEntity.Message = $"Chat history get as {blobFileName}";
            }
            catch (Exception ex)
            {
                _responseEntity.isSuccess = false;
                _responseEntity.Message = $"Error: {ex.Message}";
            }

            return _responseEntity;
        }

        static string RemoveSymbols(string input)
        {
            // Regex pattern to allow non-ASCII characters including Arabic
            string pattern = @"[^a-zA-Z0-9\s\u3000-\u303F\u3040-\u309F\u30A0-\u30FF\u4E00-\u9FFF\u0600-\u06FF]";
            return Regex.Replace(input, pattern, "");
        }

        // Encryption method using AES
        private byte[] EncryptChatHistory(string plainText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Convert.FromBase64String(aesKey);
                aes.IV = Convert.FromBase64String(aesIV);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }
                    }

                    // Add the "ENC:" prefix to the encrypted data
                    byte[] encryptedData = ms.ToArray();
                    byte[] encryptedDataWithPrefix = Encoding.UTF8.GetBytes("ENC:").Concat(encryptedData).ToArray();

                    return encryptedDataWithPrefix;
                }
            }
        }

        private string DecryptChatHistory(byte[] cipherText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Convert.FromBase64String(aesKey);
                aes.IV = Convert.FromBase64String(aesIV);

                using (MemoryStream ms = new MemoryStream(cipherText))
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        using (StreamReader sr = new StreamReader(cs))
                        {
                            // Return the decrypted string
                            return sr.ReadToEnd();
                        }
                    }
                }
            }
        }

        static string CapitalizeFirstLetter(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
            return textInfo.ToTitleCase(input.ToLower());
        }
    }
}
