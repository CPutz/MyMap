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
    }
    public enum CurveType
    {
        //streets
        Motorway, //car
        Motorway_link, //car
        Trunk, //car
        Trunk_link, //car
        Primary, //car
        Primary_link, //car
        Secondary, //car
        Secondary_link, //car
        Tertiary, //car?
        Tertiary_link, //car?
        Living_street, //all
        Pedestrian, //foot
        Residential_street,//all
        Unclassified, //all?
        Service, //none?
        Track, //none/all
        Bus_guideway, //bus
        Raceway, //none
        Road, //all
        Cycleway, //bycicle
        Construction_street, //none
        Path, //foot/bycicle
        Footway, //foot
        Proposed, //none
        Steps, //foot

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
