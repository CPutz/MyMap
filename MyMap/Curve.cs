using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace MyMap
{
    public class Curve : Edge
    {
        //hier komt iig ook nog het type van zo'n curve
        //zoals snelweg, voetpad, etc.

        private long[] nodes;
        private CurveType type;
        string name;

        public Curve(long[] nodes, string name) : base(nodes[0], nodes[nodes.Length - 1])
        {
            this.nodes = nodes;
        }


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
            return curvetype < CurveType.EndOfStreets;
        }

        private static bool AllAllowed(this CurveType curveType)
        {
            return curveType > CurveType.StartOfAll && curveType < CurveType.EndOfAll;
        }

        public static bool CarsAllowed(this CurveType curveType)
        {
            return AllAllowed(curveType) || curveType > CurveType.StartOfCar && curveType < CurveType.EndOfCar;
        }

        public static bool ByciclesAllowed(this CurveType curveType)
        {
            return AllAllowed(curveType) || curveType > CurveType.StartOfBycicle && curveType < CurveType.EndOfBycicle;
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
        Unclassified, //all?

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
        EndOfCar,

        StartOfBycicle,
        Cycleway, //bycicle
        
        StartOfFoot,

        Path, //foot/bycicle

        EndOfBycicle,

        Footway, //foot
        Pedestrian, //foot
        Steps, //foot
        EndOfFoot,

        Bus_guideway, //bus

        Track, //none/all
        Service, //none?/car
        Raceway, //none
        Construction_street, //none
        Proposed, //none

        //devision of street and landuse
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
        Water
    }
}
