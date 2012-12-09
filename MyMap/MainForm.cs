using System;
using System.Drawing;
using System.Windows.Forms;

namespace MyMap
{
    class MainForm : Form
    {
    
        public MainForm()
        {
            Graph graph = new Graph();
            RouteFinder rf = new RouteFinder();

            TextBox frombox, tobox;
            Label fromlabel, tolabel;
            Button wia, wiwtg, calcroute, mybike, mycar;
            CheckBox ov, walking, car;
            string WhatToDo;
            //wia:where i am, wiwtg:where i want to go, calcroute: calculate route
            // WhatToDo is variabelen die gebruikt moet worden als er op de kaart geklikt wordt om posities te plaatsen van fiets,auto, startpunt eindpunt.

            frombox = new TextBox();
            tobox = new TextBox();
            fromlabel = new Label();
            tolabel = new Label();
            wia= new Button();
            wiwtg= new Button();
            calcroute= new Button();
            ov = new CheckBox();
            car = new CheckBox();
            walking = new CheckBox();
            mybike = new Button();
            mycar = new Button();


            this.ClientSize = new Size(800, 600);
            this.MinimumSize = new Size(600, 500);
            this.BackColor = Color.WhiteSmoke;  

            frombox.Location = new Point(ClientSize.Width - 220, 20);
            frombox.Size = new Size(200, 30);
            frombox.Text = "";
            frombox.Anchor = (AnchorStyles.Right |AnchorStyles.Top);
            this.Controls.Add(frombox);

            fromlabel.Text = "Van:";
            fromlabel.Font = new Font("Microsoft Sans Serif", 10);
            fromlabel.Location = new Point(490, 20);
            fromlabel.Size = new Size(45, 20);
            fromlabel.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            this.Controls.Add(fromlabel);

            tolabel.Text = "Naar:";
            tolabel.Font = new Font("Microsoft Sans Serif", 10);
            tolabel.Location = new Point(490, 50);
            tolabel.Size = new Size(45, 20);
            tolabel.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            this.Controls.Add(tolabel);

            tobox.Location = new Point(ClientSize.Width - 220, 50);
            tobox.Size = new Size(200, 30);
            tobox.Text = "";
            tobox.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            this.Controls.Add(tobox);

            wia.Location = new Point(535, 20);
            wia.Size = new Size(40, 25);
            wia.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            wia.Click += (object o, EventArgs ea) => { WhatToDo = "startplace"; };
            wia.FlatStyle = FlatStyle.Flat;
            this.Controls.Add(wia);

            wiwtg.Location = new Point(535, 50);
            wiwtg.Size= new Size(40,25);
            wiwtg.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            wiwtg.Click += (object o, EventArgs ea) => { WhatToDo = "endplace"; };
            wiwtg.FlatStyle = FlatStyle.Flat;
            this.Controls.Add(wiwtg);

            calcroute.Location = new Point(580, 80);
            calcroute.Size = new Size(200, 25);
            calcroute.Text = "bereken route";
            calcroute.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            calcroute.FlatStyle = FlatStyle.Flat;
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
            this.Controls.Add(ov);

            car.Location = new Point(625, 110);
            car.Size = new Size(40, 40);
            car.Appearance = Appearance.Button;
            car.Text = "Car";
            car.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            car.FlatStyle = FlatStyle.Flat;
            car.FlatAppearance.CheckedBackColor = Color.FromArgb(224, 224, 224);
            this.Controls.Add(car);

            walking.Location = new Point(670, 110);
            walking.Size = new Size(40, 40);
            walking.Appearance = Appearance.Button;
            walking.Text = "walk";
            walking.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            walking.FlatStyle = FlatStyle.Flat;
            walking.FlatAppearance.CheckedBackColor = Color.FromArgb(224, 224, 224);
            this.Controls.Add(walking);

            mybike.Location = new Point(580, 155);
            mybike.Size = new Size(40, 40);
            mybike.Text= "my bike";
            mybike.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            mybike.FlatStyle = FlatStyle.Flat;
            mybike.Click += (object o, EventArgs ea) => { WhatToDo = "mybike"; };
            this.Controls.Add(mybike);

            mycar.Location= new Point(625,155);
            mycar.Size = new Size(40, 40);
            mycar.Text = "my car";
            mycar.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            mycar.FlatStyle = FlatStyle.Flat;
            mycar.Click += (object o, EventArgs ea) => { WhatToDo = "mycar"; };
            this.Controls.Add(mycar);

            // Dummy output, distance between nodes with id 1 and 2
            Console.WriteLine(rf.Dijkstra(graph, graph.GetNode(1),
                        graph.GetNode(2), new Vehicle()));

        }

    }
}
