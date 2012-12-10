using System;
using System.Windows.Forms;

namespace MyMap
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public enum Vehicle { Car, Bicycle, Foot, Bus, Metro, Train };

    /// <summary>
    /// Types that a Curve can be.
    /// documentation:  http://wiki.openstreetmap.org/wiki/Key:highway
    ///                 http://wiki.openstreetmap.org/wiki/Key:landuse
    /// </summary>
    public enum CurveType { 
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
