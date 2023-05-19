using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace spotify_playlist_generator
{
    internal class Cache<T>
    {
        public Func<IEnumerable<T>> Factory { get; set; }
        public int ChunkSize { get; set; }
        public string Name { get; set; }
        public bool FileCache { get; set; }

        private static string FileCacheBasePath = System.IO.Path.Join(Program.AssemblyDirectory, "caches");

        private string _FileCachePath;

        public string FileCachePath
        {
            get
            {
                _FileCachePath ??= System.IO.Path.Join(FileCacheBasePath, System.IO.Path.ChangeExtension(this.Name, ".json"));
                return _FileCachePath;
            }
        }

        public Cache(Func<IEnumerable<T>> factory, string name, int chunkSize = 20, bool fileCache = true)
        {
            Factory = factory;
            ChunkSize = chunkSize;
            Name = name;
            FileCache = fileCache;
        }

        public IEnumerable<T> GetItems(IEnumerable<string> keys)
        {
            //pull from the cache first
            //then chunk what remains

            var chunks = keys.ChunkBy(this.ChunkSize);

            // handle not chunking
            // not prioritizing this as it will rarely happen
            if (this.ChunkSize == 0)
                chunks = new List<List<string>> { keys.ToList() };

            throw new NotImplementedException();
        }
    }
}
