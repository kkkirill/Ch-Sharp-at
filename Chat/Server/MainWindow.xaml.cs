using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;

namespace Server
{

    public partial class MainWindow : Window
    {
        private Thread serverThread;
        private Socket listenerSocket;
        private List<Socket> clients;
        private List<Thread> threads;
        private SynchronizationContext context;


        private object sync = new object();

        public MainWindow()
        {
            InitializeComponent();

            context = SynchronizationContext.Current;

            clients = new List<Socket>();
            threads = new List<Thread>();

            listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenerSocket.Bind(new IPEndPoint(IPAddress.Any, 8000));
            listenerSocket.Listen(5);

            serverThread = new Thread(ListeningThread);
            serverThread.Start();
        }

        private void ListeningThread()
        {
            Socket client;
            Thread thread;
            try
            {
                while (true)
                {
                    client = listenerSocket.Accept();
                    clients.Add(client);

                    thread = new Thread(Handler);

                    thread.Start(client);
                    threads.Add(thread);
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine($"Error: {e.Message}");
            }
        }

        private void Handler(object client)
        {
            byte[] buffer;
            int readBytes;
            Socket clientSocket = client as Socket;
            string[] values;
            string msg;

            while (true)
            {
                buffer = new byte[clientSocket.SendBufferSize];

                try
                {
                    readBytes = clientSocket.Receive(buffer);
                    values = Encoding.UTF8.GetString(buffer).Replace(":  ", "*").Split('*');
                    values[1] = values[1].Replace("\0", "");

                    lock (sync)
                    {
                        if (values[1] == "CONNECT")
                        {
                            msg = $"{values[0]} connected to the chat!";
                            clients.ForEach(cl => 
                                            cl.Send(Encoding.UTF8.GetBytes(msg)));
                        }
                        else if (values[1] == "DISCONNECT") 
                        {
                            msg = $"{values[0]} disconnected from the chat!";
                            clients.Remove(clientSocket);
                            clients.ForEach(cl => 
                                            cl.Send(Encoding.UTF8.GetBytes(msg)));
                        }
                        else
                        {
                            msg = string.Join(":  ", values);
                            clients.ForEach(cl => cl.Send(buffer));
                        }
                        context.Send(logMsg, msg);
                    }
                }
                catch (SocketException)
                {
                    clients.Remove(clientSocket);
                    break;
                }
            }
        }

        private void logMsg(object msg)
        {
            LoggerTextBlock.Text += $"{msg as string}\n";
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            listenerSocket.Close();
            serverThread.Abort();
            clients.ForEach(cl => cl.Close());
            threads.ForEach(th => th.Abort());
        }
    }
}
