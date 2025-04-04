//--connection "192.0.123.37" -m "Hello from CommandLine"
//SERVER
using FlightData;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class TCPFlightConnection
{
    public TcpClient handler {  get; set; }

    public string? currentClientID { get; set; }

    public List<FlightDataTelem> flightDataList { get; set; } //store all data points for fuel calculation

    public TCPFlightConnection(TcpClient tcp)
    {
        handler = tcp;
        currentClientID = null;
        flightDataList = new List<FlightDataTelem>();
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
    public async void ServerLogic()
    {
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
            
            //handle special id message from client
            if (endMessage.StartsWith("id,"))
            {
                currentClientID = endMessage.Split(',')[1];
                Console.WriteLine($"Client connected with ID: {currentClientID}");
                continue;
            }

            if (endMessage == "end")
            {
                Console.WriteLine("End of transmission");
                keepStreaming = false;
                
                //calculate and save final average consumption
                if (flightDataList.Count > 1)
                {
                    var first = flightDataList.First();
                    var last = flightDataList.Last();

                    TimeSpan duration = last.TimeStamp.Value - first.TimeStamp.Value;
                    double fuelUsed = first.FuelLevel.Value - last.FuelLevel.Value;
                    double hours = duration.TotalHours;
                    double avgConsumption = fuelUsed / hours;

                    string outputPath = $"flight_results_{currentClientID}.txt";
                    File.WriteAllText(outputPath, $"Client ID: {currentClientID}\nFinal Average Fuel Consumption: {avgConsumption:F4} gallons/hour \n");

                    Console.WriteLine($"Final Average Fuel Consumption stored for {currentClientID}: {avgConsumption:F4} gallons/hour \n");
                }
            }
            
            else
            {
                try
                {
                    //Assuming it's just a string, convert from bytes to string.
                    //Need to provide bytes read into GetString in case the recieved message is smaller than the total buffer size.
                    FlightDataTelem flightData = FlightDataEncoder.GetFlightData(buffer, bytesRead);

                    //add telemetry to session's list
                    flightDataList.Add(flightData);
                

                    //calculate current fuel consumption if enough data
                    if (flightDataList.Count > 1)
                    {
                        var first = flightDataList.First();
                        var last = flightDataList.Last();

                        TimeSpan duration = last.TimeStamp.Value - first.TimeStamp.Value;
                        double fuelUsed = first.FuelLevel.Value - last.FuelLevel.Value;
                        double hours = duration.TotalHours;

                        double currentRate = fuelUsed / hours;

                        Console.WriteLine($"Current Fuel Consumption for {currentClientID}: {currentRate:F4} gallons/hour \n");
                    }
                    else
                    {
                        //write the received message to console.
                        Console.WriteLine($"Flight Fuel Level: {flightData.FuelLevel: .000000} | Timestamp: {flightData.TimeStamp:f} \\n");
                    }
                }
                catch (FormatException ex)
                {
                //if the incoming line is empty or invalid, skip it
                Console.WriteLine($"[Warning] Skipped invalid or blank telemetry line: \"{endMessage}\" \n");
                }
            }
        }
    }  

    static public int Main()
    {
        Console.WriteLine("Awaiting communication with client. \n");

        //-----Server

        //Specify the IPEndpoint we are allowing to connect | IpAddress, port
        IPEndPoint acceptIP = new IPEndPoint(IPAddress.Any, 53000);
    
        //init the listener
        TcpListener server =new(acceptIP);

        //Try listening for a conection.
        //This is the server's logic.
        server.Start();

        //Flag contin = new Flag();
        //Thread Interrupt = new(new ThreadStart(contin.Stop)); 

        while (true)
        {
            //This will create a client for the server to use to communicate with a connected client.
            TcpClient connHandler = new TcpClient();
            connHandler = server.AcceptTcpClient();

            //Create TcpObject
            TCPFlightConnection connection = new TCPFlightConnection(connHandler);

            //Create a thread to run internal server logic and perform communications.
            Thread connThread = new(new ThreadStart(connection.ServerLogic));
            connThread.Start();
        }

        server.Dispose();

        return 0;
    }
}

