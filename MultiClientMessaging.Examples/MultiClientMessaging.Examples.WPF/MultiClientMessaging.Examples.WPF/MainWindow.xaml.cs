using MessagesLibrary;
using MultiClientMessaging.Client.Messenger;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ClientToClientMessenger _messagesClient = new ClientToClientMessenger();
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void connectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _messagesClient.ConnectToServer("http://192.168.0.104:53353/chathub", "1");
                messagesList.Items.Add("Connection started");

                _messagesClient.Subscribe<ChatMessage>(OnMessageReceived);
            }
            catch (Exception ex)
            {
                messagesList.Items.Add(ex.Message);
            }
        }

        private Task OnMessageReceived(ChatMessage arg)
        {
            this.Dispatcher.Invoke(() =>
            {
                var newMessage = $"{arg.Text}";
                messagesList.Items.Add(newMessage);
            });

            return Task.CompletedTask;
        }

        private async void sendButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _messagesClient.SendToClient("2", new ChatMessage(messageTextBox.Text));
            }
            catch (Exception ex)
            {
                messagesList.Items.Add(ex.Message);
            }
        }
    }
}
