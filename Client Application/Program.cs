//CLIENT
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

Console.WriteLine("Client Starting... \n");
IPAddress? Server = null;
int port = 53000;   //defaults to 53000

//generate unique client ID using GUID for this session
string clientID = Guid.NewGuid().ToString();

if (args.Length > 0)
{
    int numberOfArguments = args.Length;

    //DEBUG COMMAND
    //Console.WriteLine($"{numberOfArguments} Command Line Arguments were passed.");

    if (numberOfArguments % 2 != 1)
    {
        for (int i = 0; i <= (numberOfArguments/2); i = i + 2)
        {
            ////DEBUG WRITE LINES
            //Console.WriteLine($"Option = {args[i]}");
            //Console.WriteLine($"Argument = {args[i + 1]}");

            if (args[i] == "--connection")
            {
                //Convert Commandline to IPAddress
                Server = IPAddress.Parse(args[i + 1]);


                Console.WriteLine($"Client attempting connection to: {args[i+1]}");
            }

            if (args[i] == "--port")
            {
                //Convert to int
                port = int.Parse(args[i + 1]);

                Console.WriteLine($"Communication on port {port}");
            }
        }
    }
    else
    {
        Console.WriteLine("Incorrect number of arguments were passed. Please Rectify Issue and try again.");
    }
}
else
{
    Console.WriteLine("No command line argument provided, connecting to 127.0.0.1");

    //Initialize the client
    Server = IPAddress.Parse("127.0.0.1");
}

var ipEndpoint = new IPEndPoint(Server, port);
using TcpClient clientConnection = new TcpClient();

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
catch (Exception ex)
{
    // Could not read file
    Console.WriteLine(ex.Message);
    Console.ReadKey();
    return -1;
}

Console.WriteLine($"\nUsing data file: {dataFileName}");

//--Flow Control
//Console.WriteLine("\n\nClient Application Ready.\nPress enter to Start.");
//Console.ReadLine();

try
{

    //bind and connect
    await clientConnection.ConnectAsync(ipEndpoint);

    //extract connection stream
    await using NetworkStream stream = clientConnection.GetStream();

    //send client ID to server at the start of a session
    var encodedClientID = Encoding.UTF8.GetBytes($"id,{clientID}\n");
    await stream.WriteAsync(encodedClientID);
    Console.WriteLine($"Sent Client ID: {clientID}");

    byte[] buffer = new byte[1];

    StreamReader? FileReader = File.OpenText($"{dataFilesDir}\\{dataFileName}");

    if(FileReader != null)
    {
        while(!FileReader.EndOfStream) //checking for EOF
        {
            //Read line from file
            string rawMessage = FileReader.ReadLine();

            if (rawMessage != null && rawMessage != " ")
            {
                //convert to a stream of pure bytes.
                var encodedMessage = Encoding.UTF8.GetBytes(rawMessage);


            //call the extracted network stream and send a byte-encoded message.
            await stream.WriteAsync(encodedMessage);

            Console.WriteLine($"Sent: {rawMessage}");
            stream.Read(buffer, 0, 1);
            }


        }
        //once EOF is reached
        string eofMessage = "end\n";
        var encodedEOFMessage = Encoding.UTF8.GetBytes(eofMessage);
        await stream.WriteAsync(encodedEOFMessage);
        Console.WriteLine($"Sent: {eofMessage}");

        FileReader.Close();
    }
    else
    {
        Console.WriteLine("File Not Found. Ending Process");
    }
}
catch (Exception e)
{
    Console.WriteLine($"Error encountered: {e.Message}");
}

clientConnection.Close();
clientConnection.Dispose();

//Console.WriteLine("\nClient Application Done.\nPress enter to Finish.");
//Console.ReadLine();

return 0;