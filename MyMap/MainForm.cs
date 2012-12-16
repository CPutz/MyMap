using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace MyMap
{
    public enum ButtonMode { None, From, To, NewBike, NewCar };

    class MainForm : Form
    {
        private MapDisplay map;
        public int gebruikernr;
        public string[] gebuikergegevens = new string[5];


        


        public MainForm()
        {

            this.ClientSize = new Size(800, 600);
            this.MinimumSize = new Size(815, 530);
            this.BackColor = Color.WhiteSmoke;
            this.DoubleBuffered = true;
            //this.Text = "Allstars Coders: map";


            #region UI Elements

            TextBox fromBox, toBox;
            Label fromLabel, toLabel, instructionLabel;
            MapDragButton startButton, endButton, myBike, myCar;
            Button calcRouteButton;
            CheckBox ptCheck, carCheck, walkCheck;
            TopPanel topPanel;


            fromBox = new TextBox();
            toBox = new TextBox();
            fromLabel = new Label();
            toLabel = new Label();
            calcRouteButton = new Button();
            ptCheck = new CheckBox();
            carCheck = new CheckBox();
            walkCheck = new CheckBox();
            instructionLabel = new Label();


            map = new MapDisplay(10, 30, 475, 475);
            map.Anchor = (AnchorStyles.Left | AnchorStyles.Top);
            this.Controls.Add(map);

            topPanel = new TopPanel();

            startButton = new MapDragButton(map, topPanel);
            endButton = new MapDragButton(map, topPanel);
            myBike = new MapDragButton(map, topPanel);
            myCar = new MapDragButton(map, topPanel);
            
            topPanel.SetButtons(new MapDragButton[] { startButton, endButton, myBike, myCar });


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

            startButton.Location = new Point(535, 20);
            startButton.Size = new Size(40, 25);
            startButton.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            startButton.Click += (object o, EventArgs ea) => { map.BMode = ButtonMode.From; instructionLabel.Text = "plaats startpunt op gewenste plek op kaart door op de kaart te klikken"; };
            startButton.FlatStyle = FlatStyle.Flat;
            this.Controls.Add(startButton);

            endButton.Location = new Point(535, 50);
            endButton.Size = new Size(40, 25);
            endButton.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            endButton.Click += (object o, EventArgs ea) => { map.BMode = ButtonMode.To; instructionLabel.Text = "plaats eindbesteming op gewenste plek op kaart door op de kaart te klikken"; };
            endButton.FlatStyle = FlatStyle.Flat;
            this.Controls.Add(endButton);

            calcRouteButton.Location = new Point(580, 80);
            calcRouteButton.Size = new Size(200, 25);
            calcRouteButton.Text = "bereken route";
            calcRouteButton.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            calcRouteButton.FlatStyle = FlatStyle.Flat;
            calcRouteButton.Click += (object o, EventArgs ea) => { /*bereken de Route*/;};
            calcRouteButton.BackColor = Color.FromArgb(230, 230, 230);

            this.Controls.Add(calcRouteButton);

            //moeten afbeeldingen voor komen, ipv tekst.
            ptCheck.Location = new Point(580, 110);
            ptCheck.Size = new Size(40, 40);
            ptCheck.Appearance = Appearance.Button;
            ptCheck.Text = "OV";
            ptCheck.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            ptCheck.FlatStyle = FlatStyle.Flat;
            ptCheck.FlatAppearance.CheckedBackColor = Color.FromArgb(224, 224, 224);
            ptCheck.Checked = true;
            ptCheck.FlatAppearance.CheckedBackColor = Color.LightGreen;
            ptCheck.BackColor = Color.Red;
            this.Controls.Add(ptCheck);

            carCheck.Location = new Point(625, 110);
            carCheck.Size = new Size(40, 40);
            carCheck.Appearance = Appearance.Button;
            carCheck.Text = "Car";
            carCheck.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            carCheck.FlatStyle = FlatStyle.Flat;
            carCheck.FlatAppearance.CheckedBackColor = Color.FromArgb(224, 224, 224);
            carCheck.Checked = true;
            carCheck.FlatAppearance.CheckedBackColor = Color.LightGreen;
            carCheck.BackColor = Color.Red;
            this.Controls.Add(carCheck);

            walkCheck.Location = new Point(670, 110);
            walkCheck.Size = new Size(40, 40);
            walkCheck.Appearance = Appearance.Button;
            walkCheck.Text = "walk";
            walkCheck.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            walkCheck.FlatStyle = FlatStyle.Flat;
            walkCheck.FlatAppearance.CheckedBackColor = Color.FromArgb(224, 224, 224);
            walkCheck.Checked = true;
            walkCheck.FlatAppearance.CheckedBackColor = Color.LightGreen;
            walkCheck.BackColor = Color.Red;
            this.Controls.Add(walkCheck);

            myBike.Location = new Point(580, 155);
            myBike.Size = new Size(40, 40);
            myBike.Text = "my bike";
            myBike.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            myBike.FlatStyle = FlatStyle.Flat;
            myBike.Click += (object o, EventArgs ea) => { map.BMode = ButtonMode.NewBike; instructionLabel.Text = "plaats fiets op gewenste plek op kaart door op de kaart te klikken"; };
            this.Controls.Add(myBike);

            myCar.Location = new Point(625, 155);
            myCar.Size = new Size(40, 40);
            myCar.Text = "my car";
            myCar.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            myCar.FlatStyle = FlatStyle.Flat;
            myCar.Click += (object o, EventArgs ea) => { map.BMode = ButtonMode.NewCar; instructionLabel.Text = "plaats auto op gewenste plek op kaart door op de kaart te klikken"; };
            this.Controls.Add(myCar);

            instructionLabel.Location = new Point(535, 400);
            instructionLabel.Size = new Size(245, 100);
            //instructionLabel.Text = WhatToDo;
            instructionLabel.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            instructionLabel.Font = new Font("Microsoft Sans Serif", 11);
            this.Controls.Add(instructionLabel);

            topPanel.Location = new Point(0, 0);
            topPanel.Size = this.ClientSize;
            topPanel.BackColor = Color.Empty;
            this.Controls.Add(topPanel);
            topPanel.BringToFront();

            AddMenu();

            #endregion
        }


        private void Save(object o, EventArgs ea)
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



        private void RemoveUser(object o, EventArgs ea)
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


        private void AddMenu()
        {

            MenuStrip menuStrip = new MenuStrip();
            ToolStripDropDownItem menu = new ToolStripMenuItem("File");
            
            menu.DropDownItems.Add("Save", null, this.Save);
            try
            {
                ToolStripMenuItem verwijdersubmenu = new ToolStripMenuItem("verwijdergebuiker");
                StreamReader sr = new StreamReader("gebruikers.txt");

                foreach (string g in gebuikergegevens)
                {

                    string gebruiker = sr.ReadLine();
                    if (gebruiker != null)
                        verwijdersubmenu.DropDownItems.Add(gebruiker.Remove(0, 2), null, RemoveUser);
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

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Name = "MainForm";
            this.ResumeLayout(false);

        }
    }
}
