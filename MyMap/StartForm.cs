using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.VisualBasic;

namespace MyMap
{
    public partial class StartForm : Form
    {
        Button[] users = new Button[5];
        int gebruikers = 0, t = 0, maxusers = 5;
        Button newuser = new Button();
        public StartForm()
        {
            this.ClientSize = new Size(600, 400);
            this.Text ="start scherm";
            // naam wordt niet aan het form gegeven
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
            
            InitializeComponent();
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
                if (t > gebruikers)
                    users[t].Visible = false;
                //tietel wordt niet goed weergegeven bij elk window, users[gebruikers].text, geeft alleen dee laatste gebruikers naam weer is dus iets mee fout
                users[t].Click += (object o, EventArgs ea) => { MainForm p = new MainForm(); p.Show(); p.Text = "Allstars Coders: map " + users[gebruikers].Text;/*this.Close()*/ ; };
                this.Controls.Add(users[t]);
                t++;
            }

        }
        void gebruikertoevoegen()
        {
            for(int n=0; n< gebruikers;n++)
                users[n].Visible=true;
        }
    
    }
}
