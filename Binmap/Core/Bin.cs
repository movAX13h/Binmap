

using Microsoft.Xna.Framework;
using System;

namespace Binmap.Core
{
    class Bin
    {
        public enum Formats { Hex, Decimal, Binary, NibblesDecimal, Ascii }

        public Formats Format = Formats.Hex;
        public bool LineBreak = false;
        public string Comment = string.Empty;

        public byte Value { get; private set; }
        public int Offset { get; private set; }

        public string Text
        {
            get
            {
                string text = "";

                switch (Format)
                {
                    case Formats.Decimal:
                        return Value.ToString();

                    case Formats.Hex:
                        return Value.ToString("X2");

                    case Formats.Binary:
                        return Convert.ToString(Value, 2).PadLeft(8, '0');

                    case Formats.NibblesDecimal:
                        byte n1 = (byte)((Value & 0xF0) >> 4);
                        byte n2 = (byte)(Value & 0x0F);
                        return n1.ToString() + " " + n2.ToString();

                    case Formats.Ascii:
                        return Value > 31 && Value < 127 ? System.Text.Encoding.ASCII.GetString(new byte[] { Value }) : Value.ToString();
                }

                return text;
            }
        }

        public Color Color
        {
            get
            {
                switch(Format)
                {
                    case Formats.Decimal:
                        return Main.DecColor;

                    case Formats.Hex:
                        return Main.HexColor;

                    case Formats.Binary:
                        return Main.BinColor;

                    case Formats.NibblesDecimal:
                        return Main.NibDecColor;

                    case Formats.Ascii:
                        return Value > 31 && Value < 127 ? Main.AsciiColor : Color.Red;
                }

                return Color.White;
            }
        }

        public Bin(byte value, int offset)
        {
            Value = value;
            Offset = offset;
        }
    }
}
