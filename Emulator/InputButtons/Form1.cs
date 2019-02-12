    using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks; 
using System.Windows.Forms;

namespace InputButtons
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            OnButtonPress += __dgt;
        }
        int pressAgainDelay = 500;
        private void __dgt(int id, bool f)
        {
            BtnsPressed[id] = (f == true) ? 1 : 0;
        }
        public int[] BtnsPressed = new int[16];
        //public int[] BtnsPressed
        //{
            //get
            //{
            //    return BtnsPrsd;
            //}
            //set
            //{

            //}
        //}

        public event OnBtnPress OnButtonPress;
        public delegate void OnBtnPress(int id,bool isDown);
        private void button1_Click(object sender, EventArgs e)
        {
            //OnButtonPress(Convert.ToInt32("0x" + ((Button)sender).Text, 16), false);
        }

        private void button13_MouseDown(object sender, MouseEventArgs e)
        {
            OnButtonPress(Convert.ToInt32("0x" + ((Button)sender).Text, 16), true);
        }

        private void button13_MouseUp(object sender, MouseEventArgs e)
        {
            OnButtonPress(Convert.ToInt32("0x" + ((Button)sender).Text, 16), false);
        }
    }
}
