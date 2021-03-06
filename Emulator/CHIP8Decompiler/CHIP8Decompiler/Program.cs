﻿using System;
using System.IO;

namespace CHIP8Decompiler {
    public class Decompiler {
        private const float PROGRAM_VERSION = 0.1f;
        public static string Decompile(string args) {
            String result = "";
            {
                using (BinaryReader reader = new BinaryReader(File.Open(args, FileMode.Open)))
                {
                    ushort memLocation = 0x200;
                    while (reader.BaseStream.Position != reader.BaseStream.Length)
                    {
                        var bytes = reader.ReadBytes(2);
                        Array.Reverse(bytes);

                        var opcode = BitConverter.ToUInt16(bytes, 0);
                        result += ($"{ToHex(memLocation)}: [{ToHex(opcode)}] {OpcodeDecoder(opcode)}") + "\r\n";

                        memLocation += 2;
                    }
                }
            }
            return result;
        }

        static string OpcodeDecoder(ushort opcode) {
            switch (opcode & 0xF000) {
                case 0x0000:
                    switch (opcode & 0x00FF) {
                        case 0x00E0:
                            return "clear_display()";
                        case 0x00EE:
                            return "return";
                        default:
                            return $"call {ToHex(opcode & 0x0FFF)}";
                    }
                case 0x1000:
                    return $"goto {ToHex(opcode & 0x0FFF)}";
                case 0x2000:
                    return $"*({ToHex(opcode & 0x0FFF)})()";
                case 0x3000:
                    return $"if (V{GetX(opcode)} == {opcode & 0x00FF})";
                case 0x4000:
                    return $"if (V{GetX(opcode)} != {opcode & 0x00FF})";
                case 0x5000:
                    if ((opcode & 0x000F) == 0) {
                        return $"if (V{GetX(opcode)} == V{GetY(opcode)})";
                    }
                    break;
                case 0x6000:
                    return $"V{GetX(opcode)} = {opcode & 0x00FF}";
                case 0x7000:
                    return $"V{GetX(opcode)} += {opcode & 0x00FF}";
                case 0x8000:
                    switch (opcode & 0x000F) {
                        case 0x0000:
                            return $"V{GetX(opcode)} = V{GetY(opcode)}";
                        case 0x0001:
                            return $"V{GetX(opcode)} = V{GetX(opcode)} | V{GetY(opcode)}";
                        case 0x0002:
                            return $"V{GetX(opcode)} = V{GetX(opcode)} & V{GetY(opcode)}";
                        case 0x0003:
                            return $"V{GetX(opcode)} = V{GetX(opcode)} ^ V{GetY(opcode)}";
                        case 0x0004:
                            return $"V{GetX(opcode)} += V{GetY(opcode)}";
                        case 0x0005:
                            return $"V{GetX(opcode)} -= V{GetY(opcode)}";
                        case 0x0006:
                            return $"V{GetX(opcode)} = V{GetY(opcode)} = V{GetY(opcode)} >> 1";
                        case 0x0007:
                            return $"V{GetX(opcode)} = V{GetY(opcode)} - V{GetX(opcode)}";
                        case 0x000E:
                            return $"V{GetX(opcode)} = V{GetY(opcode)} = V{GetY(opcode)} << 1";
                    }
                    break;
                case 0x9000:
                    if ((opcode & 0x000F) == 0) {
                        return $"if (V{GetX(opcode)} != V{GetY(opcode)})";
                    }
                    break;
                case 0xA000:
                    return $"I = {opcode & 0x0FFF}";
                case 0xB000:
                    return $"PC = V0 + {opcode & 0x0FFF}";
                case 0xC000:
                    return $"V{GetX(opcode)} = random() & {opcode & 0x00FF}";
                case 0xD000:
                    return $"draw({GetX(opcode)}, {GetY(opcode)}, {opcode & 0x000F})";
                case 0xE000:
                    switch (opcode & 0x00FF) {
                        case 0x009E:
                            return $"if (key() == V{GetX(opcode)})";
                        case 0x00A1:
                            return $"if (key() != V{GetX(opcode)})";
                    }
                    break;
                case 0xF000:
                    switch (opcode & 0x00FF) {
                        case 0x0007:
                            return $"V{GetX(opcode)} = get_delay()";
                        case 0x000A:
                            return $"V{GetX(opcode)} = get_key()";
                        case 0x0015:
                            return $"delay_timer(V{GetX(opcode)})";
                        case 0x0018:
                            return $"sound_timer(V{GetX(opcode)})";
                        case 0x001E:
                            return $"I += V{GetX(opcode)}";
                        case 0x0029:
                            return $"I = sprite_addr[V{GetX(opcode)}]";
                        case 0x0033:
                            return $"set_BCD(V{GetX(opcode)}); * (I + 0) = BCD(3); *(I + 1) = BCD(2); *(I + 2) = BCD(1);";
                        case 0x0055:
                            return $"reg_dump(V{GetX(opcode)}, &I)";
                        case 0x0065:
                            return $"reg_load(V{GetX(opcode)}, &I)";
                    }
                    break;
            }
            return "<unknown opcode>";
        }

        static string ToHex(int n) => $"0x{n:X4}";

        /// <summary>
        /// Extract the first register an opcode operates on from the opcode.
        /// </summary>
        /// <param name="opcode"></param>
        /// <returns>The register encoded in the opcode.</returns>
        static int GetX(int opcode) => (opcode & 0x0F00) >> 8;

        /// <summary>
        /// Extract the second register an opcode operates on from the opcode.
        /// </summary>
        /// <param name="opcode"></param>
        /// <returns>The register encoded in the opcode.</returns>
        static int GetY(int opcode) => (opcode & 0x00F0) >> 4;
    }
}
