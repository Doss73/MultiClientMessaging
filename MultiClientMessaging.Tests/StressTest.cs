using Meta.Lib.Modules.PubSub;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiClientMessaging.Client.Messenger;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MultiClientMessaging.Tests
{
    internal class MyMessage: PubSubMessageBase
    {
        public int DeliveredCount { get; set; } = 0;
    }

    //requires running server
    [TestClass]
    public class StressTest
    {
        [TestMethod]
        public async Task SendToClient_SubscribedOnlyOnce_ReceivedOnlyOnce()
        {
            List<string> subscribersId = new List<string>();
            Dictionary<string, MyMessage> messages = new Dictionary<string, MyMessage>();

            for (int i = 0; i < 1000; i++)
            {
                var newSubscriber = new ClientToClientMessenger();
                string id = Guid.NewGuid().ToString();
                newSubscriber.Subscribe<MyMessage>((m) =>
                {
                    m.DeliveredCount++;
                    messages.Add(id, m);
                    return Task.CompletedTask;
                });
                await newSubscriber.ConnectToServer(Constants.ServerAddress, id);
                subscribersId.Add(id);
            }

            ClientToClientMessenger publisher = new ClientToClientMessenger();
            await publisher.ConnectToServer(Constants.ServerAddress, Guid.NewGuid().ToString());
            foreach (var id in subscribersId)
            {
                var newMessage = new MyMessage();
                await publisher.SendToClient(id, newMessage);
            }

            Assert.AreEqual(1000, messages.Count);
            foreach (var message in messages.Values)
                Assert.AreEqual(1, message.DeliveredCount);
        }

        [TestMethod]
        public async Task SendToClient_SentFrom10Publishers_EachSubscriberReceived10Messages()
        {
            List<string> subscribersId = new List<string>();
            ConcurrentDictionary<string, MyMessage> messages = new ConcurrentDictionary<string, MyMessage>();

            for (int i = 0; i < 1000; i++)
            {
                var newSubscriber = new ClientToClientMessenger();
                string id = Guid.NewGuid().ToString();
                newSubscriber.Subscribe<MyMessage>((m) =>
                {
                    lock(id)
                    {
                        var message = messages.GetOrAdd(id, m);
                        message.DeliveredCount++;
                    }
                    return Task.CompletedTask;
                });
                await newSubscriber.ConnectToServer(Constants.ServerAddress, id);
                subscribersId.Add(id);
            }

            List<Task> tasks = new List<Task>();

            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    ClientToClientMessenger publisher = new ClientToClientMessenger();
                    await publisher.ConnectToServer(Constants.ServerAddress, Guid.NewGuid().ToString());
                    foreach (var id in subscribersId)
                    {
                        var newMessage = new MyMessage();
                        await publisher.SendToClient(id, newMessage);
                    }
                }));
            }

            await Task.WhenAll(tasks);

            Assert.AreEqual(1000, messages.Count);
            foreach (var message in messages.Values)
                Assert.AreEqual(10, message.DeliveredCount);
        }
    }
}
