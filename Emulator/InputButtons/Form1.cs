using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks; 
using System.Windows.Forms;
using System.Threading;

namespace InputButtons
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            OnButtonPress += __dgt;
            for (int i = 0; i < 16; i++) _btns_re_press.Add(i, false);
            _thread = new Thread(new ThreadStart(delegate
            {
                while (true)
                {
                    Thread.Sleep(pressAgainDelay);
                    for (int i = 0; i < 16; i++)
                    {
                        if (_btns_re_press[i]) BtnsPressed__[i] = 1;
                    }
                }
               
            }));
            //_thread.Start();
        }
        int pressAgainDelay = 50;
        private void __dgt(int id, bool f)
        {
            BtnsPressed__[id] = (f == true) ? 1 : 0;
            _btns_re_press[id] = false;
        }
        public int[] BtnsPressed__ = new int[16];
        public Dictionary<int, bool> _btns_re_press = new Dictionary<int, bool>();
        public int _lastCallKey
        {
            get
            {
                return -1;
            }
            set
            {
                BtnsPressed__[value] = 0;
                _btns_re_press[value] = true;
            }
        }
        public int[] BtnsPressed
        {
            get
            {
                int[] _result = new int[16];
                for (int i = 0; i < 16; i++)
                {
                    _result[i] = BtnsPressed__[i];
                }
                return _result;
            }
            set
            {

            }
        }
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
        Thread _thread;
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _thread.Abort();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            //return;
            if (!"0123456789ABCDEFabcdef".Contains(e.KeyCode.ToString().ToUpper())) return;
            OnButtonPress(Convert.ToInt32("0x" + e.KeyCode.ToString(), 16), true);
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            //return;
            if (!"0123456789ABCDEFabcdef".Contains(e.KeyCode.ToString().ToUpper())) return;
            OnButtonPress(Convert.ToInt32("0x" + e.KeyCode.ToString(), 16), false);
        }
    }
}
