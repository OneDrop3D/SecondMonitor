namespace SecondMonitor.Rating.Common.Championship.Scoring.Templates
{
    public static class ScoringTemplates
    {
        public static ScoringTemplate[] AllTemplates = new[] { Formula1Scoring, Formula161To90Scoring, Formula191To02Scoring, WtcrScoring, Formula2Feature, Formula2Sprint, DtmScoring, ImsaScoring };

        public static ScoringTemplate Formula1Scoring => new ScoringTemplate() { Name = "Formula 1", Scoring = new[] { 25, 18, 15, 12, 10, 8, 6, 4, 2, 1 }, MaxPositionForFastestLap = 10, PointsForFastestLap = 1 };

        public static ScoringTemplate Formula2Feature => new ScoringTemplate() { Name = "Formula 2 - Feature", Scoring = new[] { 25, 18, 15, 12, 10, 8, 6, 4, 2, 1 }, MaxPositionForFastestLap = 10, PointsForFastestLap = 1 };

        public static ScoringTemplate Formula2Sprint => new ScoringTemplate() { Name = "Formula 2 - Sprint", Scoring = new[] { 15, 12, 10, 8, 6, 4, 2, 1 }, MaxPositionForFastestLap = 10, PointsForFastestLap = 1 };

        public static ScoringTemplate Formula191To02Scoring => new ScoringTemplate() { Name = "Formula 1 ('91 - '02)", Scoring = new[] { 10, 6, 4, 3, 2, 1 } };

        public static ScoringTemplate Formula161To90Scoring => new ScoringTemplate() { Name = "Formula 1 ('62 - '90)", Scoring = new[] { 9, 6, 4, 3, 2, 1 } };

        public static ScoringTemplate WtcrScoring => new ScoringTemplate() { Name = "WTCR ", Scoring = new[] { 25, 20, 16, 13, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 } };

        public static ScoringTemplate DtmScoring => new ScoringTemplate() { Name = "DTM", Scoring = new[] { 25, 18, 15, 12, 10, 8, 6, 4, 2, 1 } };
        
        public static ScoringTemplate ImsaScoring => new ScoringTemplate() { Name = "IMSA", Scoring = new[] { 350, 320, 300, 280, 260, 250, 240, 230, 220, 210, 200, 190, 180, 170, 160, 150, 140, 130, 120, 110, 100, 90, 80, 70, 60, 50, 40, 30, 20, 10 } };
    }
}