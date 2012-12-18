using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    public enum CurveType
    {
        //streets
        Motorway = 22,
        Motorway_link = 21,
        Trunk = 28,
        Trunk_link = 25,
        Primary = 17,
        Primary_link = 27,
        Secondary = 34,
        Secondary_link = 13,
        Tertiary = 15,
        Tertiary_link = 14,
        Living_street = 52,
        Pedestrian = 38,
        Residential_street = 41,
        Unclassified = 8,
        Service = 49,
        Track = 42,
        Bus_guideway = 7,
        Raceway = 16,
        Road = 35,

        //landuses
        Allotments = 4,
        Basin = 6,
        Brownfield = 11,
        Cemetery = 9,
        Commercial = 19,
        Conservation = 39,
        Construction = 40,
        Farm = 36,
        Farmland = 10,
        Farmyard = 30,
        Forest = 24,
        Garages = 47,
        Grass = 23,
        Greenfield = 33,
        Greenhouse_horticulture = 51,
        Industrial = 46,
        Landfill = 50,
        Meadow = 48,
        Military = 12,
        Orchard = 43,
        Plant_nursery = 26,
        Quarry = 45,
        Railway = 29,
        Recreation_ground = 20,
        Reservoir = 37,
        Residential_land = 31,
        Retail = 32,
        Salt_pond = 18,
        Village_green = 44,
        Vineyard = 5,
        Building,
        Water
    }
}
