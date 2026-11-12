namespace SecondMonitor.DataModel.BasicProperties
{
    using System;
#if !SM_PORTABLE
    using System.Windows.Media;
#endif
    using System.Xml.Serialization;

    using Newtonsoft.Json;

    using SecondMonitor.DataModel.Extensions;

    [Serializable]
    public sealed class ColorDto
    {
        public static readonly ColorDto RedColor = FromHex("FFFF0000"); // Colors.Red
        public static readonly ColorDto DarkBlueColor = FromHex("FF00008B"); // Colors.DarkBlue
        public static readonly ColorDto LightBlueColor = FromHex("FFADD8E6"); // Colors.LightBlue
        public static readonly ColorDto Blue01Color = FromHex("FF0071B9");
        public static readonly ColorDto Orange01Color = FromHex("ff8c1a");
        public static readonly ColorDto OrangeColor = FromHex("FFFFA500"); // Colors.Orange
        public static readonly ColorDto Orange02Color = FromHex("994d00");
        public static readonly ColorDto GreenColor = FromHex("FF008000"); // Colors.Green
        public static readonly ColorDto LimeGreenColor = FromHex("FF32CD32"); // Colors.LimeGreen
        public static readonly ColorDto YellowColor = FromHex("FFFFFF00"); // Colors.Yellow
        public static readonly ColorDto GreenYellowColor = FromHex("FFADFF2F"); // Colors.GreenYellow
        public static readonly ColorDto WhiteColor = FromHex("FFFFFFFF"); // Colors.White
        public static readonly ColorDto BlackColor = FromHex("FF000000"); // Colors.Black
        public static readonly ColorDto Anthracite01Color = FromHex("FF181818");
        public static readonly ColorDto SilverColor = ColorDto.FromHex("FF7F7F7F");

        public ColorDto(byte alpha, byte red, byte green, byte blue)
        {
            Alpha = alpha;
            Red = red;
            Green = green;
            Blue = blue;
        }

        public ColorDto()
        {
        }

        [XmlAttribute]
        public byte Alpha { get; set; }

        [XmlAttribute]
        public byte Red { get; set; }

        [XmlAttribute]
        public byte Green { get; set; }

        [XmlAttribute]
        public byte Blue { get; set; }

        [XmlIgnore]
        [JsonIgnore]
        public double RedSc
        {
            get
            {
                double scaled = Red / 255.0;
                return scaled <= 0.03928 ? scaled / 12.92 : Math.Pow((scaled + 0.055) / 1.055, 2.4);
            }
        }

        [XmlIgnore]
        [JsonIgnore]
        public double GreenSc
        {
            get
            {
                double scaled = Green / 255.0;
                return scaled <= 0.03928 ? scaled / 12.92 : Math.Pow((scaled + 0.055) / 1.055, 2.4);
            }
        }

        [XmlIgnore]
        [JsonIgnore]
        public double BlueSc
        {
            get
            {
                double scaled = Blue / 255.0;
                return scaled <= 0.03928 ? scaled / 12.92 : Math.Pow((scaled + 0.055) / 1.055, 2.4);
            }
        }

#if !SM_PORTABLE
        public static ColorDto FromColor(Color color)
        {
            return new ColorDto()
            {
                Alpha = color.A,
                Blue = color.B,
                Green = color.G,
                Red = color.R,
            };
        }
#endif

        public static ColorDto FromHex(string hex)
        {
            if (hex.Length == 6)
            {
                hex = "FF" + hex;
            }

            hex = hex.Replace("#", string.Empty);
            return new ColorDto()
            {
                Alpha = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.AllowHexSpecifier),
                Red = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.AllowHexSpecifier),
                Green = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.AllowHexSpecifier),
                Blue = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.AllowHexSpecifier),
            };
        }

#if !SM_PORTABLE
        public Color ToColor()
        {
            return Color.FromArgb(Alpha, Red, Green, Blue);
        }

        public SolidColorBrush ToSolidColorBrush()
        {
            return new SolidColorBrush(ToColor());
        }
#endif

        private bool Equals(ColorDto other)
        {
            return Alpha == other.Alpha && Red == other.Red && Green == other.Green && Blue == other.Blue;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || (obj is ColorDto other && Equals(other));
        }
        
        public static ColorDto Interpolate(ColorDto color1, ColorDto color2, double lambda)
        {
            double red = DoubleExtension.Lerp(color1.Red, color2.Red, lambda);
            double green = DoubleExtension.Lerp(color1.Green, color2.Green, lambda);
            double blue = DoubleExtension.Lerp(color1.Blue, color2.Blue, lambda);
            return new ColorDto(255, (byte)red, (byte)green, (byte)blue);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Alpha.GetHashCode();
                hashCode = (hashCode * 397) ^ Red.GetHashCode();
                hashCode = (hashCode * 397) ^ Green.GetHashCode();
                hashCode = (hashCode * 397) ^ Blue.GetHashCode();
                return hashCode;
            }
        }
    }
}