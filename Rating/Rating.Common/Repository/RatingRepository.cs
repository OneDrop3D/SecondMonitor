namespace SecondMonitor.Rating.Common.Repository
{
    using System;
    using System.IO;
    using System.Linq;
    using DataModel;
    using ViewModels.Repository;
    using ViewModels.Settings;

    public class RatingRepository : AbstractXmlRepository<Ratings>, IRatingRepository
    {
        private readonly string _directory;
        private readonly Lazy<Ratings> _loadRatingsLazy;

        public RatingRepository(ISettingsProvider settingsProvider)
        {
            _loadRatingsLazy = new Lazy<Ratings>(LoadOrCreateNew);
            _directory = settingsProvider.RatingsRepositoryPath;
            FileName = Path.Combine(_directory, "Ratings.xml");
        }

        public Ratings SimulatorsRatings => _loadRatingsLazy.Value;

        protected override string RepositoryDirectory => _directory;
        protected override string FileName { get; }

        public override Ratings LoadOrCreateNew()
        {
            Ratings ratings = base.LoadOrCreateNew();
            ratings = MigrateUp(ratings);
            return ratings;
        }

        private static Ratings MigrateUp(Ratings ratings)
        {
            if (ratings == null)
            {
                return new Ratings();
            }

            if (ratings.DifficultyRatingsMigrated)
            {
                return ratings;
            }

            foreach (ClassRating classRating in ratings.SimulatorsRatings.SelectMany(x => x.ClassRatings).Where(x => x.DifficultyRating == null))
            {
                classRating.DifficultyRating = classRating.PlayersRating;
            }

            ratings.DifficultyRatingsMigrated = true;
            return ratings;
        }
    }
}