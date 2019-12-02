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
        private Socket listenerSocker;
        private List<Socket> clients;
        private List<Thread> threads;

        private object sync = new object();

        public MainWindow()
        {
            InitializeComponent();

            clients = new List<Socket>();
            threads = new List<Thread>();

            listenerSocker = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenerSocker.Bind(new IPEndPoint(IPAddress.Any, 8000));
            listenerSocker.Listen(5);

            serverThread = new Thread(ListeningThread);
            serverThread.Start();
        }

        private void ListeningThread()
        {
            Socket client;
            Thread thread;
            
            while (true)
            {
                client = listenerSocker.Accept();
                clients.Add(client);

                thread = new Thread(Handler);

                thread.Start(client);
                threads.Add(thread);
            }
        }

        private void Handler(object client)
        {
            byte[] buffer;
            int readBytes;
            Socket clientSocket = client as Socket;
            string[] values;

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
                            clients.ForEach(cl => 
                            cl.Send(Encoding.UTF8.GetBytes($"{values[0]} connected to the chat!")));
                        }
                        else if (values[1] == "DISCONNECT") 
                        {
                            clients.Remove(clientSocket);
                            clients.ForEach(cl => 
                            cl.Send(Encoding.UTF8.GetBytes($"{values[0]} disconnected from the chat!")));
                        }
                        else
                        {
                            clients.ForEach(cl => cl.Send(buffer));
                        }
                    }
                }
                catch (SocketException)
                {
                    clients.Remove(clientSocket);
                    break;
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            serverThread.Abort();
            clients.ForEach(cl => cl.Close());
            threads.ForEach(th => th.Abort());
        }
    }
}
