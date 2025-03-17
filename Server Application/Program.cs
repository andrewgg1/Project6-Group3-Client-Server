//--connection "192.0.123.37" -m "Hello from CommandLine"

using Microsoft.VisualBasic.FileIO;
using System.Net;
using System.Net.Sockets;
using System.Text;

if (args.Length > 0)
{
    int numberOfArguments = args.Length;

    Console.WriteLine($"{numberOfArguments} Command Line Arguments were passed.");    

    if(numberOfArguments %2 != 1)
    {
        for (int i =0; i<= numberOfArguments; i=i+2)
        { 
            Console.WriteLine($"Option = {args[i]}");
            Console.WriteLine($"Argument = {args[i+1]}");

            if (args[i] == "--connection")
            {
                //Convert Commandline to IPAddress
                IPAddress iPAddressConnection = IPAddress.Parse(args[i+1]);
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
    Console.WriteLine("loopback Test Starting. \n");

    //-----Server

    //Specify the IPEndpoint we are allowing to connect | IpAddress, port
    IPEndPoint acceptIP = new IPEndPoint(IPAddress.Any, 53000);
    
    //init the listener
    TcpListener server =new(acceptIP);

    //Try listening for a conection.
    try
    {
        //This is the server's logic.
        server.Start();

        //This will create a client for the server to use to communicate with a connected client.
        using TcpClient handler = await server.AcceptTcpClientAsync();
        
        //This async task will wait for the connected client and extract it's network stream
        await using NetworkStream datastream = handler.GetStream();


        //Initialize recieving byte buffer. 1 kb buffer
        var buffer = new byte[1_024];
    
        for(int i=0; i<20; i++)
        {
            //This simultaneously writes the recieved message into buffer
            //and also extracts the byte size of the message
            int bytesRead = await datastream.ReadAsync(buffer);
        
            //Assuming it's just a string, convert from bytes to string.
            //Need to provide bytes read into GetString in case the recieved message is smaller than the total buffer size.
            var message = Encoding.UTF8.GetString(buffer,0, bytesRead);
        
            //Write the recieved message to console.
            Console.WriteLine(message);
        }

    }
    finally
    {
        server.Stop();
    }

    server.Dispose();
}

return 0;