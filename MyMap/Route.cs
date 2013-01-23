using System;
using System.Collections.Generic;

namespace MyMap
{
    /// <summary>
    /// A List of Nodes that represent a route.
    /// It holds a length, a time and which vehicles
    /// are used.
    /// </summary>
    public class Route
    {
        private Node[] route;
        private double length;
        private double time;

        //first int is vehicle type, second int is start node-index
        private SortedList<int, Vehicle> vehicles;


        public Route(Node[] nodes, Vehicle v)
        {
            this.route = nodes;
            this.vehicles = new SortedList<int, Vehicle>();
            vehicles.Add(0, v);
        }



        public Node this[int index] {
            get
            {
                if (index >= 0 && index < route.Length)
                    return route[index];
                else
                    return null;
                }
        }


        #region Properties

        public Node[] Points
        {
            get { return route; }
        }

        public double Length
        {
            get { return length; }
            set { length = value; }
        }

        public double Time
        {
            get { return time; }
            set { time = value; }
        }

        public int NumOfNodes
        {
            get { return route.Length; }
        }

        public int NumOfVehicles
        {
            get { return vehicles.Count; }
        }

        #endregion

        /// <summary>
        /// Returns the vehicle used between Node[index] and node[index + 1]
        /// </summary>
        public Vehicle GetVehicle(int index)
        {
            Vehicle v = vehicles.Values[0];

            if (index >= 0 && index < route.Length)
            {
                for (int i = 0; i < vehicles.Count; i++)
                {
                    if (vehicles.Keys[i] > index)
                    {
                        return vehicles.Values[i - 1];
                    }
                }
            }

            return vehicles.Values[vehicles.Count - 1];
        }

        /// <summary>
        /// Sets the vehicle used between Node[index] and the
        /// next Node that has a vehicle attached to it.
        /// </summary>
        public void SetVehicle(Vehicle v, int index)
        {
            if (index >= 0 && index < route.Length)
            {
                vehicles.Add(index, v);
            }
        }


        /// <summary>
        /// Operation to add two routes together where
        /// the second route is just put after the first route.
        /// </summary>
        public static Route operator +(Route A, Route B)
        {
            if (A == null)
                return B;
            else if (B == null)
                return A;
            else
            {
                Node[] nodes = new Node[A.NumOfNodes + B.NumOfNodes];
                A.Points.CopyTo(nodes, 0);
                B.Points.CopyTo(nodes, A.NumOfNodes);

                Route res = new Route(nodes, A.GetVehicle(0));
                res.Length = A.Length + B.length;
                res.Time = A.Time + B.Time;

                Vehicle prev = res.GetVehicle(0);
                Vehicle cur;

                for (int i = 1; i < A.NumOfNodes; i++)
                {
                    cur = A.GetVehicle(i);

                    if (prev != cur)
                        res.SetVehicle(cur, i);

                    prev = cur;
                }
                for (int i = 0; i < B.NumOfNodes; i++)
                {
                    cur = B.GetVehicle(i);

                    if (prev != cur)
                        res.SetVehicle(cur, i + A.NumOfNodes);

                    prev = cur;
                }

                return res;
            }
        }
    }
}
