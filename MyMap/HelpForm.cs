using System;
using System.Drawing;
using System.Windows.Forms;
using System.Resources;
using System.Reflection;

namespace MyMap
{
    public class HelpForm : Form
    {
        Bitmap instruction1;
        Bitmap instruction2;
        Bitmap instruction3;
        Bitmap instruction4;

        int count = 1;

        PictureBox pictureBox;
        Button nextButton;
        Button prevButton;


        public HelpForm()
        {
            this.ClientSize = new Size(620, 690);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Text = "Instructions";

            ResourceManager resourcemanager
            = new ResourceManager("MyMap.Properties.Resources"
                     , Assembly.GetExecutingAssembly());

            instruction1 = new Bitmap((Image)resourcemanager.GetObject("instructions1"), 600, 600);
            instruction2 = new Bitmap((Image)resourcemanager.GetObject("instructions2"), 600, 600);
            instruction3 = new Bitmap((Image)resourcemanager.GetObject("instructions3"), 600, 600);
            instruction4 = new Bitmap((Image)resourcemanager.GetObject("instructions4"), 600, 600);

            pictureBox = new PictureBox();
            pictureBox.Size = new Size(600, 600);
            pictureBox.Location = new Point(10, 60);
            pictureBox.Image = instruction1;
            this.Controls.Add(pictureBox);

            Label label = new Label();
            label.Location = new Point(10, 8);
            label.Font = new Font("Microsoft Sans Serif", 14);
            label.Text = "Instructions: You can place a location or vehicle by dragging \nthe corresponding icon onto the map.";
            label.Size = label.PreferredSize;
            this.Controls.Add(label);

            nextButton = new Button();
            nextButton.Size = new Size(75, 23);
            nextButton.Location = new Point(535, 663);
            nextButton.Text = "Next";
            nextButton.Click += OnNextButtonPress;
            this.Controls.Add(nextButton);

            prevButton = new Button();
            prevButton.Size = new Size(75, 23);
            prevButton.Location = new Point(10, 663);
            prevButton.Text = "Previous";
            prevButton.Click += OnPreviousButtonPress;
            this.Controls.Add(prevButton);
        }


        private void OnNextButtonPress(object o, EventArgs ea)
        {
            count++;

            switch (count)
            {
                case 2:
                    this.pictureBox.Image = instruction2;
                    break;
                case 3:
                    this.pictureBox.Image = instruction3;
                    break;
                case 4:
                    this.pictureBox.Image = instruction4;
                    nextButton.Text = "Finish";
                    break;
                case 5:
                    this.Close();
                    break;
            }
            
            this.Invalidate();
        }


        private void OnPreviousButtonPress(object o, EventArgs ea)
        {
            if (count > 1)
            {
                count--;

                switch (count)
                {
                    case 1:
                        this.pictureBox.Image = instruction1;
                        break;
                    case 2:
                        this.pictureBox.Image = instruction2;
                        break;
                    case 3:
                        this.pictureBox.Image = instruction3;
                        nextButton.Text = "Next";
                        break;
                }

                this.Invalidate();
            }
        }
    }
}
