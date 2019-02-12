using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CHIP8
{
    public partial class DecompilerWindow : Form
    {
        System.Timers.Timer timer = new System.Timers.Timer(1000);
        public String _decompilerListing = "";
        public int _line = -1;
        public DW_STATE _state = DW_STATE.DW_NOTHING;
        public DecompilerWindow()
        {
            InitializeComponent();
            timer.AutoReset = true;
            timer.Elapsed += delegate
            {
                if (_state == DW_STATE.DW_DRAWN || _state == DW_STATE.DW_NOTHING) return;
                richTextBox1.Text = _decompilerListing;
                if (_state == DW_STATE.DW_LINESEL && _line != -1)
                {
                    String[] _d = _decompilerListing.Split("\r\n".ToCharArray());
                    String _f = "";
                    for (int i = 0; i < _d.Length; i++)
                    {
                        if (i == _line)
                        {
                            richTextBox1.Select(_f.Length, _d[i].Length);
                            richTextBox1.SelectionBackColor = Color.Red;
                            _line = -1;
                            _state = DW_STATE.DW_DRAWN;
                            break;
                        }
                        _f += _d[i];
                    }
                }
                else
                {
                    richTextBox1.DeselectAll();
                    _state = DW_STATE.DW_NOTHING;
                }
            };
            timer.Start();
        }
        public bool __step_pressed = false;
        public enum DW_STATE
        {
            DW_DRAWN,
            DW_LINESEL,
            DW_NOTHING
        }
        private void button1_Click(object sender, EventArgs e)
        {
            __step_pressed = true;
        }
    }
}
