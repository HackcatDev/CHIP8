using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VIRTUALDISPLAY_CHIP8
{
    public partial class VDisplay : Form
    {
        public VDisplay()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }
        private byte[,] VM_VIDEO_MEM = new byte[128, 64];

        public void SetDisplayRam(byte[,] ram)
        {
            VM_VIDEO_MEM = ram;
        }
        public int ScaleX = 1;
        public int ScaleY = 1;
        public void Draw(bool[,] video)
        {
            Bitmap bmp = new Bitmap(64 * ScaleX, 32 * ScaleY);
            pictureBox1.Size = new Size(64 * ScaleX, 32 * ScaleY);
            for (int x = 0; x < 64; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    //We have a pixel
                    Color clr = Color.Black;
                    if (video[x, y] == true) clr = Color.White;
                    for (int k = 0; k < ScaleY; k++) for (int i = 0; i < ScaleX; i++) bmp.SetPixel(x * ScaleX + i, y * ScaleY + k, clr);
                }
            }
            pictureBox1.Image = bmp;
            return;
        }
    }
}
