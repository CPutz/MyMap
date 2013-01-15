﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace MyMap
{
    public class Curve : Edge
    {
        private long[] nodes;
        private CurveType type;
        private int maxSpeed;
        private string name;

        public Curve(long[] nodes, string name) : base(nodes[0], nodes[nodes.Length - 1])
        {
            this.nodes = nodes;
            this.name = name;
        }


        #region Properties

        public long this[int index]
        {
            get 
            {
                if (index >= 0 && index < nodes.Length)
                    return nodes[index];
                else
                    return 0;
            }
            set 
            {
                if (index >= 0 && index < nodes.Length)
                    nodes[index] = value;
            }
        }

        public int AmountOfNodes
        {
            get
            {
                return nodes.Length;
            }
        }

        public long[] Nodes
        {
            get
            {
                return nodes;
            }
        }

        public CurveType Type
        {
            get { return type; }
            set { type = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public int MaxSpeed
        {
            get { return maxSpeed; }
            set { maxSpeed = value; }
        }

        #endregion
    }

    /// <summary>
    /// Curve that represents a BusRoute so it has a route property.
    /// </summary>
    public class BusCurve : Curve
    {
        private Route route;

        public BusCurve(long[] nodes, string name) : base(nodes, name) { }

        public Route Route
        {
            set { route = value; }
            get { return route; }
        }
    }


    /// <summary>
    /// Types that a Curve can be.
    /// documentation:  http://wiki.openstreetmap.org/wiki/Key:highway
    ///                 http://wiki.openstreetmap.org/wiki/Key:landuse
    /// </summary>
    public static class CurveTypeExtentions
    {
        public static bool IsStreet(this CurveType curvetype)
        {
            return (curvetype < CurveType.EndOfStreets) || (curvetype == CurveType.UnTested);
        }

        private static bool AllAllowed(this CurveType curveType)
        {
            return curveType > CurveType.StartOfAll && curveType < CurveType.EndOfAll;
        }

        public static bool CarsAllowed(this CurveType curveType)
        {
            return AllAllowed(curveType) || curveType > CurveType.StartOfCar && curveType < CurveType.EndOfCar;
        }

        public static bool BicyclesAllowed(this CurveType curveType)
        {
            return AllAllowed(curveType) || curveType > CurveType.StartOfBicycle && curveType < CurveType.EndOfBicycle;
        }

        public static bool FootAllowed(this CurveType curveType)
        {
            return AllAllowed(curveType) || curveType > CurveType.StartOfFoot && curveType < CurveType.EndOfFoot;
        }
    }
    public enum CurveType
    {
        //streets
        StartOfAll,
        Living_street, //all
        Residential_street,//all
        Road, //all
        

        Tertiary, //car?
        Tertiary_link, //car?

        EndOfAll,

        StartOfCar,
        Motorway, //car
        Motorway_link, //car
        Trunk, //car
        Trunk_link, //car
        Primary, //car
        Primary_link, //car
        Secondary, //car
        Secondary_link, //car

        Unclassified, //all?

        EndOfCar,

        StartOfBicycle,
        StartOfFoot,
        
        Cycleway, //bicycle/foot
        Path, //foot/bicycle

        EndOfBicycle,

        Footway, //foot
        Pedestrian, //foot
        Steps, //foot

        Bus, //foot

        EndOfFoot,

        Bus_guideway, //bus

        Track, //none/all
        Service, //none?/car
        Raceway, //none
        Construction_street, //none
        Proposed, //none

        //divide between streeta and landuses
        EndOfStreets,

        //landuses
        Allotments,
        Basin,
        Brownfield,
        Cemetery,
        Commercial,
        Conservation,
        Construction_land,
        Farm,
        Farmland,
        Farmyard,
        Forest,
        Garages,
        Grass,
        Greenfield,
        Greenhouse_horticulture,
        Industrial,
        Landfill,
        Meadow,
        Military,
        Orchard,
        Plant_nursery,
        Quarry,
        Railway,
        Recreation_ground,
        Reservoir,
        Residential_land,
        Retail,
        Salt_pond,
        Village_green,
        Vineyard,
        Building,
        Water,


        UnTested
    }
}
