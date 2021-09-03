using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiClientMessaging.Client.Messenger;
using MultiClientMessaging.Client.Utils;
using System;
using System.IO;

namespace MultiClientMessaging.Tests
{
    [TestClass]
    public class CallbackMessageTests
    {
        [TestMethod]
        public void CreateCallback_MessageOkResponseString_IsOkReturnsTrue()
        {
            // Arrange
            string packetId = Guid.NewGuid().ToString();
            string response = ClientResponseUtils.GetOkResponse(packetId);

            //Act
            var callback = new CallbackMessage(response);

            //Assert
            Assert.IsTrue(callback.IsMessageResponse);
            Assert.AreEqual(packetId, callback.PacketId);
            Assert.IsTrue(callback.IsOk);
        }

        [TestMethod]
        public void CreateCallback_MessageErrorResponseString_SerializedExceptionIsNotNull()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization() { ConfigureMembers = true });
            string packetId = Guid.NewGuid().ToString();
            var exception = fixture.Create<Exception>();
            string response = ClientResponseUtils.GetErrorResponse(packetId, exception);

            //Act
            var callback = new CallbackMessage(response);

            //Assert
            Assert.IsTrue(callback.IsMessageResponse);
            Assert.AreEqual(packetId, callback.PacketId);
            Assert.IsFalse(callback.IsOk);
            Assert.IsFalse(string.IsNullOrEmpty(callback.SerializedException));
        }

        [TestMethod]
        public void CreateCallback_NonResponseMessageString_SerializedMessageIsNotNull()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization() { ConfigureMembers = true });
            var message = fixture.Create<MyMessage>();
            var transmit = new WebTransmit(message, WebMessageType.Message);
            string packet = transmit.Packet;

            //Act
            var callback = new CallbackMessage(packet);

            //Assert
            Assert.IsFalse(callback.IsMessageResponse);
            Assert.AreEqual(transmit.Id, callback.PacketId);
            Assert.AreEqual(typeof(MyMessage), callback.MessageType);
            Assert.IsFalse(string.IsNullOrEmpty(callback.SerializedMessage));
        }

        [ExpectedException(typeof(InvalidDataException))]
        [TestMethod]
        public void CreateCallback_PacketWithOnlyTwoParts_InvalidDataExceptionThrown()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization() { ConfigureMembers = true });
            var packet = $"{fixture.Create<string>()}\t{fixture.Create<string>()}";

            //Act
            var callback = new CallbackMessage(packet);

            //Assert
        }

        [ExpectedException(typeof(InvalidDataException))]
        [TestMethod]
        public void CreateCallback_PacketWith5Parts_InvalidDataExceptionThrown()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization() { ConfigureMembers = true });
            string packet = string.Empty;
            for(int i=0; i< 5;i++)
            {
                packet += $"{fixture.Create<string>()}\t";
            }

            //Act
            var callback = new CallbackMessage(packet);

            //Assert
        }

        [ExpectedException(typeof(InvalidDataException))]
        [TestMethod]
        public void CreateCallback_PacketWithWrongType_InvalidDataExceptionThrown()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization() { ConfigureMembers = true });
            string packetId = Guid.NewGuid().ToString();
            var exception = fixture.Create<Exception>();
            string response = ClientResponseUtils.GetErrorResponse(packetId, exception);
            string wrongPacket = response.Replace('r','w');

            //Act
            var callback = new CallbackMessage(wrongPacket);

            //Assert
        }
    }
}
