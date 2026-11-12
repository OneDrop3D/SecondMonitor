namespace SecondMonitor.Contracts.Attributes
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class TimingApplicationLayoutComponent : System.Attribute
    {
        public TimingApplicationLayoutComponent(string layoutElementName)
        {
            LayoutElementName = layoutElementName;
        }

        private string LayoutElementName { get; }
    }
}