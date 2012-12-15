using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace MyMap
{
    public enum ButtonMode { None, From, To, NewBike, NewCar };

    class MainForm : Form
    {
        public int gebruikernr;
        public string[] gebuikergegevens = new string[5];

        public MainForm()
        {

            #region UI Elements

            TextBox fromBox, toBox;
            Label fromLabel, toLabel, instructionLabel;
            Button wia, wiwtg, calcroute, mybike, mycar;
            CheckBox ov, walking, car;
            
            //wia:where i am, wiwtg:where i want to go, calcroute: calculate route
            // WhatToDo is variabelen die gebruikt moet worden als er op de kaart geklikt wordt om posities te plaatsen van fiets,auto, startpunt eindpunt. staat is mapDisplay
            
            fromBox = new TextBox();
            toBox = new TextBox();
            fromLabel = new Label();
            toLabel = new Label();
            wia= new Button();
            wiwtg= new Button();
            calcroute= new Button();
            ov = new CheckBox();
            car = new CheckBox();
            walking = new CheckBox();
            mybike = new Button();
            mycar = new Button();
            instructionLabel = new Label();




            this.ClientSize = new Size(800, 600);
            this.MinimumSize = new Size(815, 530);
            this.BackColor = Color.WhiteSmoke;
            //this.Text = "Allstars Coders: map";

            //MapDisplay map = new MapDisplay(10, 30, 475, 475);
            //map.Anchor = (AnchorStyles.Left | AnchorStyles.Top);
            //this.Controls.Add(map);

            fromBox.Location = new Point(ClientSize.Width - 220, 20);
            fromBox.Size = new Size(200, 30);
            fromBox.Text = "";
            fromBox.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            this.Controls.Add(fromBox);

            fromLabel.Text = "Van:";
            fromLabel.Font = new Font("Microsoft Sans Serif", 10);
            fromLabel.Location = new Point(490, 20);
            fromLabel.Size = new Size(45, 20);
            fromLabel.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            this.Controls.Add(fromLabel);

            toLabel.Text = "Naar:";
            toLabel.Font = new Font("Microsoft Sans Serif", 10);
            toLabel.Location = new Point(490, 50);
            toLabel.Size = new Size(45, 20);
            toLabel.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            this.Controls.Add(toLabel);

            toBox.Location = new Point(ClientSize.Width - 220, 50);
            toBox.Size = new Size(200, 30);
            toBox.Text = "";
            toBox.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            this.Controls.Add(toBox);

            wia.Location = new Point(535, 20);
            wia.Size = new Size(40, 25);
            wia.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            wia.Click += (object o, EventArgs ea) => { map.BMode = ButtonMode.From; instructionLabel.Text = "plaats startpunt op gewenste plek op kaart door op de kaart te klikken"; };
            wia.FlatStyle = FlatStyle.Flat;
            this.Controls.Add(wia);

            wiwtg.Location = new Point(535, 50);
            wiwtg.Size= new Size(40,25);
            wiwtg.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            wiwtg.Click += (object o, EventArgs ea) => { map.BMode = ButtonMode.To; instructionLabel.Text = "plaats eindbesteming op gewenste plek op kaart door op de kaart te klikken"; };
            wiwtg.FlatStyle = FlatStyle.Flat;
            this.Controls.Add(wiwtg);

            calcroute.Location = new Point(580, 80);
            calcroute.Size = new Size(200, 25);
            calcroute.Text = "bereken route";
            calcroute.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            calcroute.FlatStyle = FlatStyle.Flat;
            calcroute.Click += (object o, EventArgs ea) => { /*bereken de Route*/;};
            calcroute.BackColor = Color.FromArgb(230, 230, 230);
            
            this.Controls.Add(calcroute);

            //moeten afbeeldingen voor komen, ipv tekst.
            ov.Location = new Point(580, 110);
            ov.Size= new Size(40,40);
            ov.Appearance = Appearance.Button;
            ov.Text = "OV";
            ov.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            ov.FlatStyle = FlatStyle.Flat;
            ov.FlatAppearance.CheckedBackColor = Color.FromArgb(224, 224, 224);
            ov.Checked = true;
            ov.FlatAppearance.CheckedBackColor = Color.LightGreen;
            ov.BackColor = Color.Red;
            this.Controls.Add(ov);

            car.Location = new Point(625, 110);
            car.Size = new Size(40, 40);
            car.Appearance = Appearance.Button;
            car.Text = "Car";
            car.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            car.FlatStyle = FlatStyle.Flat;
            car.FlatAppearance.CheckedBackColor = Color.FromArgb(224, 224, 224);
            car.Checked = true;
            car.FlatAppearance.CheckedBackColor = Color.LightGreen;
            car.BackColor = Color.Red;
            this.Controls.Add(car);

            walking.Location = new Point(670, 110);
            walking.Size = new Size(40, 40);
            walking.Appearance = Appearance.Button;
            walking.Text = "walk";
            walking.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            walking.FlatStyle = FlatStyle.Flat;
            walking.FlatAppearance.CheckedBackColor = Color.FromArgb(224, 224, 224);
            walking.Checked = true;
            walking.FlatAppearance.CheckedBackColor = Color.LightGreen;
            walking.BackColor = Color.Red;
            this.Controls.Add(walking);

            mybike.Location = new Point(580, 155);
            mybike.Size = new Size(40, 40);
            mybike.Text= "my bike";
            mybike.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            mybike.FlatStyle = FlatStyle.Flat;
            mybike.Click += (object o, EventArgs ea) => { map.BMode = ButtonMode.NewBike; instructionLabel.Text = "plaats fiets op gewenste plek op kaart door op de kaart te klikken"; };
            this.Controls.Add(mybike);

            mycar.Location= new Point(625,155);
            mycar.Size = new Size(40, 40);
            mycar.Text = "my car";
            mycar.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            mycar.FlatStyle = FlatStyle.Flat;
            mycar.Click += (object o, EventArgs ea) => { map.BMode = ButtonMode.NewCar; instructionLabel.Text = "plaats auto op gewenste plek op kaart door op de kaart te klikken"; };
            this.Controls.Add(mycar);

            instructionLabel.Location = new Point(535, 400);
            instructionLabel.Size = new Size(245, 100);
            //instructionLabel.Text = WhatToDo;
            instructionLabel.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            instructionLabel.Font = new Font("Microsoft Sans Serif", 11);
            this.Controls.Add(instructionLabel);

            addmenu();

            #endregion

            // Dummy output, distance between nodes with id 1 and 2
            //Console.WriteLine(rf.Dijkstra(graph, graph.GetNode(1),
            //            graph.GetNode(2), new Vehicle()));

            
            


        }
        void save(object o, EventArgs ea)
        {
          
            StreamWriter sw = new StreamWriter("gebruikers.txt");
            for (int n = 0; n < 5; n++)
            {
                if (gebuikergegevens[n] == (n+1).ToString() + "," + this.Text.Remove(0, 21))
                {
                    
                    sw.WriteLine(gebuikergegevens[n]);
                }
                else
                {
                    try
                    {
                        if (gebruikernr == int.Parse(gebuikergegevens[n].Remove(1)))
                        {
                            sw.WriteLine((n).ToString() + "," + this.Text.Remove(0, 21));
                        }
                        else
                        {
                            sw.WriteLine(gebuikergegevens[n]);
                        }
                    }
                    catch
                    {

                    }
                }
            }
            sw.Close();
        }
        void verwijdergebruiker(object o, EventArgs ea)
        {       
            StreamWriter sw = new StreamWriter("gebruikers.txt");
            bool naverwijderen= false;
            for (int p = 0; p < 5; p++)
            {
                if (gebuikergegevens[p] != null)
                {
                    if (gebuikergegevens[p].Remove(0, 2) == o.ToString())
                    {
                        naverwijderen = true;
                    }
                    else
                    {
                        if (naverwijderen == false)
                        {
                            sw.WriteLine(gebuikergegevens[p]);
                        }
                        else
                        {
                            sw.WriteLine((int.Parse(gebuikergegevens[p].Remove(1)) - 1).ToString() + "," + gebuikergegevens[p].Remove(0, 2));
                        }
                    }
                }
            }
            sw.Close();
            
        }

        void addmenu()
        {

            MenuStrip menuStrip = new MenuStrip();
            ToolStripDropDownItem menu = new ToolStripMenuItem("File");
            
            menu.DropDownItems.Add("Save", null, this.save);
            try
            {
                ToolStripMenuItem verwijdersubmenu = new ToolStripMenuItem("verwijdergebuiker");
                StreamReader sr = new StreamReader("gebruikers.txt");

                foreach (string g in gebuikergegevens)
                {

                    string gebruiker = sr.ReadLine();
                    if (gebruiker != null)
                        verwijdersubmenu.DropDownItems.Add(gebruiker.Remove(0, 2), null, verwijdergebruiker);
                }

                
                menu.DropDownItems.Add(verwijdersubmenu);
                sr.Close();

                
            }
            catch
            {
            }
            menuStrip.Items.Add(menu);
            this.Controls.Add(menuStrip);
        }

    }
}
