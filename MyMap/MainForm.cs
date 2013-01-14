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
        private MapDisplay map;
        //private int gebruikernr;
        private string[] userData = new string[5];
        public int gebruikerNr;
        private LoadingThread loadingThread;
        private StartForm startForm;
        Color backColor= Color.FromArgb(255,Color.WhiteSmoke);
        private Label statLabel;

        private bool userPicked = false;

        // Fires ones when the graph is loaded.
        public event EventHandler GraphLoaded;

        public MainForm()
        {
            loadingThread = new LoadingThread("input.osm.pbf");

            startForm = new StartForm(this);
            startForm.Show();

            this.Initialize();
            this.HideForm();
            
        }


        public void Initialize()
        {
            this.ClientSize = new Size(800, 600);
            this.MinimumSize = new Size(815, 530);
            this.BackColor = Color.WhiteSmoke;
            this.Text = null;
            //this.DoubleBuffered = true;


            ResourceManager resourcemanager
            = new ResourceManager("MyMap.Properties.Resources"
                                 , Assembly.GetExecutingAssembly());


           

            //this.Text = "Allstars Coders: map " + o.ToString().Remove(0, 35);

            //this.gebruikernr = startForm.NumOfUsers;
            //this.gebuikergegevens = gebuikergegevensstart;
            this.FormClosing += (object sender, FormClosingEventArgs fcea) => { 
                startForm.Close(); };

            // Sends the scroll event to the map.
            this.MouseWheel += (object o, MouseEventArgs mea) => { map.OnMouseScroll(o, new MouseEventArgs(mea.Button, 
                                                                                                           mea.Clicks, 
                                                                                                           mea.X - map.Location.X, 
                                                                                                           mea.Y - map.Location.Y, 
                                                                                                           mea.Delta)); };


            #region UI Elements

            StreetSelectBox fromBox, toBox;
            Label fromLabel, toLabel, viaLabel, instructionLabel, vervoersmiddelen;
            MapDragButton startButton, endButton, viaButton, myBike, myCar;
            Button calcRouteButton;
            CheckBox ptCheck, carCheck, walkCheck;
            GroupBox radioBox;
            RadioButton fastButton, shortButton;


            map = new MapDisplay(10, 30, 475, 475, loadingThread);
            map.Anchor = (AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom);
            this.Controls.Add(map);


            fromBox = new StreetSelectBox(map, loadingThread, ButtonMode.From);
            toBox = new StreetSelectBox(map, loadingThread, ButtonMode.To);
            fromLabel = new Label();
            toLabel = new Label();
            viaLabel = new Label();
            calcRouteButton = new Button();
            ptCheck = new CheckBox();
            carCheck = new CheckBox();
            walkCheck = new CheckBox();
            instructionLabel = new Label();
            statLabel = new Label();
            vervoersmiddelen = new Label();
            radioBox = new GroupBox();
            fastButton = new RadioButton();
            shortButton = new RadioButton();


            startButton = new MapDragButton(map, (Bitmap)resourcemanager.GetObject("start"));
            endButton = new MapDragButton(map, (Bitmap)resourcemanager.GetObject("end"));
            viaButton = new MapDragButton(map, (Bitmap)resourcemanager.GetObject("via"));
            myBike = new MapDragButton(map, (Bitmap)resourcemanager.GetObject("bike"));
            myCar = new MapDragButton(map, (Bitmap)resourcemanager.GetObject("car"));
            


            fromBox.Location = new Point(ClientSize.Width - 220, 20);
            fromBox.Size = new Size(200, 30);
            fromBox.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            this.Controls.Add(fromBox);

            toBox.Location = new Point(ClientSize.Width - 220, 50);
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
            startButton.Click += (object o, EventArgs ea) => { map.BMode = ButtonMode.From; instructionLabel.Text = "plaats startpunt op gewenste plek op kaart door op de kaart te klikken"; startButton.BackgroundImage = null; };
            startButton.FlatStyle = FlatStyle.Flat;
            startButton.BackgroundImage = (Bitmap)resourcemanager.GetObject("start");
            startButton.FlatAppearance.BorderColor = backColor;
            startButton.FlatAppearance.MouseOverBackColor = backColor;
            startButton.FlatAppearance.MouseDownBackColor = backColor;
            this.Controls.Add(startButton);

            viaButton.Location = new Point(535, 50);
            viaButton.Size = new Size(40, 32);
            viaButton.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            viaButton.Click += (object o, EventArgs ea) => { map.BMode = ButtonMode.Via; instructionLabel.Text = "plaats via-bestemming op gewenste plek op kaart door op de kaart te klikken"; viaButton.BackgroundImage = null; };
            viaButton.BackgroundImage = (Bitmap)resourcemanager.GetObject("via");
            viaButton.FlatStyle = FlatStyle.Flat;
            viaButton.FlatAppearance.BorderColor = backColor;
            viaButton.FlatAppearance.MouseOverBackColor = backColor;
            viaButton.FlatAppearance.MouseDownBackColor = backColor;
            this.Controls.Add(viaButton);

            endButton.Location = new Point(535, 80);
            endButton.Size = new Size(40, 32);
            endButton.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            endButton.Click += (object o, EventArgs ea) => { map.BMode = ButtonMode.To; instructionLabel.Text = "plaats eindbesteming op gewenste plek op kaart door op de kaart te klikken"; endButton.BackgroundImage = null; };
            endButton.BackgroundImage = (Bitmap)resourcemanager.GetObject("end");
            endButton.FlatStyle = FlatStyle.Flat;
            endButton.FlatAppearance.BorderColor = backColor;
            endButton.FlatAppearance.MouseOverBackColor = backColor;
            endButton.FlatAppearance.MouseDownBackColor = backColor;
            this.Controls.Add(endButton);


            calcRouteButton.Location = new Point(580, 80);
            calcRouteButton.Size = new Size(200, 25);
            calcRouteButton.Text = "bereken route";
            calcRouteButton.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            calcRouteButton.FlatStyle = FlatStyle.Flat;
            calcRouteButton.Click += (object o, EventArgs ea) => { /*bereken de Route*/;};
            calcRouteButton.BackColor = Color.FromArgb(230, 230, 230);

            this.Controls.Add(calcRouteButton);

            vervoersmiddelen.Location = new Point(490, 110);
            vervoersmiddelen.Text = "vervoersmiddelen:";
            vervoersmiddelen.Font = new Font("Microsoft Sans Serif", 10);
            vervoersmiddelen.Size = new Size(130, 32);
            vervoersmiddelen.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            this.Controls.Add(vervoersmiddelen);


            //moeten afbeeldingen voor komen, ipv tekst.
            ptCheck.Location = new Point(630, 110);
            ptCheck.Size = new Size(32, 32);
            ptCheck.Appearance = Appearance.Button;
            ptCheck.BackgroundImage = (Bitmap)resourcemanager.GetObject("ov");
            //ptCheck.Text = "OV";
            ptCheck.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            ptCheck.FlatStyle = FlatStyle.Flat;
            ptCheck.FlatAppearance.CheckedBackColor = Color.FromArgb(224, 224, 224);
            ptCheck.Checked = true;
            ptCheck.FlatAppearance.CheckedBackColor = Color.LightGreen;
            
            ptCheck.BackColor = Color.Red;
            
            this.Controls.Add(ptCheck);

            carCheck.Location = new Point(675, 110);
            carCheck.Size = new Size(32, 32);
            carCheck.Appearance = Appearance.Button;
            carCheck.BackgroundImage = (Bitmap)resourcemanager.GetObject("car");
            //carCheck.Text = "Car";
            carCheck.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            carCheck.FlatStyle = FlatStyle.Flat;
            carCheck.FlatAppearance.CheckedBackColor = Color.FromArgb(224, 224, 224);
            carCheck.Checked = true;
            carCheck.FlatAppearance.CheckedBackColor = Color.LightGreen;
            carCheck.BackColor = Color.Red;
            this.Controls.Add(carCheck);

            walkCheck.Location = new Point(720, 110);
            walkCheck.Size = new Size(32, 32);
            walkCheck.Appearance = Appearance.Button;
            walkCheck.BackgroundImage = (Bitmap)resourcemanager.GetObject("walk");
            //walkCheck.Text = "walk";
            walkCheck.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            walkCheck.FlatStyle = FlatStyle.Flat;
            walkCheck.FlatAppearance.CheckedBackColor = Color.FromArgb(224, 224, 224);
            walkCheck.Checked = true;
            walkCheck.FlatAppearance.CheckedBackColor = Color.LightGreen;
            walkCheck.BackColor = Color.Red;
            this.Controls.Add(walkCheck);

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
                if (loadingThread.Graph != null && userPicked) { GraphLoaded(loadingThread.Graph, new EventArgs()); timer.Dispose(); } };
            timer.Start();

            AddMenu();
            this.GraphLoaded += (object o, EventArgs ea) => { Addvehicle(); };

            #endregion
        }

        
        public string[] UserData {
            set { userData = value; }
        }

        public bool UserPicked {
            set { userPicked = value; }
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


        public void ChangeStats(double distance, double time)
        {
            string distUnit = "m";
            string timeUnit = "s";

            if (distance > 1000)
            {
                distance /= 1000;
                distUnit = "km";
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


            distance = Math.Round(distance, 0);
            time = Math.Round(time, 0);

            statLabel.Text = "Distance: " + distance.ToString() + " " + distUnit + '\n' +
                             "Time: " + time.ToString() + " " + timeUnit;
        }


        public void ShowForm()
        {
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
        }


        public void HideForm()
        {
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
        }


        public void Addvehicle()
        {
            List<string> woorden = new List<string>();
            string [] woord= new string[100];
            char[] separators = { ',' };
            int v=0;
            foreach (string userinfo in userData)
            {
                woorden.Clear();
                try
                {
                    //dit moet makkelijker kunnen denk ik, dus meteen in een list zetten ipv eerst array en dan naar list, nu kan je maar 49 voertuigen toevoegen
                    woord = (userData[v].Split(separators, StringSplitOptions.RemoveEmptyEntries));
                    woorden = woord.ToList<string>();
                    v++;  
                }
                catch
                {
                }
                if (woorden.Count != 0)
                {
                    if (this.Text.Remove(0, 21) == woorden[1])
                    {
                    for (int n = 1; n <= ((woorden.Count - 2) / 2) && woorden[n] != null; n++)
                        {
                            long x = long.Parse(woorden[2 * n + 1]);
                            Node location;
                            Vehicle vehicle;
                            location = loadingThread.Graph.GetNode(x);

                            switch (woorden[n * 2 ])
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


        public void Save(object o, EventArgs ea)
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
                    woorden.AddRange( userData[n].Split(separators, StringSplitOptions.RemoveEmptyEntries));
                }
                catch
                {
                }
                if(woorden.Count>0)
                    if (int.Parse(woorden[0])== gebruikerNr)
                    {
                        sw.WriteLine(woorden[0] +"," +woorden[1] + Vehicles);
                    }
                    else
                    {
                        sw.WriteLine(userData[n]);
                    }
                    woorden.Clear();
            }
            sw.Close();
        }



        private void RemoveUser(object o, EventArgs ea)
        {       
            StreamWriter sw = new StreamWriter("gebruikers.txt");
            bool naverwijderen= false;
            for (int p = 0; p < 5; p++)
            {
                if (userData[p] != null)
                {
                    if (userData[p].Remove(0, 2) == o.ToString())
                    {
                        naverwijderen = true;
                    }
                    else
                    {
                        if (naverwijderen == false)
                        {
                            sw.WriteLine(userData[p]);
                        }
                        else
                        {
                            sw.WriteLine((int.Parse(userData[p].Remove(1)) - 1).ToString() + "," + userData[p].Remove(0, 2));
                        }
                    }
                }
            }
            sw.Close();
            
        }


        public void AddMenu()
        {
            bool areNewUsers = false;
            MenuStrip menuStrip = new MenuStrip();
            ToolStripDropDownItem menu = new ToolStripMenuItem("File");
            List<string> woorden = new List<string>();
            int n = 0;
            char[] separators = { ',' };

            
            menu.DropDownItems.Add("Save", null, this.Save);
            menu.DropDownItems.Add("verander gebruiker", null, this.VeranderGebruiker);
            
            ToolStripMenuItem verwijdersubmenu = new ToolStripMenuItem("verwijdergebuiker");


                foreach (string g in userData)
                {
                    try { woorden.AddRange(userData[n].Split(separators, StringSplitOptions.RemoveEmptyEntries)); }
                    catch { }
                    

                    if (woorden.Count> 0)
                    {
                        verwijdersubmenu.DropDownItems.Add(woorden[1], null, RemoveUser);
                        areNewUsers = true;
                        woorden.Clear();
                    }
                    n++;
                }

            if (areNewUsers)
            menu.DropDownItems.Add(verwijdersubmenu);

            menuStrip.Items.Add(menu);
            this.Controls.Add(menuStrip);
        }

        
        void VeranderGebruiker(object o, EventArgs ea)
        {
            //this.RefToStartForm.Show();

            startForm = new StartForm(this);
            startForm.Show();

            //startForm.Show();
            //allowClosing = false;
            this.HideForm();
        }
    }
}
