using System.Net;
using System.Net.Sockets;
using System.Text;

//Write the path to the file here according to your system
string pathToFiles = "";


Console.WriteLine("Client Starting...");

//Initialize the client
IPAddress Server = IPAddress.Parse("127.0.0.1");

var ipEndpoint = new IPEndPoint(Server, 53000);

using TcpClient clientConnection = new TcpClient();

//--Flow Control
    Console.WriteLine("Client Application Ready.\nPress enter to Start.");
    Console.ReadLine();

//bind and connect
await clientConnection.ConnectAsync(ipEndpoint);

//extract connection stream
await using NetworkStream stream = clientConnection.GetStream();

StreamReader? FileReader = File.OpenText(pathToFiles);

if(FileReader != null)
{
    for (int i = 0; i <20; i++)
    {
        //Read line from file
        string rawMessage = FileReader.ReadLine();

        if (rawMessage != null)
        {
            //convert to a stream of pure bytes.
            var encodedMessage = Encoding.UTF8.GetBytes(rawMessage);


            //call the extracted network stream and send a byte-encoded message.
            await stream.WriteAsync(encodedMessage);

            Console.WriteLine($"Sent: {rawMessage}");
        }

        Thread.Sleep(1000); //Stop 1 second
    }
    FileReader.Close();
}
else
{
    Console.WriteLine("File Not Found. Ending Process");
}

    Console.WriteLine("Client Application Done.\nPress enter to Finish.");
    Console.ReadLine();


clientConnection.Close();
clientConnection.Dispose();