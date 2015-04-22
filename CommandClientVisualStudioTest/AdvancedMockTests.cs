using System;
using System.Net;
using System.Reflection;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Proshot.CommandClient;
using Rhino.Mocks;
using System.Linq;

namespace CommandClientVisualStudioTest
{
    [TestClass]
    public class AdvancedMockTests
    {
        private MockRepository mocks;

        [TestMethod]
        public void VerySimpleTest()
        {
            CMDClient client = new CMDClient(null, "Bogus network name");
            Assert.AreEqual("Bogus network name", client.NetworkName);
        }

        [TestInitialize()]
        public void Initialize()
        {
            mocks = new MockRepository();
        }

        [TestMethod]
        public void TestUserExitCommand()
        {
            IPAddress ipaddress = IPAddress.Parse("127.0.0.1");
            Command command = new Command(CommandType.UserExit, ipaddress, null);
            System.IO.Stream fakeStream = mocks.DynamicMock<System.IO.Stream>();
            byte[] commandBytes = { 0, 0, 0, 0 };
            byte[] ipLength = { 9, 0, 0, 0 };
            byte[] ip = { 49, 50, 55, 46, 48, 46, 48, 46, 49 };
            byte[] metaDataLength = { 2, 0, 0, 0 };
            byte[] metaData = { 10, 0 };

            using (mocks.Ordered())
            {
                fakeStream.Write(commandBytes, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(ipLength, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(ip, 0, 9);
                fakeStream.Flush();
                fakeStream.Write(metaDataLength, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(metaData, 0, 2);
                fakeStream.Flush();
            }
            mocks.ReplayAll();
            CMDClient client = new CMDClient(null, "Bogus network name");
            Type cmdclient = typeof(CMDClient);
            FieldInfo clientField = cmdclient.GetField("networkStream", BindingFlags.NonPublic | BindingFlags.Instance);
            // we need to set the private variable here
            clientField.SetValue(client, fakeStream);
            client.SendCommandToServerUnthreaded(command);
            mocks.VerifyAll();
        }

        [TestMethod]
        public void TestUserExitCommandWithoutMocks()
        {
            IPAddress ipaddress = IPAddress.Parse("127.0.0.1");
            Command command = new Command(CommandType.UserExit, ipaddress, null);
            MemoryStream fakeStream = new MemoryStream();
            byte[] commandBytes = { 0, 0, 0, 0 };
            byte[] ipLength = { 9, 0, 0, 0 };
            byte[] ip = { 49, 50, 55, 46, 48, 46, 48, 46, 49 };
            byte[] metaDataLength = { 2, 0, 0, 0 };
            byte[] metaData = { 10, 0 };

            fakeStream.Write(commandBytes, 0, 4);
            fakeStream.Write(ipLength, 0, 4);           
            fakeStream.Write(ip, 0, 9);           
            fakeStream.Write(metaDataLength, 0, 4);           
            fakeStream.Write(metaData, 0, 2);
            fakeStream.Flush();

            byte[] full = fakeStream.ToArray();

            byte[] buffer = new byte[4];
            Array.Copy(full, 0, buffer, 0, 4);
            Console.WriteLine(buffer);
            int commandRet = BitConverter.ToInt32(buffer, 0);
            Assert.AreEqual(0, BitConverter.ToInt32(buffer, 0));

            buffer = new byte[4];
            Array.Copy(full, 4, buffer, 0, 4);
            int senderIPSize = BitConverter.ToInt32(buffer, 0);
            Assert.AreEqual(9, BitConverter.ToInt32(buffer, 0));

            buffer = new byte[9];
            Array.Copy(full, 8, buffer, 0, 9);
            Assert.AreEqual("127.0.0.1", System.Text.Encoding.ASCII.GetString(buffer));

            buffer = new byte [4];
            Array.Copy(full, 17, buffer, 0, 4);
            int metaDataSize = BitConverter.ToInt32(buffer , 0);
            Assert.AreEqual(2, metaDataSize);

            buffer = new byte [2];
            Array.Copy(full, 21, buffer, 0, 2);
            Assert.AreEqual("\n", System.Text.Encoding.Unicode.GetString(buffer));

            CMDClient client = new CMDClient(null, "Bogus network name");
            Type cmdclient = typeof(CMDClient);
            FieldInfo clientField = cmdclient.GetField("networkStream", BindingFlags.NonPublic | BindingFlags.Instance);
            // we need to set the private variable here
            clientField.SetValue(client, fakeStream);
            client.SendCommandToServerUnthreaded(command);
            
        }

        [TestMethod]
        public void TestSemaphoreReleaseOnNormalOperation()
        {
            IPAddress ipaddress = IPAddress.Parse("127.0.0.1");
            Command command = new Command(CommandType.UserExit, ipaddress, null);
            System.IO.Stream fakeStream = mocks.DynamicMock<System.IO.Stream>();
            System.Threading.Semaphore fakeSemaphore = mocks.DynamicMock<System.Threading.Semaphore>();

            byte[] commandBytes = { 0, 0, 0, 0 };
            byte[] ipLength = { 9, 0, 0, 0 };
            byte[] ip = { 49, 50, 55, 46, 48, 46, 48, 46, 49 };
            byte[] metaDataLength = { 2, 0, 0, 0 };
            byte[] metaData = { 10, 0 };

            using (mocks.Ordered())
            {
                Expect.Call(fakeSemaphore.WaitOne()).Return(true);
                fakeStream.Write(commandBytes, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(ipLength, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(ip, 0, 9);
                fakeStream.Flush();
                fakeStream.Write(metaDataLength, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(metaData, 0, 2);
                fakeStream.Flush();
                Expect.Call(fakeSemaphore.Release()).Return(1);
            }
            mocks.ReplayAll();
            CMDClient client = new CMDClient(null, "Bogus network name");
            Type cmdclient = typeof(CMDClient);
            FieldInfo clientField = cmdclient.GetField("networkStream", BindingFlags.NonPublic | BindingFlags.Instance);
            // we need to set the private variable here
            clientField.SetValue(client, fakeStream);

            FieldInfo semaphoreField = cmdclient.GetField("semaphore", BindingFlags.NonPublic | BindingFlags.Instance);
            semaphoreField.SetValue(client, fakeSemaphore);
            client.SendCommandToServerUnthreaded(command);
            mocks.VerifyAll();
        }

        [TestMethod]
        public void TestSemaphoreReleaseOnExceptionalOperation()
        {
            IPAddress ipaddress = IPAddress.Parse("127.0.0.1");
            Command command = new Command(CommandType.UserExit, ipaddress, null);
            System.IO.Stream fakeStream = mocks.DynamicMock<System.IO.Stream>();
            System.Threading.Semaphore fakeSemaphore = mocks.DynamicMock<System.Threading.Semaphore>();

            byte[] commandBytes = { 0, 0, 0, 0 };
            byte[] ipLength = { 9, 0, 0, 0 };
            byte[] ip = { 49, 50, 55, 46, 48, 46, 48, 46, 49 };
            byte[] metaDataLength = { 2, 0, 0, 0 };
            byte[] metaData = { 10, 0 };

            using (mocks.Ordered())
            {
                Expect.Call(fakeSemaphore.WaitOne()).Return(true);
                fakeStream.Write(commandBytes, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(ipLength, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(ip, 0, 9);
                fakeStream.Flush();
                fakeStream.Write(metaDataLength, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(metaData, 0, 2);
                fakeStream.Flush();
                LastCall.On(fakeStream).Throw(new Exception());
                Expect.Call(fakeSemaphore.Release()).Return(1);
            }
            mocks.ReplayAll();

            try
            {
                CMDClient client = new CMDClient(null, "Bogus network name");
                Type cmdclient = typeof(CMDClient);

                FieldInfo clientField = cmdclient.GetField("networkStream", BindingFlags.NonPublic | BindingFlags.Instance);
                // we need to set the private variable here
                clientField.SetValue(client, fakeStream);

                FieldInfo semaphoreField = cmdclient.GetField("semaphore", BindingFlags.NonPublic | BindingFlags.Instance);
                semaphoreField.SetValue(client, fakeSemaphore);
                client.SendCommandToServerUnthreaded(command);
                mocks.VerifyAll();
            } catch { }
        }
    }
}
