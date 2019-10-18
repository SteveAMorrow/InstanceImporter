using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Win32;
using System.Globalization;
using System.Reflection;

namespace Bentley.Data.XDb
{

    /// <summary>Data types bil file ext</summary>
    public enum XDbType
    {
        Excel,
        Access,
        SQLite,
        NotSupported
    }

    /// <summary>
    /// Represents a set of global static data and methods.
    /// </summary>
    public static class XDbEnvironment
    {

        /// <summary>Flag indicating whether linked server is used for accessing MDB database.</summary>
        internal static bool _useLinkedSrv = false;

        /// <summary>Gets true if linked server is used for accessing MDB database.</summary>
        public static bool LinkedServerMode { get { return _useLinkedSrv; } }
            
        /// <summary>Name of SQL server instance that is used to access MDB database.</summary>
        private const string _sqlServerName = "SQLEXPRESSAB381";

        /// <summary>Username.</summary>
        private const string _sqlUserName = "sa";

        /// <summary>Password.</summary>
        private const string _sqlPassword = "s6kek956jhd833vb43423ahGHrd";

        private const bool _useWindowsAuthentication = false;

        private static string _linkedServerConnectionString;

        private const string _aceOledbProviderName = "Microsoft.ACE.OLEDB.12.0";
        private const string _aceOledbClsId = "{3BE786A0-0366-4F5C-9434-25CF162E475E}";

        private static string _providerName = _aceOledbProviderName;

        /// <summary>Dictionary to used linked server names. 
        /// Key is path to mdb/accdb file, value is linked server name.
        /// </summary>
        private static Dictionary<string, string> _linkedSrvNameDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static Dictionary<string, List<string>> _memoTables = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);       

        /// <summary>Static constructor.</summary>
        static XDbEnvironment()
        {
            _linkedServerConnectionString = _useWindowsAuthentication ?
                            string.Format(@"server =.\{0}; Integrated Security = SSPI;", _sqlServerName) :
                            string.Format(@"server =.\{0}; User Id={1}; Password={2};", _sqlServerName, _sqlUserName, _sqlPassword);

            _useLinkedSrv = false;
            if (IsAccessDriver64Installed())
                return;
            if (IsSqlServerExpressInstalled())
                _useLinkedSrv = true;
        }

        /// <summary>
        /// Checks if access driver or proper SQL Server is installed, otherwise exception.
        /// </summary>
        public static void Initialize()
        {
            if (!IsAccessDriver64Installed() && !IsSqlServerExpressInstalled())
                throw new ApplicationException(string.Format("Access driver for 64 bit or SQL Server Express with name='{0}' is not found", _sqlServerName));
        }

        /// <summary>
        /// Get linked server name by mdb/accdb file path.
        /// </summary>
        /// <param name="sMDBPath"></param>
        /// <returns></returns>
        public static string GetLinkedSrvName(string sMDBPath)
        {
            string sLinkedSrvName;
            if (_linkedSrvNameDictionary.TryGetValue(sMDBPath, out sLinkedSrvName))
                return sLinkedSrvName;
            sLinkedSrvName = CalculateLinkedSrvName(sMDBPath);
            CheckSqlLinkedServer(sMDBPath);
            _linkedSrvNameDictionary[sMDBPath] = sLinkedSrvName;

            // Evaluate memo tables cache.
            List<string> memoLst = new List<string>();
            using (XDbConnection con = new XDbConnection(GetAceConnectString(sMDBPath)))
            {
                if (!_memoTables.TryGetValue(CalculateLinkedSrvName(sMDBPath), out memoLst))
                {
                    con.Open();
                    _memoTables[CalculateLinkedSrvName(sMDBPath)] = GetMemoTableList(con);
                }
            }

            return sLinkedSrvName;
        }

        /// <summary>
        /// Returns a dictionary with list of the names of tables, which have memo fields. The key is a mdb file path.
        /// </summary>
        public static Dictionary<string, List<string>> MemoTables
        {
            get { return _memoTables; }
        }

        private static string CalculateLinkedSrvName(string sMDBPath)
        {
            return Path.GetExtension(sMDBPath).TrimStart('.').ToUpper() + 
                   string.Format("_{0:X8}", sMDBPath.ToLower().GetHashCode());
        }

        /// <summary>
        /// Get connection string.
        /// </summary>
        /// <param name="inputFile">Path to mdb/accdb file.</param>
        /// <returns>Connection string</returns>
        public static string GetConnectionString(string inputFile)
        {
            if (LinkedServerMode && !IsSQLiteDatabase(inputFile)) 
                return _linkedServerConnectionString;
            return GetAceConnectString(inputFile);
        }

        /// <summary>
        /// Get ACE connection string by mdb/accdb file.
        /// </summary>
        /// <param name="mdbPath">Path to mdb/accdb file.</param>
        /// <returns></returns>
        public static string GetAceConnectString(string mdbPath)
        {
            XDbConnectedType = XDbType.NotSupported;
            if (string.IsNullOrEmpty(mdbPath) || !IsSupportedFileType(mdbPath))
                throw new ArgumentException("Wrong path to access/excel file");

            string extProp = "";

            //by default (because of code structure) set to access
            //DO NOT SET at bottom
            XDbConnectedType = XDbType.Access; 

            // HDR=yes by default, can be removed
            if (IsNewExcelFileType(mdbPath))
            {
                extProp = " ;Extended Properties = \"Excel 12.0 Xml;HDR=YES\"";
                XDbConnectedType = XDbType.Excel;
            }

            if (IsOldExcelFileType(mdbPath))
            {
                extProp = " ;Extended Properties = \"Excel 8.0;HDR=YES\"";
                XDbConnectedType = XDbType.Excel;
            }

            if (IsSQLiteDatabase(mdbPath))
            {
                XDbConnectedType = XDbType.SQLite;
                return "Data Source=" + mdbPath + ";Version=3";
            }

            //do not
            return "Provider=" + _providerName + "; Data Source=" + mdbPath + extProp;
        }

        public static XDbType XDbConnectedType { get; private set; } = XDbType.NotSupported;
        

        internal static bool IsSupportedFileType(string mdbPath)
        {
            string low = mdbPath.ToLower();
            return low.Contains(".mdb") || low.Contains(".accdb") || IsSQLiteDatabase(mdbPath) || IsNewExcelFileType(mdbPath) || IsOldExcelFileType(mdbPath);
        }

        internal static bool IsNewExcelFileType(string mdbPath)
        {
            string low = mdbPath.ToLower();
            return low.Contains(".xlsx") || low.Contains(".xlsb");
        }
        internal static bool IsOldExcelFileType(string mdbPath)
        {
            string low = mdbPath.ToLower();
            return low.Contains(".xls") ;
        }

        public static bool IsSQLiteDatabase(string mdbPath)
        {
            string low = mdbPath.ToLower();
            return low.Contains(".db") || low.Contains(".db3") || low.Contains(".sqlite") || low.Contains(".sqlite3");
        }

        private static bool IsAccessDriver64Installed()
        {
            try
            {
                OleDbEnumerator enumerator = new OleDbEnumerator();
                DataTable table = enumerator.GetElements();
                bool bNameFound = false;
                bool bCLSIDFound = false;

                foreach (DataRow row in table.Rows)
                {
                    foreach (DataColumn col in table.Columns)
                    {
                        if ((col.ColumnName.Contains("SOURCES_NAME")) && (row[col].ToString().Contains(_aceOledbProviderName)))
                            bNameFound = true;
                        if ((col.ColumnName.Contains("SOURCES_CLSID")) && (row[col].ToString().Contains(_aceOledbClsId)))
                            bCLSIDFound = true;
                    }
                }
                return bNameFound && bCLSIDFound;
            }
            catch
            {
                return false;
            }
        }


        private static bool IsSqlServerExpressInstalled()
        {
            try
            {
                //check if sqlserver exists
                using (RegistryKey reg = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Microsoft SQL Server\\" + _sqlServerName, false))
                {
                    if (reg != null)
                    {
                        reg.Close();
                        return true;
                    }
                }
            }
            catch 
            {
                Debug.Fail("Cannot open registry key");
            }
            return false;
        }

        private static List<string> GetMemoTableList(XDbConnection con)
        {
            List<string> lst = new List<string>();
            DataTable dt = new DataTable();
            
            string commandString = string.Format("Exec SP_COLUMNS_EX N'{0}'", XDbEnvironment.CalculateLinkedSrvName(con.InputFile));
            using (DbCommand comm = new XDbCommand(commandString, con))
            {
                using (DbDataReader reader = comm.ExecuteReader())
                {
                    dt.Load(reader, LoadOption.OverwriteChanges);
                }

                // Go through the table rows and search for the memo fields( TYPE_NAME = VarChar && COLUMN_SIZE = 0 )
                foreach (DataRow row in dt.Rows)
                {
                    if ((row["TYPE_NAME"].ToString() == "VarChar") && (row["COLUMN_SIZE"].ToString() == "0"))
                        if (!lst.Contains(row["TABLE_NAME"].ToString()))
                            lst.Add(row["TABLE_NAME"].ToString());
                }
            }
            return lst;
        }

        /// <summary>
        /// Check existence of linked server for specified MDB database and create one if needed.
        /// </summary>
        /// <param name="sMDBPath">Path to MDB database</param>
        private static bool CheckSqlLinkedServer(string sMDBPath)
        {
            string sLinkedSrvName;
            if (_linkedSrvNameDictionary.TryGetValue(sMDBPath, out sLinkedSrvName))
                return true;

            if (!IsSqlServerExpressInstalled())
                throw new ApplicationException("SqlExpress Server was not installed. Please, install SqlExpress 2014 or Microsoft Access drivers");

            if (LinkedServerMode == false)
                throw new ApplicationException("Mode to use linked server is not set!");

            if (string.IsNullOrEmpty(sMDBPath))
                throw new ArgumentException("Path to mdb file is empty!");
            if (!File.Exists(sMDBPath))
                throw new ArgumentException("File path is not found: " + sMDBPath);

            try
            {
                sLinkedSrvName = CalculateLinkedSrvName(sMDBPath);
                using (SqlConnection con = new SqlConnection(GetConnectionString(sMDBPath)))
                {
                    con.Open();

                    SqlCommand comm = new SqlCommand(string.Format("select datasource from sysservers where srvname = '{0}'",
                                                     sLinkedSrvName), con);
                    string dbname = (string)comm.ExecuteScalar();
                    if (dbname == null || !dbname.Equals(sMDBPath, StringComparison.OrdinalIgnoreCase))
                        ExecuteScriptForLinkedServerCreation(sMDBPath, sLinkedSrvName);
                    con.Close();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }


        private static void ExecuteScriptForLinkedServerCreation(string sMDBPath, string linkedSrvName)
        {
            string tmpFileName = Path.GetTempFileName();
            tmpFileName = Path.ChangeExtension(tmpFileName, "sql");
            bool excel = false;
            string excelVersion = "Excel 12.0";
            if (IsNewExcelFileType(sMDBPath))
                excel = true;
            else if (IsOldExcelFileType(sMDBPath))
            {
                excel = true;
                excelVersion = "Excel 8.0";
            }
            string query = string.Format(Properties.Resources.script, linkedSrvName, sMDBPath, _providerName,
                       excel ? " ,@provstr=N'" + excelVersion + ";HDR=Yes'" : "",
                       excel ? excelVersion : "Access");
            File.WriteAllText(tmpFileName, query);
            Execute(tmpFileName);
            if (File.Exists(tmpFileName))
                File.Delete(tmpFileName);
        }


        private static string Execute(string scriptPath)
        {
            using (Process process = new Process())
            {
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.FileName = "sqlcmd.exe";
                process.StartInfo.Arguments = GetSqlcmdCommand(string.Format("-i \"{0}\"", scriptPath), true).ToString();
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output;
            }
        }

        /// <summary>
        /// Create Sql command line
        /// </summary>
        private static StringBuilder GetSqlcmdCommand(string sqlCmd, bool isParam)
        {
            var cmd = new StringBuilder("-r -b ");

            if (!_useWindowsAuthentication)
              cmd.Append("-U ").Append(_sqlUserName)
                 .Append(" -P ").Append(_sqlPassword);

            cmd.Append(" -S ").Append(string.Format(@".\{0} ", _sqlServerName));

            if (isParam)
            {
                cmd.Append(sqlCmd);
            }
            else
            {
                cmd
                .Append(" -Q \"")
                .Append(sqlCmd)
                .Append("\"");
            };

            return cmd;
        }

        /// <summary>
        /// Create empty mdb file.
        /// </summary>
        /// <param name="sMDBPath">Path to mdb file to create.</param>
        public static void CreateEmptyMdbFile(string sMDBPath)
        {
            object obj = Properties.Resources.ResourceManager.GetObject("template", Properties.Resources.Culture);
            File.WriteAllBytes(sMDBPath, (byte[])(obj));
        }

        /// <summary>
        /// Create empty mdb file in tmp.
        /// </summary>
        /// <returns>Return file path to temporary mdb file.</returns>
        public static string CreateEmptyMdbFile()
        {
            string tmpFileName = Path.GetTempFileName();
            tmpFileName = Path.ChangeExtension(tmpFileName, "mdb");
            CreateEmptyMdbFile(tmpFileName);
            return tmpFileName;
        }
    }

}
