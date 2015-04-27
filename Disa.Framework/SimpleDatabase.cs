using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Disa.Framework
{

    /// <summary>
    /// This is a very primitive database. When Remove is called, the whole list gets resaved. Therefore, the class
    /// is only designed to be used on small lists (e.g. saving sent receipts to be paried up with their received counterparts).
    /// </summary>
    public class SimpleDatabase<TObject, TSerializable> : 
        IEnumerable<SimpleDatabase<TObject, TSerializable>.Container>
    {
        private readonly List<Container> _items = new List<Container>();
        private readonly string _fileLocation;
        private readonly object _stateLock = new object();
        private readonly bool _truncate;
        private readonly long _truncateDifference;

        public SimpleDatabase(string fileName, bool truncate = false, int truncateDifference = 604800)
        {
            lock (_stateLock)
            {
                _fileLocation = Path.Combine(Platform.GetSettingsPath(), fileName + ".db");
                _truncate = truncate;
                _truncateDifference = truncateDifference;
                Load();
            }
        }

        public SimpleDatabase(string settingsPath, string fileName, 
            bool truncate = false, int truncateDifference = 604800)
        {
            lock (_stateLock)
            {
                _fileLocation = Path.Combine(settingsPath, fileName + ".db");
                _truncate = truncate;
                _truncateDifference = truncateDifference;
                Load();
            }
        }

        public void Add(Container item)
        {
            Add(new[] { item });
        }

        public void Add(TObject @object, TSerializable serialiable)
        {
            Add(new[] {new Container(@object, serialiable)});
        }

        public void Clear()
        {
            lock (_stateLock)
            {
                _items.Clear();
                Delete();
            }
        }

        public void Add(IEnumerable<Container> items)
        {
            lock (_stateLock)
            {
                foreach (var item in items)
                {
                    _items.Add(item);
                }

                AddEntries(items);
            }
        }

        public void Remove(TObject @object)
        {
            Remove(new [] {@object});
        }

        public void Remove(TObject @object, TSerializable serialiable)
        {
            Remove(new[] { new Container(@object, serialiable) });
        }

        public void Remove(Container item)
        {
            Remove(new[] { item });
        }

        public void Remove(IEnumerable<TObject> @objects)
        {
            lock (_stateLock)
            {
                var items =
                    objects.Select(@object => _items.FirstOrDefault(x => x.Object.Equals(@object)))
                           .Where(item => item != null)
                           .ToList();

                Remove(items);
            }
        }

        public void Remove(IEnumerable<Container> items)
        {
            lock (_stateLock)
            {
                foreach (var item in items)
                {
                    _items.Remove(item);
                }

                Rewrite();
            }
        }

        public void SaveChanges()
        {
            Rewrite();
        }

        public IEnumerator<Container> GetEnumerator()
        {
            lock (_stateLock)
            {
                return _items.ToList().GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            lock (_stateLock)
            {
                return GetEnumerator();
            }
        }
        
        private void Delete()
        {
            if (File.Exists(_fileLocation))
            {
                File.Delete(_fileLocation);
            }
        }

        private void Load()
        {
            var loadTime = Time.GetNowUnixTimestamp();

            if (!File.Exists(_fileLocation))
                return;

            try
            {
                using (var fs = File.OpenRead(_fileLocation))
                {
                    using (var binaryReader = new BinaryReader(fs))
                    {
                        while (binaryReader.PeekChar() != -1)
                        {
                            var strTime = ReadHeader(binaryReader);
                            var time = long.Parse(strTime);
                            var serialiazableBytes = ReadBytes(binaryReader);

                            if (_truncate && _truncateDifference < (loadTime - time))
                            {
                                continue;
                            }

                            using (var ms = new MemoryStream(serialiazableBytes))
                            {
                                var serializableObject = Serializer.Deserialize<TSerializable>(ms);
                                _items.Add(new Container(default(TObject), serializableObject));
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                Utils.DebugPrint("Failed to load database " + typeof (TObject).Name +
                                         ". It is corrupt. Deleting.");
                Delete();
            }
        }

        private void Rewrite()
        {
            using (var fs = File.Create(_fileLocation))
            {
                using (var binaryWriter = new BinaryWriter(fs))
                {
                    foreach (var container in _items)
                    {
                        using (var ms = new MemoryStream())
                        {
                            Serializer.Serialize(ms, container.Serializable);
                            WriteHeader(binaryWriter);
                            WriteBytes(binaryWriter, ms.ToArray());
                        }
                    }
                }
            }
        }

        private void AddEntries(IEnumerable<Container> items)
        {
            using (var fs = File.Open(_fileLocation, FileMode.Append))
            {
                using (var binaryWriter = new BinaryWriter(fs))
                {
                    foreach (var container in items)
                    {
                        using (var ms = new MemoryStream())
                        {
                            Serializer.Serialize(ms, container.Serializable);
                            WriteHeader(binaryWriter);
                            WriteBytes(binaryWriter, ms.ToArray());
                        }
                    }
                }
            }
        }

        private static string ReadHeader(BinaryReader s)
        {
            var strLength = s.ReadInt32();
            var strBytes = s.ReadBytes(strLength);
            return Encoding.ASCII.GetString(strBytes);
        }

        private static void WriteHeader(BinaryWriter s)
        {
            WriteString(s, Time.GetNowUnixTimestamp().ToString(CultureInfo.InvariantCulture));
        }

        private static void WriteString(BinaryWriter s, string str)
        {
            var strLength = str.Length;
            s.Write(strLength);
            s.Write(Encoding.ASCII.GetBytes(str), 0, strLength);
        }

        private static void WriteBytes(BinaryWriter s, byte[] bytes)
        {
            var bytesLength = bytes.Length;
            s.Write(bytesLength);
            s.Write(bytes, 0, bytesLength);
        }

        private static byte[] ReadBytes(BinaryReader s)
        {
            var bytesLength = s.ReadInt32();
            return s.ReadBytes(bytesLength);
        }

        public class Container
        {
            public TObject Object { get; set; }
            public TSerializable Serializable { get; set; }

            public Container(TObject @object, TSerializable serializable)
            {
                Object = @object;
                Serializable = serializable;
            }
        }
    }
}