﻿using GalaSoft.MvvmLight;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading.Tasks;
using UwpNetworkingEssentials.Channels.AppServices;
using UwpNetworkingEssentials.Channels.MultiChannel;
using UwpNetworkingEssentials.Channels.StreamSockets;
using UwpNetworkingEssentials.Rpc;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UwpNetworkingEssentials.ChatSample.ViewModels
{
    public partial class ServerViewModel : ViewModelBase, IRpcTarget
    {
        private readonly MultiChannelConnectionListener _multiChannelListener;
        private readonly Frame _frame = Window.Current.Content as Frame;
        private readonly IObjectSerializer _serializer;
        private string _port = "1234";

        public RpcServer Server { get; private set; }

        public string Port { get => _port; set => Set(ref _port, value); }

        public int ClientCount => Server?.Connections.Count ?? 0;

        public ObservableCollection<string> Messages { get; } = new ObservableCollection<string>();

        public ServerViewModel()
        {
            _serializer = new DefaultJsonSerializer(GetType().GetTypeInfo().Assembly);

            _multiChannelListener = new MultiChannelConnectionListener();
            _multiChannelListener.StartAsync().Wait();

            Server = new RpcServer(_multiChannelListener, this);
            Messages.Add($"Server started");
            RaisePropertyChanged(nameof(Server));
        }

        public async void StartStreamSocketConnectionListener()
        {
            try
            {
                var listener = new StreamSocketConnectionListener(Port, _serializer);
                await listener.StartAsync();
                _multiChannelListener.Listeners.Add(listener);
            }
            catch
            {
                Messages.Add($"Failed to start all connection listeners");
            }
        }

        public async void StartASConnectionListener()
        {
            var listener = new ASConnectionListener("Chat", _serializer);
            await listener.StartAsync();
            _multiChannelListener.Listeners.Add(listener);
            App.TheASConnectionListener = listener;
        }

        public async void SendMessage(string message)
        {
            // Add message to own message list and broadcast message
            // to all connected clients
            Messages.Add("I say: " + message);
            await Server.AllClients.AddMessage(message);
        }

        public async void CloseServer()
        {
            await Server.DisposeAsync();
            Messages.Add("Server closed");
            await Task.Delay(2000);
            _frame.GoBack();
        }
    }
}
