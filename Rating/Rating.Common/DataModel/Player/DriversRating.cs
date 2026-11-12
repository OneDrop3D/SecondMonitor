namespace SecondMonitor.Rating.Common.DataModel.Player
{
    using System;
    using System.Xml.Serialization;

    public class DriversRating
    {
        public DriversRating(int rating, int deviation, double volatility)
        {
            Rating = rating;
            Deviation = deviation;
            Volatility = volatility;
            CreationTime = DateTime.Now;
        }

        public DriversRating()
        {
            CreationTime = DateTime.Now;
        }

        [XmlAttribute]
        public int Rating { get; set; }

        [XmlAttribute]
        public int Deviation { get; set; }

        [XmlAttribute]
        public double Volatility { get; set; }

        [XmlAttribute]
        public int Difficulty { get; set; }

        [XmlIgnore]
        public DateTime CreationTime { get; set; }

        [XmlAttribute]
        public string CreationTimeFormatted
        {
            get => CreationTime.ToString("yyyy-MM-dd HH:mm:ss");
            set => CreationTime = DateTime.Parse(value);
        }
    }
}