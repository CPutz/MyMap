﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using System.IO;
using System.Resources;
using System.Reflection;
//using System.Threading;

namespace MyMap
{
    
    public class StartForm : Form
    {
        private int numOfUsers = 0, maxUsers = 6;
        
        private Button[] userButtons;
        private Button newUserButton;
        private MenuStrip menuStrip;
        private ToolStripMenuItem menu;
        private ToolStripMenuItem removeUserSubMenu;
        private ToolStripButton exitMenuButton;
        
        public string[] UserData = new string[5];
        public int Gebruiker = -1;

        public StartForm()
        {
            this.ClientSize = new Size(600, 500);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Text ="FlexiMaps";

            menuStrip = new MenuStrip();
            removeUserSubMenu = new ToolStripMenuItem("Remove User");

            menu = new ToolStripMenuItem("File");
            menuStrip.Items.Add(menu);

            exitMenuButton = new ToolStripButton("Exit");
            exitMenuButton.Click += (object o, EventArgs ea) => { this.Close(); };
            exitMenuButton.AutoSize = false;
            menu.DropDownItems.Add(exitMenuButton);
            
            this.Controls.Add(menuStrip);

            ResourceManager resourcemanager
            = new ResourceManager("MyMap.Properties.Resources"
                     , Assembly.GetExecutingAssembly());
            
            PictureBox flexilogo = new PictureBox();
            flexilogo.Image = new Bitmap((Image)resourcemanager.GetObject("logo"), 333, 100);
            flexilogo.Location = new Point(133,50);
            flexilogo.Size = flexilogo.PreferredSize;
            flexilogo.Anchor = (AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Left);
            this.Controls.Add(flexilogo);

            
            Label titel1 = new Label();
            titel1.Location = new Point(50, 25);
            titel1.Text = "Welkom bij";
            titel1.Font = new Font("Microsoft Sans Serif", 20);
            titel1.Size = new Size(500, 50);
            titel1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            titel1.Anchor = (AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Left);
            this.Controls.Add(titel1);

            /*Label titel2 = new Label();
            titel2.Location = new Point(50, 60);
            titel2.Text = "FlexiMaps";
            titel2.Font = new Font("Microsoft Sans Serif", 40);
            titel2.Size = new Size(500, 80);
            titel2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            titel2.Anchor = (AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Left);
            this.Controls.Add(titel2);*/

            userButtons = new Button[maxUsers];
            newUserButton = new Button();

            for (int q = 0; q < maxUsers; q++)
            {
                userButtons[q] = new Button();
            }

            newUserButton.Location = new Point(50, 100 +  60 * (numOfUsers + 2));
            newUserButton.Size = new Size(500, 50);
            newUserButton.Click += OnNewUser;
            newUserButton.Text = "New User";
            newUserButton.Font = new Font("Microsoft Sans Serif", 16);
            newUserButton.Anchor = (AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Left);
            this.Controls.Add(newUserButton);
            //userButtons[t] = new Button();

            gebruikerknop();
            zoekgebruikers();
            AddMenu();
            if (this.Height <= (200 + 60 * (numOfUsers + 2)))
            {
                this.Height = this.Height + 60;
            }
            // Hide the form so it seems like it closes faster
            this.Closing += (sender, e) => {
                this.Hide();
            };
        }
        
        #region Properties

        public int NumOfUsers
        {
            get { return numOfUsers; }
        }

        #endregion




        private void OnNewUser(object o, EventArgs ea)
        {


            string x = Interaction.InputBox("What is your username?", "New User", "", 300, 300);

            if (x != "" && x != "Guest User")
            {

                numOfUsers++;
                userButtons[numOfUsers].Text = x;
                userButtons[numOfUsers].Visible = true;
                newUserButton.Location = new Point(50,100+ 60 * (numOfUsers + 2));
                UserData[numOfUsers - 1] = (numOfUsers).ToString() + "," + x;
                AddMenu();
                if (this.Height <= (200+ 60 * (numOfUsers + 2))&& numOfUsers+1!=maxUsers)
                {
                    this.Height= this.Height +60;
                }

            }
            /*else
            {
                MessageBox.Show("Enter name:");
            }*/
            if (numOfUsers >= maxUsers - 1)
            {
                newUserButton.Visible = false;
            }
            Save();         
        }


        public void gebruikerknop()
        {
            int t = 0;
            userButtons[0].Text = "Guest User";
            foreach (Button userButton in userButtons)
            {
                //userButtons[0].Text = "standaard gebruiker";
                userButtons[t].Location = new Point(50, 100 +  60 * (t + 1));
                userButtons[t].Size = new Size(500, 50);
                userButtons[t].FlatStyle = FlatStyle.Flat;
                userButtons[t].Font = new Font("Microsoft Sans Serif", 16);
                userButtons[t].Anchor = (AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Left);
                if (t>0)
                userButtons[t].Text = "";
                if (t > numOfUsers)
                    userButtons[t].Visible = false;
                userButtons[t].Click += OnButtonClick;
                this.Controls.Add(userButtons[t]);
                t++;
            }

        }


        private void OnButtonClick(object o, EventArgs ea)
        {
            this.Hide();

            int n =0;

            foreach (Button b in userButtons)
            {

                if(b == o)
                {
                    Gebruiker = n;
                    break;
                }
                n++;
            }
            Console.WriteLine("Closing startform");
            this.Close();
        }


        private void zoekgebruikers()
        {
            try
            {
                StreamReader sr = new StreamReader("gebruikers.txt");
                string[] woorden = new string[50];
                char[] separators = { ',' };
                int t = 0;
                string regel;
                while ((regel = sr.ReadLine()) != null)
                {

                    t++;
                    woorden = regel.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                    userButtons[int.Parse(woorden[0])].Text = woorden[1];
                    userButtons[int.Parse(woorden[0])].Visible = true;
                    if (int.Parse(woorden[0]) >= t)
                    {
                        UserData[t - 1] = regel;
                    }
                    if (int.Parse(woorden[0]) >= t)
                    {
                        numOfUsers++;
                        newUserButton.Location = new Point(50, 100 +  60 * (numOfUsers+ 2));
                        if (numOfUsers>= maxUsers - 1)
                        {
                            newUserButton.Visible = false;
                        }
                    }
                }
                sr.Close();
            }
            catch
            {

            }
        }


        public void Save()
        {

            StreamWriter sw = new StreamWriter("gebruikers.txt");
            for (int n = 0; n < 5; n++)
            {

                {
                   sw.WriteLine(UserData[n]);
                }

            }
            sw.Close();
        }


        public void AddMenu()
        {
            removeUserSubMenu.Dispose();
            removeUserSubMenu = new ToolStripMenuItem("Remove User");
            bool areNewUsers = false;
            List<string> woorden = new List<string>();
            char[] separators = { ',' };
            

            foreach (string g in UserData)
            {
                try { woorden.AddRange(g.Split(separators, StringSplitOptions.RemoveEmptyEntries)); }
                catch { }
                    

                if (woorden.Count> 0)
                {
                    removeUserSubMenu.DropDownItems.Add(woorden[1], null, RemoveUser);
                    areNewUsers = true;
                    woorden.Clear();
                }
            }

            if (areNewUsers)
            {
                //menu.DropDownItems.Add(removeUserSubMenu);
                menu.DropDownItems.Insert(0, removeUserSubMenu);
            }
        }
    

        private void RemoveUser(object o, EventArgs ea)
        {       
            StreamWriter sw = new StreamWriter("gebruikers.txt");
            bool naverwijderen= false;
            int removedUser = -1,n=0;
            char[] separators = { ',' };
            string[] oldUserdata= new string[5];
            oldUserdata=UserData;
            List<string> woorden = new List<string>();
            foreach (string g in UserData)
            {
                woorden.Clear();
                try { woorden.AddRange(g.Split(separators, StringSplitOptions.RemoveEmptyEntries)); }
                catch { }

                if (g != null)
                {
                    if (woorden[1] == o.ToString())
                    {
                        naverwijderen = true;
                        removedUser = int.Parse(woorden[0]);
                    }
                    else
                    {
                        if (naverwijderen == false)
                        {
                            sw.WriteLine(g);
                            UserData[n] = oldUserdata[n];
                        }
                        else
                        {
                            sw.WriteLine((int.Parse(woorden[0]) - 1).ToString() + "," + g.Remove(0, 2));
                            UserData[n - 1] = (int.Parse(oldUserdata[n].Remove(1))-1).ToString() + "," + oldUserdata[n].Remove(0, 2);
                        }
                    }
                    n++;
                }

            }
            UserData[n - 1] = null;
            sw.Close();
            AddMenu();
            RefreshButton();
        }


        public void RefreshButton()
        {
            int n = 0;
            char[] separators = { ',' };
            List<string> woorden = new List<string>();
            numOfUsers--;
            foreach (Button b in userButtons)
            {
                if (b.Text == "Guest User")
                {
                    
                }
                else
                {
                    try { woorden.AddRange(UserData[n].Split(separators, StringSplitOptions.RemoveEmptyEntries)); }
                    catch { }
                    if (woorden.Count > 0)
                    {
                        b.Text = woorden[1];
                        n++;
                    }
                    woorden.Clear();
                }
            }
            userButtons[n+1].Visible = false;
            newUserButton.Location = new Point(50, 100 +  60 * (numOfUsers + 2));
            if (numOfUsers >= maxUsers - 2)
            {
                newUserButton.Visible = true;
            }
        }
    }
}
