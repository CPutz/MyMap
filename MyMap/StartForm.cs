using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using System.IO;
//using System.Threading;

namespace MyMap
{
    
    public class StartForm : Form
    {
        private int numOfUsers = 0, maxUsers = 6, t = 0;
        
        private Button[] userButtons;
        private Button newUserButton;
        
        public string[] UserData = new string[5];
        public int Gebruiker = -1;

        public StartForm()
        {
            this.ClientSize = new Size(600, 500);
            this.Text ="start scherm";

            userButtons = new Button[maxUsers];
            newUserButton = new Button();

            for (int q = 0; q < maxUsers; q++)
            {
                userButtons[q] = new Button();
            }

            newUserButton.Location = new Point(50, 60 * (numOfUsers + 2));
            newUserButton.Size = new Size(500, 50);
            newUserButton.Click += OnNewUser;
            newUserButton.Text = "nieuwe gebruiker";
            newUserButton.Font = new Font("Microsoft Sans Serif", 16);
            newUserButton.Anchor = (AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Left);
            this.Controls.Add(newUserButton);
            //userButtons[t] = new Button();
            gebruikerknop();
            zoekgebruikers();
            AddMenu();

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
            
            t = 0;
            string x = Interaction.InputBox("wat is je gebruikersnaam?", "wat is je gebruikersnaam?", "", 300, 300);

            if (x != "")
            {
                
                numOfUsers++;
                userButtons[numOfUsers].Text = x;
                userButtons[numOfUsers].Visible = true;
                newUserButton.Location = new Point(50, 60 * (numOfUsers + 2));
                UserData[numOfUsers - 1] = (numOfUsers).ToString() + "," + x;
                
            }
            if (numOfUsers >= maxUsers - 1)
            {
                newUserButton.Visible = false;
            }
            Save();
           
        }

        public void gebruikerknop()
        {

            foreach (Button userButton in userButtons)
            {
                userButtons[0].Text = "standaard gebruiker";
                userButtons[t].Location = new Point(50, 60 * (t + 1));
                userButtons[t].Size = new Size(500, 50);
                userButtons[t].FlatStyle = FlatStyle.Flat;
                userButtons[t].Font = new Font("Microsoft Sans Serif", 16);
                userButtons[t].Anchor = (AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Left);
                userButtons[t].Text = "";
                if (t > numOfUsers)
                    userButtons[t].Visible = false;
                //userButtons[t].Click += clickeventopenprogram;
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
                        newUserButton.Location = new Point(50, 60 * (numOfUsers+ 2));
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
            bool areNewUsers = false;
            MenuStrip menuStrip = new MenuStrip();
            ToolStripDropDownItem menu = new ToolStripMenuItem("File");
            List<string> woorden = new List<string>();
            int n = 0;
            char[] separators = { ',' };
            
            ToolStripMenuItem verwijdersubmenu = new ToolStripMenuItem("verwijder gebuiker");


                foreach (string g in UserData)
                {
                    try { woorden.AddRange(g.Split(separators, StringSplitOptions.RemoveEmptyEntries)); }
                    catch { }
                    

                    if (woorden.Count> 0)
                    {
                        verwijdersubmenu.DropDownItems.Add(woorden[1], null, RemoveUser);
                        areNewUsers = true;
                        woorden.Clear();
                    }
                }

            if (areNewUsers)
            {
                menu.DropDownItems.Add(verwijdersubmenu);

                menuStrip.Items.Add(menu);
                this.Controls.Add(menuStrip);
            }
        }
    
        private void RemoveUser(object o, EventArgs ea)
        {       
            StreamWriter sw = new StreamWriter("gebruikers.txt");
            bool naverwijderen= false;
            for (int p = 0; p < 5; p++)
            {
                if (UserData[p] != null)
                {
                    if (UserData[p].Remove(0, 2) == o.ToString())
                    {
                        naverwijderen = true;
                    }
                    else
                    {
                        if (naverwijderen == false)
                        {
                            sw.WriteLine(UserData[p]);
                        }
                        else
                        {
                            sw.WriteLine((int.Parse(UserData[p].Remove(1)) - 1).ToString() + "," + UserData[p].Remove(0, 2));
                        }
                    }
                }
            }
            sw.Close();
            
        }
    }
}
