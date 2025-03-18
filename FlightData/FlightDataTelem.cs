using System.Runtime.ConstrainedExecution;
using System.Text;

namespace FlightData
{
    public class FlightDataTelem
    {
        public DateTime TimeStamp { get; set; }
        
        public double FuelLevel {  get; set; }

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
            string[] seperated= dataString.Split(',', StringSplitOptions.RemoveEmptyEntries);
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
