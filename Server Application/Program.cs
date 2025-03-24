//--connection "192.0.123.37" -m "Hello from CommandLine"
//SERVER
using FlightData;
using Microsoft.VisualBasic.FileIO;
using System.Net;
using System.Net.Sockets;
using System.Text;


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
        int bytesRead = datastream.Read(buffer);

        //Assuming it's just a string, convert from bytes to string.
        //Need to provide bytes read into GetString in case the recieved message is smaller than the total buffer size.
        FlightDataTelem flightData = FlightDataEncoder.GetFlightData(buffer, bytesRead);
        
        //Write the recieved message to console.
        Console.WriteLine($"Flight Fuel Level: {flightData.FuelLevel: .000000} | Timestamp: {flightData.TimeStamp:f}");
    }

}
finally
{
    server.Stop();
}

server.Dispose();

return 0;