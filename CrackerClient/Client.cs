using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using PasswordCrackerCentralized.model;
using PasswordCrackerCentralized.util;

namespace CrackerClient
{
    public class Client
    {
        private List<UserInfo> userInfos;
        private List<UserInfoClearText> result;
        private readonly HashAlgorithm _messageDigest;

        public Client()
        {
            _messageDigest = new SHA1CryptoServiceProvider();
        }

        public void Start()
        {
            Console.WriteLine("Waiting for server");
            //TcpClient client = new TcpClient("127.0.0.1", 7000);
            TcpClient client = new TcpClient("192.168.104.137", 7000);


            NetworkStream ns = client.GetStream();

            StreamReader sr = new StreamReader(ns);
            StreamWriter sw = new StreamWriter(ns);
            sw.AutoFlush = true;

            Console.WriteLine("Connected to server " + client.Client.RemoteEndPoint);


            string message = "";
            string inCommingMessage = "";

            while (message != null || message != "")
            {
                message = Console.ReadLine();

                if (message == "hack")
                {
                    sw.WriteLine(message);
                    inCommingMessage = sr.ReadLine(); //userinfo
                    Console.WriteLine(inCommingMessage);
                    userInfos = JsonConvert.DeserializeObject<List<UserInfo>>(inCommingMessage);
                    //extract userinfo
                    while (true)
                    {
                        inCommingMessage = sr.ReadLine(); //chunk
                        Console.WriteLine(inCommingMessage);
                        //work on chunk =
                        if (inCommingMessage == "done") break;
                        List<String> chunk = JsonConvert.DeserializeObject<List<String>>(inCommingMessage);
                        List<UserInfoClearText> resultToReturn = new List<UserInfoClearText>();
                        foreach (string line in chunk)
                        {
                            //IEnumerable<UserInfoClearText> partialResult = CheckWordWithVariations(line, userInfos);
                            resultToReturn.AddRange(CheckWordWithVariations(line, userInfos));

                        }

                        //return result list
                        if (resultToReturn.Count <= 0)
                        {
                            sw.WriteLine("empty");
                        }
                        else
                        {
                            sw.WriteLine(JsonConvert.SerializeObject(resultToReturn));
                        }
                    }

                    Console.WriteLine("Server said we were done");
                }
            }

            ns.Close();
            client.Close();

        }


        #region Copy Paste

        private IEnumerable<UserInfoClearText> CheckWordWithVariations(String dictionaryEntry, List<UserInfo> userInfos)
        {
            List<UserInfoClearText> result = new List<UserInfoClearText>(); //might be empty

            String possiblePassword = dictionaryEntry;
            IEnumerable<UserInfoClearText> partialResult = CheckSingleWord(userInfos, possiblePassword);
            result.AddRange(partialResult);

            String possiblePasswordUpperCase = dictionaryEntry.ToUpper();
            IEnumerable<UserInfoClearText> partialResultUpperCase = CheckSingleWord(userInfos, possiblePasswordUpperCase);
            result.AddRange(partialResultUpperCase);

            String possiblePasswordCapitalized = StringUtilities.Capitalize(dictionaryEntry);
            IEnumerable<UserInfoClearText> partialResultCapitalized = CheckSingleWord(userInfos, possiblePasswordCapitalized);
            result.AddRange(partialResultCapitalized);

            String possiblePasswordReverse = StringUtilities.Reverse(dictionaryEntry);
            IEnumerable<UserInfoClearText> partialResultReverse = CheckSingleWord(userInfos, possiblePasswordReverse);
            result.AddRange(partialResultReverse);

            for (int i = 0; i < 100; i++)
            {
                String possiblePasswordEndDigit = dictionaryEntry + i;
                IEnumerable<UserInfoClearText> partialResultEndDigit = CheckSingleWord(userInfos, possiblePasswordEndDigit);
                result.AddRange(partialResultEndDigit);
            }

            for (int i = 0; i < 100; i++)
            {
                String possiblePasswordStartDigit = i + dictionaryEntry;
                IEnumerable<UserInfoClearText> partialResultStartDigit = CheckSingleWord(userInfos, possiblePasswordStartDigit);
                result.AddRange(partialResultStartDigit);
            }

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    String possiblePasswordStartEndDigit = i + dictionaryEntry + j;
                    IEnumerable<UserInfoClearText> partialResultStartEndDigit = CheckSingleWord(userInfos, possiblePasswordStartEndDigit);
                    result.AddRange(partialResultStartEndDigit);
                }
            }

            return result;
        }

        /// <summary>
        /// Checks a single word (or rather a variation of a word): Encrypts and compares to all entries in the password file
        /// </summary>
        /// <param name="userInfos"></param>
        /// <param name="possiblePassword">List of (username, encrypted password) pairs from the password file</param>
        /// <returns>A list of (username, readable password) pairs. The list might be empty</returns>
        private IEnumerable<UserInfoClearText> CheckSingleWord(IEnumerable<UserInfo> userInfos, String possiblePassword)
        {
            char[] charArray = possiblePassword.ToCharArray();
            byte[] passwordAsBytes = Array.ConvertAll(charArray, PasswordFileHandler.GetConverter());

            byte[] encryptedPassword = _messageDigest.ComputeHash(passwordAsBytes);
            //string encryptedPasswordBase64 = System.Convert.ToBase64String(encryptedPassword);

            List<UserInfoClearText> results = new List<UserInfoClearText>();

            foreach (UserInfo userInfo in userInfos)
            {
                if (CompareBytes(userInfo.EntryptedPassword, encryptedPassword))  //compares byte arrays
                {
                    results.Add(new UserInfoClearText(userInfo.Username, possiblePassword));
                    Console.WriteLine(userInfo.Username + " " + possiblePassword);
                }
            }
            return results;
        }

        /// <summary>
        /// Compares to byte arrays. Encrypted words are byte arrays
        /// </summary>
        /// <param name="firstArray"></param>
        /// <param name="secondArray"></param>
        /// <returns></returns>
        private static bool CompareBytes(IList<byte> firstArray, IList<byte> secondArray)
        {
            //if (secondArray == null)
            //{
            //    throw new ArgumentNullException("firstArray");
            //}
            //if (secondArray == null)
            //{
            //    throw new ArgumentNullException("secondArray");
            //}
            if (firstArray.Count != secondArray.Count)
            {
                return false;
            }
            for (int i = 0; i < firstArray.Count; i++)
            {
                if (firstArray[i] != secondArray[i])
                    return false;
            }
            return true;
        }

        #endregion
    }
}
