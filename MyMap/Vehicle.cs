using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyMap
{

    public enum Vehicle { Car, Bicycle, Foot, Bus, Metro, Train, All };

    /// <summary>
    /// An object that represent a personal vehicle.
    /// It has a position which is a Node and it has a VehicleType.
    /// Only two VehicleTypes that are recognized and possible
    /// to use in this program are Cars and Bicycles.
    /// </summary>
    public class MyVehicle
    {
        private Vehicle vehicle;
        private Node location;

        // route variable is used by the RouteFinder class
        // to store the route to the MyVehicle.
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
