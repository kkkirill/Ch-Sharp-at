using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Client
{
    public partial class MainWindow : Window
    {
        private Socket socket;
        private Thread thread;
        private bool isConnected;
        private SynchronizationContext context;

        public MainWindow()
        {
            InitializeComponent();

            context = SynchronizationContext.Current;
            isConnected = false;
        }

        private void HandleMessages(object syncContext)
        {
            byte[] recBuf = new byte[2048];
            try
            {
                while (true)
                {
                    socket.Receive(recBuf);
                    if (socket.ReceiveBufferSize != 0)
                        ((SynchronizationContext)syncContext).Send(AddMessage, Encoding.UTF8.GetString(recBuf));
                }
            }
            catch (SocketException)
            {
                disconnectButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                MessageBox.Show("Internal server error! Disconnected", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddMessage(object message)
        {
            var msg = (message as string).Replace("\0", "");
            if (msg.Length != 0)
            {
                var msgTextBpx = new TextBox()
                {
                    Padding = new Thickness(0, 0, 18, 0),
                    Margin = new Thickness(4, 0, 0, 0),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Text = msg
                };
                MessagesPanel.Children.Add(msgTextBpx);
            }
        }

        private void connectButton_Click(object sender, RoutedEventArgs e)
        {
            if (usernameField.Text.Length == 0)
            {
                MessageBox.Show("Enter username!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect("127.0.0.1", 8000);

                thread = new Thread(HandleMessages);
                thread.Start(context);
                socket.Send(Encoding.UTF8.GetBytes($"{usernameField.Text}:  CONNECT"));
                MessageBox.Show("Connected", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                isConnected = true;
            }
            catch (SocketException)
            {
                MessageBox.Show("Cannot connect to the server", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                isConnected = false;
            }
        }

        private void sendButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("You aren't connected", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            socket.Send(Encoding.UTF8.GetBytes($"{usernameField.Text}:  {inputMessageTextBox.Text}"));
            inputMessageTextBox.Text = "";
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            socket?.Close();
            thread?.Abort();
        }

        private void disconnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                MessageBox.Show("You aren't connected", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            socket.Send(Encoding.UTF8.GetBytes($"{usernameField.Text}:  DISCONNECT"));
            socket.Close();
            thread.Abort();
            MessagesPanel.Children.Clear();
            inputMessageTextBox.Clear();
            MessageBox.Show("Disconnected", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            isConnected = false;
        }
    }
}
