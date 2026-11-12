namespace SecondMonitor.Rating.Common.DataModel.Championship
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    [Serializable]
    public class ScoringDto
    {
        public ScoringDto()
        {
            Scoring = new List<int>();
        }

        public List<int> Scoring { get; set; }

        [XmlAttribute]
        public int PointsForFastestLap { get; set; }

        [XmlAttribute]
        public int MaxPositionForFastestLap { get; set; }

        public int GetPointsForPosition(int position)
        {
            int index = position - 1;
            return index < Scoring.Count ? Scoring[index] : 0;
        }
    }
}