using System;
namespace Airline_reservation_system
{
    class Admin
    {
        public string fullName { get; set; }
        public string address { get; set; }
        public string phone { get; set; }
        public string birthday { get; set; }
        public string username { get; private set; }
        public string password { get; private set; }

        public Admin(string fullName, string address, string phone, string birthday, string username, string password)
        {
            this.fullName = fullName;
            this.address = address;
            this.phone = phone;
            this.birthday = birthday;
            this.username = username;
            this.password = password;
        }
    }
}

