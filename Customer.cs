using System;
namespace Airline_reservation_system
{
    class Customer
    {
        public string fullName { get; set; }
        public string address { get; set; }
        public string phone { get; set; }
        public string birthday { get; set; }
        public string creditCard { get; set; }
        public string username { get; private set; }
        public string password { get; private set; }


        public Customer(string fullName, string address, string phone, string birthday, string creditCard, string username, string password)
        {
            this.fullName = fullName;
            this.address = address;
            this.phone = phone;
            this.birthday = birthday;
            this.creditCard = creditCard;
            this.username = username;
            this.password = password;
        }
    }
}

