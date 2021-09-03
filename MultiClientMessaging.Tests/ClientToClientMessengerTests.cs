using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiClientMessaging.Client.Messenger;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MultiClientMessaging.Tests
{
    [TestClass]
    public class ClientToClientMessengerTests
    {
        //requires running server
        [TestMethod]
        public async Task ConnectToServer_Disconnected_IsConnectedIsTrue()
        {
            // Arrange
            var messenger = new ClientToClientMessenger();

            //Act
            await messenger.ConnectToServer(Constants.ServerAddress, Guid.NewGuid().ToString());

            //Assert
            Assert.IsTrue(messenger.IsConnected);
        }

        //requires running server
        [TestMethod]
        public async Task DisconnectFromServer_ConnectedToServer_IsConnectedIsFalse()
        {
            // Arrange
            var messenger = new ClientToClientMessenger();
            await messenger.ConnectToServer(Constants.ServerAddress, Guid.NewGuid().ToString());

            //Act
            await messenger.DisconnectFromServer();

            //Assert
            Assert.IsFalse(messenger.IsConnected);
        }

        //requires stopped server
        [TestMethod]
        public async Task ConnectToServer_ServerIsNotAvailable_IsConnectedFalse()
        {
            // Arrange
            var messenger = new ClientToClientMessenger();

            //Act
            await messenger.ConnectToServer(Constants.ServerAddress, Guid.NewGuid().ToString());

            //Assert
            Assert.IsFalse(messenger.IsConnected);
        }

        //requires running server
        [TestMethod]
        public async Task Subscribe_SubscribedSameHandlerTwice_DeliveredMessageOnce()
        {
            // Arrange
            int deliveredCount = 0;
            var subscriber = new ClientToClientMessenger();
            string subscriberId = Guid.NewGuid().ToString();
            await subscriber.ConnectToServer(Constants.ServerAddress, subscriberId);
            subscriber.Subscribe<MyMessage>((mesasge) =>
            {
                deliveredCount++;
                return Task.CompletedTask;
            });

            var publisher = new ClientToClientMessenger();
            await publisher.ConnectToServer(Constants.ServerAddress, Guid.NewGuid().ToString());

            //Act
            await publisher.SendToClient(subscriberId, new MyMessage());

            //Assert
            Assert.AreEqual(1, deliveredCount);
        }
    }
}
