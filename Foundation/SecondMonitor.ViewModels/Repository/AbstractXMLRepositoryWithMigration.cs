namespace SecondMonitor.ViewModels.Repository
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using SecondMonitor.DataModel.Dtos;

    public abstract class AbstractXmlRepositoryWithMigration<T> : AbstractXmlRepository<T> where T : class, IDtoWithVersion, new()
    {
        private readonly List<IDtoMigration<T>> _migrations;

        protected AbstractXmlRepositoryWithMigration(IEnumerable<IDtoMigration<T>> migrations)
        {
            _migrations = migrations.OrderBy(x => x.VersionAfterMigration).ToList();
        }

        public override T LoadOrCreateNew()
        {
            string fileName = Path.Combine(RepositoryDirectory, FileName);
            lock (LockObject)
            {
                CheckDirectory();
                if (TryLoadFile(fileName, out T deserializedObject))
                {
                    deserializedObject = MigrateUp(deserializedObject);
                    return deserializedObject;
                }

                if (TryLoadBackup(fileName, out deserializedObject))
                {
                    return deserializedObject;
                }

                T newObject = new T();
                newObject.Version = _migrations.LastOrDefault()?.VersionAfterMigration ?? 0;
                return newObject;
            }
        }

        private T MigrateUp(T deserializedObject)
        {
            foreach (IDtoMigration<T> migration in _migrations.Where(x => x.VersionAfterMigration > deserializedObject.Version).ToList())
            {
                deserializedObject = migration.ApplyMigration(deserializedObject);
                deserializedObject.Version = migration.VersionAfterMigration;
            }

            return deserializedObject;
        }
    }
}
