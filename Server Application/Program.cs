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
    public TcpClient handler { get; set; }

    public string? currentClientID { get; set; }

    public List<FlightDataTelem> flightDataList { get; set; } //store all data points for fuel calculation

    ~TCPFlightConnection()
    {
        handler.Dispose();
    }

    private DateTime lastCalcTime; //track last time avg was computed

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
        try
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
                    //split on just the first comma so we only get the part after id,
                    var parts = endMessage.Split(',', 2);
                    var rawId = parts[1];

                    //find if there's an embedded newline. If so, remove everything after it
                    var newlineIndex = rawId.IndexOf('\n');
                    if (newlineIndex >= 0)
                    {
                        rawId = rawId.Substring(0, newlineIndex);
                    }

                    //remove \r as well and trim spaces
                    rawId = rawId.Replace("\r", "").Trim();

                    currentClientID = rawId;
                    Console.WriteLine($"Client connected with ID: {currentClientID}");

                    lastCalcTime = DateTime.Now;
                    continue;
                }

                if (endMessage == "end" || endMessage == "")
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

                        //write final avg to final
                        string outputPath = $"flight_results_{currentClientID}.txt";
                        using (StreamWriter writer = new StreamWriter(outputPath, append: true))
                        {
                            writer.WriteLine("------------------------------------------");
                            writer.WriteLine($"Final Average Fuel Consumption for {currentClientID}: {avgConsumption:F4} gallons/hour");
                            writer.WriteLine($"Timestamp: {DateTime.Now}");
                        }

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

                        //only compute & log average if 5 mins has passed
                        if ((DateTime.Now - lastCalcTime).TotalMinutes >= 5.0)
                        {
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
                                // Append partial average to file
                                string outputPath = $"flight_results_{currentClientID}.txt";
                                using (StreamWriter writer = new StreamWriter(outputPath, append: true))
                                {
                                    writer.WriteLine($"Partial Average @ {DateTime.Now}: {currentRate:F4} gallons/hour");
                                }
                            }
                            else
                            {
                                // if only 1 data point, just note that we can't compute yet
                                Console.WriteLine($"(Partial Calc) Only 1 data point so far for {currentClientID}. \n");
                            }

                            // reset the lastCalcTime for the next 5-minute interval
                            lastCalcTime = DateTime.Now;
                        }
                        else
                        {
                            // If less than 5 real-time minutes, just print the newly received data once if you want
                            // or skip printing altogether. For demonstration:
                            Console.WriteLine($"[Received Telem for {currentClientID}] Fuel: {flightData.FuelLevel}, Time: {flightData.TimeStamp}");
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
        catch (Exception ex)
        {
            Console.WriteLine("Client disconnected");
        }
    }

    public class Listener
    {
        public List<Thread> threads { get; set; }
        public bool flag { get; set; }

        public Listener(List<Thread> List, bool state)
        {
            threads = List;
            flag = state;
        }
        //Listener Thread
        public void ListenerLogic()
        {
            //Specify the IPEndpoint we are allowing to connect | IpAddress, port
            IPEndPoint acceptIP = new IPEndPoint(IPAddress.Any, 53000);

            //init the listener
            TcpListener server = new(acceptIP);

            try
            {
                //Try listening for a conection.    
                server.Start();

                while (flag)
                {
                    //This will create a client for the server to use to communicate with a connected client.
                    TcpClient connHandler = new TcpClient();
                    connHandler = server.AcceptTcpClient();


                    //Create TcpObject
                    TCPFlightConnection connection = new TCPFlightConnection(connHandler);

                    //Create a thread to run internal server logic and perform communications.
                    Thread connThread = new Thread(connection.ServerLogic);
                    connThread.IsBackground = true;
                    connThread.Start();
                    threads.Add(connThread);
                }

                server.Dispose();
            }
            catch (ThreadInterruptedException e)
            {
                server.Dispose();
            }
        }
    }
    public class Server
    {
        public static int Main()
        {
            Console.WriteLine("Awaiting communication with client. \n");

            //Initalize Needed Data Trackers.
            List<Thread> threads = new List<Thread>();
            Listener listener = new Listener(threads, true);

            //Start Listener
            Thread Listener = new Thread(listener.ListenerLogic);
            Listener.IsBackground = true;
            Listener.Start();

            while (listener.flag)
            {
                //Measured in milliseconds, Sleeps for 1 minute
                Thread.Sleep(1000 * 60);

                //If all connections are Dead, no more messages are being recieved.
                if (threads.All(t => t.ThreadState == ThreadState.Stopped))
                {
                    listener.flag = false;
                    Console.WriteLine("No TCP connection was made after alloted time. Stopping Server.");
                    Listener.Interrupt();
                    threads.ForEach(t => t.Interrupt());
                }
            }

            //Safe 
            return 0;
        }
    }
}