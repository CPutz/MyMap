using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Collections.Generic;
using System.Linq;


namespace MyMap
{
    public enum ButtonMode { None, From, To, Via, NewBike, NewCar };

    public class MainForm : Form
    {
        public string[] UserData;
        public int User = -1;

        private MapDisplay map;
        private LoadingThread loadingThread;
        Color backColor= Color.FromArgb(255,Color.WhiteSmoke);

        private CheckBox ptCheck, carCheck, bikeCheck;

        // Fires ones when the graph is loaded.
        public event EventHandler GraphLoaded;

        public MainForm(string[] data, int user) : this(data, user,
                                                        new LoadingThread("input.osm.pbf")) {}

        public MainForm(string[] userData, int user, LoadingThread loadingThread)
        {
            this.UserData = userData;
            User = user;

            this.loadingThread = loadingThread;

            this.Initialize();
        }


        public void Initialize()
        {
            this.ClientSize = new Size(600, 600);
            this.MinimumSize = new Size(600, 530);
            this.BackColor = Color.WhiteSmoke;
            this.Text = null;
            ResourceManager resourcemanager
            = new ResourceManager("MyMap.Properties.Resources"
                     , Assembly.GetExecutingAssembly());
            this.Icon = (Icon)resourcemanager.GetObject("F_icon");
            MainFormText();
            //this.DoubleBuffered = true;

            // Hide the form so it seems like it closes faster
            this.Closing += (sender, e) => {
                this.Hide();
            };

            // Sends the scroll event to the map.
            this.MouseWheel += (object o, MouseEventArgs mea) =>
            {
                map.OnMouseScroll(o, new MouseEventArgs(mea.Button,
                                                        mea.Clicks,
                                                        mea.X - map.Location.X,
                                                        mea.Y - map.Location.Y,
                                                        mea.Delta));
            };

            #region UI Elements

            StreetSelectBox fromBox, toBox, viaBox;
            Label fromLabel, toLabel, viaLabel, checkLabel;
            MapDragButton startButton, endButton, viaButton, myBike, myCar;
            GroupBox radioBox;
            RadioButton fastButton, shortButton;
            ToolTip toolTipStart = new ToolTip(), 
                    toolTipEnd = new ToolTip(), 
                    toolTipVia = new ToolTip(), 
                    toolTipBike = new ToolTip(), 
                    toolTipCar = new ToolTip(), 
                    toolTipCheckBike = new ToolTip(), 
                    toolTipCheckCar = new ToolTip(), 
                    toolTipCheckPT = new ToolTip(),
                    toolTipStartBox = new ToolTip(),
                    toolTipViaBox = new ToolTip(),
                    toolTipEndBox = new ToolTip();
            

            map = new MapDisplay(10, 110, this.ClientSize.Width - 20, this.ClientSize.Height - 120, loadingThread);
            map.Anchor = (AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom);
            this.Controls.Add(map);


            fromLabel = new Label();
            toLabel = new Label();
            viaLabel = new Label();
            ptCheck = new CheckBox();
            carCheck = new CheckBox();
            bikeCheck = new CheckBox();
            checkLabel = new Label();
            radioBox = new GroupBox();
            fastButton = new RadioButton();
            shortButton = new RadioButton();

            startButton = new MapDragButton(map, (Bitmap)resourcemanager.GetObject("start"), ButtonMode.From, this, true);
            endButton = new MapDragButton(map, (Bitmap)resourcemanager.GetObject("end"), ButtonMode.To, this, true);
            viaButton = new MapDragButton(map, (Bitmap)resourcemanager.GetObject("via"), ButtonMode.Via, this, false);
            myBike = new MapDragButton(map, (Bitmap)resourcemanager.GetObject("bike"), ButtonMode.NewBike, this, false);
            myCar = new MapDragButton(map, (Bitmap)resourcemanager.GetObject("car"), ButtonMode.NewCar, this, false);

            fromBox = new StreetSelectBox(map, loadingThread, IconType.Start, startButton, this);
            toBox = new StreetSelectBox(map, loadingThread, IconType.End, endButton, this);
            viaBox = new StreetSelectBox(map, loadingThread, IconType.Via, viaButton, this);


            fromBox.Location = new Point(100, 8);
            fromBox.Size = new Size(200, 30);
            toolTipStartBox.SetToolTip(fromBox, "Search for streets, press Enter to place from icon.");
            this.Controls.Add(fromBox);

            viaBox.Location = new Point(100, 38);
            viaBox.Size = new Size(200, 30);
            toolTipStartBox.SetToolTip(fromBox, "Search for streets, press Enter to place via icon.");
            this.Controls.Add(viaBox);

            toBox.Location = new Point(100, 68);
            toBox.Size = new Size(200, 30);
            toolTipStartBox.SetToolTip(fromBox, "Search for streets, press Enter to place to icon.");
            this.Controls.Add(toBox);


            fromLabel.Text = "From:";
            fromLabel.Font = new Font("Microsoft Sans Serif", 10);
            fromLabel.Location = new Point(10, 8);
            fromLabel.Size = new Size(45, 20);
            this.Controls.Add(fromLabel);

            viaLabel.Text = "Via:";
            viaLabel.Font = new Font("Microsoft Sans Serif", 10);
            viaLabel.Location = new Point(10, 38);
            viaLabel.Size = new Size(45, 20);
            this.Controls.Add(viaLabel);

            toLabel.Text = "To:";
            toLabel.Font = new Font("Microsoft Sans Serif", 10);
            toLabel.Location = new Point(10, 68);
            toLabel.Size = new Size(45, 20);
            this.Controls.Add(toLabel);

            startButton.Location = new Point(55, 3);
            startButton.Size = new Size(40, 32);
            //startButton.Click += (object o, EventArgs ea) => { map.BMode = ButtonMode.From; };
            startButton.FlatStyle = FlatStyle.Flat;
            startButton.BackgroundImage = (Bitmap)resourcemanager.GetObject("start");
            startButton.FlatAppearance.BorderColor = backColor;
            startButton.FlatAppearance.MouseOverBackColor = backColor;
            startButton.FlatAppearance.MouseDownBackColor = backColor;
            toolTipStart.SetToolTip(startButton, "Drag icon to map to set your start location");
            this.Controls.Add(startButton);

            viaButton.Location = new Point(55, 33);
            viaButton.Size = new Size(40, 32);
            //viaButton.Click += (object o, EventArgs ea) => { map.BMode = ButtonMode.Via; };
            viaButton.BackgroundImage = (Bitmap)resourcemanager.GetObject("via");
            viaButton.FlatStyle = FlatStyle.Flat;
            viaButton.FlatAppearance.BorderColor = backColor;
            viaButton.FlatAppearance.MouseOverBackColor = backColor;
            viaButton.FlatAppearance.MouseDownBackColor = backColor;
            toolTipVia.SetToolTip(viaButton, "Drag icon to map to add a through location");
            this.Controls.Add(viaButton);

            endButton.Location = new Point(55, 63);
            endButton.Size = new Size(40, 32);
            //endButton.Click += (object o, EventArgs ea) => { map.BMode = ButtonMode.To;};
            endButton.BackgroundImage = (Bitmap)resourcemanager.GetObject("end");
            endButton.FlatStyle = FlatStyle.Flat;
            endButton.FlatAppearance.BorderColor = backColor;
            endButton.FlatAppearance.MouseOverBackColor = backColor;
            endButton.FlatAppearance.MouseDownBackColor = backColor;
            toolTipEnd.SetToolTip(endButton, "Drag icon to map to set your end location");
            this.Controls.Add(endButton);


            checkLabel.Location = new Point(309, 8);
            checkLabel.Text = "Enable/Disable";
            checkLabel.Font = new Font("Microsoft Sans Serif", 10);
            checkLabel.Size = new Size(130, 20);
            this.Controls.Add(checkLabel);


            bikeCheck.Location = new Point(309, 29);
            bikeCheck.Size = new Size(34, 34);
            bikeCheck.Appearance = Appearance.Button;
            bikeCheck.BackgroundImage = (Bitmap)resourcemanager.GetObject("bike_check");
            bikeCheck.FlatStyle = FlatStyle.Flat;
            bikeCheck.FlatAppearance.CheckedBackColor = Color.FromArgb(224, 224, 224);
            bikeCheck.Checked = true;
            bikeCheck.FlatAppearance.CheckedBackColor = Color.LightGreen;
            bikeCheck.BackColor = Color.Red;
            bikeCheck.CheckedChanged += (object o, EventArgs ea) => { map.UpdateRoute(); };
            toolTipCheckBike.SetToolTip(bikeCheck, "Disable Bicycles");
            bikeCheck.CheckedChanged += (object o, EventArgs ea) =>
            {
                toolTipCheckBike.RemoveAll();
                if (bikeCheck.Checked)
                    toolTipCheckBike.SetToolTip(bikeCheck, "Disable Bicycles");
                else
                    toolTipCheckBike.SetToolTip(bikeCheck, "Enable Bicycles");
            };
            this.Controls.Add(bikeCheck);

            carCheck.Location = new Point(354, 29);
            carCheck.Size = new Size(34, 34);
            carCheck.Appearance = Appearance.Button;
            carCheck.BackgroundImage = (Bitmap)resourcemanager.GetObject("car_check");
            carCheck.FlatStyle = FlatStyle.Flat;
            carCheck.FlatAppearance.CheckedBackColor = Color.FromArgb(224, 224, 224);
            carCheck.Checked = true;
            carCheck.FlatAppearance.CheckedBackColor = Color.LightGreen;
            carCheck.BackColor = Color.Red;
            carCheck.CheckedChanged += (object o, EventArgs ea) => { map.UpdateRoute(); };
            toolTipCheckCar.SetToolTip(carCheck, "Disable Cars");
            carCheck.CheckedChanged += (object o, EventArgs ea) =>
            {
                toolTipCheckCar.RemoveAll();
                if (carCheck.Checked)
                    toolTipCheckCar.SetToolTip(carCheck, "Disable Cars");
                else
                    toolTipCheckCar.SetToolTip(carCheck, "Enable Cars");
            };
            this.Controls.Add(carCheck);

            ptCheck.Location = new Point(399, 29);
            ptCheck.Size = new Size(34, 34);
            ptCheck.Appearance = Appearance.Button;
            ptCheck.BackgroundImage = (Bitmap)resourcemanager.GetObject("ov");
            ptCheck.FlatStyle = FlatStyle.Flat;
            ptCheck.FlatAppearance.CheckedBackColor = Color.FromArgb(224, 224, 224);
            ptCheck.Checked = true;
            ptCheck.FlatAppearance.CheckedBackColor = Color.LightGreen;
            ptCheck.BackColor = Color.Red;
            ptCheck.CheckedChanged += (object o, EventArgs ea) => { map.UpdateRoute(); };
            toolTipCheckPT.SetToolTip(ptCheck, "Disable Public Transport");
            ptCheck.CheckedChanged += (object o, EventArgs ea) =>
            {
                toolTipCheckPT.RemoveAll();
                if (ptCheck.Checked)
                    toolTipCheckPT.SetToolTip(ptCheck, "Disable Public Transport");
                else
                    toolTipCheckPT.SetToolTip(ptCheck, "Enable Public Transport");
            };
            this.Controls.Add(ptCheck);

            myBike.Location = new Point(310, 74);
            myBike.Size = new Size(32, 32);
            myBike.BackgroundImage = (Bitmap)resourcemanager.GetObject("bike");
            myBike.FlatStyle = FlatStyle.Flat;
            myBike.FlatAppearance.BorderColor = backColor;
            myBike.FlatAppearance.MouseOverBackColor = backColor;
            myBike.FlatAppearance.MouseDownBackColor = backColor;
            myBike.FlatAppearance.BorderSize = 0;
            toolTipBike.SetToolTip(myBike, "Drag icon to map to place a personal bycicle");
            this.Controls.Add(myBike);

            myCar.Location = new Point(355, 74);
            myCar.Size = new Size(32, 32);
            myCar.BackgroundImage = (Bitmap)resourcemanager.GetObject("car");
            myCar.FlatStyle = FlatStyle.Flat;
            myCar.FlatAppearance.BorderColor = backColor;
            myCar.FlatAppearance.MouseOverBackColor = backColor;
            myCar.FlatAppearance.MouseDownBackColor = backColor;
            myCar.FlatAppearance.BorderSize = 0;
            toolTipCar.SetToolTip(myCar, "Drag icon to map to place a personal car");
            this.Controls.Add(myCar);


            radioBox.Location = new Point(445, 8);
            radioBox.Size = new Size(80, 65);
            radioBox.Text = "Route Options";

            fastButton.Location = new Point(450, 23);
            fastButton.Size = new Size(67, 17);
            fastButton.Text = "Fastest";
            fastButton.Checked = true;
            fastButton.CheckedChanged += (object o, EventArgs ea) => { if (fastButton.Checked) { map.RouteMode = RouteMode.Fastest; } };
            this.Controls.Add(fastButton);

            shortButton.Location = new Point(450, 48);
            shortButton.Size = new Size(67, 17);
            shortButton.Text = "Shortest";
            shortButton.CheckedChanged += (object o, EventArgs ea) => { if (shortButton.Checked) { map.RouteMode = RouteMode.Shortest; } };
            this.Controls.Add(shortButton);

            this.Controls.Add(radioBox);

            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 10;
            timer.Tick += (object o, EventArgs ea) => { 
                if (loadingThread.Graph != null && User != -1) { GraphLoaded(loadingThread.Graph, new EventArgs()); timer.Dispose(); } };
            timer.Start();

            this.GraphLoaded += (object o, EventArgs ea) => { Addvehicle(); this.Save(); };


            #endregion
        }


        /// <summary>
        /// Changes the cursor to the bitmap "icon"
        /// </summary>
        public void ChangeCursor(Bitmap icon) {
            Cursor myCursor = new Cursor(icon.GetHicon());
            this.Cursor = myCursor;
        }

        /// <summary>
        /// Changes the cursor back to the default cursor
        /// </summary>
        public void ChangeCursorBack() {
            this.Cursor = null;
        }


        /// <summary>
        /// Returns True if the RouteFinder is allowed to use a Vehicle of type v.
        /// </summary>
        public bool VehicleAllowed(Vehicle v)
        {
            bool res = true;

            switch (v)
            {
                case Vehicle.Bicycle:
                    if (!bikeCheck.Checked)
                        res = false;
                    break;
                case Vehicle.Car:
                    if (!carCheck.Checked)
                        res = false;
                    break;
                case Vehicle.Bus:
                case Vehicle.Metro:
                case Vehicle.Train:
                    if (!ptCheck.Checked)
                        res = false;
                break;
            }

            return res;
        }


        public void MainFormText()
        {
            string[] words;
            char[] separators = { ',' };

            if (User != 0)
            {
                words = (UserData[User - 1].Split(separators, StringSplitOptions.RemoveEmptyEntries));

                this.Text = "FlexiMap " + words[1];
            }
            else
            {
                this.Text = "FlexiMap Guest";

                HelpForm help = new HelpForm();
                Application.Run(help);
            }
        }

        public void Addvehicle()
        {
            if (User > 0)
            {
                string[] words;
                char[] separators = { ',' };

                if (UserData[User - 1] == null)
                    return;

                words = (UserData[User - 1].Split(separators, StringSplitOptions.RemoveEmptyEntries));

                if (words.Count() != 0)
                {
                    if (User == int.Parse(words[0]))
                    {
                        for (int n = 1; n <= ((words.Count() - 2) / 2) && words[n] != null; n++)
                        {
                            long x = long.Parse(words[2 * n + 2]);
                            Node location;
                            Vehicle vehicle;
                            location = loadingThread.Graph.GetNode(x);

                            switch (words[n * 2 + 1])
                            {
                                case "Car":
                                    vehicle = Vehicle.Car;
                                    break;
                                case "Bicycle":
                                    vehicle = Vehicle.Bicycle;
                                    break;
                                default:
                                    vehicle = Vehicle.Car;
                                    break;

                            }

                            map.AddVehicle(new MyVehicle(vehicle, location));
                        }
                    }
                }
            }
        }


        public void Save()
        {
            List<string> words;
            string Vehicles= null,naam;
            int my;
            char[] separators = { ',' };
            words = new List<string>();
            my = map.MyVehicles.Count;
            foreach (MyVehicle p in map.MyVehicles)
            {
                Vehicles += ","+p.VehicleType.ToString() +","+ p.Location.ID.ToString();
            }
            StreamWriter sw = new StreamWriter("gebruikers.txt");
            
            for (int n = 0; n < 5; n++)
            {
                try
                {
                    words.AddRange(UserData[n].Split(separators, StringSplitOptions.RemoveEmptyEntries));
                }
                catch
                {
                }
                if (words.Count > 0)
                {
                    if (int.Parse(words[0]) == User)
                    {
                        sw.WriteLine(words[0] + "," + words[1] + ",0," + Vehicles);
                    }
                    else
                    {
                        sw.WriteLine(UserData[n]);
                    }
                    words.Clear();
                }
            }
            sw.Close();
        }
    }
}
