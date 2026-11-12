namespace SecondMonitor.PCarsConnector.Enums
{
    using System.ComponentModel;

    public enum ESessionState
    {
        [Description("No Session")]
        SessionInvalid = 0,

        [Description("Practise")]
        SessionPractice,

        [Description("Testing")]
        SessionTest,

        [Description("Qualifying")]
        SessionQualify,

        [Description("Formation Lap")]
        SessionFormationlap,

        [Description("Racing")]
        SessionRace,

        [Description("Time Trial")]
        SessionTimeAttack,

        // -------------
        SessionMax
    }
}
