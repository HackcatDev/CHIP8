using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LoggerForm
{
    public partial class VLogger : Form
    {
        public VLogger()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }
        public void AddLogRecord(String logRecord)
        {
            textBox1.Text += "[" + DateTime.Now.ToShortTimeString() + "] " + logRecord + "\r\n";
            textBox1.ScrollToCaret();
        }
    }
}
