using System;
using CsvHelper;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Linq;
using System.Security.Cryptography;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Globalization;
using Microsoft.VisualBasic;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Airline_reservation_system
{
    class Program
    {
        // Save password in the database using SHA-512 algorithm
        private static string getSHA512Password(string password)
        {
            using (SHA512 sha512 = SHA512.Create())
            {
                byte[] passBytes = Encoding.UTF8.GetBytes(password);
                byte[] hashedBytes = sha512.ComputeHash(passBytes);
                return Convert.ToBase64String(hashedBytes);
            }
        }

        // Generate a 6-digit ID, can't start with 0
        public static string generateRdID()
        {
            Random rd = new Random();
            int uniqueID = rd.Next(100000, 1000000);
            while (uniqueID.ToString()[0] == '0')
            {
                uniqueID = rd.Next(100000, 1000000);
            }
            return uniqueID.ToString();
        }

        // Check User Credentials
        public static bool checkUserCredentials(string username, string password, string database, bool dbExists)
        {
            if (!dbExists)
            {
                return false;
            }
            using (StreamReader reader = new StreamReader(database))
            {
                // Look for credentials in the entire database
                while (!reader.EndOfStream)
                {
                    // Read data line by line
                    string line = "";
                    line = reader.ReadLine();
                    // Parse data in an array of strings
                    string[] dataFound = line.Split(',');

                    // Username and password are in column 6 and 7 in the user database
                    if (username == dataFound[5] && password == dataFound[6])
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // Get a customer's attribute
        public static string getUserAttribute(string database, string username, int index)
        {
            // Read all database
            string[] data = File.ReadAllLines(database);

            for (int i = 0; i <= data.Length - 1; i++)
            {
                string[] line = data[i].Split(',');

                // If username matches the database, return the info requested based on index
                if (line[5] == username)
                {
                    return line[index];
                }
            }
            return null;
        }

        // Change customer attribute
        public static void editUserAttribute(string usersDb, string username, string newData, int index)
        {
            // Read all database
            string[] data = File.ReadAllLines(usersDb);

            for (int i = 0; i <= data.Length - 1; i++)
            {

                string[] line = data[i].Split(',');
                // If username matches the database, put the new data instead of the old one in database
                if (line[5] == username)
                {
                    line[index] = newData;
                    data[i] = string.Join(",", line);
                    break;
                }
            }
            File.WriteAllLines(usersDb, data);
        }

        // Change customer password
        public static void changeCustomerPassword(string username, string oldPassword, string newPassword, string database, bool dbExists)
        {
            // Read all database
            string[] usersData = File.ReadAllLines(database);

            for (int i = 0; i <= usersData.Length - 1; i++)
            {

                string[] dataFound = usersData[i].Split(',');
                // If username and old password matches the database
                if (username == dataFound[5] && oldPassword == dataFound[6])
                {
                    // Replace old password with new password
                    dataFound[6] = newPassword;
                    usersData[i] = string.Join(",", dataFound);
                    break;
                }
            }
            // Write data back to database
            File.WriteAllLines(database, usersData);
        }



        // Change customer points
        public static void changeCustomerPoints(string username, string usersDb, string points, string op)
        {
            // Read all database
            string[] usersData = File.ReadAllLines(usersDb);

            for (int i = 0; i <= usersData.Length - 1; i++)
            {

                string[] dataFound = usersData[i].Split(',');
                // If the logged in username matches the database
                if (username == dataFound[5])
                {
                    // Data is saved in database as string, needs to be parsed as int
                    int oldPoints = int.Parse(dataFound[7]);
                    int pointsUsed = int.Parse(dataFound[8]);
                    int pointsInt = int.Parse(points);
                    int newPoints = 0;

                    // If booking a flight with credit card, add points to user's account balance
                    if (op == "+")
                    {
                        newPoints = oldPoints + pointsInt;

                    }

                    // If booking a flight with points, remove points from user's account balance
                    else if (op == "-")
                    {
                        newPoints = oldPoints - pointsInt;
                        pointsUsed += pointsInt;
                    }
                    // Turn data back into strings and store in the database
                    dataFound[7] = newPoints.ToString();
                    dataFound[8] = pointsUsed.ToString();
                    usersData[i] = string.Join(",", dataFound);
                    break;
                }
            }
            File.WriteAllLines(usersDb, usersData);
        }

        // Refund points when cancelling a flight
        public static void refundPoints(string username, string database, string points)
        {
            // Read all database
            string[] usersData = File.ReadAllLines(database);

            for (int i = 0; i <= usersData.Length - 1; i++)
            {

                string[] dataFound = usersData[i].Split(',');
                if (username == dataFound[5])
                {
                    // Update points available, and points used 

                    int oldPoints = int.Parse(dataFound[7]);
                    int pointsUsed = int.Parse(dataFound[8]);
                    int pointsInt = int.Parse(points);
                    int newPoints = 0;
                    newPoints = oldPoints + pointsInt;
                    pointsUsed -= pointsInt;

                    // Turn data back into strings and store in the database
                    dataFound[7] = newPoints.ToString();
                    dataFound[8] = pointsUsed.ToString();

                    usersData[i] = string.Join(",", dataFound);
                    break;
                }
            }
            File.WriteAllLines(database, usersData);
        }

        // Function to check admin credentials
        public static bool checkAdminCredentials(string username, string password, string database, bool dbExists)
        {
            if (!dbExists)
            {
                return false;
            }
            using (StreamReader reader = new StreamReader(database))
            {
                while (!reader.EndOfStream)
                {
                    string line = "";
                    line = reader.ReadLine();
                    // Parse data in an array of strings
                    string[] dataFound = line.Split(',');
                    // Username and password are column 5 and 6 in the admin database
                    if (username == dataFound[4] && password == dataFound[5])
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // Function to print header for flights
        public static void printHeader(string flightsDb, bool dbExists)
        {
            if (!dbExists)
            {
                Console.WriteLine("Flights database is not found!");
                return;
            }
            string[] flightsData = File.ReadAllLines(flightsDb);

            // Print only first row in the database
            string[] line = flightsData[0].Split(',');
            Console.Write($"{line[0],-15} {line[1],-6} {line[2],-4} {line[3],-16} {line[4],-16} {line[5],-14} {line[6],-14} {line[7],-11} {line[8],-20}");
        }

        // Function to get fligth attribute
        public static string getFlightAttribute(string flightsDb, string flightNumber, int index)
        {
            // Read all database
            string[] flightsData = File.ReadAllLines(flightsDb);

            for (int i = 0; i <= flightsData.Length - 1; i++)
            {
                string[] line = flightsData[i].Split(',');
                // If flight number match, return attribute  requested based on index
                if (line[0] == flightNumber)
                {
                    return line[index];
                }
            }
            return null;
        }

        // Function to print a flight's manifest given a flight number
        public static void printManifest(string flightsDb, string flightNumber)
        {
            // Read all database
            string[] flightsData = File.ReadAllLines(flightsDb);

            for (int i = 0; i <= flightsData.Length - 1; i++)
            {
                string[] line = flightsData[i].Split(',');
                // If flight number match, change attribute based on index
                if (line[0] == flightNumber)
                {
                    // If no travelers have booked this flight
                    if (line[13] == "0")
                    {
                        Console.WriteLine("No travelers have booked this flight!");
                        Console.WriteLine();
                        return;
                    }
                    // Print a list of who is to board the flight
                    Console.WriteLine("{0}:", flightNumber);
                    string[] travelers = line[13].Split('|');
                    for (int j = 0; j <= travelers.Length - 1; j++)
                    {
                        Console.WriteLine("{0}", travelers[j]);
                    }
                    Console.WriteLine();
                    return;
                }
            }
        }

        // Function to print manifests of ALL flights
        public static void printAllManifests(string flightsDb)
        {
            // Read all database
            bool oneFlightBooked = false;
            string[] flightsData = File.ReadAllLines(flightsDb);

            for (int i = 1; i <= flightsData.Length - 1; i++)
            {
                string[] line = flightsData[i].Split(',');

                if (line[13] == "0")
                {
                    continue;
                }
                Console.WriteLine("{0}:", line[0]);
                string[] travelers = line[13].Split('|');
                for (int j = 0; j <= travelers.Length - 1; j++)
                {
                    Console.WriteLine("{0}", travelers[j]);
                    oneFlightBooked = true;
                }
                Console.WriteLine();
            }
            if (oneFlightBooked == false)
            {
                Console.WriteLine("No available manifest to print since no one booked a flight!");
                Console.WriteLine();
            }
        }

        // Function to edit flight attribute given a flight number
        public static void editFlightAttribute(string flightsDb, string flightNumber, string newData, int index)
        {
            // Read all database
            string[] flightsData = File.ReadAllLines(flightsDb);

            for (int i = 0; i <= flightsData.Length - 1; i++)
            {

                string[] line = flightsData[i].Split(',');
                // If flight number match, change attribute based on index
                if (line[0] == flightNumber)
                {
                    line[index] = newData;
                    flightsData[i] = string.Join(",", line);
                    break;
                }
            }
            File.WriteAllLines(flightsDb, flightsData);
        }


        // Function to get check if a flight is available given dep and ret airports, and dep date
        public static bool isFlightAvailable(string flightsDb, string depAir, string arrAir, string depDate, bool dbExists)
        {
            if (!dbExists)
            {
                Console.WriteLine("Flights database is not found!");
                return false;
            }
            // Read all database
            string[] flightsData = File.ReadAllLines(flightsDb);

            for (int i = 0; i <= flightsData.Length - 1; i++)
            {
                string[] line = flightsData[i].Split(',');
                // If flight is found with the given information, return true
                if ((line[1] == depAir && line[2] == arrAir && line[3] == depDate))
                {
                    return true;
                }
            }
            return false;
        }

        // Function to get one-way flight 
        public static void searchOneWayFlights(string flightsDb, string depAir, string arrAir, string depDate, bool dbExists, List<string> flights)
        {
            int count = 1;
            string flightToAdd = "";
            if (!dbExists)
            {
                Console.WriteLine("Flights database is not found!");
                return;
            }
            // Read all database
            string[] flightsData = File.ReadAllLines(flightsDb);

            for (int i = 0; i <= flightsData.Length - 1; i++)
            {
                string[] line = flightsData[i].Split(',');
                // If flight is found with the given information, print it on screen for user to choose from
                if ((line[1] == depAir && line[2] == arrAir && line[3] == depDate))
                {
                    Console.Write(count + ". ");
                    count++;
                    flightToAdd = $"{line[0],-12} {line[1],-6} {line[2],-4} {line[3],-16} {line[4],-16} {line[5],-14} {line[6],-14} {line[7],-11} {line[8],-20}";
                    // Add to a list for ease of access when user's picks the flight he wants
                    flights.Add(flightToAdd);
                    Console.Write($"{line[0],-12} {line[1],-6} {line[2],-4} {line[3],-16} {line[4],-16} {line[5],-14} {line[6],-14} {line[7],-11} {line[8],-20}");
                    Console.WriteLine();
                }
            }
        }

        // Book a flight
        public static void bookFlight(string username, string name, string usersDb, string flightToAdd, string flightNumber, string flightsDb)
        {
            // Read all database
            string[] usersData = File.ReadAllLines(usersDb);
            string temp = "";

            for (int i = 0; i <= usersData.Length - 1; i++)
            {
                string[] line = usersData[i].Split(',');

                if (username == line[5])
                {
                    // If "Flights booked" row is empty for this user's account, in other words no previous flight booked
                    if (line[9] == "0")
                    {
                        line[9] = flightToAdd;
                    }
                    // If there are previous flights booked, concatenate flights in one cell with '|' as seperator
                    else
                    {
                        temp = line[9];
                        line[9] = string.Concat(temp, '|', flightToAdd);
                    }
                    usersData[i] = string.Join(",", line);
                    break;
                }
            }
            File.WriteAllLines(usersDb, usersData);

            // Adding the user's name to the flight's boarding list
            string[] flightsData = File.ReadAllLines(flightsDb);
            string travelersTemp = "";
            for (int i = 0; i <= flightsData.Length - 1; i++)
            {
                string[] line = flightsData[i].Split(',');
                if (flightNumber == line[0])
                {
                    // If no other travelers, simply replace
                    if (line[13] == "0")
                    {
                        line[13] = name;
                    }
                    // Otherwise concatenate names in one cell with '|' as seperator
                    else
                    {
                        travelersTemp = line[13];
                        line[13] = string.Concat(travelersTemp, '|', name);
                    }
                    flightsData[i] = string.Join(",", line);
                    break;
                }
            }
            File.WriteAllLines(flightsDb, flightsData);

        }

        // Cancel a flight
        public static void cancelFlight(string username, string name, string usersDb, string flightNumber, string flightsDb)
        {
            // Read all database
            string[] usersData = File.ReadAllLines(usersDb);
            // Keep track of "Flights booked" to take out the one to be canceled
            List<string> fBooked = new List<string>();
            bool found = false;
            string temp = "";
            for (int i = 0; i <= usersData.Length - 1; i++)
            {
                string[] line = usersData[i].Split(',');
                if (username == line[5])
                {
                    // If flight "Flights booked" cell is empty
                    if (line[9] == "0")
                    {
                        Console.WriteLine("Flight has not been booked!");
                        Console.WriteLine();
                        return;
                    }
                    else
                    {
                        // Split all the "Flights booked" to find the one to move to "Flights canceled" then delete from "Flights Booked"
                        string[] flightsBooked = line[9].Split('|');
                        for (int j = 0; j <= flightsBooked.Length - 1; j++)
                        {
                            fBooked.Add(flightsBooked[j]);
                            if (flightNumber == getFlightNumber(flightsBooked[j]))
                            {
                                found = true;
                                // If no previous "Flights canceled"
                                if (line[10] == "0")
                                {
                                    line[10] = flightsBooked[j];
                                    fBooked.Remove(flightsBooked[j]);
                                }
                                // Otherwise concatenate "Flights canceled" in one cell with '|' as seperator
                                else
                                {
                                    temp = line[10];
                                    line[10] = string.Concat(temp, '|', flightsBooked[j]);
                                    fBooked.Remove(flightsBooked[j]);
                                }

                                // If booked with a credit card, refund card
                                if (flightsBooked[j][flightsBooked[j].Length - 1] == 'C')
                                {
                                    string priceRefunded = flightsBooked[j].Substring(88, 7);
                                    priceRefunded = priceRefunded.Replace(" ", string.Empty);
                                    int priceRefundedInt = int.Parse(priceRefunded);
                                    int oldPoints = int.Parse(line[7]);
                                    int newPoints = oldPoints - priceRefundedInt * 10;
                                    line[7] = newPoints.ToString();
                                    Console.WriteLine("${0} will be refunded to your credit card!", priceRefunded);
                                    Console.WriteLine("{0} points will be taken from your available points!", priceRefundedInt * 10);

                                }
                                // Otherwsie, simply refund points to account balance
                                else
                                {
                                    string pointsRefunded = flightsBooked[j].Substring(100, 7);
                                    pointsRefunded = pointsRefunded.Replace(" ", string.Empty);
                                    int oldPoints = int.Parse(line[7]);
                                    int pointsUsed = int.Parse(line[8]);
                                    int pointsInt = int.Parse(pointsRefunded);
                                    int newPoints = 0;
                                    newPoints = oldPoints + pointsInt;
                                    pointsUsed -= pointsInt;
                                    line[7] = newPoints.ToString();
                                    line[8] = pointsUsed.ToString();
                                    Console.WriteLine("{0} points will be refunded to your account!", pointsRefunded);
                                }
                            }
                        }

                        // Join back "Flights booked" in one cell after removing the canceled flight
                        line[9] = string.Join("|", fBooked);
                        if (string.IsNullOrEmpty(line[9]))
                        {
                            line[9] = "0";
                        }

                        if (found)
                        {
                            Console.WriteLine("Flight {0} canceled successfully!", flightNumber); ;
                        }
                        else
                        {
                            Console.WriteLine("Flight {0} was not found in your booked flights. Try again!", flightNumber);
                        }
                    }
                    usersData[i] = string.Join(",", line);
                    break;
                }
            }
            File.WriteAllLines(usersDb, usersData);

            // Remove the user's name from the boarding list of the canceled flight
            string[] flightsData = File.ReadAllLines(flightsDb);
            List<string> travelers = new List<string>();
            for (int i = 0; i <= flightsData.Length - 1; i++)
            {
                string[] line = flightsData[i].Split(',');
                if (flightNumber == line[0])
                {
                    string[] travelersArr = line[13].Split('|');
                    for (int j = 0; j <= travelersArr.Length - 1; j++)
                    {
                        travelers.Add(travelersArr[j]);
                        if (travelersArr[j] == name)
                        {
                            travelers.Remove(travelersArr[j]);
                        }
                    }
                    line[13] = string.Join("|", travelers);
                    if (string.IsNullOrEmpty(line[13]))
                    {
                        line[13] = "0";
                    }
                    flightsData[i] = string.Join(",", line);
                    break;
                }
            }
            File.WriteAllLines(flightsDb, flightsData);

        }

        // Function to print flights
        public static void printFlights(string flights)
        {
            // Read all database
            string[] data = flights.Split('|');

            for (int i = 0; i <= data.Length - 1; i++)
            {
                Console.WriteLine(data[i]);
            }
        }

        // Validate credit card number by making sure it is 16 digits
        public static bool IsValidCreditCard(string creditCard)
        {
            string pattern = @"^\d{16}$";
            return Regex.IsMatch(creditCard, pattern);
        }

        // Calculate distance between two points given latitude and longitude, rounded to nearest integer
        public static double calculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Earth radius in miles
            const double radius = 3958.8;
            lat1 = toRadians(lat1);
            lat2 = toRadians(lat2);
            lon1 = toRadians(lon1);
            lon2 = toRadians(lon2);
            // Haversine formula
            return Math.Round(radius * Math.Acos((Math.Sin(lat1) * Math.Sin(lat2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(lon2 - lon1))));
        }
        // Convert degrees to radians
        private static double toRadians(double deg)
        {
            return deg * (Math.PI / 180);
        }

        // Function to calculate discount
        public static double calculateDiscount(string departureTimeInput, string arrivalTimeInput)
        {
            DateTime departureTime = DateTime.Parse(departureTimeInput);
            DateTime arrivalTime = DateTime.Parse(arrivalTimeInput);
            // If departing before 8 AM or arriving after 7 PM, apply 10% discount
            bool isOffPeak = departureTime.Hour < 8 || arrivalTime.Hour > 19;
            // If departing or arriving between 12:00 AM and 5 AM , apply 20% discount
            bool isRedEye = ((departureTime.Hour >= 0 && departureTime.Hour <= 5) || (arrivalTime.Hour >= 0 && arrivalTime.Hour <= 5));

            if (isRedEye)
            {
                return 0.2;
            }
            else if (isOffPeak)
            {
                return 0.1;
            }
            return 1;
        }

        // Function to calculate price of a ticket
        public static double calculateTicketPrice(double distance, double discount, int takeOff)
        {
            // $50 fixed costs
            double basePrice = 50.0;
            // $0.12 per mile
            double pricePerMile = 0.12;
            // $8 each time the plane takes off
            double tsaPrice = 8.0;
            // Total price before applying discount
            double totalCostBeforeDiscount = (basePrice + (distance * pricePerMile) + tsaPrice * takeOff);
            double totalCost;
            if (discount == 1)
            {
                // If no discount, return total price
                totalCost = totalCostBeforeDiscount;
            }
            else
            {
                // Otherwise apply discount
                totalCost = totalCostBeforeDiscount - totalCostBeforeDiscount * discount;
            }
            return Math.Round(totalCost);
        }

        // Get flight number giventhe whole flight as a string
        public static string getFlightNumber(string flightsDetails)
        {
            string flightNumber = flightsDetails.Replace(" ", string.Empty);
            return flightNumber.Substring(0, Math.Min(6, flightNumber.Length));
        }

        // Print boarding pass
        public static string printBoardingPass(string username, string flightNumber, string database)
        {
            // Read all database
            string[] data = File.ReadAllLines(database);
            string result = "";
            for (int i = 0; i <= data.Length - 1; i++)
            {
                string[] line = data[i].Split(',');
                if (username == line[5])
                {
                    // If no flight booked, return empty string
                    if (line[9] == "0")
                    {
                        return "";
                    }
                    else
                    {
                        string[] flightsBooked = line[9].Split('|');
                        for (int j = 0; j <= flightsBooked.Length - 1; j++)
                        {
                            if (flightNumber == getFlightNumber(flightsBooked[j]))
                            {
                                // Otherwise print the name of user's with relevant flight info on boarding pass
                                result = line[0] + "\t" + flightsBooked[j].Substring(0, 82);
                                return result;
                            }
                        }
                    }
                }
            }
            return "";
        }

        // Print accounting report for the company
        public static void accounting(string flightsDb, bool flightsDbExists)
        {
            if (!flightsDbExists)
            {
                Console.WriteLine("Flights database is not found!");
                return;
            }

            // Keep track of total income and total number of flights taken
            int totalIncome = 0;
            int totalFlights = 0;

            int incomePerFlight = 0;
            int nbOfTravelers = 0;
            int flightPrice = 0;
            double capacityPer = 0;
            double availableSeats;
            double aircraftCapacity;
            // Read all database
            string[] flightsData = File.ReadAllLines(flightsDb);

            for (int i = 1; i <= flightsData.Length - 1; i++)
            {
                string[] line = flightsData[i].Split(',');

                aircraftCapacity = Convert.ToDouble(line[10]);
                availableSeats = int.Parse(line[11]);

                // If someone has booked the flight, print % capacity and income of flight
                if (availableSeats < aircraftCapacity)
                {
                    totalFlights++;
                    flightPrice = int.Parse(line[7]);
                    capacityPer = ((aircraftCapacity - availableSeats) / aircraftCapacity) * 100;
                    nbOfTravelers = Convert.ToInt32(aircraftCapacity - availableSeats);
                    incomePerFlight = nbOfTravelers * flightPrice;
                    totalIncome += incomePerFlight;
                    Console.WriteLine("{0}:\t%{1}\t${2}", line[0], capacityPer, incomePerFlight);
                }
            }
            Console.WriteLine();
            // Print the report for total info
            Console.WriteLine("Total number of flights: {0}", totalFlights);
            Console.WriteLine("Total income as a company: ${0}", totalIncome);
        }

        public static void printAirports(string airportsDb, bool airportsDbExists)
        {
            if (!airportsDbExists)
            {
                Console.WriteLine("No available serviced airports at the moment");
                return;
            }
            Console.WriteLine("Available serviced airports: ");

            // Read all database
            string[] airportsData = File.ReadAllLines(airportsDb);

            for (int i = 1; i <= airportsData.Length - 1; i++)
            {
                string[] line = airportsData[i].Split(',');
                Console.Write("{0}\t", line[0]);
                airportsData[i] = string.Join(",", line);

            }
            File.WriteAllLines(airportsDb, airportsData);
        }

        //
        //
        // Main function
        //
        //

        public static void Main(string[] args)
        {
            // Needed info
            int option;
            int customerOption;
            int customerOption2;
            int adminOption;
            int marketingManOption;
            int flightManOption;
            int accountantOption;
            int loadEngineerOption;
            int editOption;
            double distance;
            string flightNumber;
            int flightToBookInt;
            bool transactionComplete = false;
            string name;

            // CSV files needed so far
            string usersDb = "usersDatabase.csv";
            string IdsDb = "IDsDatabase.csv";
            string adminsDb = "adminsDatabase.csv";
            string flightsDb = "flightsDatabase.csv";
            string airportsDb = "airportsDatabase.csv";

            // Bool to ensure if the databases exist
            bool usersDbExists, IdsDbExists, adminsDbExists, flightsDbExists, airportsDbExists;

            // Bool to ensure no same ID is already in the database
            bool IdExists;

            do
            {
                // Always check if databases have been created at the beginning of the loop
                usersDbExists = File.Exists(usersDb);
                adminsDbExists = File.Exists(adminsDb);
                IdsDbExists = File.Exists(IdsDb);
                airportsDbExists = File.Exists(airportsDb);
                flightsDbExists = File.Exists(flightsDb);

                // Print options and ask user for input
                Console.WriteLine("Welcome to Royal Skyzone Airlines! Please choose one of the following options: ");
                Console.WriteLine("\t 1.Customer Login");
                Console.WriteLine("\t 2.Create an account");
                Console.WriteLine("\t 3.Administration");
                Console.WriteLine("\t 9.Exit");
                Console.Write("Your choice: ");

                string input = Console.ReadLine();
                Console.WriteLine();
                int.TryParse(input, out option);

                // Switch statement depending on user input
                switch (option)
                {
                    // Customer Login 
                    case 1:
                        Console.WriteLine("Welcome valued customer!");
                        // Ask for username and password
                        Console.Write("\t Username: ");
                        string username = Console.ReadLine();
                        Console.Write("\t Password: ");
                        string enteredPass = getSHA512Password(Console.ReadLine());

                        // Check if input username and password matches the user's database
                        bool loginVerified = checkUserCredentials(username, enteredPass, usersDb, usersDbExists);

                        // If verified, give other options
                        if (loginVerified)
                        {
                            Console.WriteLine("Login Successfull!");
                            Console.WriteLine();
                            // Keep track of logged in user's name
                            name = getUserAttribute(usersDb, username, 0);

                            do
                            {
                                Console.WriteLine("Choose one of the following options: ");
                                Console.WriteLine("\t 1.Search for flights");
                                Console.WriteLine("\t 2.Print boarding pass");
                                Console.WriteLine("\t 3.Cancel flight");
                                Console.WriteLine("\t 4.View account history");
                                Console.WriteLine("\t 5.Change password");
                                Console.WriteLine("\t 0.Go back");
                                Console.WriteLine("\t 9.Exit");

                                Console.Write("Your choice: ");
                                string customerInput = Console.ReadLine();
                                Console.WriteLine();
                                int.TryParse(customerInput, out customerOption);

                                switch (customerOption)
                                {
                                    case 1:
                                        printAirports(airportsDb, airportsDbExists);
                                        Console.WriteLine();
                                        Console.WriteLine();
                                        // Enter departure airport, return airport and departure date
                                        Console.Write("Enter your departure airport: ");
                                        string depAir = Console.ReadLine();
                                        Console.Write("Enter your arrival airport: ");
                                        string arrAir = Console.ReadLine();
                                        Console.Write("Enter your departure date (MM/DD/YYYY): ");
                                        string depDate = Console.ReadLine();
                                        // Keep track if round trip
                                        string roundTrip;
                                        string returnDate = "";
                                        bool flightsFound = false;
                                        bool returnFlight = false;
                                        // Keep track if paid by points or card
                                        string pointsOrCard = "";
                                        do
                                        {
                                            Console.Write("Round Trip? (Y/N): ");
                                            roundTrip = Console.ReadLine();
                                            roundTrip = roundTrip.ToUpper();
                                            // If round trip, ask for return date
                                            if (roundTrip == "Y")
                                            {
                                                Console.Write("Enter your return date (MM/DD/YYYY): ");
                                                returnDate = Console.ReadLine();
                                            }
                                            // Otherwise leave loop
                                            else if (roundTrip == "N")
                                            {
                                                break;
                                            }
                                            else
                                            {
                                                Console.WriteLine("Invalid entry. Try again!");
                                            }
                                        } while (roundTrip != "Y" && roundTrip != "N");

                                        Console.WriteLine();
                                        // Make sure there are flights available
                                        flightsFound = isFlightAvailable(flightsDb, depAir, arrAir, depDate, flightsDbExists);

                                        // If there are
                                        if (flightsFound)
                                        {
                                            List<string> flights = new List<string>();
                                            // If one-way
                                            if (roundTrip == "N")
                                            {
                                                // Print available flights on screen
                                                flights.Clear();
                                                Console.WriteLine("Available flights: ");
                                                printHeader(flightsDb, flightsDbExists);
                                                Console.WriteLine();
                                                searchOneWayFlights(flightsDb, depAir, arrAir, depDate, flightsDbExists, flights);
                                                Console.WriteLine();
                                                do
                                                {

                                                    Console.Write("Choose one of the above flights to book: ");
                                                    string flightToBook = Console.ReadLine();
                                                    int.TryParse(flightToBook, out flightToBookInt);
                                                    Console.WriteLine();
                                                    flightNumber = getFlightNumber(flights[flightToBookInt - 1]);
                                                    string pointsToEarn = getFlightAttribute(flightsDb, flightNumber, 8);

                                                    // Print payment options
                                                    Console.WriteLine("Choose one of the following options: ");
                                                    Console.WriteLine("\t 1.Book chosen flight with credit card");
                                                    Console.WriteLine("\t 2.Book chosen flight with points");
                                                    Console.WriteLine("\t 0.Go back");
                                                    Console.WriteLine("\t 9.Exit");
                                                    Console.Write("Your choice: ");
                                                    string customerInput2 = Console.ReadLine();
                                                    Console.WriteLine();
                                                    int.TryParse(customerInput2, out customerOption2);

                                                    switch (customerOption2)
                                                    {
                                                        // Paying by card
                                                        case 1:
                                                            string cardNumber = "";
                                                            do
                                                            {
                                                                // Keep track that this flight was paid by card
                                                                pointsOrCard = "C";
                                                                Console.WriteLine("Flight Number: {0}", flightNumber);
                                                                // Print amount to pay on screen
                                                                Console.WriteLine("Price to pay: ${0}", getFlightAttribute(flightsDb, flightNumber, 7));
                                                                // Ask for card number confirmation
                                                                Console.Write("Enter you credit card number: ");
                                                                cardNumber = Console.ReadLine();

                                                                // If valid
                                                                if (IsValidCreditCard(cardNumber))
                                                                {
                                                                    using (StreamWriter writer = new StreamWriter(usersDb, true))
                                                                    {
                                                                        // Book flight chosen
                                                                        bookFlight(username, name, usersDb, flights[flightToBookInt - 1] + pointsOrCard, flightNumber, flightsDb);
                                                                        string availableCapacity = getFlightAttribute(flightsDb, flightNumber, 11);
                                                                        int availableCapacityInt = int.Parse(availableCapacity);
                                                                        // Update the available capacity of the plane
                                                                        availableCapacityInt--;
                                                                        availableCapacity = availableCapacityInt.ToString();
                                                                        editFlightAttribute(flightsDb, flightNumber, availableCapacity, 11);
                                                                    }
                                                                    // Update user's points balance
                                                                    changeCustomerPoints(username, usersDb, pointsToEarn, "+");
                                                                    Console.WriteLine("Flight booked successfully!");
                                                                    transactionComplete = true;
                                                                }
                                                                else
                                                                {
                                                                    Console.WriteLine("Invalid card number. Try again!");
                                                                    Console.WriteLine();
                                                                }
                                                            } while (!IsValidCreditCard(cardNumber));
                                                            break;

                                                        // Paying by points
                                                        case 2:
                                                            pointsOrCard = "P";
                                                            string availablePoints = getUserAttribute(usersDb, username, 7);
                                                            Console.WriteLine("Points available: {0}", availablePoints);
                                                            Console.WriteLine("Points required: {0}", pointsToEarn);
                                                            int availablePointsInt = int.Parse(availablePoints);
                                                            int pointsToEarnInt = int.Parse(pointsToEarn);
                                                            // Enough available points?
                                                            if (availablePointsInt >= pointsToEarnInt)
                                                            {
                                                                // If yes, update points
                                                                changeCustomerPoints(username, usersDb, pointsToEarn, "-");
                                                                using (StreamWriter writer = new StreamWriter(usersDb, true))
                                                                {
                                                                    bookFlight(username, name, usersDb, flights[flightToBookInt - 1] + pointsOrCard, flightNumber, flightsDb);
                                                                    string availableCapacity = getFlightAttribute(flightsDb, flightNumber, 11);
                                                                    int availableCapacityInt = int.Parse(availableCapacity);
                                                                    availableCapacityInt--;
                                                                    availableCapacity = availableCapacityInt.ToString();
                                                                    editFlightAttribute(flightsDb, flightNumber, availableCapacity, 11);
                                                                }
                                                                Console.WriteLine("Flight booked successfully!");
                                                                transactionComplete = true;
                                                                Console.WriteLine();
                                                            }
                                                            // Otherwise ask to pay by card
                                                            else
                                                            {
                                                                Console.WriteLine("Insufficient points. Please pay with a credit card!");
                                                                Console.WriteLine();
                                                            }
                                                            break;

                                                        case 0:
                                                            break;

                                                        case 9:
                                                            Console.WriteLine("Thank you for working with Royal Airlines!");
                                                            Console.Write("Goodbye!\u263A");
                                                            return;

                                                        default:
                                                            Console.WriteLine("Invalid entry. Try again!");
                                                            Console.WriteLine();
                                                            break; ;
                                                    }

                                                } while ((customerOption2 != 0) && !transactionComplete);
                                            }

                                            // If round trip
                                            else if (roundTrip == "Y")
                                            {
                                                List<string> flightsDep = new List<string>();
                                                List<string> flightsRet = new List<string>();

                                                // Make sure there are returning flughts for the return date
                                                returnFlight = isFlightAvailable(flightsDb, arrAir, depAir, returnDate, flightsDbExists);
                                                // If there is
                                                if (returnFlight)
                                                {
                                                    // Print departing flights
                                                    Console.WriteLine("Available departing flights: ");
                                                    printHeader(flightsDb, flightsDbExists);
                                                    Console.WriteLine();
                                                    searchOneWayFlights(flightsDb, depAir, arrAir, depDate, flightsDbExists, flightsDep);
                                                    Console.WriteLine();
                                                    // Keep track of departing flights for ease of access when user's chooses
                                                    Console.Write("Choose one of the above flights to book: ");
                                                    string flightDepToBook = Console.ReadLine();
                                                    int flightDepToBookInt;
                                                    int.TryParse(flightDepToBook, out flightDepToBookInt);
                                                    Console.WriteLine();
                                                    string flightDepNumber = getFlightNumber(flightsDep[flightDepToBookInt - 1]);
                                                    string pointsDepToEarn = getFlightAttribute(flightsDb, flightDepNumber, 8);
                                                    int pointsDepToEarnInt = int.Parse(pointsDepToEarn);

                                                    // Print returning flights
                                                    Console.WriteLine("Available returning flights: ");
                                                    printHeader(flightsDb, flightsDbExists);
                                                    Console.WriteLine();
                                                    searchOneWayFlights(flightsDb, arrAir, depAir, returnDate, flightsDbExists, flightsRet);
                                                    Console.WriteLine();
                                                    // Keep track of departing flights for ease of access when user's chooses
                                                    Console.Write("Choose one of the above flights to book: ");
                                                    string flightRetToBook = Console.ReadLine();
                                                    int flightRetToBookInt;
                                                    int.TryParse(flightRetToBook, out flightRetToBookInt);
                                                    Console.WriteLine();
                                                    string flightRetNumber = getFlightNumber(flightsRet[flightRetToBookInt - 1]);
                                                    string pointsRetToEarn = getFlightAttribute(flightsDb, flightRetNumber, 8);
                                                    int pointsRetToEarnInt = int.Parse(pointsRetToEarn);
                                                    string totalPoints = (pointsDepToEarnInt + pointsRetToEarnInt).ToString();

                                                    do
                                                    {
                                                        // How to pay for both flights?
                                                        Console.WriteLine("Choose one of the following options: ");
                                                        Console.WriteLine("\t 1.Book chosen flight with credit card");
                                                        Console.WriteLine("\t 2.Book chosen flight with points");
                                                        Console.WriteLine("\t 0.Go back");
                                                        Console.WriteLine("\t 9.Exit");
                                                        Console.Write("Your choice: ");
                                                        string customerInput2 = Console.ReadLine();
                                                        Console.WriteLine();
                                                        int.TryParse(customerInput2, out customerOption2);

                                                        switch (customerOption2)
                                                        {
                                                            // Pay by card
                                                            case 1:
                                                                string cardNumber = "";
                                                                do
                                                                {
                                                                    pointsOrCard = "C";
                                                                    Console.WriteLine("Flight Numbers: {0} {1}", flightDepNumber, flightRetNumber);
                                                                    int priceDep = int.Parse(getFlightAttribute(flightsDb, flightDepNumber, 7));
                                                                    int priceRet = int.Parse(getFlightAttribute(flightsDb, flightRetNumber, 7));
                                                                    Console.WriteLine("Price to pay: ${0}", priceDep + priceRet);

                                                                    Console.Write("Enter you credit card number: ");
                                                                    cardNumber = Console.ReadLine();

                                                                    if (IsValidCreditCard(cardNumber))
                                                                    {
                                                                        using (StreamWriter writer = new StreamWriter(usersDb, true))
                                                                        {
                                                                            // Book departing flight
                                                                            bookFlight(username, name, usersDb, flightsDep[flightDepToBookInt - 1] + pointsOrCard, flightDepNumber, flightsDb);
                                                                            string availableCapacity = getFlightAttribute(flightsDb, flightDepNumber, 11);
                                                                            int availableCapacityInt = int.Parse(availableCapacity);
                                                                            availableCapacityInt--;
                                                                            availableCapacity = availableCapacityInt.ToString();
                                                                            editFlightAttribute(flightsDb, flightDepNumber, availableCapacity, 11);

                                                                            // book returning flight
                                                                            bookFlight(username, name, usersDb, flightsRet[flightRetToBookInt - 1] + pointsOrCard, flightRetNumber, flightsDb);
                                                                            availableCapacity = getFlightAttribute(flightsDb, flightRetNumber, 11);
                                                                            availableCapacityInt = int.Parse(availableCapacity);
                                                                            availableCapacityInt--;
                                                                            availableCapacity = availableCapacityInt.ToString();
                                                                            editFlightAttribute(flightsDb, flightRetNumber, availableCapacity, 11);

                                                                        }
                                                                        // Update points
                                                                        changeCustomerPoints(username, usersDb, totalPoints, "+");
                                                                        Console.WriteLine("Flights {0} and {1} booked successfully!", flightDepNumber, flightRetNumber);
                                                                        transactionComplete = true;
                                                                    }
                                                                    else
                                                                    {
                                                                        Console.WriteLine("Invalid card number. Try again!");
                                                                        Console.WriteLine();
                                                                    }
                                                                } while (!IsValidCreditCard(cardNumber));
                                                                break;

                                                            // Pay by points
                                                            case 2:

                                                                pointsOrCard = "P";
                                                                string availablePoints = getUserAttribute(usersDb, username, 7);
                                                                Console.WriteLine("Points available: {0}", availablePoints);
                                                                Console.WriteLine("Points required: {0}", totalPoints);
                                                                int availablePointsInt = int.Parse(availablePoints);
                                                                int pointsToEarnInt = int.Parse(totalPoints);
                                                                // Enough avilable points?
                                                                if (availablePointsInt >= pointsToEarnInt)
                                                                {
                                                                    changeCustomerPoints(username, usersDb, totalPoints, "-");
                                                                    using (StreamWriter writer = new StreamWriter(usersDb, true))
                                                                    {
                                                                        // Book departing flights
                                                                        bookFlight(username, name, usersDb, flightsDep[flightDepToBookInt - 1] + pointsOrCard, flightDepNumber, flightsDb);
                                                                        string availableCapacity = getFlightAttribute(flightsDb, flightDepNumber, 11);
                                                                        int availableCapacityInt = int.Parse(availableCapacity);
                                                                        availableCapacityInt--;
                                                                        availableCapacity = availableCapacityInt.ToString();
                                                                        editFlightAttribute(flightsDb, flightDepNumber, availableCapacity, 11);

                                                                        // Book returning flights
                                                                        bookFlight(username, name, usersDb, flightsRet[flightDepToBookInt - 1] + pointsOrCard, flightRetNumber, flightsDb);
                                                                        availableCapacity = getFlightAttribute(flightsDb, flightRetNumber, 11);
                                                                        availableCapacityInt = int.Parse(availableCapacity);
                                                                        availableCapacityInt--;
                                                                        availableCapacity = availableCapacityInt.ToString();
                                                                        editFlightAttribute(flightsDb, flightRetNumber, availableCapacity, 11);
                                                                    }
                                                                    Console.WriteLine("Flights {0} and {1} booked successfully!", flightDepNumber, flightRetNumber);
                                                                    transactionComplete = true;
                                                                    Console.WriteLine();
                                                                }
                                                                else
                                                                {
                                                                    Console.WriteLine("Insufficient points. Please pay with a credit card!");
                                                                    Console.WriteLine();
                                                                    goto case 1;
                                                                }
                                                                break;

                                                            case 0:
                                                                break;

                                                            case 9:
                                                                Console.WriteLine("Thank you for working with Royal Airlines!");
                                                                Console.Write("Goodbye!\u263A");
                                                                return;

                                                            default:
                                                                Console.WriteLine("Invalid entry. Try again!");
                                                                Console.WriteLine();
                                                                break; ;
                                                        }

                                                    } while ((customerOption2 != 0) && !transactionComplete);

                                                }
                                                // If no retun flights available, break
                                                else
                                                {
                                                    Console.WriteLine("No return flights available. Try again!");
                                                }
                                            }
                                        }
                                        // If no flight at all available, ask for new info
                                        else
                                        {
                                            Console.WriteLine("No flight available for the entered information. Please use different airports or dates!");
                                        }
                                        Console.WriteLine();
                                        break;

                                    // Print boarding pass
                                    case 2:
                                        Console.Write("Enter flight number: ");
                                        flightNumber = Console.ReadLine();
                                        Console.WriteLine();
                                        string boardingPass = printBoardingPass(username, flightNumber, usersDb);
                                        // If empty
                                        if (boardingPass == "")
                                        {
                                            Console.WriteLine("Flight entered has not been booked. Try again!");
                                            Console.WriteLine();
                                            break;
                                        }
                                        // Otherwise print
                                        Console.WriteLine("Boarding Pass for {0}:", flightNumber);
                                        Console.WriteLine(boardingPass);
                                        Console.WriteLine();
                                        break;

                                    // Cancel a flight
                                    case 3:
                                        Console.Write("Enter flight number to be canceled: ");
                                        flightNumber = Console.ReadLine();
                                        string retFlightNumber = "";
                                        do
                                        {

                                            Console.Write("Round Trip? (Y/N): ");
                                            roundTrip = Console.ReadLine();
                                            roundTrip = roundTrip.ToUpper();
                                            // If round trip, ask for returning flight number
                                            if (roundTrip == "Y")
                                            {
                                                Console.Write("Enter your returning flight number: ");
                                                retFlightNumber = Console.ReadLine();
                                            }
                                            else if (roundTrip == "N")
                                            {
                                                break;
                                            }
                                            else
                                            {
                                                Console.WriteLine("Invalid entry. Try again!");
                                            }
                                        } while (roundTrip != "Y" && roundTrip != "N");

                                        // If round trip
                                        if (roundTrip == "Y")
                                        {
                                            // Cancel departing flight
                                            cancelFlight(username, name, usersDb, flightNumber, flightsDb);
                                            string availableCapacity = getFlightAttribute(flightsDb, flightNumber, 11);
                                            int availableCapacityInt = int.Parse(availableCapacity);
                                            availableCapacityInt++;
                                            availableCapacity = availableCapacityInt.ToString();
                                            editFlightAttribute(flightsDb, flightNumber, availableCapacity, 11);
                                            // Cancel returning flight
                                            cancelFlight(username, name, usersDb, retFlightNumber, flightsDb);
                                            // Update available seats
                                            availableCapacity = getFlightAttribute(flightsDb, retFlightNumber, 11);
                                            availableCapacityInt = int.Parse(availableCapacity);
                                            availableCapacityInt++;
                                            availableCapacity = availableCapacityInt.ToString();
                                            editFlightAttribute(flightsDb, retFlightNumber, availableCapacity, 11);

                                        }
                                        // If one-way
                                        else
                                        {
                                            // Cancel flight
                                            cancelFlight(username, name, usersDb, flightNumber, flightsDb);
                                            // Update available seats
                                            string availableCapacity = getFlightAttribute(flightsDb, flightNumber, 11);
                                            int availableCapacityInt = int.Parse(availableCapacity);
                                            availableCapacityInt++;
                                            availableCapacity = availableCapacityInt.ToString();
                                            editFlightAttribute(flightsDb, flightNumber, availableCapacity, 11);
                                        }
                                        Console.WriteLine();
                                        break;

                                    // View account history
                                    case 4:
                                        Console.WriteLine("Flights Booked: ");
                                        printFlights(getUserAttribute(usersDb, username, 9));
                                        Console.WriteLine();
                                        Console.WriteLine("Flights Canceled: ");
                                        printFlights(getUserAttribute(usersDb, username, 10));
                                        Console.WriteLine();
                                        Console.WriteLine("Points Available: {0}", getUserAttribute(usersDb, username, 7));
                                        Console.WriteLine("Points Used: {0}", getUserAttribute(usersDb, username, 8));
                                        Console.WriteLine();
                                        break;

                                    // Change password
                                    case 5:
                                        Console.Write("Enter your old password: ");
                                        string oldPassword = getSHA512Password(Console.ReadLine());
                                        if (oldPassword == enteredPass)
                                        {
                                            // If password matches, request new password
                                            Console.Write("Enter your new password: ");
                                            string newPassword1 = getSHA512Password(Console.ReadLine());
                                            Console.Write("Confirm your new password: ");
                                            string newPassword2 = getSHA512Password(Console.ReadLine());
                                            if (newPassword1 == newPassword2)
                                            {
                                                // Store new password in database
                                                changeCustomerPassword(username, oldPassword, newPassword2, usersDb, usersDbExists);
                                                Console.WriteLine("Password changed successfully!");
                                                Console.WriteLine();
                                            }
                                            else
                                            {
                                                Console.WriteLine("Passwords don't match! Try again!");
                                                Console.WriteLine();
                                                goto case 5;
                                            }
                                        }
                                        else
                                        {
                                            Console.WriteLine("Old password entered is incorrect. Try again!");
                                            Console.WriteLine();
                                        }
                                        break;

                                    case 0:
                                        break;

                                    case 9:
                                        Console.WriteLine("Thank you for working with Royal Airlines!");
                                        Console.Write("Goodbye!\u263A");
                                        return;

                                    default:
                                        Console.WriteLine("Invalid entry. Try again!");
                                        Console.WriteLine();
                                        break;
                                }
                            } while (customerOption != 5 && customerOption != 0);
                        }

                        // If not try again
                        else
                        {
                            Console.WriteLine("Your credentials are either invalid or they don't exist. Try again!");
                            Console.WriteLine();
                        }
                        Console.WriteLine();
                        break;

                    // Creating a customer account 
                    case 2:
                        // Customer's info
                        Console.Write("Enter your full name: ");
                        string fullName = Console.ReadLine();

                        Console.WriteLine("Enter your address: ");
                        Console.Write(string.Format("{0,17}", "Street: "));
                        string street = Console.ReadLine();
                        Console.Write(string.Format("{0,17}", "City: "));
                        string city = Console.ReadLine();
                        Console.Write(string.Format("{0,17}", "State: "));
                        string state = Console.ReadLine();
                        string address = street + " " + city + " " + state;

                        Console.Write("Enter your phone: ");
                        string phone = Console.ReadLine();

                        Console.Write("Enter your birthday in the following format MM/DD/YYYY: ");
                        string birthday = Console.ReadLine();

                        Console.Write("Enter your credit card number: ");
                        string creditCard = Console.ReadLine();

                        // Create a unique 6-digit ID 
                        string createdID = "";
                        IdExists = true;
                        while (IdExists == true)
                        {
                            createdID = generateRdID();

                            if (!IdsDbExists)
                            {
                                using (StreamWriter writer = new StreamWriter(IdsDb, true))
                                {
                                    writer.WriteLine("Generated IDs");
                                }
                                IdExists = false;
                            }
                            else
                            {
                                using (StreamReader reader = new StreamReader(IdsDb))
                                {
                                    // Check if ID already exists in database
                                    string line = "";
                                    while (!reader.EndOfStream)
                                    {
                                        // Parse data line by line
                                        line = reader.ReadLine();
                                        string[] dataFound = line.Split(',');
                                        // If ID already generated, generate a new one and check again
                                        if (createdID == dataFound[0])
                                        {
                                            Console.WriteLine("Oops. Someone already has that ID");
                                            IdExists = true;
                                            break;
                                        }
                                        // ID generated is good to go, break out of loop
                                        IdExists = false;
                                    }
                                }

                            }
                        }
                        using (StreamWriter writer = new StreamWriter(IdsDb, true))
                        {
                            writer.WriteLine(string.Format("{0}", createdID));
                        }
                        // Print unique username created
                        Console.WriteLine("Your unique username is " + createdID);
                        Console.Write("Create your password: ");
                        string password = getSHA512Password(Console.ReadLine());

                        // Create a user's object with the given information
                        Customer customer1 = new Customer(fullName, address, phone, birthday, creditCard, createdID, password);

                        // Store given information in database
                        using (StreamWriter writer = new StreamWriter(usersDb, true))
                        {
                            if (!usersDbExists)
                            {
                                writer.WriteLine("Name,Address,Phone,Birthday,Credit Card Number,Username,Password,Points Available,Points Used,Flights Booked,Flights Canceled");
                            }
                            writer.WriteLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}", customer1.fullName, customer1.address, customer1.phone, customer1.birthday, customer1.creditCard, customer1.username, customer1.password, "0", "0", "0", "0"));
                        }
                        Console.WriteLine("Account created successfully.");
                        Console.WriteLine();
                        break;

                    // Administration options
                    case 3:
                        do
                        {
                            usersDbExists = File.Exists(usersDb);
                            adminsDbExists = File.Exists(adminsDb);
                            IdsDbExists = File.Exists(IdsDb);
                            airportsDbExists = File.Exists(airportsDb);
                            flightsDbExists = File.Exists(flightsDb);

                            Console.WriteLine("Choose one of the following options: ");
                            Console.WriteLine("\t 1.Log in as Marketing Manager");
                            Console.WriteLine("\t 2.Log in as Load Engineer");
                            Console.WriteLine("\t 3.Log in as Flight Manager");
                            Console.WriteLine("\t 4.Log in as Accountant");
                            Console.WriteLine("\t 5.Create an Administrative account");
                            Console.WriteLine("\t 0.Go back");
                            Console.WriteLine("\t 9.Exit");

                            Console.Write("Your choice: ");
                            string adminInput = Console.ReadLine();
                            Console.WriteLine();
                            int.TryParse(adminInput, out adminOption);

                            switch (adminOption)
                            {
                                case 1:
                                    Console.WriteLine("Welcome Marketing Manager!");
                                    // Ask for username and password
                                    Console.Write("\t Username: ");
                                    username = Console.ReadLine();
                                    Console.Write("\t Password: ");
                                    // Save password using SHA-512 security
                                    enteredPass = getSHA512Password(Console.ReadLine());


                                    // Check if input username and password matches the equivalent in the database
                                    loginVerified = checkAdminCredentials(username, enteredPass, adminsDb, adminsDbExists);

                                    // If verified, give other options
                                    if (loginVerified)
                                    {
                                        Console.WriteLine("Login Successfull!");
                                        Console.WriteLine();
                                        do
                                        {
                                            // Marketing manager options
                                            Console.WriteLine("Choose one of the following options: ");
                                            Console.WriteLine("\t 1.Assign aircraft model");
                                            Console.WriteLine("\t 0.Go back");
                                            Console.WriteLine("\t 9.Exit");

                                            Console.Write("Your choice: ");
                                            string marketingManInput = Console.ReadLine();
                                            Console.WriteLine();
                                            int.TryParse(marketingManInput, out marketingManOption);

                                            switch (marketingManOption)
                                            {
                                                // Assign an aircrft given a flight number
                                                case 1:
                                                    Console.Write("Enter flight number: ");
                                                    flightNumber = Console.ReadLine();
                                                    string manData = getFlightAttribute(flightsDb, flightNumber, 12);
                                                    double dist = 0;
                                                    if (!flightsDbExists)
                                                    {
                                                        Console.WriteLine("Flights database not found!");
                                                        Console.WriteLine();
                                                    }
                                                    else
                                                    {
                                                        if (manData == null)
                                                        {
                                                            Console.WriteLine("Flight not found!");
                                                            Console.WriteLine();
                                                            break;
                                                        }
                                                        // Assign based on distance
                                                        else
                                                        {
                                                            Console.WriteLine("Flight {0} distance: {1}", flightNumber, manData);
                                                            dist = Double.Parse(manData);

                                                            if (dist >= 400 && dist <= 900)
                                                            {
                                                                Console.WriteLine("Recommended Aircraft: 737");
                                                            }
                                                            else if (dist >= 901 && dist <= 1300)
                                                            {
                                                                Console.WriteLine("Recommended Aircraft: 757");
                                                            }
                                                            else if (dist >= 1301 && dist <= 1700)
                                                            {
                                                                Console.WriteLine("Recommended Aircraft: 767");
                                                            }
                                                            else if (dist >= 1701 && dist <= 2100)
                                                            {
                                                                Console.WriteLine("Recommended Aircraft: 777");
                                                            }
                                                            else if (dist >= 2101 && dist <= 5000)
                                                            {
                                                                Console.WriteLine("Recommended Aircraft: 787");
                                                            }
                                                            else
                                                            {
                                                                Console.WriteLine("No available aircraft recommended");
                                                            }

                                                            Console.Write("Your choice: ");
                                                            string manChoice = Console.ReadLine();
                                                            // Add chosen aircraft capacity to the database
                                                            if (manChoice == "737")
                                                            {
                                                                editFlightAttribute(flightsDb, flightNumber, manChoice, 9);
                                                                editFlightAttribute(flightsDb, flightNumber, "200", 10);
                                                                editFlightAttribute(flightsDb, flightNumber, "200", 11);

                                                            }
                                                            else if (manChoice == "757")
                                                            {
                                                                editFlightAttribute(flightsDb, flightNumber, manChoice, 9);
                                                                editFlightAttribute(flightsDb, flightNumber, "200", 10);
                                                                editFlightAttribute(flightsDb, flightNumber, "200", 11);

                                                            }
                                                            else if (manChoice == "767")
                                                            {
                                                                editFlightAttribute(flightsDb, flightNumber, manChoice, 9);
                                                                editFlightAttribute(flightsDb, flightNumber, "200", 10);
                                                                editFlightAttribute(flightsDb, flightNumber, "200", 11);

                                                            }
                                                            else if (manChoice == "777")
                                                            {
                                                                editFlightAttribute(flightsDb, flightNumber, manChoice, 9);
                                                                editFlightAttribute(flightsDb, flightNumber, "200", 10);
                                                                editFlightAttribute(flightsDb, flightNumber, "200", 11);

                                                            }
                                                            else if (manChoice == "787")
                                                            {
                                                                editFlightAttribute(flightsDb, flightNumber, manChoice, 9);
                                                                editFlightAttribute(flightsDb, flightNumber, "200", 10);
                                                                editFlightAttribute(flightsDb, flightNumber, "200", 11);

                                                            }
                                                            else
                                                            {
                                                                Console.WriteLine("Invalid entry. Try again!");
                                                            }
                                                            Console.WriteLine("Aircraft assigned successfully!");
                                                            Console.WriteLine();
                                                        }
                                                    }
                                                    break;

                                                case 0:
                                                    break;

                                                case 9:
                                                    Console.WriteLine("Thank you for working with Royal Airlines!");
                                                    Console.Write("Goodbye!\u263A");
                                                    return;

                                                default:
                                                    Console.WriteLine("Invalid entry. Try again!");
                                                    Console.WriteLine();
                                                    break;
                                            }


                                        } while (marketingManOption != 0);
                                    }

                                    // If not try again
                                    else
                                    {
                                        Console.WriteLine("Your credentials are either invalid or they don't exist. Try again!");
                                        Console.WriteLine();
                                    }
                                    Console.WriteLine();
                                    break;

                                // Load engineer options
                                case 2:
                                    Console.WriteLine("Welcome Load Engineer!");
                                    // Ask for username and password
                                    Console.Write("\t Username: ");
                                    username = Console.ReadLine();
                                    Console.Write("\t Password: ");
                                    // Save password using SHA-512 security
                                    enteredPass = getSHA512Password(Console.ReadLine());


                                    // Check if input username and password matches the equivalent in the database
                                    loginVerified = checkAdminCredentials(username, enteredPass, adminsDb, adminsDbExists);

                                    // If verified, give other options
                                    if (loginVerified)
                                    {
                                        Console.WriteLine("Login Successfull!");
                                        Console.WriteLine();
                                        do
                                        {
                                            usersDbExists = File.Exists(usersDb);
                                            adminsDbExists = File.Exists(adminsDb);
                                            IdsDbExists = File.Exists(IdsDb);
                                            airportsDbExists = File.Exists(airportsDb);
                                            flightsDbExists = File.Exists(flightsDb);

                                            bool validLoc = false;
                                            string strLat1, strLon1, strLat2, strLon2;
                                            double lat1 = 0;
                                            double lon1 = 0;
                                            double lat2 = 0;
                                            double lon2 = 0;

                                            Console.WriteLine("Choose one of the following options: ");
                                            Console.WriteLine("\t 1.Add flights");
                                            Console.WriteLine("\t 2.Edit flights");
                                            Console.WriteLine("\t 3.Delete flights");
                                            Console.WriteLine("\t 0.Go back");
                                            Console.WriteLine("\t 9.Exit");
                                            Console.Write("Your choice: ");
                                            string loadEngineerInput = Console.ReadLine();
                                            Console.WriteLine();
                                            int.TryParse(loadEngineerInput, out loadEngineerOption);

                                            switch (loadEngineerOption)
                                            {
                                                // Add a flight
                                                case 1:
                                                    Console.Write("Enter flight number: ");
                                                    flightNumber = Console.ReadLine();

                                                    Console.Write("Enter the departure city: ");
                                                    string from = Console.ReadLine();

                                                    Console.Write("Enter the destination city: ");
                                                    string to = Console.ReadLine();

                                                    // Keep track if airports are already in database
                                                    bool fromAirportExists = false;
                                                    bool toAirportExists = false;

                                                    // If the database does not exist, create it
                                                    if (!airportsDbExists)
                                                    {
                                                        using (StreamWriter writer = new StreamWriter(airportsDb, true))
                                                        {
                                                            writer.WriteLine("Airport,Latitude,Longitude");
                                                        }
                                                    }
                                                    // If the database exists
                                                    else
                                                    {
                                                        using (StreamReader reader = new StreamReader(airportsDb))
                                                        {
                                                            string line = "";
                                                            while (!reader.EndOfStream)
                                                            {
                                                                // Parse data line by line
                                                                line = reader.ReadLine();
                                                                string[] dataFound = line.Split(',');
                                                                // If from airport exists, get its latitude and longitude
                                                                if (from == dataFound[0])
                                                                {
                                                                    fromAirportExists = true;
                                                                    lat1 = Double.Parse(dataFound[1]);
                                                                    lon1 = Double.Parse(dataFound[2]);
                                                                }
                                                                // If to airport exists, get its latitude and longitude
                                                                else if (to == dataFound[0])
                                                                {
                                                                    toAirportExists = true;
                                                                    lat2 = Double.Parse(dataFound[1]);
                                                                    lon2 = Double.Parse(dataFound[2]);
                                                                }
                                                            }
                                                        }
                                                    }

                                                    // If the from airport does not exist, ask for latitude and longitude
                                                    if (!fromAirportExists)
                                                    {
                                                        do
                                                        {
                                                            Console.Write("Enter {0} latitude: ", from);
                                                            strLat1 = Console.ReadLine();
                                                            Console.Write("Enter {0} longitude: ", from);
                                                            strLon1 = Console.ReadLine();
                                                            if (Double.TryParse(strLat1, out lat1) && Double.TryParse(strLon1, out lon1))
                                                            {
                                                                validLoc = true;
                                                            }
                                                            else
                                                            {
                                                                Console.WriteLine("Invalid coordiantes. Try again!");
                                                                validLoc = false;
                                                            }
                                                        } while (!validLoc);

                                                        // Store info in airports database
                                                        using (StreamWriter writer = new StreamWriter(airportsDb, true))
                                                        {
                                                            writer.WriteLine(string.Format("{0},{1},{2}", from, strLat1, strLon1));
                                                        }
                                                    }
                                                    // If the to airport does not exist, ask for latitude and longitude
                                                    if (!toAirportExists)
                                                    {
                                                        do
                                                        {
                                                            Console.Write("Enter {0} latitude: ", to);
                                                            strLat2 = Console.ReadLine();
                                                            Console.Write("Enter {0} longitude: ", to);
                                                            strLon2 = Console.ReadLine();
                                                            if (Double.TryParse(strLat2, out lat2) && Double.TryParse(strLon2, out lon2))
                                                            {
                                                                validLoc = true;
                                                            }
                                                            else
                                                            {
                                                                Console.WriteLine("Invalid coordiantes. Try again!");
                                                                validLoc = false;
                                                            }
                                                        } while (!validLoc);

                                                        // Store info in airports database
                                                        using (StreamWriter writer = new StreamWriter(airportsDb, true))
                                                        {
                                                            writer.WriteLine(string.Format("{0},{1},{2}", to, strLat2, strLon2));
                                                        }
                                                    }

                                                    Console.Write("Enter the departure date (MM/DD/YYYY): ");
                                                    string departureDate = Console.ReadLine();
                                                    Console.Write("Enter the departure time (hh:mm tt): ");
                                                    string departureTime = Console.ReadLine();
                                                    Console.Write("Enter the arrival date (MM/DD/YYYY): ");
                                                    string arrivalDate = Console.ReadLine();
                                                    Console.Write("Enter the arrival time (hh:mm tt): ");
                                                    string arrivalTime = Console.ReadLine();

                                                    // Get the distance between from and to airports
                                                    distance = calculateDistance(lat1, lon1, lat2, lon2);
                                                    // Get eligible discount
                                                    double discount = calculateDiscount(departureTime, arrivalTime);
                                                    // Get price
                                                    double price = calculateTicketPrice(distance, discount, 1);
                                                    Console.WriteLine("Price based on entered information: ${0}", price);

                                                    int points = (int)(price * 10);
                                                    // Store given information in database
                                                    Flights flight1 = new Flights(flightNumber, from, to, departureDate, departureTime, arrivalDate, arrivalTime, price, distance);
                                                    using (StreamWriter writer = new StreamWriter(flightsDb, true))
                                                    {
                                                        if (!flightsDbExists)
                                                        {
                                                            writer.WriteLine("Flight Number,From,To,Departure Date,Departure Time,Arrival Date,Arrival Time,Price ($),Points to be Earned,Aircraft,Aircraft Capacity,Available Seats,Distance (miles),Travelers");
                                                        }
                                                        writer.WriteLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13}", flight1.flightNumber, flight1.departureCity, flight1.arrivalCity, flight1.departureDate, flight1.departureTime, flight1.arrivalDate, flight1.arrivalTime, flight1.price, points, flight1.aircraft, flight1.aircraftCapacity, flight1.availableSeats, flight1.distance, "0"));
                                                    }
                                                    Console.WriteLine("Flight {0} added successfully!", flightNumber);

                                                    Console.WriteLine();
                                                    break;

                                                // Edit flight given flight number
                                                case 2:
                                                    do
                                                    {
                                                        Console.WriteLine("Edit flights");
                                                        Console.WriteLine("\t 1.Edit flight number");
                                                        Console.WriteLine("\t 2.Edit departure city");
                                                        Console.WriteLine("\t 3.Edit arrival city");
                                                        Console.WriteLine("\t 4.Edit departure date");
                                                        Console.WriteLine("\t 5.Edit departure time");
                                                        Console.WriteLine("\t 6.Edit arrival date");
                                                        Console.WriteLine("\t 7.Edit arrival time");
                                                        Console.WriteLine("\t 0.Go back");
                                                        Console.WriteLine("\t 9.Exit");

                                                        Console.Write("Your choice: ");
                                                        string editInput = Console.ReadLine();
                                                        Console.WriteLine();
                                                        int.TryParse(editInput, out editOption);

                                                        switch (editOption)
                                                        {
                                                            // Edit flight number
                                                            case 1:
                                                                Console.Write("Enter flight number to be edited: ");
                                                                flightNumber = Console.ReadLine();

                                                                if (getFlightAttribute(flightsDb, flightNumber, 0) == null)
                                                                {
                                                                    Console.WriteLine("Flight not found!");
                                                                    Console.WriteLine();
                                                                    break;
                                                                }

                                                                Console.Write("Enter your new flight number: ");
                                                                string newData = Console.ReadLine();
                                                                if (!flightsDbExists)
                                                                {
                                                                    Console.WriteLine("Flights database not found!");
                                                                    Console.WriteLine();
                                                                    break;
                                                                }
                                                                else
                                                                {
                                                                    editFlightAttribute(flightsDb, flightNumber, newData, editOption - 1);
                                                                    Console.WriteLine("Flight edited successfully!");
                                                                    Console.WriteLine();
                                                                }
                                                                break;

                                                            // Edit departure city
                                                            case 2:
                                                                Console.Write("Enter flight number to be edited: ");
                                                                flightNumber = Console.ReadLine();

                                                                if (getFlightAttribute(flightsDb, flightNumber, 0) == null)
                                                                {
                                                                    Console.WriteLine("Flight not found!");
                                                                    Console.WriteLine();
                                                                    break;
                                                                }

                                                                Console.Write("Enter your new departure city: ");
                                                                newData = Console.ReadLine();
                                                                if (!flightsDbExists)
                                                                {
                                                                    Console.WriteLine("Flights database not found!");
                                                                    Console.WriteLine();
                                                                    break;
                                                                }
                                                                else
                                                                {
                                                                    from = newData;
                                                                    to = getFlightAttribute(flightsDb, flightNumber, 2);
                                                                    departureTime = getFlightAttribute(flightsDb, flightNumber, 4);
                                                                    arrivalTime = getFlightAttribute(flightsDb, flightNumber, 6);


                                                                    // If the database does not exist, create it
                                                                    if (!airportsDbExists)
                                                                    {
                                                                        Console.WriteLine("Airports database does not exist!");
                                                                        break;
                                                                    }
                                                                    // If the database exists
                                                                    else
                                                                    {
                                                                        using (StreamReader reader = new StreamReader(airportsDb))
                                                                        {
                                                                            string line = "";
                                                                            while (!reader.EndOfStream)
                                                                            {
                                                                                // Parse data line by line
                                                                                line = reader.ReadLine();
                                                                                string[] dataFound = line.Split(',');
                                                                                // If from airport exists, get its latitude and longitude
                                                                                if (from == dataFound[0])
                                                                                {
                                                                                    fromAirportExists = true;
                                                                                    lat1 = Double.Parse(dataFound[1]);
                                                                                    lon1 = Double.Parse(dataFound[2]);
                                                                                }
                                                                                // If to airport exists, get its latitude and longitude
                                                                                else if (to == dataFound[0])
                                                                                {
                                                                                    toAirportExists = true;
                                                                                    lat2 = Double.Parse(dataFound[1]);
                                                                                    lon2 = Double.Parse(dataFound[2]);
                                                                                }
                                                                            }
                                                                        }

                                                                        // Get the distance between from and to airports
                                                                        distance = calculateDistance(lat1, lon1, lat2, lon2);
                                                                        // Get eligible discount
                                                                        discount = calculateDiscount(departureTime, arrivalTime);
                                                                        // Get price
                                                                        price = calculateTicketPrice(distance, discount, 1);
                                                                        Console.WriteLine("Price based on entered information: ${0}", price);

                                                                        points = (int)(price * 10);

                                                                        string priceStr = price.ToString();
                                                                        string pointsStr = points.ToString();
                                                                        string distanceStr = distance.ToString();

                                                                        editFlightAttribute(flightsDb, flightNumber, from, 1);
                                                                        editFlightAttribute(flightsDb, flightNumber, priceStr, 7);
                                                                        editFlightAttribute(flightsDb, flightNumber, pointsStr, 8);
                                                                        editFlightAttribute(flightsDb, flightNumber, distanceStr, 12);
                                                                    }

                                                                    Console.WriteLine("Flight edited successfully!");
                                                                    Console.WriteLine();
                                                                }
                                                                break;

                                                            // Edit arrival city
                                                            case 3:
                                                                Console.Write("Enter flight number to be edited: ");
                                                                flightNumber = Console.ReadLine();

                                                                if (getFlightAttribute(flightsDb, flightNumber, 0) == null)
                                                                {
                                                                    Console.WriteLine("Flight not found1");
                                                                    Console.WriteLine();
                                                                    break;
                                                                }

                                                                Console.Write("Enter your new arrival city: ");
                                                                newData = Console.ReadLine();


                                                                if (!flightsDbExists)
                                                                {
                                                                    Console.WriteLine("Flights database not found!");
                                                                    Console.WriteLine();
                                                                    break;
                                                                }
                                                                else
                                                                {
                                                                    to = newData;
                                                                    from = getFlightAttribute(flightsDb, flightNumber, 1);
                                                                    departureTime = getFlightAttribute(flightsDb, flightNumber, 4);
                                                                    arrivalTime = getFlightAttribute(flightsDb, flightNumber, 6);


                                                                    // If the database does not exist, create it
                                                                    if (!airportsDbExists)
                                                                    {
                                                                        Console.WriteLine("Airports database does not exist!");
                                                                        break;
                                                                    }
                                                                    // If the database exists
                                                                    else
                                                                    {
                                                                        using (StreamReader reader = new StreamReader(airportsDb))
                                                                        {
                                                                            string line = "";
                                                                            while (!reader.EndOfStream)
                                                                            {
                                                                                // Parse data line by line
                                                                                line = reader.ReadLine();
                                                                                string[] dataFound = line.Split(',');
                                                                                // If from airport exists, get its latitude and longitude
                                                                                if (from == dataFound[0])
                                                                                {
                                                                                    lat1 = Double.Parse(dataFound[1]);
                                                                                    lon1 = Double.Parse(dataFound[2]);
                                                                                }
                                                                                // If to airport exists, get its latitude and longitude
                                                                                else if (to == dataFound[0])
                                                                                {
                                                                                    lat2 = Double.Parse(dataFound[1]);
                                                                                    lon2 = Double.Parse(dataFound[2]);
                                                                                }
                                                                            }
                                                                        }

                                                                        // Get the distance between from and to airports
                                                                        distance = calculateDistance(lat1, lon1, lat2, lon2);
                                                                        // Get eligible discount
                                                                        discount = calculateDiscount(departureTime, arrivalTime);
                                                                        // Get price
                                                                        price = calculateTicketPrice(distance, discount, 1);
                                                                        Console.WriteLine("Price based on entered information: ${0}", price);

                                                                        points = (int)(price * 10);

                                                                        string priceStr = price.ToString();
                                                                        string pointsStr = points.ToString();
                                                                        string distanceStr = distance.ToString();

                                                                        editFlightAttribute(flightsDb, flightNumber, to, 2);
                                                                        editFlightAttribute(flightsDb, flightNumber, priceStr, 7);
                                                                        editFlightAttribute(flightsDb, flightNumber, pointsStr, 8);
                                                                        editFlightAttribute(flightsDb, flightNumber, distanceStr, 12);
                                                                    }
                                                                    Console.WriteLine("Flight edited successfully!");
                                                                    Console.WriteLine();
                                                                }
                                                                break;

                                                            // Edit departure date
                                                            case 4:
                                                                Console.Write("Enter flight number to be edited: ");
                                                                flightNumber = Console.ReadLine();

                                                                if (getFlightAttribute(flightsDb, flightNumber, 0) == null)
                                                                {
                                                                    Console.WriteLine("Flight not found!");
                                                                    Console.WriteLine();
                                                                    break;
                                                                }

                                                                Console.Write("Enter your new departure date: ");
                                                                newData = Console.ReadLine();

                                                                if (!flightsDbExists)
                                                                {
                                                                    Console.WriteLine("Flights database not found!");
                                                                    Console.WriteLine();
                                                                    break;
                                                                }
                                                                else
                                                                {
                                                                    editFlightAttribute(flightsDb, flightNumber, newData, editOption - 1);
                                                                    Console.WriteLine("Flight edited successfully!");
                                                                    Console.WriteLine();
                                                                }
                                                                break;

                                                            // Edit departure time
                                                            case 5:
                                                                Console.Write("Enter flight number to be edited: ");
                                                                flightNumber = Console.ReadLine();

                                                                if (getFlightAttribute(flightsDb, flightNumber, 0) == null)
                                                                {
                                                                    Console.WriteLine("Flight not found!");
                                                                    Console.WriteLine();
                                                                    break;
                                                                }

                                                                Console.Write("Enter your new departure time: ");
                                                                newData = Console.ReadLine();

                                                                if (!flightsDbExists)
                                                                {
                                                                    Console.WriteLine("Flights database not found!");
                                                                    Console.WriteLine();
                                                                    break;
                                                                }
                                                                else
                                                                {
                                                                    departureTime = newData;
                                                                    arrivalTime = getFlightAttribute(flightsDb, flightNumber, 6);
                                                                    from = getFlightAttribute(flightsDb, flightNumber, 1);
                                                                    to = getFlightAttribute(flightsDb, flightNumber, 2);


                                                                    // If the database does not exist, create it
                                                                    if (!airportsDbExists)
                                                                    {
                                                                        Console.WriteLine("Airports database does not exist!");
                                                                        break;
                                                                    }
                                                                    // If the database exists
                                                                    else
                                                                    {
                                                                        using (StreamReader reader = new StreamReader(airportsDb))
                                                                        {
                                                                            string line = "";
                                                                            while (!reader.EndOfStream)
                                                                            {
                                                                                // Parse data line by line
                                                                                line = reader.ReadLine();
                                                                                string[] dataFound = line.Split(',');
                                                                                // If from airport exists, get its latitude and longitude
                                                                                if (from == dataFound[0])
                                                                                {
                                                                                    lat1 = Double.Parse(dataFound[1]);
                                                                                    lon1 = Double.Parse(dataFound[2]);
                                                                                }
                                                                                // If to airport exists, get its latitude and longitude
                                                                                else if (to == dataFound[0])
                                                                                {
                                                                                    lat2 = Double.Parse(dataFound[1]);
                                                                                    lon2 = Double.Parse(dataFound[2]);
                                                                                }
                                                                            }
                                                                        }

                                                                        // Get the distance between from and to airports
                                                                        distance = calculateDistance(lat1, lon1, lat2, lon2);
                                                                        // Get eligible discount
                                                                        discount = calculateDiscount(departureTime, arrivalTime);
                                                                        // Get price
                                                                        price = calculateTicketPrice(distance, discount, 1);
                                                                        Console.WriteLine("Price based on entered information: ${0}", price);

                                                                        points = (int)(price * 10);

                                                                        string priceStr = price.ToString();
                                                                        string pointsStr = points.ToString();
                                                                        string distanceStr = distance.ToString();

                                                                        editFlightAttribute(flightsDb, flightNumber, departureTime, 4);
                                                                        editFlightAttribute(flightsDb, flightNumber, priceStr, 7);
                                                                        editFlightAttribute(flightsDb, flightNumber, pointsStr, 8);
                                                                        editFlightAttribute(flightsDb, flightNumber, distanceStr, 12);
                                                                    }
                                                                    Console.WriteLine("Flight edited successfully!");
                                                                    Console.WriteLine();
                                                                }
                                                                break;

                                                            // Edit arrival date
                                                            case 6:
                                                                Console.Write("Enter flight number to be edited: ");
                                                                flightNumber = Console.ReadLine();

                                                                if (getFlightAttribute(flightsDb, flightNumber, 0) == null)
                                                                {
                                                                    Console.WriteLine("Flight not found!");
                                                                    Console.WriteLine();
                                                                    break;
                                                                }

                                                                Console.Write("Enter your new arrival date: ");
                                                                newData = Console.ReadLine();
                                                                if (!flightsDbExists)
                                                                {
                                                                    Console.WriteLine("Flights database not found!");
                                                                    Console.WriteLine();
                                                                    break;
                                                                }
                                                                else
                                                                {
                                                                    editFlightAttribute(flightsDb, flightNumber, newData, editOption - 1);
                                                                    Console.WriteLine("Flight edited successfully!");
                                                                    Console.WriteLine();
                                                                }
                                                                break;

                                                            // Edit arrival time
                                                            case 7:
                                                                Console.Write("Enter flight number to be edited: ");
                                                                flightNumber = Console.ReadLine();

                                                                if (getFlightAttribute(flightsDb, flightNumber, 0) == null)
                                                                {
                                                                    Console.WriteLine("Flight not found!");
                                                                    Console.WriteLine();
                                                                    break;
                                                                }

                                                                Console.Write("Enter your new arrival time: ");
                                                                newData = Console.ReadLine();

                                                                if (!flightsDbExists)
                                                                {
                                                                    Console.WriteLine("Flights database not found!");
                                                                    Console.WriteLine();
                                                                    break;
                                                                }
                                                                else
                                                                {
                                                                    arrivalTime = newData;
                                                                    departureTime = getFlightAttribute(flightsDb, flightNumber, 4);
                                                                    from = getFlightAttribute(flightsDb, flightNumber, 1);
                                                                    to = getFlightAttribute(flightsDb, flightNumber, 2);


                                                                    // If the database does not exist, create it
                                                                    if (!airportsDbExists)
                                                                    {
                                                                        Console.WriteLine("Airports database does not exist!");
                                                                        break;
                                                                    }
                                                                    // If the database exists
                                                                    else
                                                                    {
                                                                        using (StreamReader reader = new StreamReader(airportsDb))
                                                                        {
                                                                            string line = "";
                                                                            while (!reader.EndOfStream)
                                                                            {
                                                                                // Parse data line by line
                                                                                line = reader.ReadLine();
                                                                                string[] dataFound = line.Split(',');
                                                                                // If from airport exists, get its latitude and longitude
                                                                                if (from == dataFound[0])
                                                                                {
                                                                                    lat1 = Double.Parse(dataFound[1]);
                                                                                    lon1 = Double.Parse(dataFound[2]);
                                                                                }
                                                                                // If to airport exists, get its latitude and longitude
                                                                                else if (to == dataFound[0])
                                                                                {
                                                                                    lat2 = Double.Parse(dataFound[1]);
                                                                                    lon2 = Double.Parse(dataFound[2]);
                                                                                }
                                                                            }
                                                                        }

                                                                        // Get the distance between from and to airports
                                                                        distance = calculateDistance(lat1, lon1, lat2, lon2);
                                                                        // Get eligible discount
                                                                        discount = calculateDiscount(departureTime, arrivalTime);
                                                                        // Get price
                                                                        price = calculateTicketPrice(distance, discount, 1);
                                                                        Console.WriteLine("Price based on entered information: ${0}", price);

                                                                        points = (int)(price * 10);

                                                                        string priceStr = price.ToString();
                                                                        string pointsStr = points.ToString();
                                                                        string distanceStr = distance.ToString();

                                                                        editFlightAttribute(flightsDb, flightNumber, arrivalTime, 6);
                                                                        editFlightAttribute(flightsDb, flightNumber, priceStr, 7);
                                                                        editFlightAttribute(flightsDb, flightNumber, pointsStr, 8);
                                                                        editFlightAttribute(flightsDb, flightNumber, distanceStr, 12);
                                                                    }
                                                                    Console.WriteLine("Flight edited successfully!");
                                                                    Console.WriteLine();
                                                                }
                                                                break;

                                                            case 0:
                                                                break;

                                                            case 9:
                                                                Console.WriteLine("Thank you for working with Royal Airlines!");
                                                                Console.Write("Goodbye!\u263A");
                                                                return;

                                                            default:
                                                                Console.WriteLine("Invalid entry. Try again!");
                                                                Console.WriteLine();
                                                                break;
                                                        }

                                                    } while (editOption != 0);
                                                    break;

                                                // Delete flight given flight number
                                                case 3:
                                                    Console.Write("Enter flight number to be deleted: ");
                                                    flightNumber = Console.ReadLine();

                                                    if (!flightsDbExists)
                                                    {
                                                        Console.WriteLine("Flights database not found!");
                                                        Console.WriteLine();
                                                        break;
                                                    }
                                                    else if (getFlightAttribute(flightsDb, flightNumber, 0) == null)
                                                    {
                                                        Console.WriteLine("Flight not found!");
                                                        Console.WriteLine();
                                                        break;
                                                    }
                                                    else
                                                    {
                                                        string[] flightsData = File.ReadAllLines(flightsDb);
                                                        for (int i = 0; i < flightsData.Length - 1; i++)
                                                        {
                                                            string[] line = flightsData[i].Split(',');
                                                            if (line[0] == flightNumber)
                                                            {
                                                                flightsData[i] = flightsData[i + 1];
                                                            }
                                                        }
                                                        Array.Resize(ref flightsData, flightsData.Length - 1);
                                                        File.WriteAllLines(flightsDb, flightsData);
                                                    }
                                                    Console.WriteLine("Flight {0} deleted successfully!", flightNumber);
                                                    Console.WriteLine();
                                                    break;

                                                case 0:
                                                    break;

                                                case 9:
                                                    Console.WriteLine("Thank you for working with Royal Airlines!");
                                                    Console.Write("Goodbye!\u263A");
                                                    return;

                                                default:
                                                    Console.WriteLine("Invalid entry. Try again!");
                                                    Console.WriteLine();
                                                    break;
                                            }


                                        } while (loadEngineerOption != 0);
                                    }

                                    // If not try again
                                    else
                                    {
                                        Console.WriteLine("Your credentials are either invalid or they don't exist. Try again!");
                                        Console.WriteLine();
                                    }
                                    Console.WriteLine();
                                    break;

                                // Flight manager 
                                case 3:
                                    Console.WriteLine("Welcome Flight Manager!");
                                    // Ask for username and password
                                    Console.Write("\t Username: ");
                                    username = Console.ReadLine();
                                    Console.Write("\t Password: ");
                                    // Save password using SHA-512 security
                                    enteredPass = getSHA512Password(Console.ReadLine());


                                    // Check if input username and password matches the equivalent in the database
                                    loginVerified = checkAdminCredentials(username, enteredPass, adminsDb, adminsDbExists);

                                    // If verified, give other options
                                    if (loginVerified)
                                    {
                                        Console.WriteLine("Login Successfull!");
                                        Console.WriteLine();

                                        do
                                        {
                                            // Flight manager options
                                            Console.WriteLine("Choose one of the following options: ");
                                            Console.WriteLine("\t 1.Print flight manifest of a flight");
                                            Console.WriteLine("\t 2.Print flight manifest of ALL flights");
                                            Console.WriteLine("\t 0.Go back");
                                            Console.WriteLine("\t 9.Exit");
                                            Console.Write("Your choice: ");
                                            string flightManInput = Console.ReadLine();
                                            Console.WriteLine();
                                            int.TryParse(flightManInput, out flightManOption);

                                            // Flight manager options
                                            switch (flightManOption)
                                            {
                                                // Print manifest of flight given flight number
                                                case 1:
                                                    Console.Write("Enter flight number: ");
                                                    flightNumber = Console.ReadLine();
                                                    Console.WriteLine();
                                                    if (!flightsDbExists)
                                                    {
                                                        Console.WriteLine("Flights database not found!");
                                                        Console.WriteLine();
                                                    }
                                                    else
                                                    {
                                                        printManifest(flightsDb, flightNumber);
                                                    }
                                                    break;

                                                // Print manifest of all flights
                                                case 2:
                                                    if (!flightsDbExists)
                                                    {
                                                        Console.WriteLine("Flights database not found!");
                                                        Console.WriteLine();
                                                    }
                                                    else
                                                    {
                                                        printAllManifests(flightsDb);
                                                    }
                                                    break;

                                                case 0:
                                                    break;

                                                case 9:
                                                    Console.WriteLine("Thank you for working with Royal Airlines!");
                                                    Console.Write("Goodbye!\u263A");
                                                    return;

                                                default:
                                                    Console.WriteLine("Invalid entry. Try again!");
                                                    Console.WriteLine();
                                                    break;
                                            }
                                        } while (flightManOption != 0);
                                    }

                                    // If not try again
                                    else
                                    {
                                        Console.WriteLine("Your credentials are either invalid or they don't exist. Try again!");
                                        Console.WriteLine();
                                    }
                                    Console.WriteLine();
                                    break;

                                // Accountant
                                case 4:
                                    Console.WriteLine("Welcome Accountant!");
                                    // Ask for username and password
                                    Console.Write("\t Username: ");
                                    username = Console.ReadLine();
                                    Console.Write("\t Password: ");
                                    // Save password using SHA-512 security
                                    enteredPass = getSHA512Password(Console.ReadLine());

                                    // Check if input username and password matches the equivalent in the database
                                    loginVerified = checkAdminCredentials(username, enteredPass, adminsDb, adminsDbExists);

                                    // If verified, give other options
                                    if (loginVerified)
                                    {
                                        Console.WriteLine("Login Successfull!");
                                        Console.WriteLine();

                                        do
                                        {
                                            // Accountant options
                                            Console.WriteLine("Choose one of the following options: ");
                                            Console.WriteLine("\t 1.Print accounting report");
                                            Console.WriteLine("\t 0.Go back");
                                            Console.WriteLine("\t 9.Exit");

                                            Console.Write("Your choice: ");
                                            string accountantInput = Console.ReadLine();
                                            Console.WriteLine();
                                            int.TryParse(accountantInput, out accountantOption);

                                            switch (accountantOption)
                                            {
                                                // Print accounting report
                                                case 1:
                                                    if (!flightsDbExists)
                                                    {
                                                        Console.WriteLine("Flights database not found!");
                                                        Console.WriteLine();
                                                    }
                                                    else
                                                    {
                                                        accounting(flightsDb, flightsDbExists);
                                                        Console.WriteLine();
                                                    }
                                                    break;

                                                case 0:
                                                    break;

                                                case 9:
                                                    Console.WriteLine("Thank you for working with Royal Airlines!");
                                                    Console.Write("Goodbye!\u263A");
                                                    return;

                                                default:
                                                    Console.WriteLine("Invalid entry. Try again!");
                                                    Console.WriteLine();
                                                    break;
                                            }

                                        } while (accountantOption != 0);
                                    }

                                    // If not try again
                                    else
                                    {
                                        Console.WriteLine("Your credentials are either invalid or they don't exist. Try again!");
                                        Console.WriteLine();
                                    }
                                    Console.WriteLine();
                                    break;

                                // Create an admin account
                                case 5:
                                    Console.Write("Enter your full name: ");
                                    fullName = Console.ReadLine();

                                    Console.WriteLine("Enter your address: ");
                                    Console.Write(string.Format("{0,17}", "Street: "));
                                    street = Console.ReadLine();
                                    Console.Write(string.Format("{0,17}", "City: "));
                                    city = Console.ReadLine();
                                    Console.Write(string.Format("{0,17}", "State: "));
                                    state = Console.ReadLine();
                                    address = street + " " + city + " " + state;

                                    Console.Write("Enter your phone: ");
                                    phone = Console.ReadLine();

                                    Console.Write("Enter your birthday in the following format MM/DD/YYYY: ");
                                    birthday = Console.ReadLine();

                                    createdID = "";
                                    IdExists = true;
                                    while (IdExists == true)
                                    {
                                        createdID = generateRdID();

                                        if (!IdsDbExists)
                                        {
                                            using (StreamWriter writer = new StreamWriter(IdsDb, true))
                                            {
                                                writer.WriteLine("Generated IDs");
                                            }
                                            IdExists = false;
                                        }
                                        else
                                        {
                                            using (StreamReader reader = new StreamReader(IdsDb))
                                            {
                                                // Check if ID already exists in database
                                                string line = "";
                                                while (!reader.EndOfStream)
                                                {
                                                    // Parse data line by line
                                                    line = reader.ReadLine();
                                                    string[] dataFound = line.Split(',');
                                                    // If ID already generated, generate a new one and check again
                                                    if (createdID == dataFound[0])
                                                    {
                                                        Console.WriteLine("Oops. Someone already has that ID");
                                                        IdExists = true;
                                                        break;
                                                    }
                                                    // ID generated is good to go, break out of loop
                                                    IdExists = false;
                                                }
                                            }

                                        }

                                        using (StreamWriter writer = new StreamWriter(IdsDb, true))
                                        {
                                            writer.WriteLine(string.Format("{0}", createdID));
                                        }
                                    }

                                    Console.WriteLine("Your unique username is " + createdID);
                                    Console.Write("Create your password: ");
                                    password = getSHA512Password(Console.ReadLine());

                                    // Create an admin's object with the given information
                                    Admin admin1 = new Admin(fullName, address, phone, birthday, createdID, password);

                                    using (StreamWriter writer = new StreamWriter(adminsDb, true))
                                    {
                                        if (!adminsDbExists)
                                        {
                                            writer.WriteLine("Name,Address,Phone,Birthday,Username,Password");
                                        }
                                        writer.WriteLine(string.Format("{0},{1},{2},{3},{4},{5}", admin1.fullName, admin1.address, admin1.phone, admin1.birthday, admin1.username, admin1.password));
                                    }
                                    Console.WriteLine();
                                    break;

                                case 0:
                                    break;

                                case 9:
                                    Console.WriteLine("Thank you for working with Royal Airlines!");
                                    Console.Write("Goodbye!\u263A");
                                    return;

                                default:
                                    Console.WriteLine("Invalid entry. Try again!");
                                    Console.WriteLine();
                                    break;
                            }

                        } while (adminOption != 0);

                        break;

                    case 9:
                        Console.WriteLine("Thank you for working with Royal Airlines!");
                        Console.Write("Goodbye!\u263A");
                        return;

                    default:
                        Console.WriteLine("Invalid entry. Try again!");
                        Console.WriteLine();
                        break;
                }
            } while (option != 9);
        }
    }
}


