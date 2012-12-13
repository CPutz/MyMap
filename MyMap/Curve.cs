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

        private Node[] nodes;
        private CurveType type;

        public Curve(Node[] nodes, string name) : base(nodes[0], nodes[nodes.Length - 1], name)
        {
            this.nodes = nodes;
        }


        public Node this[int index]
        {
            get 
            {
                if (index >= 0 && index < nodes.Length)
                    return nodes[index];
                else
                    return null;
            }
            set 
            {
                if (index >= 0 && index < nodes.Length)
                    nodes[index] = value;
            }
        }
        public int LengthOfNodes
        {
            get
            {
                return nodes.Length;
            }
        }
        public Node[] Nodes
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
    }

    /// <summary>
    /// Types that a Curve can be.
    /// documentation:  http://wiki.openstreetmap.org/wiki/Key:highway
    ///                 http://wiki.openstreetmap.org/wiki/Key:landuse
    /// </summary>
    public enum CurveType
    {
        //streets
        Motorway
        ,Motoway_link
        ,Trunk
        ,Trunk_link
        ,Primary
        ,Primary_link
        ,Secondary
        ,Secondary_link
        ,Tertairy
        ,Tertairy_link
        ,Living_street
        ,Pedestrian
        ,Residential_street
        ,Unclassified
        ,Service
        ,Track
        ,Bus_guideway
        ,Raceway
        ,Road

        //landuses
        ,Allotments
        ,Basin
        ,Brownfield
        ,Cemetery
        ,Commercial
        ,Conservation
        ,Construction
        ,Farm
        ,Farmland
        ,Farmyard
        ,Forest
        ,Garages
        ,Grass
        ,Greenfield
        ,Greenhouse_horticulture
        ,Industrial
        ,Landfill
        ,Meadow
        ,Military
        ,Orchard
        ,Plant_nursery
        ,Quarry
        ,Railway
        ,Recreation_ground
        ,Reservoir
        ,Residential_land
        ,Retail
        ,Salt_pond
        ,Village_green
        ,Vineyard
        ,Building
        ,Water
    }
}
