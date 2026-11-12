namespace SecondMonitor.R3EConnector.Rating
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NLog;

    public class RatingProvider
    {
        private const string RatingFileName = "R3ERating.json";
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<int, RatingInfo> _ratingMap;
        private Task _initializeRatingTask;

        public RatingProvider()
        {
            _ratingMap = new Dictionary<int, RatingInfo>();
        }

        public async Task InitializeRating()
        {
            if (_initializeRatingTask != null)
            {
                await _initializeRatingTask;
            }

            _initializeRatingTask = InitializeRatingAsync();
        }

        public bool TryGetRating(int userId, out RatingInfo rating)
        {
            if (_initializeRatingTask?.IsCompleted == true)
            {
                return _ratingMap.TryGetValue(userId, out rating);
            }

            rating = null;
            return false;
        }

        private async Task InitializeRatingAsync()
        {
            try
            {
                await Task.Run(InitializeRatingDictionary);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void InitializeRatingDictionary()
        {
            FileInfo ratingFile = DownloadRatingFile();
            JArray jsonDb = JsonConvert.DeserializeObject<JArray>(File.ReadAllText(ratingFile.FullName));

            if (jsonDb == null)
            {
                return;
            }

            _ratingMap.Clear();
            foreach (JToken jToken in jsonDb)
            {
                int userId = jToken["UserId"].Value<int>();
                if (jToken is not JObject jObject)
                {
                    continue;
                }

                JValue rating = jObject["Rating"] as JValue;
                JValue racesCompleted = jObject["RacesCompleted"] as JValue;
                JValue reputation = jObject["Reputation"] as JValue;

                if (rating?.Value == null || racesCompleted?.Value == null || reputation?.Value == null)
                {
                    continue;
                }

                RatingInfo newRating = new RatingInfo(rating.Value<double>(),
                    (jObject["Fullname"] ?? string.Empty).Value<string>(),
                    racesCompleted.Value<int>(),
                    reputation.Value<double>());

                _ratingMap.TryAdd(userId, newRating);
            }
        }

        private FileInfo DownloadRatingFile()
        {
            string fileFullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SecondMonitor\\" + RatingFileName);
            if (File.Exists(fileFullPath))
            {
                File.Delete(fileFullPath);
            }

            using (WebClient wc = new WebClient())
            {
                wc.DownloadFile("http://game.raceroom.com/multiplayer-rating/ratings.json", fileFullPath);
            }

            return new FileInfo(fileFullPath);
        }
    }
}