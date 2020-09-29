using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PasswordCrackerCentralized.model;
using PasswordCrackerCentralized.util;

namespace PasswordCrackerCentralized
{
    public class Server
    {
        private readonly HashAlgorithm _messageDigest;
        private int _clientsConnected = 0;
        private int _currentChunk = 0;
        private List<int> _inCompletedChunks = new List<int>();
        private List<int> _doingChunks = new List<int>();
        private List<List<string>> Chunks;

        private Stopwatch stopwatch;
        private List<UserInfo> userInfos;
        private List<UserInfoClearText> result;

        private object _lock;



        public void Start()
        { 
            stopwatch = new Stopwatch();
            _lock = new object();
            userInfos =
                PasswordFileHandler.ReadPasswordFile("passwords.txt");
            //Console.WriteLine("passwd opeend");

            result = new List<UserInfoClearText>();

            Chunks = CreateChunks("webster-dictionary.txt", 10000);

            //IP configuration
            //IPAddress ipa = IPAddress.Parse("127.0.0.1");
            IPAddress ipa = IPAddress.Parse("192.168.104.137");

            TcpListener tcp = new TcpListener(ipa, 7000);

            //Starting Server and sending the accepted client to a "DoClient"
            tcp.Start();
            Console.WriteLine("Server Started");

            while (true)
            {
                Task.Run(() =>
                {
                    TcpClient tempSocket = tcp.AcceptTcpClient();
                    EndPoint clientIP = tempSocket.Client.RemoteEndPoint;
                    Console.WriteLine(clientIP + ": CONNECTED");
                    _clientsConnected++;

                    DoClient(tempSocket);

                    if (!tempSocket.Connected)
                    {
                        Console.WriteLine(clientIP + ": DISCONNECTED");
                        _clientsConnected--;
                    }
                    tempSocket.Close();
                });

                if (_inCompletedChunks.Count <= 0)
                {
                    stopwatch.Stop();
                    break;
                }
            }

            Console.WriteLine("Done");
            Console.WriteLine($"Took: {stopwatch.Elapsed}");

            tcp.Stop();
        }
        public void DoClient(System.Net.Sockets.TcpClient socket)
        {
            NetworkStream ns = socket.GetStream();

            StreamReader sr = new StreamReader(ns);
            StreamWriter sw = new StreamWriter(ns);
            sw.AutoFlush = true;

            string message;

            while (true)
            {
                try
                {
                    message = sr.ReadLine();

                    if (message == "hack")
                    {
                        stopwatch.Start();
                        Console.WriteLine(message);
                        sw.WriteLine(JsonConvert.SerializeObject(userInfos));

                        while (_inCompletedChunks.Count > 0)
                        {
                            if (_inCompletedChunks[0] == -1)
                            {
                                break;
                            }

                            int inCompletedChunk;
                            //if this chunk is already being processed, and quickly setting this to doingChunks
                            //----------------------------------------------------------------------------------------------------------------------
                            lock (_lock)
                            {
                                //taking the first not completed chunk
                                inCompletedChunk = _inCompletedChunks[0];
                                while (_doingChunks.Contains(inCompletedChunk))
                                {
                                    if (_inCompletedChunks.Last() > inCompletedChunk)
                                    {
                                        inCompletedChunk++;
                                    }
                                    else
                                    {
                                        inCompletedChunk = -1;
                                    }
                                }
                                _doingChunks.Add(inCompletedChunk);
                                //chunkToWorkOn = inCompletedChunk;
                                if (inCompletedChunk != -1)
                                {
                                    List<string> listToWorkOn = Chunks[inCompletedChunk];
                                    String toSend = JsonConvert.SerializeObject(listToWorkOn);
                                    Console.WriteLine($"WORKING ON: {inCompletedChunk} BY: {socket.Client.RemoteEndPoint}");
                                    sw.WriteLine(toSend);
                                    //sw.WriteLine("chunk");
                                }
                                else
                                {
                                    sw.WriteLine("done");
                                    _inCompletedChunks = new List<int>(){-1};
                                    _doingChunks = new List<int>();
                                }
                            }
                            //----------------------------------------------------------------------------------------------------------------------

                            String resultSerialized = sr.ReadLine();
                            if (resultSerialized == "empty")
                            {
                                //Console.WriteLine("empty");
                            }
                            else
                            {
                                IEnumerable<UserInfoClearText> partialResult = JsonConvert.DeserializeObject<IEnumerable<UserInfoClearText>>(resultSerialized);
                                result.AddRange(partialResult);
                                foreach (UserInfoClearText hej in result)
                                {
                                    Console.WriteLine(hej);
                                }
                            }
                            //Console.WriteLine(resultSerialized);
                        }
                    }
                    else //if we don't write hack
                    {
                        //Console.WriteLine(sr.ReadLine());
                    }
                    Console.WriteLine("We reached the bottom");
                    stopwatch.Stop();
                    Console.WriteLine(stopwatch.Elapsed);
                }
                catch (IOException e)
                {
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine("hahahha: " + e.Message);
                }
            }

            #region while
            /*
                while (true)
                {
                    try
                    {
                        message = sr.ReadLine();
                        Console.WriteLine("input: " + message);
                        string answer = "";

                        //if (string.IsNullOrWhiteSpace(message)) break;

                        //////////////////////////////////////////////////////////////////////
                        /// HERE YOU WRITE YOUR PROTOCOL (WHAT TO DO WITH THE MESSAGE)
                        //////////////////////////////////////////////////////////////////////

                        answer = message.ToUpper();

                        Console.WriteLine("output: " + answer);
                        sw.WriteLine(answer);
                    }
                    catch (IOException e)
                    {
                        break;
                    }
                }
                */
            #endregion

            ns.Close();
        }

        public List<List<string>> CreateChunks(string path, int chunkSize)
        {
            List<List<string>> toReturn = new List<List<string>>();

            //using (FileStream fs = new FileStream("webster-dictionary.txt", FileMode.Open, FileAccess.Read))
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))

            using (StreamReader dictionary = new StreamReader(fs))
            {
                int counter = 0;
                int listCounter = 0;
                while (!dictionary.EndOfStream)
                {
                    String dictionaryEntry = dictionary.ReadLine();
                    if (toReturn.Count == 0) toReturn.Add(new List<string>());

                    if (counter < chunkSize)
                    {
                        counter++;
                        toReturn[listCounter].Add(dictionaryEntry);
                    }
                    else
                    {
                        listCounter++;
                        toReturn.Add(new List<string>());
                        counter = 0;
                    }
                }

                for (int i = 0; i <= listCounter; i++)
                {
                    _inCompletedChunks.Add(i);
                }
            }

            return toReturn;
        }

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
    }
}
