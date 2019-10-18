using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bentley.Data.XDb;

namespace Bentley.Plant.InstanceImporter
{
    
    public class ImportReader : IDisposable
    {
        private XDbConnection m_XDbConnection = null;
        private string m_fileName = string.Empty;

        //may need to put XDbType in this class 
        //currently in D:\Connect\WIP\CurrDev\OPM\src\openplant\Modeler\spectools\XDbProvider\XDbEnvironment.cs)
        private XDbType m_XDbType = XDbType.NotSupported;

        public ImportReader(string fileName)
        {
            m_fileName = fileName;
            ApplicationExceptions = new List<ApplicationException>();
            try
            {
                Initialize();
            }
            catch (Exception ex)
            {
                ApplicationExceptions.Add(new ApplicationException(ex.Message, ex.InnerException));
            }
            if (m_XDbConnection == null)
                return;
        }

        public List<ApplicationException> ApplicationExceptions { get;private set; }
        public DataSet ImportDataSet { get; private set; }

        public bool IsConnectionValid
        {
            get
            {
                if (m_XDbConnection == null || m_XDbConnection.State == ConnectionState.Closed)
                    return false;

                return true;
            }
        }

        public DataSet ReadAndCreateDataSet()
        {
            BuildDataSetFromTableNames();
            return ImportDataSet;
        }

        private void BuildDataSetFromTableNames()
        {
            if (!IsConnectionValid)
                return;

            ImportDataSet = new DataSet("ImportTables");
            IList<string> tableNames = GetTableNames();
            foreach (string tableName in tableNames)
            {
                try
                {
                    DataTable table = GetDataTable(string.Format("SELECT {0} FROM [{1}]", "*", tableName));
                    table.TableName = SetTableName(tableName);
                    ImportDataSet.Tables.Add(table);
                }
                catch (Exception ex)
                {
                    ApplicationExceptions.Add(new ApplicationException(ex.Message, ex.InnerException));
                }
            }
        }

        private void Initialize()
        {
            if (string.IsNullOrEmpty(m_fileName) || !File.Exists(m_fileName))
            {
                string msg = string.Format("{0} does not exist or is null.", string.IsNullOrEmpty(m_fileName) ? "[Invalid file name passed in]" : m_fileName);
                throw new ApplicationException(msg);
            }

            string conStr = XDbEnvironment.GetConnectionString(m_fileName);
            m_XDbConnection = new XDbConnection(conStr);
            try
            {
                m_XDbConnection.Open();

            }
            catch (Exception ex)
            {
                m_XDbConnection = null; 
                ApplicationExceptions.Add(new ApplicationException(ex.Message, ex.InnerException));
                return;
            }
            m_XDbType = XDbEnvironment.XDbConnectedType;
        }

        private DataTable GetDataTable(string sql)
        {
            if (!IsConnectionValid)
                return null;

            using (DbCommand cmd = new XDbCommand(sql, m_XDbConnection))
            {
                using (DbDataReader rd = cmd.ExecuteReader())
                {
                    DataTable data = new DataTable();
                    data.Load(rd);
                    return data;
                }
            }
        }

        private IList<string> GetTableNames()
        {
            //this ensures that the XDbEnvironment.XDbConnectedType get set correctly
            XDbEnvironment.GetConnectionString(m_fileName);

            IList<string> tableNames = new List<string>();
            try
            {
                string[] restrictionValues = new string[4] { null, null, null, "TABLE" };
                using (DataTable tables = m_XDbConnection.GetSchema("Tables", restrictionValues))
                    {
                        foreach (DataRow table in tables.Rows)
                        {
                            string tableName = table["TABLE_NAME"].ToString();
                            if (IsTableNameFormatCorrect(tableName))
                                tableNames.Add(tableName);                            
                        }
                    }
            }
            catch (Exception ex)
            {
                string msg = string.Format("{0}\n{1}",Resources.ResourceMessages.InstallMsg,ex.Message);
                ApplicationExceptions.Add(new ApplicationException(msg, ex.InnerException));
            }
            return tableNames;
        }

        private string SetTableName (string tableName)
        {
            switch (XDbEnvironment.XDbConnectedType)
            {
                case XDbType.Excel:
                    return tableName.Replace("$", ""); //for excel
                case XDbType.Access:
                case XDbType.SQLite:
                    return tableName;
            }
            return tableName;
        }
        private bool IsTableNameFormatCorrect(string tableName)
        {
            switch (XDbEnvironment.XDbConnectedType)
            {
                case XDbType.Excel:
                    if (!tableName.Contains("$"))
                        return false;
                    string[] testSplit = tableName.Split('$');
                    if (testSplit.Length == 2 && (string.IsNullOrEmpty(testSplit[1]) || testSplit[1].Equals("'")))
                        return true;
                    return false;

                case XDbType.Access:
                case XDbType.SQLite:
                    return true;
            }
            return false;
        }

        public void Dispose()
        {
            if (m_XDbConnection == null)
                return;
            try
            {
                if (m_XDbConnection.State == ConnectionState.Open)
                    m_XDbConnection.Close();
                ImportDataSet.Clear();
                ImportDataSet.Dispose();
                ImportDataSet = null;
            }
            catch { }
            finally
            {
                m_XDbConnection = null;
            }          
        }
    }
}
