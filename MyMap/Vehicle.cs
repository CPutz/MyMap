using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyMap
{

    public enum Vehicle { Car, Bicycle, Foot, Bus, Metro, Train };


    public class MyVehicle
    {
        private Vehicle vehicle;
        private Node location;
        private Route route;

        public MyVehicle(Vehicle vehicle, Node location)
        {
            this.vehicle = vehicle;
            this.location = location;
        }

        public Vehicle VehicleType
        {
            get { return vehicle; }
            set { VehicleType = value; }
        }

        public Node Location
        {
            get { return location; }
            set { location = value; }
        }

        public Route Route
        {
            get { return route; }
            set { route = value; }
        }
    }
}
