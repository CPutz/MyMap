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
        private MainForm parentForm;
        private bool graphLoaded;

        
        public string[] gebuikergegevensstart = new string[5];

        public StartForm(MainForm parentForm)
        {
            this.parentForm = parentForm;

            this.ClientSize = new Size(600, 500);
            this.Text ="start scherm";
            this.parentForm.GraphLoaded += (object o, EventArgs ea) => { graphLoaded = true; };


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
        }
        
        #region Properties

        public int NumOfUsers
        {
            get { return numOfUsers; }
        }

        #endregion


        protected override void OnClosing(CancelEventArgs e)
        {
            //userButtons[0].PerformClick();
            //parentForm.Close();
            base.OnClosing(e);
        }


        private void OnNewUser(object o, EventArgs ea)
        {
            
            t = 0;
            string x = Interaction.InputBox("wat is je gebruikersnaam?", "wat is je gebruikersnaam?", "", 300, 300);

            if (x != "")
            {
                
                numOfUsers++;
                userButtons[numOfUsers].Text = x;
                userButtons[numOfUsers].Visible = true;
                gebruikertoevoegen();
                newUserButton.Location = new Point(50, 60 * (numOfUsers + 2));
                gebuikergegevensstart[numOfUsers - 1] = (numOfUsers).ToString() + "," + x;
                
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
            if (graphLoaded)
            {
                parentForm.UserData = gebuikergegevensstart;
                parentForm.Text = "Allstars Coders: map " + o.ToString().Remove(0, 35);
                parentForm.Addvehicle();
                parentForm.ShowForm();

                //this.Hide();
                this.Close();
            }
            else
            {
                MessageBox.Show("Graph isn't fully loaded.");
            }
        }

/*        private void clickeventopenprogram(object o, EventArgs ea)
        {
            //MainForm p = new MainForm(loadingThread);
            p.Text = "Allstars Coders: map " + o.ToString().Remove(0, 35);
            p.gebruikernr = numOfUsers;
            p.gebuikergegevens = gebuikergegevensstart;
            p.FormClosing += (object sender, FormClosingEventArgs fcea) =>
            {
                if (p.allowClosing == true)
                {
                    this.Close();
                }
            };
            p.Show();
            p.RefToStartForm = this;
            this.Hide();
        }*/

        private void gebruikertoevoegen()
        {
            for(int n=0; n<= numOfUsers;n++)
                userButtons[n].Visible=true;
            parentForm.UserData = gebuikergegevensstart;
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
                        gebuikergegevensstart[t - 1] = regel;
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
            parentForm.UserData = gebuikergegevensstart;
        }
        public void Save()
        {

            StreamWriter sw = new StreamWriter("gebruikers.txt");
            for (int n = 0; n < 5; n++)
            {

                {
                   sw.WriteLine(gebuikergegevensstart[n]);
                }

            }
            sw.Close();
        }
    
    }
}
