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

namespace MyMap
{
    public partial class StartForm : Form
    {
        Button[] users = new Button[6];
        int gebruikers = 0, t = 0, maxusers = 6;
        Button newuser = new Button();
        string[] gebuikergegevensstart = new string[5];
        public StartForm()
        {
            this.ClientSize = new Size(600, 500);
            this.Text ="start scherm";
            for (int q = 0; q < maxusers; q++)
                users[q] = new Button();
            

            newuser.Location= new Point(50,60*(gebruikers+2));
            newuser.Size = new Size(500, 50);
            newuser.Click += userevent;
            newuser.Text = "nieuwe gebruiker";
            newuser.Font= new Font("Microsoft Sans Serif", 16);
            newuser.Anchor = (AnchorStyles.Right | AnchorStyles.Top| AnchorStyles.Left);
            this.Controls.Add(newuser);
            users[t] = new Button();
            gebruikerknop();
            zoekgebruikers();         
        }
        void userevent(object o, EventArgs ea)
        {
            
            t = 0;
            string x= Interaction.InputBox("wat is je gebruikersnaam?", "wat is je gebruikersnaam?", "", 300, 300);

            if (x != "")
            {
                
                gebruikers++;
                users[gebruikers].Text = x;
                users[gebruikers].Visible = true;
                gebruikertoevoegen();
                newuser.Location = new Point(50, 60 * (gebruikers + 2));
                gebuikergegevensstart[gebruikers-1] =(gebruikers).ToString()+","+ x;
            }
            if (gebruikers >= maxusers-1)
            {
                
                newuser.Visible = false;
            }
           
        }
        public void gebruikerknop()
        {

            foreach (Button user in users)
            {

                users[0].Text = "standaard gebruiker";
                users[t].Location = new Point(50, 60 * (t + 1));
                users[t].Size = new Size(500, 50);
                users[t].FlatStyle = FlatStyle.Flat;
                users[t].Font = new Font("Microsoft Sans Serif", 16);
                users[t].Anchor = (AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Left);
                users[t].Text = "";
                if (t > gebruikers)
                    users[t].Visible = false;
                users[t].Click += clickeventopenprogram;
                this.Controls.Add(users[t]);
                t++;
            }

        }
        void clickeventopenprogram(object o, EventArgs ea)
        {

            MainForm p = new MainForm();
            p.Text = "Allstars Coders: map " + o.ToString().Remove(0,35);
            p.gebruikernr = gebruikers;
            p.gebuikergegevens = gebuikergegevensstart;
            
            p.Show(); 
            this.Hide() ;  
        }
        void gebruikertoevoegen()
        {
            for(int n=0; n<= gebruikers;n++)
                users[n].Visible=true;
        }
        void zoekgebruikers()
        {
            StreamReader sr = new StreamReader("gebruikers.txt");
            string[] woorden = new string[10];
            char[] separators = { ',' };
            int t = 0;
            string regel;
            while ((regel = sr.ReadLine()) != null)
            {

                t++;
                woorden = regel.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                users[int.Parse(woorden[0])].Text = woorden[1];
                users[int.Parse(woorden[0])].Visible = true;
                if (int.Parse(woorden[0]) >= t)
                {
                    gebuikergegevensstart[t-1] = regel;
                }
                if (int.Parse(woorden[0]) >= t)
                {
                    gebruikers++;
                    newuser.Location = new Point(50, 60 * (gebruikers + 2));
                    if (gebruikers >= maxusers - 1)
                    {
                        newuser.Visible = false;
                    }
                }
            }
            sr.Close();   
        }
    
    }
}
