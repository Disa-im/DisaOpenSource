using System;
using System.IO;
using SQLite;
using System.Collections.Generic;

namespace Disa.Framework
{
    public class SqlDatabase<T> : IDisposable where T : new()
    {
        private readonly string _fileLocation;
        private readonly SQLiteConnection _connection;

        public SqlDatabase(string fileLocation)
        {                
            _fileLocation = fileLocation;
            Retry:
            try
            {
                _connection = new SQLiteConnection(_fileLocation);
                _connection.CreateTable<T>();
            }
            catch (Exception ex)
            {
                Utils.DebugPrint("Failed to load SqlDatabase: " + ex + ". Nuking database if possible...");
                if (File.Exists(_fileLocation))
                {
                    File.Delete(_fileLocation);
                }
                goto Retry;
            }
        }  

        public void Dispose()
        {
            try
            {
                _connection.Close();
            }
            catch (Exception ex)
            {
                Utils.DebugPrint("Problem in DisaSqlDatabase close: " + ex);
            }
        }

        public TableQuery<T> Store
        {
            get
            {
                return _connection.Table<T>();
            }
        }

        public void Update(T item)
        {
            _connection.Update(item);
        }

        public void Add(T item)
        {
            _connection.Insert(item);
        }
            
        public void Remove(object item)
        {
            _connection.Delete(item);
        }
    }
}

