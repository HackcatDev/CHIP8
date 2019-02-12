using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using VIRTUALDISPLAY_CHIP8;
using InputButtons;
using System.Timers;
using LoggerForm;
using CHIP8Decompiler;

namespace CHIP8CPU
{
    public class CPU
    {
        public byte[]                   VM_RAM = new byte[4096];        //4 kB of RAM 
        public byte[]                   VM_REGISTERS = new byte[16];    //16 registers V0-VF
        public ushort                   VM_REG_I = 0;                   //Register I has 16-bit size
        public ushort[]                 VM_STACK = new ushort[16];      //Stack originally has only 12 positions, but it's 16-pos now
        public byte                     VM_STACK_PTR = 0;               //Stack pointer
        public byte[,]                  VM_DISPLAY = new byte[64,32];  //VM's video memory
        public int                      VM_CODE_PTR = 0;                //Current code pointer
        public bool[,]                  VM_MONOCOLOR_DISPLAY = new bool[64,32];
        public ushort                   opcode = 0;
        public byte                     delay_timer = 0;
        public byte                     sound_timer = 0;
        public byte[]                   hp48_flags = new byte[8];
        public bool                     stop = false;
        public int                      mode = 0;
        public VDisplay                 Display = new VDisplay();
        public Form1                    Buttons = new Form1();
        public System.Timers.Timer      timers = new System.Timers.Timer(17);
        public bool                     wasStopRequested = false;
        public bool                     hasStopped = false;
        public int                      emuSpeed = 0;
        public bool                     showLog = false;
        public VLogger                  logger = new VLogger();
        public bool                     paused = false;
        public bool                     showRegDump = false;
        public System.Timers.Timer      updateDisplayTimer = new System.Timers.Timer(25); //40 fps
        public System.Timers.Timer      systemMeterTimer = new System.Timers.Timer(1000); //Frequency meter, fps meter, speed meter
        public int                      fps = 0; //True fps meter
        public int                      cpuOpcodesRan = 0;
        public String                   cpuFreq = "0 Hz";
        public String                   displayFps = "0 fps";
        public bool                     isDebugging = false;
        public String                   decompiledListing = "";
        public int                      dbg_curent_line = -1;
        public bool                     dbg_stepped = false;
        
        public void ShowAsync(object f)
        {
            System.Windows.Forms.Form fg = (System.Windows.Forms.Form)f;
            System.Windows.Forms.Application.Run(fg);
        }

        public void InitWindows()
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            //System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            Thread t1 = new Thread(new ParameterizedThreadStart(ShowAsync));
            t1.Start(Display);
            Thread t2 = new Thread(new ParameterizedThreadStart(ShowAsync));
            t2.Start(Buttons);
            Thread t3 = new Thread(new ParameterizedThreadStart(ShowAsync));
            t3.Start(logger);
        }

        public void Init()
        {
            VM_CODE_PTR = 0x200;
            VM_STACK_PTR = 0;
            VM_REG_I = 0;
            delay_timer = 0;
            sound_timer = 0;

            for (int i = 0; i < 4096; i++) VM_RAM[VM_REG_I] = 0; //Clear RAM

            //for (int y = 0; y < 64; y++) for (int x = 0; x < 128; x++) VM_DISPLAY[x,y] = 0;  //Clear display

            for (int i = 0; i < 16; i++) VM_REGISTERS[VM_REG_I] = 0; //Clear registers

            for (int i = 0; i < 16; i++) VM_STACK[VM_REG_I] = 0;  //Clear stack

            for (int i = 0; i < 32; i++) for (int y = 0; y < 64; y++) VM_MONOCOLOR_DISPLAY[y, i] = false;

            byte[] font = {
                0xF0, 0x90, 0x90, 0x90, 0xF0,   // 0
                0x20, 0x60, 0x20, 0x20, 0x70,   // 1
                0xF0, 0x10, 0xF0, 0x80, 0xF0,   // 2
                0xF0, 0x10, 0xF0, 0x10, 0xF0,   // 3
                0x90, 0x90, 0xF0, 0x10, 0x10,   // 4
                0xF0, 0x80, 0xF0, 0x10, 0xF0,   // 5
                0xF0, 0x80, 0xF0, 0x90, 0xF0,   // 6
                0xF0, 0x10, 0x20, 0x40, 0x40,   // 7
                0xF0, 0x90, 0xF0, 0x90, 0xF0,   // 8
                0xF0, 0x90, 0xF0, 0x10, 0xF0,   // 9
                0xF0, 0x90, 0xF0, 0x90, 0x90,   // A
                0xE0, 0x90, 0xE0, 0x90, 0xE0,   // B
                0xF0, 0x80, 0x80, 0x80, 0xF0,   // C
                0xE0, 0x90, 0x90, 0x90, 0xE0,   // D
                0xF0, 0x80, 0xF0, 0x80, 0xF0,   // E
                0xF0, 0x80, 0xF0, 0x80, 0x80    // F
            };

            //Load font to memory
            for (int i = 0; i < 80; i++) VM_RAM[VM_REG_I] = font[VM_REG_I];

            byte[] bigfont = {
                0xFF, 0xFF, 0xC3, 0xC3, 0xC3, 0xC3, 0xC3, 0xC3, 0xFF, 0xFF,     // 0
                0x18, 0x78, 0x78, 0x18, 0x18, 0x18, 0x18, 0x18, 0xFF, 0xFF,     // 1
                0xFF, 0xFF, 0x03, 0x03, 0xFF, 0xFF, 0xC0, 0xC0, 0xFF, 0xFF,     // 2
                0xFF, 0xFF, 0x03, 0x03, 0xFF, 0xFF, 0x03, 0x03, 0xFF, 0xFF,     // 3
                0xC3, 0xC3, 0xC3, 0xC3, 0xFF, 0xFF, 0x03, 0x03, 0x03, 0x03,     // 4
                0xFF, 0xFF, 0xC0, 0xC0, 0xFF, 0xFF, 0x03, 0x03, 0xFF, 0xFF,     // 5
                0xFF, 0xFF, 0xC0, 0xC0, 0xFF, 0xFF, 0xC3, 0xC3, 0xFF, 0xFF,     // 6
                0xFF, 0xFF, 0x03, 0x03, 0x06, 0x0C, 0x18, 0x18, 0x18, 0x18,     // 7
                0xFF, 0xFF, 0xC3, 0xC3, 0xFF, 0xFF, 0xC3, 0xC3, 0xFF, 0xFF,     // 8
                0xFF, 0xFF, 0xC3, 0xC3, 0xFF, 0xFF, 0x03, 0x03, 0xFF, 0xFF,     // 9
                0x7E, 0xFF, 0xC3, 0xC3, 0xC3, 0xFF, 0xFF, 0xC3, 0xC3, 0xC3,     // A
                0xFC, 0xFC, 0xC3, 0xC3, 0xFC, 0xFC, 0xC3, 0xC3, 0xFC, 0xFC,     // B
                0x3C, 0xFF, 0xC3, 0xC0, 0xC0, 0xC0, 0xC0, 0xC3, 0xFF, 0x3C,     // C
                0xFC, 0xFE, 0xC3, 0xC3, 0xC3, 0xC3, 0xC3, 0xC3, 0xFE, 0xFC,     // D
                0xFF, 0xFF, 0xC0, 0xC0, 0xFF, 0xFF, 0xC0, 0xC0, 0xFF, 0xFF,     // E
                0xFF, 0xFF, 0xC0, 0xC0, 0xFF, 0xFF, 0xC0, 0xC0, 0xC0, 0xC0      // F
            };

            // Load big font
            for (int i = 0; i < 160; i++) VM_RAM[VM_REG_I + 80] = bigfont[VM_REG_I];

            for (int i = 0; i < 8; i++) hp48_flags[VM_REG_I] = 0;

            mode = 0;
            stop = false;
            timers.AutoReset = true;
            updateDisplayTimer.AutoReset = true;
            systemMeterTimer.AutoReset = true;
            timers.Elapsed += delegate
            {
                DecreaseTimers();

            };
            updateDisplayTimer.Elapsed += delegate
            {
                Display.Draw(VM_MONOCOLOR_DISPLAY);
                fps++;
            };
            systemMeterTimer.Elapsed += delegate
            {
                displayFps = fps.ToString() + " fps";
                fps = 0;
                cpuFreq = GetHzReadable(cpuOpcodesRan);
                cpuOpcodesRan = 0;
                dbg_stepped = false;
            };
            timers.Start();
            updateDisplayTimer.Start();
            systemMeterTimer.Start();
        }
        
        public string GetHzReadable(long i)
        {
            // Get absolute value
            long absolute_i = (i < 0 ? -i : i);
            // Determine the suffix and readable value
            string suffix;
            double readable;
            if (absolute_i >= 0x40000000)
            {
                suffix = "GHz";
                readable = (i >> 20);
            }
            else if (absolute_i >= 0x100000)
            {
                suffix = "MHz";
                readable = (i >> 10);
            }
            else if (absolute_i >= 0x400)
            {
                suffix = "KHz";
                readable = i;
            }
            else
            {
                return i.ToString("0 Hz");
            }
            // Divide by 1024 to get fractional value
            readable = (readable / 1024);
            // Return formatted number with suffix
            return readable.ToString("0.### ") + suffix;
        }

        public void DrawSprite(byte X, byte Y, byte N)
        {
            return;
            //VM_REGISTERS[0xF] = 0;
            ////Only CHIP-8 mode is supported
            //if (N == 0) N = 16;
            //for (int yline = 0; yline < N; yline++)
            //{
            //    byte data = VM_RAM[VM_REG_I + yline];
            //    for (int xpix = 0; xpix < 8; xpix++)
            //    {
            //        if ((data & (0x80 >> xpix)) != 0)
            //        {
            //            if ((VM_REGISTERS[X] + xpix) < 64 && (VM_REGISTERS[Y] + yline) < 32 && (VM_REGISTERS[X] + xpix) >= 0 && (VM_REGISTERS[Y] + yline) >= 0)
            //            {
            //                if (VM_DISPLAY[(VM_REGISTERS[X] + xpix) * 2,(VM_REGISTERS[Y] + yline) * 2] == 1) VM_REGISTERS[0xF] = 1;
            //                VM_DISPLAY[(VM_REGISTERS[X] + xpix) * 2     ,  (VM_REGISTERS[Y] + yline) * 2    ] ^= 1;
            //                VM_DISPLAY[(VM_REGISTERS[X] + xpix) * 2 + 1 ,  (VM_REGISTERS[Y] + yline) * 2    ] ^= 1;
            //                VM_DISPLAY[(VM_REGISTERS[X] + xpix) * 2     ,  (VM_REGISTERS[Y] + yline) * 2 + 1] ^= 1;
            //                VM_DISPLAY[(VM_REGISTERS[X] + xpix) * 2 + 1 ,  (VM_REGISTERS[Y] + yline) * 2 + 1] ^= 1;
            //            }
            //        }
            //    }
            //}
            //Display.SetDisplayRam(VM_DISPLAY);
            //Display.Draw();
        }

        public void ExecuteDrwOpcode(ushort opcode)
        {
            byte registerX = (byte)((opcode & 0x0F00) >> 8);
            byte registerY = (byte)((opcode & 0x00F0) >> 4);

            byte x = VM_REGISTERS[registerX];
            byte y = VM_REGISTERS[registerY];

            byte n = (byte)(opcode & 0x000F);

            VM_REGISTERS[0xF] = 0x0;

            for (int height = 0; height < n; height++)
            {
                byte spriteData = VM_RAM[(ushort)(VM_REG_I + height)];

                for (int width = 0; width < 8; width++)
                {

                    if ((spriteData & (0x80 >> width)) != 0)
                    {
                        ushort drawX = (ushort)((x + width) % 64);
                        ushort drawY = (ushort)((y + height) % 32);

                        if (VM_MONOCOLOR_DISPLAY[drawX, drawY])
                        {
                            VM_REGISTERS[0xF] = 1;
                        }

                        VM_MONOCOLOR_DISPLAY[drawX, drawY] ^= true;
                    }
                }
            }
            //Display.Draw(VM_MONOCOLOR_DISPLAY);
        }

        public void ExecuteNextOpcode()
        {
            if (wasStopRequested)
            {
                timers.Stop();
                updateDisplayTimer.Stop();
                systemMeterTimer.Stop();
                Display.Close();
                Buttons.Close();
                logger.Close();
                VM_CODE_PTR = 0;
                hasStopped = true;
                logger.AddLogRecord("Stop signal caught. CPU has been stopped.");
                return;
            }
            if (showLog)
            {
                logger.Show();
            } else { logger.Hide(); }
            if (paused)
            {
                System.IO.File.WriteAllBytes("_tmp_dc.lis", VM_RAM);
                decompiledListing = Decompiler.Decompile("_tmp_dc.lis");
                System.IO.File.Delete("_tmp_dc.lis");
                dbg_curent_line = (VM_CODE_PTR - 0x200) / 2;
            }
            while (paused)
            {
                if (showRegDump)
                {
                    showRegDump = false;
                    String dump = "V[0] = " + VM_REGISTERS[0].ToString("X2") + "\r\n";
                    for (int i = 1; i < 16; i++)
                    {
                        dump += "        " + "V[" + i.ToString("X") + "] = " + VM_REGISTERS[i].ToString("X2") + "\r\n";
                    }
                    dump += "        " + "I = " + VM_REG_I.ToString("X2");
                    logger.AddLogRecord(dump);
                }
                if (isDebugging)
                {
                    isDebugging = false;
                    break;
                }
                continue;
            }
            dbg_stepped = true;
            opcode = Convert.ToUInt16((VM_RAM[VM_CODE_PTR] << 8) + VM_RAM[VM_CODE_PTR + 1]);
            VM_CODE_PTR += 2;
            cpuOpcodesRan++;
            if (emuSpeed > 0) Thread.Sleep(emuSpeed);
            switch ((opcode & 0xF000) >> 12)
            {
                case 0x0:
                    switch (opcode & 0x00FF)
                    {
                        case 0xE0:              // 00E0 - clear the screen
                            for (int y = 0; y < 32; y++)
                                for (int x = 0; x < 64; x++)
                                    VM_MONOCOLOR_DISPLAY[x,y] = false;
                            break;

                        case 0xEE:              // 00EE - return from subroutine
                            VM_CODE_PTR = VM_STACK[--VM_STACK_PTR];
                            break;

                        case 0xFD:              // 00FD - Quit the emulator
                            stop = true;
                            wasStopRequested = true;
                            break;
                            
                        default:
                            logger.AddLogRecord("Unknown opcode: " + opcode.ToString("X2"));
                            break;
                    }
                    break;

                case 0x1:                               // 1NNN - jump to addr
                    VM_CODE_PTR = opcode & 0x0FFF;
                    break;

                case 0x2:                               // 2NNN - call subroutine
                    VM_STACK[VM_STACK_PTR++] = (ushort)VM_CODE_PTR;
                    VM_CODE_PTR = opcode & 0x0FFF;
                    break;

                case 0x3:                               // 3XKK - skip next instruction if VX == Byte
                    if (VM_REGISTERS[((opcode & 0x0F00) >> 8)] == (opcode & 0x00FF)) VM_CODE_PTR += 2;
                    break;

                case 0x4:                               // 4XKK - skip next instruction if VX != Byte
                    if (VM_REGISTERS[((opcode & 0x0F00) >> 8)] != (opcode & 0x00FF))VM_CODE_PTR+= 2;
                    break;

                case 0x5:                               // 5XY0 - skip next instruction if VX == VY
                    if (VM_REGISTERS[((opcode & 0x0F00) >> 8)] == VM_REGISTERS[((opcode & 0x00F0) >> 4)])VM_CODE_PTR+= 2;
                    break;

                case 0x6:                               // 6XKK - set VX = Byte
                    VM_REGISTERS[((opcode & 0x0F00) >> 8)] = (byte)(opcode & 0x00FF);
                    break;

                case 0x7:                               // 7XKK - set VX = VX + Byte
                    VM_REGISTERS[((opcode & 0x0F00) >> 8)] += (byte)(opcode & 0x00FF);
                    break;

                case 0x8:
                    switch (opcode & 0x000F)
                    {
                        case 0x0:               // 8XY0 - set VX = VY
                            VM_REGISTERS[((opcode & 0x0F00) >> 8)] = VM_REGISTERS[((opcode & 0x00F0) >> 4)];

                            break;

                        case 0x1:               // 8XY1 - set VX = VX | VY
                            VM_REGISTERS[((opcode & 0x0F00) >> 8)] |= VM_REGISTERS[((opcode & 0x00F0) >> 4)];
                            break;

                        case 0x2:               // 8XY2 - set VX = VX & VY
                            VM_REGISTERS[((opcode & 0x0F00) >> 8)] &= VM_REGISTERS[((opcode & 0x00F0) >> 4)];
                            break;

                        case 0x3:               // 8XY3 - set VX = VX ^ VY
                            VM_REGISTERS[((opcode & 0x0F00) >> 8)] ^= VM_REGISTERS[((opcode & 0x00F0) >> 4)];
                            break;

                        case 0x4:               // 8XY4 - set VX = VX + VY, VF = carry
                            int i;
                            i = (int)(VM_REGISTERS[((opcode & 0x0F00) >> 8)]) + (int)(VM_REGISTERS[((opcode & 0x00F0) >> 4)]);

                            if (i > 255)
                                VM_REGISTERS[0xF] = 1;
                            else
                                VM_REGISTERS[0xF] = 0;

                            VM_REGISTERS[((opcode & 0x0F00) >> 8)] = (byte)i;
                            break;

                        case 0x5:               // 8XY5 - set VX = VX - VY, VF = !borrow
                            if (VM_REGISTERS[((opcode & 0x0F00) >> 8)] >= VM_REGISTERS[((opcode & 0x00F0) >> 4)])
                                VM_REGISTERS[0xF] = 1;
                            else
                                VM_REGISTERS[0xF] = 0;

                            VM_REGISTERS[((opcode & 0x0F00) >> 8)] -= VM_REGISTERS[((opcode & 0x00F0) >> 4)];
                            break;

                        case 0x6:               // 8XY6 - set VX = VX >> 1, VF = carry
                            VM_REGISTERS[0xF] = (byte)(VM_REGISTERS[((opcode & 0x0F00) >> 8)] & 0x1);
                            VM_REGISTERS[((opcode & 0x0F00) >> 8)] >>= 1;
                            break;

                        case 0x7:               // 8XY7 - set VX = VY - VX, VF = !borrow
                            if (VM_REGISTERS[((opcode & 0x00F0) >> 4)] >= VM_REGISTERS[((opcode & 0x0F00) >> 8)])
                                VM_REGISTERS[0xF] = 1;
                            else
                                VM_REGISTERS[0xF] = 0;

                            VM_REGISTERS[((opcode & 0x0F00) >> 8)] = (byte)(VM_REGISTERS[((opcode & 0x00F0) >> 4)] - VM_REGISTERS[((opcode & 0x0F00) >> 8)]);
                            break;

                        case 0xE:               // 8XYE - set VX = VX << 1, VF = carry
                            VM_REGISTERS[0xF] = (byte)((VM_REGISTERS[((opcode & 0x0F00) >> 8)] >> 7) & 0x01);
                            VM_REGISTERS[((opcode & 0x0F00) >> 8)] <<= 1;
                            break;

                        default:
                            logger.AddLogRecord("Unknown opcode: " + opcode.ToString("X2"));
                            break;
                    }
                    break;

                case 0x9:                               // 9XY0 - skip next instruction if VX != VY
                    if (VM_REGISTERS[((opcode & 0x0F00) >> 8)] != VM_REGISTERS[((opcode & 0x00F0) >> 4)])VM_CODE_PTR+= 2;
                    break;

                case 0xA:                               // ANNN - set I = Addr
                    VM_REG_I = (ushort)(opcode & 0x0FFF);
                    break;

                case 0xB:                               // BNNN - jump to Addr + V0
                   VM_CODE_PTR= (opcode & 0x0FFF) + VM_REGISTERS[0];
                    break;

                case 0xC:                               // CXKK - set VX = random & Byte
                    VM_REGISTERS[((opcode & 0x0F00) >> 8)] = (byte)((new Random().Next(256) % 255) & (opcode & 0x00FF));
                    break;

                case 0xD:                               // DXYN - Draw sprite
                                                        //DrawSprite((byte)((opcode & 0x0F00) >> 8), (byte)((opcode & 0x00F0) >> 4), (byte)(opcode & 0x000F));
                    ExecuteDrwOpcode(opcode);
                    break;

                case 0xE:
                    switch (opcode & 0x00FF)
                    {
                        case 0x9E:              // EX9E - skip next instruction if key VX down
                            int __k = Buttons.BtnsPressed[VM_REGISTERS[((opcode & 0x0F00) >> 8)]];
                            Buttons._lastCallKey = VM_REGISTERS[((opcode & 0x0F00) >> 8)];
                            if (__k == 1)
                               VM_CODE_PTR+= 2;
                            break;

                        case 0xA1:              // EXA1 - skip next instruction if key VX up
                            int __kh = Buttons.BtnsPressed[VM_REGISTERS[((opcode & 0x0F00) >> 8)]];
                            Buttons._lastCallKey = VM_REGISTERS[((opcode & 0x0F00) >> 8)];
                            if (__kh == 0)
                               VM_CODE_PTR+= 2;
                            break;

                        default:
                            logger.AddLogRecord("Unknown opcode: " + opcode.ToString("X2"));
                            break;
                    }
                    break;

                case 0xF:
                    switch (opcode & 0x00FF)
                    {
                        case 0x07:              // FX07 - set VX = delaytimer
                            VM_REGISTERS[((opcode & 0x0F00) >> 8)] = delay_timer;
                            break;

                        case 0x0A:              // FX0A - set VX = key, wait for keypress
                           VM_CODE_PTR-= 2;
                            for (byte ng = 0; ng < 16; ng++)
                            {
                                int __lk = Buttons.BtnsPressed[ng];
                                Buttons._lastCallKey = ng;
                                if (__lk == 1)
                                {
                                    VM_REGISTERS[((opcode & 0x0F00) >> 8)] = ng;
                                   VM_CODE_PTR+= 2;
                                    break;
                                }
                            }
                            break;

                        case 0x15:              // FX15 - set delaytimer = VX
                            delay_timer = VM_REGISTERS[((opcode & 0x0F00) >> 8)];
                            break;

                        case 0x18:              // FX18 - set soundtimer = VX
                            sound_timer = VM_REGISTERS[((opcode & 0x0F00) >> 8)];
                            break;

                        case 0x1E:              // FX1E - set I = I + VX; set VF if buffer overflow
                            if ((VM_REG_I += VM_REGISTERS[((opcode & 0x0F00) >> 8)]) > 0xfff)
                                VM_REGISTERS[0xF] = 1;
                            else
                                VM_REGISTERS[0xF] = 0;
                            break;

                        case 0x29:              // FX29 - point I to 5 byte numeric sprite for value in VX
                            VM_REG_I = (ushort)(VM_REGISTERS[((opcode & 0x0F00) >> 8)] * 5);
                            break;

                        case 0x33:              // FX33 - store BCD of VX in [VM_REG_I], [VM_REG_I+1], [VM_REG_I+2]
                            int n;
                            n = VM_REGISTERS[((opcode & 0x0F00) >> 8)];
                            VM_RAM[VM_REG_I] = (byte)((n - (n % 100)) / 100);
                            n -= VM_RAM[VM_REG_I] * 100;
                            VM_RAM[VM_REG_I + 1] = (byte)((n - (n % 10)) / 10);
                            n -= VM_RAM[VM_REG_I + 1] * 10;
                            VM_RAM[VM_REG_I + 2] = (byte)n;
                            break;

                        case 0x55:              // FX55 - store V0 .. VX in [VM_REG_I] .. [VM_REG_I+X]
                            for (int nj = 0; nj <= ((opcode & 0x0F00) >> 8); nj++)
                                VM_RAM[VM_REG_I + nj] = VM_REGISTERS[nj];
                            break;

                        case 0x65:              // FX65 - read V0 .. VX from [VM_REG_I] .. [VM_REG_I+X]
                            for (int nk = 0; nk <= ((opcode & 0x0F00) >> 8); nk++)
                                VM_REGISTERS[nk] = VM_RAM[VM_REG_I + nk];
                            break;

                        default:
                            logger.AddLogRecord("Unknown opcode: " + opcode.ToString("X2"));
                            break;
                    }
                    break;

                default:
                    logger.AddLogRecord("Unknown opcode: " + opcode.ToString("X2"));
                    break;
            }
        }

        public void LoadGame(String path)
        {
            byte[] game = System.IO.File.ReadAllBytes(path);
            for (int i = 0; i < game.Length; i++)
            {
                VM_RAM[i + 0x200] = game[i];
            }
        }

        public void DecreaseTimers()
        {
            if (delay_timer > 0) --delay_timer;
            if (sound_timer > 0) --sound_timer;
        }

        public void MainLoop()
        {
            while (!hasStopped)
            {
                ExecuteNextOpcode();
            }
        }
        /*  00E0  Clear screen
         *  00EE  Return
         *  1nnn  Jump to nnn (nnn is 12-bit)
         *  2nnn  Call nnn
         *  3xkk  Skip next command if Vx == kk
         *  4xkk  Skip next command if Vx != kk
         *  5xy0  Skip next command if Vx == Vy
         *  6xkk  Load kk into Vx
         *  7xkk  Load Vx+kk into Vx
         *  8xy0  Copy Vy to Vx
         *  8xy1  Vx = Vx | Vy
         *  8xy2  Vx = Vx & Vy
         *  8xy3  Vx = Vx ^ Vy
         *  8xy4  Vx = Vx + Vy, if result is greater than 255, VF = 1
         *  8xy5  Vx = Vx - Vy, if Vx >= Vy, VF = 1
         *  8xy6  Vx = Vx >> 1, if rightest bit of Vx = 1 then VF = 1
         *  8xy7  Vx = Vy - Vx, if Vy >= Vx, VF = 1
         *  8xyE  Vx = Vx << 1, if rightest bit of Vx = 1 then VF = 1
         *  9xy0  Skip next instruction if Vx != Vy
         *  Annn  I = nnn
         *  Bnnn  Jump to nnn + V0
         *  Cxkk  Vx = (random 0-255) & kk
         *  Dxyn  Draw sprite. Read n bytes from [VM_REG_I] and draw on (Vx,Vy). If one or more pixels were overdrawn VF = 1
         *  Ex9E  Skip next instruction if key #Vx is pressed
         *  ExA1  Opposite to previous command
         *  Fx07  Copy delay timer value to Vx
         *  Fx0A  Wait for any key. When it's pressed, its value is on Vx
         *  Fx15  Set delay timer value Vx
         *  Fx18  Set sound timer value Vx
         *  Fx1E  Set I = I + Vx
         *  Fx29  Load to I sprite address. Example: 5 is needed. Vx = 5; Fx29. Now I contain address containing '5' digit
         *  Fx33  Save Vx in bin-dec form in [VM_REG_I],[VM_REG_I+1],[VM_REG_I+2]
         *  Fx55  Save V0-Vx in memory starting from [VM_REG_I]
         *  Fx65  Load V0-Vx from memory starting from [VM_REG_I]
         */
    }
}
