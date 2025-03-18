using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

// Data files directory and filename
string dataFilesDir = "DataFiles";
string dataFileName = "";

// Get random data file
try
{
    // Get all files with a .txt extension and randomly pick one
    var allFiles = new DirectoryInfo(dataFilesDir).GetFiles("*.*").Where(f => f.Extension.ToLower() == ".txt");
    dataFileName = allFiles.ElementAt(new Random().Next(0, allFiles.Count())).Name;
}
catch(Exception ex)
{
    // Could not read file
    Console.WriteLine(ex.Message);
    Console.ReadKey();
    return;
}

Console.WriteLine($"Using data file: {dataFileName}");

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

StreamReader? FileReader = File.OpenText($"{dataFilesDir}\\{dataFileName}");

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