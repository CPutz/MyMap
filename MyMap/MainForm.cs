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


        private Label statLabel;
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
            this.ClientSize = new Size(800, 600);
            this.MinimumSize = new Size(815, 530);
            this.BackColor = Color.WhiteSmoke;
            this.Text = null;
            MainFormText();
            //this.DoubleBuffered = true;

            // Hide the form so it seems like it closes faster
            this.Closing += (sender, e) => {
                this.Hide();
            };

            ResourceManager resourcemanager
            = new ResourceManager("MyMap.Properties.Resources"
                                 , Assembly.GetExecutingAssembly());


            // Sends the scroll event to the map.
            this.MouseWheel += (object o, MouseEventArgs mea) => { map.OnMouseScroll(o, new MouseEventArgs(mea.Button, 
                                                                                                           mea.Clicks, 
                                                                                                           mea.X - map.Location.X, 
                                                                                                           mea.Y - map.Location.Y, 
                                                                                                           mea.Delta)); };


            #region UI Elements

            StreetSelectBox fromBox, toBox, viaBox;
            Label fromLabel, toLabel, viaLabel, instructionLabel, vervoersmiddelen;
            MapDragButton startButton, endButton, viaButton, myBike, myCar;
            GroupBox radioBox;
            RadioButton fastButton, shortButton;


            map = new MapDisplay(10, 30, 475, 475, loadingThread);
            map.Anchor = (AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom);
            this.Controls.Add(map);


            fromLabel = new Label();
            toLabel = new Label();
            viaLabel = new Label();
            //calcRouteButton = new Button();
            ptCheck = new CheckBox();
            carCheck = new CheckBox();
            bikeCheck = new CheckBox();
            instructionLabel = new Label();
            statLabel = new Label();
            vervoersmiddelen = new Label();
            radioBox = new GroupBox();
            fastButton = new RadioButton();
            shortButton = new RadioButton();

            startButton = new MapDragButton(map, (Bitmap)resourcemanager.GetObject("start"), true);
            endButton = new MapDragButton(map, (Bitmap)resourcemanager.GetObject("end"), true);
            viaButton = new MapDragButton(map, (Bitmap)resourcemanager.GetObject("via"), false);
            myBike = new MapDragButton(map, (Bitmap)resourcemanager.GetObject("bike"), false);
            myCar = new MapDragButton(map, (Bitmap)resourcemanager.GetObject("car"), false);

            fromBox = new StreetSelectBox(map, loadingThread, IconType.Start, startButton);
            toBox = new StreetSelectBox(map, loadingThread, IconType.End, endButton);
            viaBox = new StreetSelectBox(map, loadingThread, IconType.Via, viaButton);


            fromBox.Location = new Point(ClientSize.Width - 220, 20);
            fromBox.Size = new Size(200, 30);
            fromBox.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            this.Controls.Add(fromBox);

            viaBox.Location = new Point(ClientSize.Width - 220, 50);
            viaBox.Size = new Size(200, 30);
            viaBox.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            this.Controls.Add(viaBox); 

            toBox.Location = new Point(ClientSize.Width - 220, 80);
            toBox.Size = new Size(200, 30);
            toBox.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            this.Controls.Add(toBox);


            fromLabel.Text = "Van:";
            fromLabel.Font = new Font("Microsoft Sans Serif", 10);
            fromLabel.Location = new Point(490, 20);
            fromLabel.Size = new Size(45, 20);
            fromLabel.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            this.Controls.Add(fromLabel);

            viaLabel.Text = "Via:";
            viaLabel.Font = new Font("Microsoft Sans Serif", 10);
            viaLabel.Location = new Point(490, 50);
            viaLabel.Size = new Size(45, 20);
            viaLabel.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            this.Controls.Add(viaLabel);

            toLabel.Text = "Naar:";
            toLabel.Font = new Font("Microsoft Sans Serif", 10);
            toLabel.Location = new Point(490, 80);
            toLabel.Size = new Size(45, 20);
            toLabel.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            this.Controls.Add(toLabel);

            startButton.Location = new Point(535, 20);
            startButton.Size = new Size(40, 32);
            startButton.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            startButton.Click += (object o, EventArgs ea) => { map.BMode = ButtonMode.From; 
                                                              instructionLabel.Text = "plaats startpunt op gewenste plek op kaart door op de kaart te klikken"; };
            startButton.FlatStyle = FlatStyle.Flat;
            startButton.BackgroundImage = (Bitmap)resourcemanager.GetObject("start");
            startButton.FlatAppearance.BorderColor = backColor;
            startButton.FlatAppearance.MouseOverBackColor = backColor;
            startButton.FlatAppearance.MouseDownBackColor = backColor;
            this.Controls.Add(startButton);

            viaButton.Location = new Point(535, 50);
            viaButton.Size = new Size(40, 32);
            viaButton.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            viaButton.Click += (object o, EventArgs ea) => { map.BMode = ButtonMode.Via; 
                                                             instructionLabel.Text = "plaats via-bestemming op gewenste plek op kaart door op de kaart te klikken"; };
            viaButton.BackgroundImage = (Bitmap)resourcemanager.GetObject("via");
            viaButton.FlatStyle = FlatStyle.Flat;
            viaButton.FlatAppearance.BorderColor = backColor;
            viaButton.FlatAppearance.MouseOverBackColor = backColor;
            viaButton.FlatAppearance.MouseDownBackColor = backColor;
            this.Controls.Add(viaButton);

            endButton.Location = new Point(535, 80);
            endButton.Size = new Size(40, 32);
            endButton.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            endButton.Click += (object o, EventArgs ea) => { map.BMode = ButtonMode.To; 
                                                             instructionLabel.Text = "plaats eindbesteming op gewenste plek op kaart door op de kaart te klikken"; };
            endButton.BackgroundImage = (Bitmap)resourcemanager.GetObject("end");
            endButton.FlatStyle = FlatStyle.Flat;
            endButton.FlatAppearance.BorderColor = backColor;
            endButton.FlatAppearance.MouseOverBackColor = backColor;
            endButton.FlatAppearance.MouseDownBackColor = backColor;
            this.Controls.Add(endButton);


            /*calcRouteButton.Location = new Point(580, 80);
            calcRouteButton.Size = new Size(200, 25);
            calcRouteButton.Text = "bereken route";
            calcRouteButton.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            calcRouteButton.FlatStyle = FlatStyle.Flat;
            calcRouteButton.Click += (object o, EventArgs ea) => { bereken de Route;};
            calcRouteButton.BackColor = Color.FromArgb(230, 230, 230);
            this.Controls.Add(calcRouteButton);*/

            vervoersmiddelen.Location = new Point(490, 110);
            vervoersmiddelen.Text = "vervoersmiddelen:";
            vervoersmiddelen.Font = new Font("Microsoft Sans Serif", 10);
            vervoersmiddelen.Size = new Size(130, 32);
            vervoersmiddelen.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            this.Controls.Add(vervoersmiddelen);


            bikeCheck.Location = new Point(630, 110);
            bikeCheck.Size = new Size(32, 32);
            bikeCheck.Appearance = Appearance.Button;
            bikeCheck.BackgroundImage = (Bitmap)resourcemanager.GetObject("bike");
            bikeCheck.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            bikeCheck.FlatStyle = FlatStyle.Flat;
            bikeCheck.FlatAppearance.CheckedBackColor = Color.FromArgb(224, 224, 224);
            bikeCheck.Checked = true;
            bikeCheck.FlatAppearance.CheckedBackColor = Color.LightGreen;
            bikeCheck.BackColor = Color.Red;
            bikeCheck.CheckedChanged += (object o, EventArgs ea) => { map.UpdateRoute(); };
            this.Controls.Add(bikeCheck);

            carCheck.Location = new Point(675, 110);
            carCheck.Size = new Size(32, 32);
            carCheck.Appearance = Appearance.Button;
            carCheck.BackgroundImage = (Bitmap)resourcemanager.GetObject("car");
            carCheck.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            carCheck.FlatStyle = FlatStyle.Flat;
            carCheck.FlatAppearance.CheckedBackColor = Color.FromArgb(224, 224, 224);
            carCheck.Checked = true;
            carCheck.FlatAppearance.CheckedBackColor = Color.LightGreen;
            carCheck.BackColor = Color.Red;
            carCheck.CheckedChanged += (object o, EventArgs ea) => { map.UpdateRoute(); };
            this.Controls.Add(carCheck);

            ptCheck.Location = new Point(720, 110);
            ptCheck.Size = new Size(32, 32);
            ptCheck.Appearance = Appearance.Button;
            ptCheck.BackgroundImage = (Bitmap)resourcemanager.GetObject("ov");
            ptCheck.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            ptCheck.FlatStyle = FlatStyle.Flat;
            ptCheck.FlatAppearance.CheckedBackColor = Color.FromArgb(224, 224, 224);
            ptCheck.Checked = true;
            ptCheck.FlatAppearance.CheckedBackColor = Color.LightGreen;
            ptCheck.BackColor = Color.Red;
            ptCheck.CheckedChanged += (object o, EventArgs ea) => { map.UpdateRoute(); };
            this.Controls.Add(ptCheck);

            myBike.Location = new Point(630, 155);
            myBike.Size = new Size(32, 32);
            myBike.BackgroundImage = (Bitmap)resourcemanager.GetObject("bike");
            
            //myBike.Text = "my bike";
            myBike.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            myBike.FlatStyle = FlatStyle.Flat;
            myBike.Click += (object o, EventArgs ea) => { map.BMode = ButtonMode.NewBike; instructionLabel.Text = "plaats fiets op gewenste plek op kaart door op de kaart te klikken"; };
            myBike.FlatAppearance.BorderColor = backColor;
            myBike.FlatAppearance.MouseOverBackColor = backColor;
            myBike.FlatAppearance.MouseDownBackColor = backColor;
            this.Controls.Add(myBike);

            myCar.Location = new Point(675, 155);
            myCar.Size = new Size(32, 32);
            myCar.BackgroundImage = (Bitmap)resourcemanager.GetObject("car");
            //myCar.Text = "my car";
            myCar.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            myCar.FlatStyle = FlatStyle.Flat;
            myCar.Click += (object o, EventArgs ea) => { map.BMode = ButtonMode.NewCar; instructionLabel.Text = "plaats auto op gewenste plek op kaart door op de kaart te klikken"; };
            myCar.FlatAppearance.BorderColor = backColor;
            myCar.FlatAppearance.MouseOverBackColor = backColor;
            myCar.FlatAppearance.MouseDownBackColor = backColor;

            this.Controls.Add(myCar);

            statLabel.Location = new Point(535, 275);
            statLabel.Size = new Size(245, 100);
            statLabel.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            statLabel.Font = new Font("Microsoft Sans Serif", 11);
            this.Controls.Add(statLabel);

            instructionLabel.Location = new Point(535, 400);
            instructionLabel.Size = new Size(245, 100);
            instructionLabel.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            instructionLabel.Font = new Font("Microsoft Sans Serif", 11);
            this.Controls.Add(instructionLabel);

            radioBox.Location = new Point(535, 200);
            radioBox.Size = new Size(245, 65);
            radioBox.Text = "Options";
            radioBox.Anchor = (AnchorStyles.Right | AnchorStyles.Top);

            fastButton.Location = new Point(540, 215);
            fastButton.Size = new Size(67, 17);
            fastButton.Text = "Fastest route";
            fastButton.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            fastButton.Checked = true;
            fastButton.CheckedChanged += (object o, EventArgs ea) => { if (fastButton.Checked) { map.RouteMode = RouteMode.Fastest; } };
            this.Controls.Add(fastButton);

            shortButton.Location = new Point(540, 240);
            shortButton.Size = new Size(67, 17);
            shortButton.Text = "Shortest route";
            shortButton.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            shortButton.CheckedChanged += (object o, EventArgs ea) => { if (shortButton.Checked) { map.RouteMode = RouteMode.Shortest; } };
            this.Controls.Add(shortButton);

            this.Controls.Add(radioBox);

            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 10;
            timer.Tick += (object o, EventArgs ea) => { 
                if (loadingThread.Graph != null && User != -1) { GraphLoaded(loadingThread.Graph, new EventArgs()); timer.Dispose(); } };
            timer.Start();

            //AddMenu();
            this.GraphLoaded += (object o, EventArgs ea) => { Addvehicle(); };

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
        /// Sets the text of the route-statistics label.
        /// </summary>
        public void ChangeStats(double distance, double time)
        {
            string distUnit = "m";
            string timeUnit = "s";

            if (distance > 1000)
            {
                distance /= 1000;
                distUnit = "km";
                distance = Math.Round(distance, 1);
            }
            else
            {
                distance = Math.Round(distance, 0);
            }

            if (time > 60)
            {
                time /= 60;
                timeUnit = "min";
            }
            if (time > 60)
            {
                time /= 60;
                timeUnit = "h";
            }


            
            time = Math.Round(time, 0);

            statLabel.Text = "Distance: " + distance.ToString() + " " + distUnit + '\n' +
                             "Time: " + time.ToString() + " " + timeUnit;
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
            string[] woorden;
            char[] separators = { ',' };

            if (User != 0)
            {
                woorden = (UserData[User - 1].Split(separators, StringSplitOptions.RemoveEmptyEntries));

                this.Text = "Map " + woorden[1];
            }
            else
            {
                this.Text = "Map gast";
            }
        }

        public void Addvehicle()
        {
            if (User > 0)
            {
                string[] woorden;
                char[] separators = { ',' };

                if (UserData[User - 1] == null)
                    return;

                woorden = (UserData[User - 1].Split(separators, StringSplitOptions.RemoveEmptyEntries));

                if (woorden.Count() != 0)
                {
                    if (User == int.Parse(woorden[0]))
                    {
                        for (int n = 1; n <= ((woorden.Count() - 2) / 2) && woorden[n] != null; n++)
                        {
                            long x = long.Parse(woorden[2 * n + 1]);
                            Node location;
                            Vehicle vehicle;
                            location = loadingThread.Graph.GetNode(x);

                            switch (woorden[n * 2])
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

                            //map.MyVehicles.Add(new MyVehicle(vehicle, location));
                            map.AddVehicle(new MyVehicle(vehicle, location));
                        }
                    }
                }
            }
        }


        public void Save()
        {
            List<string> woorden;
            string Vehicles= null,naam;
            int my;
            char[] separators = { ',' };
            woorden = new  List<string>();
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
                    woorden.AddRange(UserData[n].Split(separators, StringSplitOptions.RemoveEmptyEntries));
                }
                catch
                {
                }
                if(woorden.Count>0)
                    if (int.Parse(woorden[0]) == User)
                    {
                        sw.WriteLine(woorden[0] +"," +woorden[1] + Vehicles);
                    }
                    else
                    {
                        sw.WriteLine(UserData[n]);
                    }
                    woorden.Clear();
            }
            sw.Close();
        }
    }
}
