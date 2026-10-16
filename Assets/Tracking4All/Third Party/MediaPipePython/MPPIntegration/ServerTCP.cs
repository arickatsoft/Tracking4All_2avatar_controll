using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Tracking4All
{
    // Assumes packet format:
    // message_length | message_type/id | byte_data

    /// <summary>
    /// A basic TCP server that listens to a port for incoming data.
    /// </summary>
    class ServerTCP
    {
        private TcpListener server;
        private TcpClient client;
        private NetworkStream stream;
        private bool open;

        const int MAX_BUFFER_SIZE = 8388608; // about 8.3 mb
        private byte[] headerBuffer = new byte[8]; // fixed
        private uint payloadType;
        private byte[] payloadBuffer = new byte[1024]; // dynamically resizes

        private bool suppressWarnings;

        private int port;

        public ServerTCP(string ip, int port, bool suppressWarnings = true)
        {
            server = new TcpListener(IPAddress.Parse(ip), port);
            this.port = port;
            this.suppressWarnings = suppressWarnings;
        }

        public void Connect()
        {
            server.Start();
            open = true;
            StartListeningAsync();
        }

        public void Disconnect()
        {
            open = false;
            stream?.Close();
            client?.Close();
            server?.Stop();
        }

        public void StartListeningAsync()
        {
            Thread t = new Thread(new ThreadStart(StartListening));
            t.Start();
        }

        public void StartListening()
        {
            Console.WriteLine("Waiting for messages @Port:" + port);
            open = true;

            while (open)
            {
                try
                {
                    if (server.Pending())
                    {
                        client = server.AcceptTcpClient();
                        stream = client.GetStream();
                    }

                    if (client != null && client.Connected && stream != null && stream.DataAvailable)
                    {
                        byte[] headerBuffer = new byte[8];

                        // Ensure full header is received (8 bytes)
                        ReadExactly(stream, headerBuffer, headerBuffer.Length);

                        uint payloadSize = BitConverter.ToUInt32(headerBuffer, 0);
                        payloadSize = (uint)IPAddress.NetworkToHostOrder((int)payloadSize);

                        uint messageType = BitConverter.ToUInt32(headerBuffer, 4);
                        messageType = (uint)IPAddress.NetworkToHostOrder((int)messageType);

                        // Read in the payload
                        if (payloadSize > MAX_BUFFER_SIZE)
                        {
                            Logger.LogError("Payload size is over the maximum threshold!", this);
                            continue;
                        }
                        if (payloadBuffer.Length < payloadSize)
                        {
                            payloadBuffer = new byte[payloadSize];
                        }
                        payloadType = messageType;
                        ReadExactly(stream, payloadBuffer, (int)payloadSize);

                        OnPayloadConstructed?.Invoke(payloadType, payloadBuffer);
                    }
                }
                catch (SocketException)
                {
                    Console.WriteLine("Connection lost.");
                    Thread.Sleep(1000);
                }
            }
        }

        // Reads exactly 'size' bytes from the stream
        static void ReadExactly(NetworkStream stream, byte[] buffer, int size)
        {
            int totalRead = 0;
            while (totalRead < size)
            {
                int bytesRead = stream.Read(buffer, totalRead, size - totalRead);
                if (bytesRead == 0)
                {
                    throw new Exception("Connection closed unexpectedly.");
                }
                totalRead += bytesRead;
            }
        }

        /// <summary>
        /// The payload as soon as its done being constructed. The buffer pointer may be overwritten at anytime after this.
        /// </summary>
        /// <param name="msgType"></param>
        /// <param name="payload"></param>
        public delegate void PayloadConstructed(uint msgType, byte[] payload);
        public event PayloadConstructed OnPayloadConstructed;

        private void print(object o)
        {
            Console.WriteLine(o);
        }
    }

    public struct TCPMessageWrapper
    {
        public uint messageType;
        public byte[] bytes;
    }

}
