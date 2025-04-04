using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.ConstrainedExecution;
using System.Text;

namespace FlightData
{
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

    public class FlightDataTelem
    {
        //Not sure if you actually need it
        public int? FlightID {  get; set; }

        public DateTime? TimeStamp { get; set; }
        
        public double? FuelLevel {  get; set; }

        public FlightDataTelem() 
        {
            TimeStamp = DateTime.Now;
            FuelLevel = 0;           
        }
        public FlightDataTelem(DateTime date, double fuel)
        {
            TimeStamp = date;
            FuelLevel = fuel;
        }

    }

    public class FlightDataEncoder
    {
        //For encoding into bytes, just utilize built-in GetBytes() command.

        /// <summary>
        /// Converts byte array into a FlightData Object.
        /// </summary>
        /// <param name="bytes">Byte Array to parse</param>
        /// <param name="Count">Number of Bytes to convert</param>
        /// <returns>FlightDataTelem Object</returns>
        static public FlightDataTelem GetFlightData(byte[] bytes, int Count)
        {
            string data = Encoding.UTF8.GetString(bytes, 0 , Count);

            FlightDataTelem flight = GetFlightData(data);
            
            return flight;
        }

        /// <summary>
        /// Deserializes a given string into a FlightData Object
        /// </summary>
        /// <param name="dataString">String to parse</param>
        /// <returns>FlightDataTelem Object</returns>
        static public FlightDataTelem GetFlightData(string dataString)
        {
            //Turn the single string into an array of multiple strings.
            //The removeemptyentries array will hopefully remove the trailing last ' ' in the files.
            string[] seperated = dataString.Split(',', StringSplitOptions.RemoveEmptyEntries);
            FlightDataTelem data = new FlightDataTelem();

            if (seperated.Length == 4)
            {
                //If there are four seperated data lines, that means this is the initial line of data from the client.
                //It is the starting point and initial fuel levels/date for the rest of that flight.


                //Prepare Datetime for Conversion
                seperated[1] = seperated[1].Replace('_', '/');

                //string converts
                DateTime flightDate = DateTime.Parse(seperated[1]);
                double fuelLevel = double.Parse(seperated[2]);

                //Update Object
                data.FuelLevel = fuelLevel;
                data.TimeStamp = flightDate;
            }
            else if (seperated.Length == 3)
            {
                //Three data strings indicate this is just a continuing flight data, not the initial flight plan.

                //Prepare Datetime for Conversion
                seperated[0] = seperated[0].Replace('_', '/');

                //String convert
                DateTime flightDate = DateTime.Parse(seperated[0]);
                double fuel = double.Parse(seperated[1]);

                data.FuelLevel = fuel;
                data.TimeStamp = flightDate;
            }
            else
            {
                throw new Exception("Could not Extract Data");
            }

            return data;
        }
    }
}
