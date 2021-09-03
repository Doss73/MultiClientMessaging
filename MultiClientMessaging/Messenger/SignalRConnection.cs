using Meta.Lib.Modules.Logger;
using Meta.Lib.Modules.PubSub;
using Meta.Lib.Utils;
using Microsoft.AspNetCore.SignalR.Client;
using MultiClientMessaging.Client.Utils;
using MultiClientMessaging.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MultiClientMessaging.Client.Messenger
{
    internal class SignalRConnection
    {
        static int InstanceNo = 0;

        protected readonly ConcurrentDictionary<string, WebTransmit> _transmits =
            new ConcurrentDictionary<string, WebTransmit>();

        readonly IMetaPubSub _hub;
        readonly IMetaLogger _logger;

        HubConnection _connection;
        object _connectionLock = new object();
        int _reconnectionPeriod;
        int _isRunningReconnectionLoop = 0;
        bool _stoppedReconnection = false;

        protected string Id { get; } = (++InstanceNo).ToString();
        internal bool IsConnected => _connection?.State == HubConnectionState.Connected;

        internal event EventHandler Connected;
        internal event EventHandler<Exception> Disconnected;

        internal SignalRConnection(IMetaPubSub hub, IMetaLogger logger)
        {
            _hub = hub;
            _logger = logger;
        }

        internal async Task<bool> Connect(string address, string endpointId, int reconnectionPeriod = 5_000)
        {
            lock (_connectionLock)
            {
                _connection = new HubConnectionBuilder()
                    .WithUrl(address, options =>
                    {
                        options.Headers["ClientId"] = endpointId;
                    })
                    .Build();

                _reconnectionPeriod = reconnectionPeriod;
                _stoppedReconnection = false;

                _connection.Closed += Connection_Closed;

                _connection.On<string, string>("SendMessageCallback", OnSendMessageCallback);
            }

            try
            {
                await _connection.StartAsync();
                if (IsConnected)
                {
                    _logger.Info($"Successfully connected to the hub.");
                    Connected?.Invoke(this, EventArgs.Empty);
                }
            }
            catch(Exception ex)
            {
                if (!IsConnected)
                {
                    StartReconnectionLoop(reconnectionPeriod);
                    return false;
                }
            }

            return true;
        }

        Task Connection_Closed(Exception arg)
        {
            if (arg == null)
            {
                Disconnected?.Invoke(this, new AlreadyRegisteredException());
                _stoppedReconnection = true;
            }
            else
            {
                _logger.Error(arg.Message);
                Disconnected?.Invoke(this, arg);
            }

            StartReconnectionLoop(_reconnectionPeriod);
            return Task.CompletedTask;
        }

        void StartReconnectionLoop(int reconnectionPeriod)
        {
            Task.Run(async () =>
            {
                if (Interlocked.CompareExchange(ref _isRunningReconnectionLoop, 1, 0) == 0)
                {
                    try
                    {
                        while (!IsConnected && !_stoppedReconnection && _connection != null)
                        {
                            try
                            {
                                _logger.Info($"Connecting to hub...");
                                await _connection.StartAsync().ConfigureAwait(false);
                                if (IsConnected)
                                {
                                    _logger.Info($"Successfully connected to the hub.");
                                    Connected?.Invoke(this, EventArgs.Empty);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.Error($"Connect to the hub failed: {ex.Message}; Attempting reconnect...");
                                while (ex.InnerException != null)
                                {
                                    _logger.Error($"Hub error inner exception: {ex.InnerException.Message}");
                                    ex = ex.InnerException;
                                }

                                await Task.Delay(reconnectionPeriod);
                            }
                        }
                    }
                    finally
                    {
                        Interlocked.Exchange(ref _isRunningReconnectionLoop, 0);
                    }
                }
            });
        }

        internal async Task Stop()
        {
            HubConnection connection = null;
            _stoppedReconnection = true;

            lock (_connectionLock)
            {
                connection = _connection;
                _connection = null;
            }

            try
            {
                if (connection != null)
                {
                    connection.Closed -= Connection_Closed;
                    await connection.StopAsync().ConfigureAwait(false);
                    await connection.DisposeAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        async Task SendToHub(string receiverId, string message)
        {
            if (IsConnected)
            {
                var response = await _connection.InvokeAsync<HubResponse>("SendMessage", receiverId, message);
                if (!response.IsSuccessful)
                    throw new Exception(response.ExceptionMessage);
            }
            else
            {
                throw new Exception("Hub isn't connected");
            }
        }

        internal Task<bool> SendMessage(string receiverId, IPubSubMessage message, WebMessageType pipeMessageType)
        {
            var transmit = new WebTransmit(message, pipeMessageType);
            _transmits.TryAdd(transmit.Id, transmit);

            SendTransmit(receiverId, transmit);
            return transmit.Tcs.Task;
        }

        void SendTransmit(string receiverId, WebTransmit transmit)
        {
            Task.Run(async () =>
            {
                try
                {
                    await SendToHub(receiverId, transmit.Packet);
                    await transmit.Tcs.Task.TimeoutAfter(transmit.Timeout);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Send transmit error: '{ex.Message}, packet: {transmit.Packet}'");
                    if (_transmits.TryRemove(transmit.Id, out var removed))
                        removed.Tcs.SetException(ex);
                }
            });
        }

        void OnSendMessageCallback(string message, string publisherId)
        {
            if (message != null)
            {
                ProcessPacket(message, publisherId);
            }
        }

        void ProcessPacket(string packet, string publisherId)
        {
            Task.Run(async () =>
            {
                try
                {
                    CallbackMessage callbackMessage = new CallbackMessage(packet);
                    if (callbackMessage.IsMessageResponse)
                    {
                        OnMessageResponse(callbackMessage);
                    }
                    else
                    {
                        await OnMessage(callbackMessage, publisherId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                }
            });
        }

        async Task OnMessage(CallbackMessage callbackMessage, string publisherId)
        {
            var message = JsonConvert.DeserializeObject(callbackMessage.SerializedMessage, callbackMessage.MessageType) as IPubSubMessage;
            message.RemoteConnectionId = Id;

            try
            {
                await _hub.Publish(message);
                await SendOkResponse(publisherId, callbackMessage.PacketId);
            }
            catch (Exception ex)
            {
                await SendErrorResponse(publisherId, callbackMessage.PacketId, ex);
            }
        }

        void OnMessageResponse(CallbackMessage callbackMessage)
        {
            if (_transmits.TryRemove(callbackMessage.PacketId, out var transmit))
            {
                try
                {
                    if (callbackMessage.IsOk)
                    {
                        transmit.Tcs.SetResult(true);
                    }
                    else
                    {
                        var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
                        var aggException = JsonConvert.DeserializeObject<AggregateException>(callbackMessage.SerializedException, settings);

                        if (aggException.InnerExceptions.Count == 1 &&
                            (aggException.InnerException is NoSubscribersException ||
                             aggException.InnerException is TimeoutException))
                        {
                            transmit.Tcs.SetException(aggException.InnerException);
                        }
                        else
                        {
                            transmit.Tcs.SetException(aggException);
                        }
                    }
                }
                catch (Exception ex)
                {
                    transmit.Tcs.SetException(new AggregateException("The message response has an invalid format", ex));
                }
            }
        }

        protected async Task SendOkResponse(string receiverId, string packetId)
        {
            var str = ClientResponseUtils.GetOkResponse(packetId);
            await SendToHub(receiverId, str);
        }

        protected async Task SendErrorResponse(string receiverId, string packetId, Exception exception)
        {
            var str = ClientResponseUtils.GetErrorResponse(packetId, exception);
            await SendToHub(receiverId, str);
        }
    }
}
