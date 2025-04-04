//--connection "192.0.123.37" -m "Hello from CommandLine"
//SERVER
using FlightData;
using Microsoft.VisualBasic.FileIO;
using System.Net;
using System.Net.Sockets;
using System.Text;


Console.WriteLine("Awaiting communication with client. \n");

//-----Server

//Specify the IPEndpoint we are allowing to connect | IpAddress, port
IPEndPoint acceptIP = new IPEndPoint(IPAddress.Any, 53000);
    
//init the listener
TcpListener server =new(acceptIP);

//Try listening for a conection.
//This is the server's logic.
server.Start();

await new TCPFlightConnection().ServerLogic(server);

server.Dispose();


/// new implementation - untested
/// await new TCPFlightConnection().ServerLogicAsync(53000);


return 0;