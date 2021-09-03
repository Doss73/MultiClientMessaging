using MessagesLibrary;
using MultiClientMessaging.Client.Messenger;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace XamarinExample
{
    public partial class MainPage : ContentPage
    {
        ClientToClientMessenger _messagesClient = new ClientToClientMessenger();

        public MainPage()
        {
            InitializeComponent();
            messagesList.ItemsSource = messages;
        }

        ObservableCollection<string> messages = new ObservableCollection<string>();
        private async void Button_Clicked_1(object sender, EventArgs e)
        {
            try
            {
                await _messagesClient.ConnectToServer("http://192.168.0.104:53353/chathub", "1");
                this.Dispatcher.BeginInvokeOnMainThread(() =>
                {
                    messages.Add("Connection started");
                });

                _messagesClient.Subscribe<ChatMessage>(OnMessageReceived);
            }
            catch (Exception ex)
            {
                messages.Add(ex.Message);
            }
        }
        private Task OnMessageReceived(ChatMessage arg)
        {
            this.Dispatcher.BeginInvokeOnMainThread(() =>
            {
                var newMessage = $"{arg.Text}";
                messages.Add(newMessage);
            });

            return Task.CompletedTask;
        }

        private async void Button_Clicked(object sender, EventArgs e)
        {
            try
            {
                await _messagesClient.SendToClient("2", new ChatMessage(messageTextBox.Text));
            }
            catch (Exception ex)
            {
                this.Dispatcher.BeginInvokeOnMainThread(() =>
                {
                    messages.Add(ex.Message);
                });
            }
        }
    }
}
