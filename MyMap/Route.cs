﻿using System;
using System.Collections.Generic;

namespace MyMap
{
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
        /// Sets the vehicle used between Node[index] and node[index + 1]
        /// </summary>
        public void SetVehicle(Vehicle v, int index)
        {
            if (index >= 0 && index < route.Length)
            {
                vehicles.Add(index, v);
            }
        }


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

                for (int i = 1; i < A.NumOfVehicles; i++)
                {
                    res.SetVehicle(A.GetVehicle(i), i);
                }
                for (int i = 0; i < B.NumOfVehicles; i++)
                {
                    res.SetVehicle(B.GetVehicle(i), A.NumOfNodes + i);
                }

                return res;
            }
        }
    }
}
