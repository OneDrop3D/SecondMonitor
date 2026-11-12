namespace SecondMonitor.R3EConnector
{
    public class R3ELivery
    {
        public R3ELivery(int carNumber, string teamName)
        {
            CarNumber = carNumber;
            TeamName = teamName;
        }
        
        public int CarNumber { get; }
        public string TeamName { get; }
    }
}