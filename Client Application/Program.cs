﻿using System.Net;
using System.Net.Sockets;
using System.Text;

//Write the path to the file here according to your system

string pathToFiles = "./Data Files/Telem_2023_3_12 16_26_4.txt";

Console.WriteLine("Client Starting... \n\n");

IPAddress? Server = null;

if (args.Length > 0)
{
    int numberOfArguments = args.Length;

    Console.WriteLine($"{numberOfArguments} Command Line Arguments were passed.\n");

    if (numberOfArguments % 2 != 1)
    {
        for (int i = 0; i <= (numberOfArguments/2)-1; i = i + 2)
        {
            //DEBUG WRITE LINES
            //Console.WriteLine($"Option = {args[i]}");
            //Console.WriteLine($"Argument = {args[i + 1]}");

            if (args[i] == "--connection")
            {
                //Convert Commandline to IPAddress
                Server = IPAddress.Parse(args[i + 1]);


                Console.WriteLine($"Client attempting connection to: {args[i+1]}");
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

var ipEndpoint = new IPEndPoint(Server, 53000);

using TcpClient clientConnection = new TcpClient();

//--Flow Control
    Console.WriteLine("\n\nClient Application Ready.\nPress enter to Start.");
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


clientConnection.Close();
clientConnection.Dispose();

    Console.WriteLine("\nClient Application Done.\nPress enter to Finish.");
    Console.ReadLine();

return 0;