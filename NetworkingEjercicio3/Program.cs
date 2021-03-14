using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;

namespace NetworkingEjercicio3
{
    class Program
    {
        static bool gameFinished, playerOFF;
        private static readonly object l = new object();
        static IPEndPoint ie;
        static Socket s, sClient;
        static int countClients = 0;
        static int temp, selectedNumber, highestNumber;
        static List<int> availableNumbers = new List<int>();
        static List<int> playerDisconnected = new List<int>();
        static bool portUsing;
        static List<StreamWriter> sw = new List<StreamWriter>();
        static System.Timers.Timer tm;

        static Hashtable hashCliente = new Hashtable();
        static Random rm = new Random();

        static void Main(string[] args)
        {
            temp = 20;
            highestNumber = 0;
            gameFinished = false;
            tm = new System.Timers.Timer(400);
            tm.Elapsed += OnTimedEvent;
            tm.AutoReset = true;

            for (int i = 0; i <= 20; i++)
            {
                availableNumbers.Add(i);
            }

            portUsing = true;
            int port = 22222;

            s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            while (portUsing)
            {
                ie = new IPEndPoint(IPAddress.Any, port);
                try
                {
                    s.Bind(ie);
                    Console.WriteLine("Port listening " + port);
                    portUsing = false;
                }
                catch (SocketException)
                {
                    Console.WriteLine("Port in use: " + port);
                    port++;
                }
            }
            s.Listen(20);

            while (true)
            {
                sClient = s.Accept();
                if (gameFinished)
                {
                    temp = 20;
                    highestNumber = 0;
                    gameFinished = false;
                    hashCliente = new Hashtable();
                    countClients = 0;
                    sw = new List<StreamWriter>();
                    availableNumbers = new List<int>();

                    for (int i = 0; i <= 20; i++)
                    {
                        availableNumbers.Add(i);
                    }

                    playerDisconnected = new List<int>();
                }

                Thread t = new Thread(threadClient);
                t.Start(sClient);
                countClients++;

                if (countClients >= 2)
                {
                    tm.Start();
                }
            }
        }
        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            Console.WriteLine("Time {0}", temp);
            temp--;

            if (temp < 0)
            {
                tm.Stop();
                Console.WriteLine("Time Over");
                gameFinished = true;

                lock (l)
                {
                    foreach (DictionaryEntry player in hashCliente)
                    {
                        StreamWriter swPlayer = (StreamWriter)player.Value;

                        if (highestNumber == (int)player.Key)
                        {
                            swPlayer.WriteLine("You got the highest number {0}", player.Key);
                        }
                        else
                        {
                            swPlayer.WriteLine("You lose, highest number was {0}", highestNumber);
                        }
                        swPlayer.Flush();
                    }
                    Monitor.PulseAll(l);
                }
            }
            else
            {
                lock (l)
                {
                    playerOFF = false;
                    foreach (DictionaryEntry player in hashCliente)
                    {
                        try
                        {
                            StreamWriter swPlayer = (StreamWriter)player.Value;
                            swPlayer.WriteLine("{0} seconds left", temp);
                            swPlayer.Flush();
                        }
                        catch (IOException)
                        {
                            playerDisconnected.Add((int)player.Key);
                            playerOFF = true;
                            countClients--;
                        }
                    }
                    if (playerOFF)
                    {
                        foreach (int jugador in playerDisconnected)
                        {
                            hashCliente.Remove(jugador);
                        }
                        playerDisconnected.Clear();
                    }
                }
            }
        }

        private static void threadClient(object socket)
        {
            Socket client = (Socket)socket;
            IPEndPoint ieClient = (IPEndPoint)client.RemoteEndPoint;

            using (NetworkStream ns = new NetworkStream(client))
            using (StreamWriter sw = new StreamWriter(ns))
            {
                sw.WriteLine("user connected");
                sw.Flush();

                lock (l)
                {
                    selectedNumber = availableNumbers[rm.Next(availableNumbers.Count)];
                    availableNumbers.Remove(selectedNumber);
                    hashCliente.Add(selectedNumber, sw);

                    if (selectedNumber > highestNumber)
                    {
                        highestNumber = selectedNumber;
                    }

                    sw.WriteLine("Your number :{0}", selectedNumber);
                    sw.Flush();
                    Monitor.Wait(l);
                    try
                    {
                        sw.WriteLine("Time Over");
                        sw.Flush();
                    }
                    catch (IOException)
                    {
                        client.Close();
                    }
                }
            }
            client.Close();
        }
    }
}
