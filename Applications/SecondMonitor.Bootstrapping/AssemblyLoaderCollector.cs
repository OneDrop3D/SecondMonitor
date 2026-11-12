namespace SecondMonitor.Bootstrapping
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    internal class AssemblyLoaderCollector
    {
        private readonly List<Assembly> _loadedAssemblies;

        public AssemblyLoaderCollector(int size = 4)
        {
            _loadedAssemblies = new List<Assembly>(4);
        }

        public IReadOnlyList<Assembly> LoadedAssemblies => _loadedAssemblies.AsReadOnly();

        public void AssemblyLoaded(object sender, AssemblyLoadEventArgs args)
        {
            _loadedAssemblies.Add(args.LoadedAssembly);
        }
    }
}