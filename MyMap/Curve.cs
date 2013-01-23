using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace MyMap
{
    /// <summary>
    /// A object that contains a list of nodes that can represent a street,
    /// piece of land or building, dependent on the CurveType of the Curve.
    /// It contains the name and maxSpeed of the Curve if available.
    /// </summary>
    public class Curve : Edge
    {
        private long[] nodes;
        private string curveName;

        public Curve(long[] nodes, string name) : base(nodes[0], nodes[nodes.Length - 1])
        {
            this.nodes = nodes;
            this.curveName = name;
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

        #endregion
    }


    public static class CurveTypeExtentions
    {
        public static bool IsStreet(this CurveType curvetype)
        {
            return (curvetype < CurveType.EndOfStreets) || (curvetype == CurveType.UnTested);
        }

        public static bool IsBuilding(this CurveType curvetype)
        {
            return (curvetype < CurveType.EndOfBuildings) && (curvetype > CurveType.StartOfBuildings);
        }

        // Return true if all kinds of vehicles are allowed on a Curve of type curveType.
        private static bool AllAllowed(this CurveType curveType)
        {
            return curveType > CurveType.StartOfAll && curveType < CurveType.EndOfAll;
        }

        public static bool CarsAllowed(this CurveType curveType)
        {
            return AllAllowed(curveType) ||
                (curveType > CurveType.StartOfCar &&
                 curveType < CurveType.EndOfCar);
        }

        public static bool BicyclesAllowed(this CurveType curveType)
        {
            return AllAllowed(curveType) ||
                (curveType > CurveType.StartOfBicycle &&
                    curveType < CurveType.EndOfBicycle) ||
                    curveType == CurveType.BusBicycle ||
                    curveType == CurveType.BusFootBicycle;
        }

        public static bool FootAllowed(this CurveType curveType)
        {
            return AllAllowed(curveType) ||
                (curveType > CurveType.StartOfFoot &&
                 curveType < CurveType.EndOfFoot);
        }

        public static bool BusAllowed(this CurveType curveType)
        {
            return AllAllowed(curveType) ||
                CarsAllowed(curveType) ||
                    (curveType > CurveType.StartOfBus &&
                     curveType < CurveType.EndOfBus);
        }
    }


    /// <summary>
    /// List of all types that a Curve can be.
    /// Documentation:  http://wiki.openstreetmap.org/wiki/Key:highway
    ///                 http://wiki.openstreetmap.org/wiki/Key:landuse
    /// 
    /// Excluding types that start with "StartOf" or "EndOf",
    /// which are saperations of different groups of types and therefore
    /// no types themselfs.
    /// </summary>
    public enum CurveType
    {
        //streets
        StartOfAll,
        CarBicycleFoot, //all
        Living_street, //all
        Residential_street,//all
        Road, //all
        Unclassified, //all
        Tertiary, //car
        Tertiary_link, //car
        Service, //all
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

        StartOfBicycle,

        CarBicycleNoFoot, //bicycle/foot

        EndOfCar,

        NoCarBicycleNoFoot, //bicycle

        StartOfFoot,
        
        Cycleway, //bicycle/foot
        Path, //foot/bicycle

        EndOfBicycle,

        Footway, //foot
        Pedestrian, //foot
        Steps, //foot
        Bus, //foot, because you can only enter a bus when you are on foot.
        BusWalkway, //foot, walkway to the bus
        AbstractBusRoute,
        PublicTransportPlatform,

        StartOfBus,

        BusFoot, //bus/foot
        BusFootBicycle, //bus/foot/bicycle Specifically allowed in BicyclesAllowed()

        EndOfFoot,

        Bus_guideway, //bus
        PublicServiceVehicles,
        BusStreetConnection,
        BusBicycle, //bus/bicycle Specifically allowed in BicyclesAllowed()

        EndOfBus,

        Track, //none/all
        Raceway, //none
        Construction_street, //none
        Proposed, //none
        Waterway, //none
	    NoneAllowed, //none

        //divides between streets and landuses
        EndOfStreets,

        //landuses
        StartOfBuildings,

        Building,
        Power,

        EndOfBuildings,

        Allotments,
        Basin,
        Brownfield,
        Canal,
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
        Parking,
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
        Water,


        UnTested
    }
}
