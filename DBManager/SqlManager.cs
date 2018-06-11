using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBManager
{
    internal class SqlManager
    {
        private SqlConnection cnn;

        private readonly string dbName = "ShellServer";

        public string TableNameFormat(string tableName)
        {
            return string.Format("[{0}].[dbo].[{1}]", dbName, tableName);
        }

        public void Connect()
        {
            var connectionString = @"Data Source=localhost;Initial Catalog=master;Integrated Security=True;";
            cnn = new SqlConnection(connectionString);
            try
            {
                cnn.Open();
                Console.WriteLine("Connection Open  !");
            }
            catch(Exception e)
            {
                throw new Exception(string.Format("Could not open SQL connection with the connection string {0}", connectionString),e);
            }
        }

        public void Disconnect()
        {
            if (cnn.State == ConnectionState.Open)
                cnn.Close();
        }

        public void CreateDataBase(string path = null)
        {
            if (cnn == null)
                throw new Exception("Trying to Create database but need to connect to database first!");

            var createDatabaseStr = string.IsNullOrEmpty(path) ?
                  string.Format("CREATE DATABASE {0}", dbName) :
                  string.Format("CREATE DATABASE {0} ON PRIMARY ", dbName) +
                  string.Format("(NAME = {0}_Data, ", dbName) +
                  string.Format("FILENAME = '{0}\\{1}.mdf', ", path, dbName) +
                  "SIZE = 2MB, MAXSIZE = 10MB, FILEGROWTH = 10%) " +
                  string.Format("LOG ON (NAME = {0}_Log, ", dbName) +
                  string.Format("FILENAME = '{0}\\{1}.ldf', ", path, dbName) +
                  "SIZE = 1MB, " +
                  "MAXSIZE = 5MB, " +
                  "FILEGROWTH = 10%)";

            var deleteBatabaseStr = string.Format("DROP DATABASE {0}", dbName);

            if (CheckDatabaseExists(dbName))
            {
                using (var createDatabaseCommand = new SqlCommand(deleteBatabaseStr, cnn))
                {
                    createDatabaseCommand.ExecuteNonQuery();
                }
            }

            using (var createDatabaseCommand = new SqlCommand(createDatabaseStr, cnn))
            {
                createDatabaseCommand.ExecuteNonQuery();
            }

            foreach (var schema in Schema.TableNameToSchema)
            {
                StringBuilder command = new StringBuilder();
                command.AppendFormat("CREATE TABLE {0}", TableNameFormat(schema.Key));
                command.AppendFormat("(", schema.Key);
                var lastElement = schema.Value.Last();
                foreach (var col in schema.Value)
                {
                    command.AppendFormat("{0} {1} ({2}) ", col.ColumnName, col.ColumnType, col.ColumnLength);
                    if (col != lastElement)
                        command.Append(",");
                }
                command.Append(")");

                if (CheckIfTableExsits(TableNameFormat(schema.Key)))
                {
                    var dropTableStr = string.Format("DROP TABLE {0}", TableNameFormat(schema.Key));

                    using (var deleteTableCommand = new SqlCommand(dropTableStr, cnn))
                    {
                        var res = deleteTableCommand.ExecuteNonQuery();
                    }
                }
                using (var createTableCommand = new SqlCommand(command.ToString(), cnn))
                {
                    var res = createTableCommand.ExecuteNonQuery();
                }
            }
        }

        public void InsertIntoDataBase(string tableName, string[] values)
        {
            if (cnn == null)
                throw new Exception("Trying to save to database but need to connect to database first!");

            if (!Schema.TableNameToSchema.ContainsKey(tableName))
                throw new Exception("No sutch table!");

            if(values.Length < Schema.TableNameToSchema[tableName].Count)
                throw new Exception("Too less values to that table!");

            //var tableCols = Schema.TableNameToSchema[tableName].Select(se => "'" + se.ColumnName + "'").ToArray();
            var tableCols = Schema.TableNameToSchema[tableName].Select(se =>se.ColumnName).ToArray();
            values = values.Select(val => "'" + val + "'").ToArray();
            var saveValuestr = string.Format("INSERT INTO {0} ", TableNameFormat(tableName)) +
                               string.Format("({0}) ", ToSeperatedCommaList(tableCols)) +
                               string.Format("VALUES ({0})", ToSeperatedCommaList(values));

            using (var saveCommand = new SqlCommand(saveValuestr, cnn))
            {
                saveCommand.ExecuteNonQuery();
            }
               
        }

        public void UpdateDataBaseRecord(string tableName, string id, string[] columns, string[] values)
        {
            if (columns.Length != values.Length)
                throw new Exception("Columns number and values number are not the same!");

            if (columns.Length == 0 || values.Length == 0)
                throw new Exception("Cannot update 0 columns or 0 values!");

            if (!Schema.TableNameToSchema.ContainsKey(tableName))
                throw new Exception("No sutch table!");

            StringBuilder command = new StringBuilder();
            command.AppendFormat("UPDATE {0} ", TableNameFormat(tableName));
            command.Append("SET ");
            var zippedColVals = columns.Zip(values, (col, val) => new Tuple<string, string>(col, val)).ToList();
            var last = zippedColVals.Last();
            foreach (var colVal in zippedColVals)
            {
                command.AppendFormat("{0} = '{1}' ",colVal.Item1, colVal.Item2);
                if(colVal != last)
                    command.Append(",");
            };
            command.AppendFormat(" WHERE Id = '{0}';", id);

            using (var updateCommand = new SqlCommand(command.ToString(), cnn))
            {
                updateCommand.ExecuteNonQuery();
            }    
        }

        public string[] GetRecord(string tableName, string colName, string colVal)
        {
            if (cnn == null)
                throw new Exception("Trying to delete from database but need to connect to database first!");


            if (!Schema.TableNameToSchema.ContainsKey(tableName))
                throw new Exception("No sutch table!");

            var SelectRecord = string.Format("SELECT * FROM {0} ", TableNameFormat(tableName)) +
                               string.Format("WHERE {0} = '{1}'", colName, colVal);


            using (var updateCommand = new SqlCommand(SelectRecord, cnn))
            {
                using (var reader = updateCommand.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;

                    var readList = new List<string>();
                    for (int i = 0; i < Schema.TableNameToSchema[tableName].Count; i++)
                    {
                        readList.Add(reader.GetString(i));
                    }

                    if (colName == "Id" && reader.Read())
                        throw new InvalidOperationException("Multiple records were returned. Id is not unique!");

                    return readList.ToArray();
                }
            }
        }

        public void DeleteRecord(string tableName, string id)
        {
            if (cnn == null)
                throw new Exception("Trying to delete from database but need to connect to database first!");


            if (!Schema.TableNameToSchema.ContainsKey(tableName))
                throw new Exception("No sutch table!");

            var deleteStr = string.Format("DELETE FROM {0} ", TableNameFormat(tableName)) +
                            string.Format("WHERE Id = '{0}'", id);

            using (var updateCommand = new SqlCommand(deleteStr, cnn))
            {
                updateCommand.ExecuteNonQuery();
            }

        }

        private bool CheckDatabaseExists(string databaseName)
        {
            bool result = false;

            try
            {
                var sqlCreateDBQuery = string.Format("SELECT database_id FROM sys.databases WHERE Name = '{0}'", databaseName);
        
                    using (SqlCommand sqlCmd = new SqlCommand(sqlCreateDBQuery, cnn))
                    {
                        
                        object resultObj = sqlCmd.ExecuteScalar();

                        int databaseID = 0;

                        if (resultObj != null)
                        {
                            int.TryParse(resultObj.ToString(), out databaseID);
                        }
                        result = (databaseID > 0);
                    }
            }
            catch (Exception ex)
            {
                result = false;
            }

            return result;
        }

        private bool CheckIfTableExsits(string tableName)
        {
            bool exists;

            try
            {
                // ANSI SQL way.  Works in PostgreSQL, MSSQL, MySQL.  
                var sqlSelectDBQuery = string.Format(
                  "select case when exists((select * from information_schema.tables where table_name = '" + tableName + "')) then 1 else 0 end");

                using (SqlCommand sqlCmd = new SqlCommand(sqlSelectDBQuery, cnn))
                {
                    exists = (int)sqlCmd.ExecuteScalar() == 1;
                }
            }
            catch
            {
                try
                {
                    // Other RDBMS.  Graceful degradation
                    exists = true;
                    var sqlSelectDBQueryOthers = string.Format("select 1 from " + tableName + " where 1 = 0");
                    using (SqlCommand sqlCmd = new SqlCommand(sqlSelectDBQueryOthers, cnn))
                    {
                        sqlCmd.ExecuteScalar();
                    }
                }
                catch
                {
                    exists = false;
                }
            }
            return exists;
        }

        public string ToSeperatedCommaList(string[] values)
        {
            return string.Join(", ", values);
        }
    }
}
