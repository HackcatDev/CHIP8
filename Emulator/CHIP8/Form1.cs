using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using CHIP8CPU;

namespace CHIP8
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            telemetryUpdateTimer.AutoReset = true;
            telemetryUpdateTimer.Elapsed += delegate
            {
                textBox2.Text = c8cpu.cpuFreq;
                textBox3.Text = c8cpu.displayFps;
            };
            _dbg_cpu_communication.AutoReset = true;
            _dbg_cpu_communication.Elapsed += delegate { __call_cpu_dbg(); };
        }

        System.Timers.Timer telemetryUpdateTimer = new System.Timers.Timer(1000);

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = openFileDialog1.FileName;
            }
        }

        CPU c8cpu;// = new CPU();
        bool isRunning = false;
        System.Timers.Timer _dbg_cpu_communication = new System.Timers.Timer(1000);
        bool __dbg_has_drawn = false;
        private void __call_cpu_dbg()
        {
            if (checkBox2.Checked && c8cpu.paused && c8cpu.dbg_stepped) __dbg_has_drawn = false;
            if (this.checkBox2.Checked && c8cpu.paused && !__dbg_has_drawn)
            {
                _dw._decompilerListing = c8cpu.decompiledListing;
                _dw._line = c8cpu.dbg_curent_line;
                _dw._state = DecompilerWindow.DW_STATE.DW_LINESEL;
                __dbg_has_drawn = true;
            }
            c8cpu.isDebugging = _dw.__step_pressed;
        }
        public DecompilerWindow _dw = new DecompilerWindow();
        private void button2_Click(object sender, EventArgs e)
        {
            if (isRunning)
            {
                c8cpu.wasStopRequested = true;
                c8cpu.paused = false;
                telemetryUpdateTimer.Stop();
                button2.Enabled = false;
                while (!c8cpu.hasStopped) continue;
                button2.Text = "Запустить";
                button2.Enabled = true;
                isRunning = false;
                _dbg_cpu_communication.Stop();
                _dw.Close();
            }
            else
            {
                if (!String.IsNullOrWhiteSpace(textBox1.Text))
                {
                    c8cpu = new CPU();
                    c8cpu.Init();
                    c8cpu.InitWindows();
                    c8cpu.LoadGame(textBox1.Text);
                    c8cpu.showLog = checkBox1.Checked;
                    c8cpu.Display.ScaleX = Convert.ToInt32(numericUpDown2.Value);
                    c8cpu.Display.ScaleY = Convert.ToInt32(numericUpDown3.Value);
                    c8cpu.emuSpeed = Convert.ToInt32(numericUpDown1.Value);
                    Thread t = new Thread(new ThreadStart(c8cpu.MainLoop));
                    t.Start();
                    telemetryUpdateTimer.Start();
                    isRunning = true;
                    _dbg_cpu_communication.Start();
                    if (checkBox2.Checked)
                    {
                        Thread t__ = new Thread(new ThreadStart(delegate { _dw.ShowDialog(); }));
                        t__.Start();
                    }
                    button2.Text = "Остановить";
                }
                else
                {
                    MessageBox.Show("Не выбран образ игры!");
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (c8cpu.paused)
            {
                //CPU already paused
                c8cpu.logger.AddLogRecord("CPU has been resumed.");
                c8cpu.paused = false;
                button3.Text = "Приостановить";
            }
            else
            {
                //Pause the CPU
                c8cpu.paused = true;
                c8cpu.logger.AddLogRecord("CPU has been paused due to debug mode activated.");
                button3.Text = "Возобновить";
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (!c8cpu.paused)
            {
                MessageBox.Show("This feature is available only when debug mode is activated. Activate it by pausing virtual CPU", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            c8cpu.showRegDump = true;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isRunning)
            {
                c8cpu.wasStopRequested = true;
                c8cpu.paused = false;
                telemetryUpdateTimer.Stop();
                button2.Enabled = false;
                while (!c8cpu.hasStopped) continue;
                button2.Text = "Запустить";
                button2.Enabled = true;
                isRunning = false;
                _dbg_cpu_communication.Stop();
                _dw.Close();
            }
        }
    }
}
