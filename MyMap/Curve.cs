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
        Motorway,
        Motorway_link,
        Trunk,
        Trunk_link,
        Primary,
        Primary_link ,
        Secondary,
        Secondary_link,
        Tertiary,
        Tertiary_link,
        Living_street,
        Pedestrian,
        Residential_street,
        Unclassified,
        Service,
        Track,
        Bus_guideway,
        Raceway,
        Road,
        Cycleway,
        Construction_street,
        Path,
        Footway,

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
