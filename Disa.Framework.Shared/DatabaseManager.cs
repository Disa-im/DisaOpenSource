using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using SQLite;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Disa.Framework
{
	public class DatabaseManager
	{
	    protected static T ExecuteTask<T>(Task<T> task, [CallerMemberName] string callerName = "")
	    {
		    try
		    {
			    task.Wait();
			    return task.Result;
		    }
		    catch (SQLiteException ex)
		    {
			    Utils.DebugPrint($"{callerName} failed. SQLiteException {ex.Message}");
			    return default(T);
		    }
		    catch (AggregateException ex)
		    {
			    Utils.DebugPrint($"{callerName} failed. {nameof(AggregateException)} {ex.Message}");
			    return default(T);
		    }
		    catch (Exception ex)
		    {
                Utils.DebugPrint($"{callerName} failed. {ex.GetType()} {ex.Message}");
                throw;
		    }
	    }
		
		protected readonly string filePath;
		protected readonly SQLiteAsyncConnection sqliteAsyncConnection;

		public SQLiteAsyncConnection SqliteConnection
		{ get => sqliteAsyncConnection; }
        
        public DatabaseManager(string filePath)
        {
            this.filePath = filePath;
            sqliteAsyncConnection = new SQLiteAsyncConnection(filePath);
        }

        // Creates table if they don't exist
        public AsyncTableQuery<T> SetupTableObject<T>() where T : class, ISerializableType<T>, new()
        {
            var connection = sqliteAsyncConnection.GetConnection();
            var tableInfo = connection.GetTableInfo(nameof(T));
            if (!tableInfo.Any())
            {
                var tableSuccess = CreateTable<T>();
            }

            return sqliteAsyncConnection.Table<T>();
		}

		public List<T> ReadRows<T>() where T : ISerializableType<T>, new()
        {
			var query = sqliteAsyncConnection.Table<T>();

			var task = query.ToListAsync();
			var result = ExecuteTask(task);

            foreach (var row in result)
            {
                row.DeserializeProperties();
            }

            return result;
		}
    
        protected bool CreateTable<T>() where T : class, ISerializableType<T>, new()
		{
			var task = sqliteAsyncConnection.CreateTableAsync<T>();
			var result = ExecuteTask(task);
			return !task.IsFaulted && !task.IsCanceled && task.IsCompleted;
		}
        
		public bool DropTable<T>() where T : new()
		{
			var task = sqliteAsyncConnection.DropTableAsync<T>();
			var result = ExecuteTask(task);
			return !task.IsFaulted && !task.IsCanceled && task.IsCompleted;
		}
        
        public bool InsertOrReplaceRow<T>(T row) where T : ISerializableType<T>
        {
            row.SerializeProperties();

            var task = sqliteAsyncConnection.InsertOrReplaceAsync(row);
	        var result = ExecuteTask(task);
	        return !task.IsFaulted && !task.IsCanceled && task.IsCompleted;
	    }

        public void InsertOrReplaceRows<T>(IEnumerable<T> rows) where T : ISerializableType<T>
        {
	        var enumerable = rows as IList<T> ?? rows.ToList();
	        foreach (var row in enumerable)
            {
                row.SerializeProperties();
            }

            var c = sqliteAsyncConnection.GetConnection();
			foreach (var row in enumerable)
			{
				var task = sqliteAsyncConnection.InsertOrReplaceAsync(row);
				var result = ExecuteTask(task);
			}
        }
        
        public bool InsertRow<T>(T row) where T : ISerializableType<T>
        {
            row.SerializeProperties();
            var task = sqliteAsyncConnection.InsertAsync(row);
	        var result = ExecuteTask(task);
	        return !task.IsFaulted && !task.IsCanceled && task.IsCompleted;
	    }

        public bool InsertRows<T>(IEnumerable<T> rows) where T : ISerializableType<T>
        {
	        var enumerable = rows as IList<T> ?? rows.ToList();
	        foreach (var row in enumerable)
            {
                row.SerializeProperties();
            }

            var task = sqliteAsyncConnection.InsertAllAsync(enumerable);
	        var result = ExecuteTask(task);
			return !task.IsFaulted && !task.IsCanceled && task.IsCompleted;
		}

        public T FindRow<T>(Expression<Func<T, bool>> filter)
            where T : class, ISerializableType<T>, new()
        {
            var table = sqliteAsyncConnection.Table<T>();
            var task = table.Where(filter).FirstOrDefaultAsync();
	        var result = ExecuteTask(task);

            if (result == null)
            {
                return null;
            }

	        result.DeserializeProperties();
            return result;
        }

        public List<T> FindRows<T>(Expression<Func<T, bool>> filter)
            where T : class, ISerializableType<T>, new()
        {
            var table = sqliteAsyncConnection.Table<T>();
            var task = table.Where(filter).ToListAsync();
            var rows = ExecuteTask(task);
            if (rows == null)
            {
                return new List<T>();
            }
            foreach (var row in rows)
            {
                row.DeserializeProperties();
            }
            return rows;
        }

        public bool UpdateRow<T>(Expression<Func<T, bool>> filter, Func<T, T> updateFunc) 
            where T : class, ISerializableType<T>, new()
        {
            var table = sqliteAsyncConnection.Table<T>();
            var task = table.Where(filter).FirstOrDefaultAsync();
	        var row = ExecuteTask(task);

            if (row == null)
            {
                return false;
            }

            row.DeserializeProperties();
            row = updateFunc(row);
            row.SerializeProperties();

            var updateTask = sqliteAsyncConnection.UpdateAsync(row);
	        ExecuteTask(updateTask);
            return !task.IsFaulted && !task.IsCanceled && task.IsCompleted;
        }

        public bool UpdateRows<T>(Expression<Func<T, bool>> filter, Func<T, T> updateFunc)
            where T : class, ISerializableType<T>, new()
        {
            var table = sqliteAsyncConnection.Table<T>();
            var task = table.Where(filter).ToListAsync();
	        var rows = ExecuteTask(task);

            if (rows == null)
            {
                return true;
            }

            rows = rows.Select(r => 
            {
                r.DeserializeProperties();
                var updatedRow = updateFunc(r);
                return updatedRow.SerializeProperties();
            }).ToList();
            
            var updateTask = sqliteAsyncConnection.UpdateAllAsync(rows);
	        ExecuteTask(task);
            return !task.IsFaulted && !task.IsCanceled && task.IsCompleted;
        }

        public bool UpdateRow<T>(T row) where T : ISerializableType<T>
        {
            row.SerializeProperties();

            var task = sqliteAsyncConnection.UpdateAsync(row);
	        var rows = ExecuteTask(task);
            return !task.IsFaulted && !task.IsCanceled && task.IsCompleted;
        }

        public bool UpdateRows<T>(IEnumerable<T> rows) where T : ISerializableType<T>
        {
	        var enumerable = rows as IList<T> ?? rows.ToList();
	        foreach (var row in enumerable)
            {
                row.SerializeProperties();
            }

            var task = sqliteAsyncConnection.UpdateAllAsync(enumerable);
	        ExecuteTask(task);
			return !task.IsFaulted && !task.IsCanceled && task.IsCompleted;
		}
		
		public bool DeleteRow<T>(T row)
		{
			try
			{
				var deleteTask = sqliteAsyncConnection.DeleteAsync(row);
				var rows = ExecuteTask(deleteTask);
				return !deleteTask.IsFaulted && !deleteTask.IsCanceled && deleteTask.IsCompleted;
			}
			catch (Exception ex)
			{
				Utils.DebugPrint($"Exception {nameof(DeleteRow)}: {ex}");
				return false;
			}
		}

        public bool DeleteRows<T>(Expression<Func<T, bool>> filter)
            where T : class, ISerializableType<T>, new()
        {
            var table = sqliteAsyncConnection.Table<T>();
            var task = table.Where(filter).ToListAsync();
	        var rows = ExecuteTask(task);
	        var flag = true;
	        
            foreach (var row in rows)
            {
                var deleteTask = sqliteAsyncConnection.DeleteAsync(row);
                ExecuteTask(deleteTask);
            }
            return flag;
        }

        public bool DeleteRows<T>(IEnumerable<T> rows)
        {
	        return rows.Aggregate(true, (current, row) => current & DeleteRow(row));
        }
    }
}
