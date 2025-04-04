//--connection "192.0.123.37" -m "Hello from CommandLine"
//SERVER
using FlightData;
using Microsoft.VisualBasic.FileIO;
using System.Net;
using System.Net.Sockets;
using System.Text;

//-----Server
Console.WriteLine("Awaiting communication with client. \n");

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


public class TCPFlightConnection
{
    public TCPFlightConnection()
    {

    }

    //new unlimited client connection changes
    public async Task ClientHandlerAsync(TcpClient client)
    {
        //This async task will wait for the connected client and extract it's network stream
        await using NetworkStream datastream = client.GetStream();

        //Initialize recieving byte buffer. 1 kb buffer
        var buffer = new byte[1_024];

        try
        {
            bool keepStreaming = true;
            while (keepStreaming) //check for end message
            {
                //This simultaneously writes the recieved message into buffer
                //and also extracts the byte size of the message
                int bytesRead = await datastream.ReadAsync(buffer);

                //check for EOF
                string endMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                if (endMessage == "end")
                {
                    Console.WriteLine("End of transmission");
                    keepStreaming = false;
                }
                else
                { //Assuming it's just a string, convert from bytes to string.
                  //Need to provide bytes read into GetString in case the recieved message is smaller than the total buffer size.
                    FlightDataTelem flightData = FlightDataEncoder.GetFlightData(buffer, bytesRead);

                    //Write the recieved message to console.
                    Console.WriteLine($"Flight Fuel Level: {flightData.FuelLevel: .000000} | Timestamp: {flightData.TimeStamp:f}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"The Client error is: {ex.Message}");
        }
        finally
        {
            client.Close();
            Console.WriteLine("Client connection closed.");
        }
    }

    //new server logic handling unlimited clients
    public async Task ServerLogicAsync(int port)
    {
        //Specify the IPEndpoint we are allowing to connect | IpAddress, port = 53000
        IPEndPoint acceptIP = new IPEndPoint(IPAddress.Any, port);

        //init & start the listener
        TcpListener server = new(acceptIP);
        server.Start();

        Console.WriteLine($"Server listening on port {port}...");

        //infinite loop = unlimited client connections
        while (true)
        {
            TcpClient client = await server.AcceptTcpClientAsync();
            Console.WriteLine("Client connected.");

            // Handle each client in its own task (no blocking!)
            _ = ClientHandlerAsync(client);
        }
    }

    //regular server logic
    public async Task ServerLogic(TcpListener server)
    {
        //This will create a client for the server to use to communicate with a connected client.
        using TcpClient handler = await server.AcceptTcpClientAsync();

        //This async task will wait for the connected client and extract it's network stream
        await using NetworkStream datastream = handler.GetStream();

        //Initialize recieving byte buffer. 1 kb buffer
        var buffer = new byte[1_024];

        bool keepStreaming = true;
        while (keepStreaming) //check for end message
        {
            //This simultaneously writes the recieved message into buffer
            //and also extracts the byte size of the message
            int bytesRead = await datastream.ReadAsync(buffer);

            //check for EOF
            string endMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

            if (endMessage == "end")
            {
                Console.WriteLine("End of transmission");
                keepStreaming = false;
            }
            else
            { //Assuming it's just a string, convert from bytes to string.
              //Need to provide bytes read into GetString in case the recieved message is smaller than the total buffer size.
                FlightDataTelem flightData = FlightDataEncoder.GetFlightData(buffer, bytesRead);

                //Write the recieved message to console.
                Console.WriteLine($"Flight Fuel Level: {flightData.FuelLevel: .000000} | Timestamp: {flightData.TimeStamp:f}");
            }
        }
    }
}
