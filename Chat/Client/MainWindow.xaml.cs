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
        private SynchronizationContext context;

        public MainWindow()
        {
            InitializeComponent();

            context = SynchronizationContext.Current;
        }

        private void HandleMessages(object syncContext)
        {
            byte[] recBuf = new byte[2048];

            while (true)
            {
                socket.Receive(recBuf);
                if (socket.ReceiveBufferSize != 0)
                    ((SynchronizationContext)syncContext).Send(AddMessage, Encoding.UTF8.GetString(recBuf));
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
            }
            catch (SocketException)
            {
                MessageBox.Show("Cannot connect to the server", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                connectButton.Click -= connectButton_Click;
            }
        }

        private void sendButton_Click(object sender, RoutedEventArgs e)
        {
            socket.Send(Encoding.UTF8.GetBytes($"{usernameField.Text}:  {inputMessageTextBox.Text}"));
            inputMessageTextBox.Text = "";
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            socket.Close();
            thread?.Abort();
        }

        private void disconnectButton_Click(object sender, RoutedEventArgs e)
        {
            socket.Send(Encoding.UTF8.GetBytes($"{usernameField.Text}:  DISCONNECT"));
            socket.Close();
            thread?.Abort();
            MessagesPanel.Children.Clear();
            inputMessageTextBox.Clear();
            MessageBox.Show("Disconnected", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
