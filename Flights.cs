using System;
namespace Airline_reservation_system
{
    public class Flights
    {
        public string flightNumber;
        public string departureCity;
        public string arrivalCity;
        public string departureDate;
        public string departureTime;
        public string arrivalDate;
        public string arrivalTime;
        public int aircraft;
        public int aircraftCapacity;
        public int availableSeats;
        public double price;
        public double distance;


        public Flights(string flightNumber, string departureCity, string arrivalCity, string departureDate, string departureTime, string arrivalDate, string arrivalTime, double price, double distance)
        {
            this.flightNumber = flightNumber;
            this.departureCity = departureCity;
            this.arrivalCity = arrivalCity;
            this.departureDate = departureDate;
            this.departureTime = departureTime;
            this.arrivalDate = arrivalDate;
            this.arrivalTime = arrivalTime;
            this.price = price;
            this.distance = distance;
        }

        public void displayInfo()
        {
            Console.WriteLine("Flight \t From \t To \t Depart \t Arrive \t Aircraft \t Available Seats \t Price");
            Console.WriteLine("{0} \t {1} \t {2} \t {3} \t {4} \t {5} \t {6} \t {7} \t {8}", flightNumber, departureCity, arrivalCity, departureDate, departureTime, arrivalDate, arrivalTime, price, distance);
        }
    }
}

