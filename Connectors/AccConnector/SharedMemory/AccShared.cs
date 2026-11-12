namespace SecondMonitor.AccConnector.SharedMemory
{
    public class AccShared
    {
        public const string SharedMemoryNamePhysics = "Local\\acpmf_physics";
        public const string SharedMemoryNameGraphic = "Local\\acpmf_graphics";
        public const string SharedMemoryNameStatic = "Local\\acpmf_static";

        public SPageFilePhysics AcsPhysics { get; set; }
        public SPageFileGraphic AcsGraphic { get; set; }
        public SPageFileStatic AcsStatic { get; set; }
    }
}