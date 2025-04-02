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

//THIS IS TEMPORARY SINCE WE ONLY CONNECT ONE CLIENT AT A TIME RIGHT NOW
//WHEN WE MULTITHREAD THIS WILL HAVE TO BE REPLACE WITH PER-CLIENT DATA STRUCTURE
//global vars to hold state
string currentClientID = "";
List<FlightDataTelem> flightDataList = new(); //store all data points for fuel calculation

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

    bool Continue = true;
    while (Continue) //check for end message
    {
        //This simultaneously writes the recieved message into buffer
        //and also extracts the byte size of the message
        int bytesRead = await datastream.ReadAsync(buffer);

        //check for EOF
        string incomingString = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

        //handle special id message from client
        if (incomingString.StartsWith("id,"))
        {
            currentClientID = incomingString.Split(',')[1];
            Console.WriteLine($"Client connected with ID: {currentClientID}");
            continue;
        }

        if (incomingString == "end")
        {
            Console.WriteLine("End of transmission");
            Continue = false;

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
                File.WriteAllText(outputPath, $"Client ID: {currentClientID}\nFinal Average Fuel Consumption: {avgConsumption:F4} gallons/hour");

                Console.WriteLine($"Final Average Fuel Consumption stored for {currentClientID}: {avgConsumption:F4} gallons/hour");
            }
        }
        else
        {
            //Assuming it's just a string, convert from bytes to string.
            //Need to provide bytes read into GetString in case the recieved message is smaller than the total buffer size.
            FlightDataTelem flightData = FlightDataEncoder.GetFlightData(buffer, bytesRead);

            //add telemetry to session's list
            flightDataList.Add(flightData);

            //calculate current fuel consumption if we have more than one reading
            if (flightDataList.Count > 1)
            {
                var first = flightDataList.First();
                var last = flightDataList.Last();

                TimeSpan duration = last.TimeStamp.Value - first.TimeStamp.Value;
                double fuelUsed = first.FuelLevel.Value - last.FuelLevel.Value;
                double hours = duration.TotalHours;

                double currentRate = fuelUsed / hours;

                Console.WriteLine($"Current Fuel Consumption for {currentClientID}: {currentRate:F4} gallons/hour");
            }
            else
            {
                //Write the received message to console.
                Console.WriteLine($"Flight Fuel Level: {flightData.FuelLevel: .000000} | Timestamp: {flightData.TimeStamp:f}");
            }
        }
    }
}
finally
{
    server.Stop();
}

server.Dispose();

return 0;