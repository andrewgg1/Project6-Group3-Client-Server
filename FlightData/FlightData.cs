using System.Text;

namespace FlightData
{
    public class FlightData
    {
        public DateTime TimeStamp { get; set; }
        
        public float FuelLevel {  get; set; }

        public FlightData() 
        {
            TimeStamp = DateTime.Now;
            FuelLevel = 0;
        }
        public FlightData(DateTime date, float fuel)
        {
            TimeStamp = date;
            FuelLevel = fuel;
        }

    }

    public class FlightDataEncoder
    {
        //For encoding into bytes, just utilize built-in GetBytes() command.

        //Converts data stream into string and passes to other flightdata funciton.
        static public FlightData GetFlightData(byte[] bytes)
        {
            string data = Encoding.UTF8.GetString(bytes);

            FlightData flight = GetFlightData(data);
            
            return flight;
        }

        //Deserializes the string into a FlightData Object
        static public FlightData GetFlightData(string dataString)
        {
            //Turn the single string into an array of multiple strings.
            //The removeemptyentries array will hopefully remove the trailing last ' ' in the files.
            string[] seperated= dataString.Split(',', StringSplitOptions.RemoveEmptyEntries);
            FlightData data = new FlightData();

            if (seperated.Length == 3)
            {
                //If there are three seperated data lines, that means this is the initial line of data from the client.
                //It is the starting point and initial fuel levels/date for the rest of that flight.
                
                //string converts
                DateTime flightDate = DateTime.Parse(seperated[1]);
                float fuelLevel = float.Parse(seperated[2]);

                //Update Object
                data.FuelLevel = fuelLevel;
                data.TimeStamp = flightDate;                
            }
            else if (seperated.Length == 2)
            {
                //Two data strings indicate this is just a continuing flight data, not the initial flight plan.

                //String convert
                DateTime flightDate = DateTime.Parse(seperated[0]);
                float fuel = float.Parse(seperated[1]);

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
