/*--------------------------------------------------------------------------------------+
|
|   $Source: PipelineManager/PhysicalPipelineManager.cs $
|
|   $Copyright: (c) 2019 Bentley Systems, Incorporated. All rights reserved. $
|
+--------------------------------------------------------------------------------------*/
using Bentley.ECObjects.Schema;
using System;
using System.Collections.Generic;
using System.Data;

namespace Bentley.Plant.InstanceImporter
{
    public interface InstanceImport : IDisposable
    {
        List<ApplicationException> ApplicationExceptions { get; set; }
        void Sort();
        bool Import();
        bool Valid { get; set; }
        List<DataTable> TablesToImport { get; }

        List<ImportItem> ImportInstancesToCreate {get; set;}
    }
    public abstract class InstanceImportAdapter : InstanceImport
    {
        private IECClass m_PBSParentCls = null;
        protected string m_filetoImport = string.Empty;
        protected ImportReader m_importReader = null;
        protected List<DataTable> m_sortedDataTables = null;
        protected IECSchema m_AppSchema = null;

        #region public

        public InstanceImportAdapter(string filetoImport, IECSchema AppSchema)
        {
            m_filetoImport = filetoImport;
            m_AppSchema = AppSchema;
            m_importReader = new ImportReader(filetoImport);

            //
            SetIgnorePropertyNames();
            ApplicationExceptions = m_importReader.ApplicationExceptions;
            Valid = m_importReader.IsConnectionValid;
        }

        public virtual void Sort()
        {
            m_sortedDataTables = new List<DataTable>();
            foreach (DataTable dt in m_importReader.ImportDataSet.Tables)
            {
                if (string.IsNullOrEmpty(dt.TableName))
                    continue;

                if (IsAPBS(dt.TableName))
                    m_sortedDataTables.Insert(0, dt);
                else
                    m_sortedDataTables.Add(dt);
            }    
        }
 
        public virtual bool Import()
        {
            return false;
        }        

        public bool Valid { get; set; } = false;

        public List<DataTable> TablesToImport { get { return m_sortedDataTables; } }

        public List<ApplicationException> ApplicationExceptions { get; set; }

        public virtual List<string> IgnorePropertyName { get; set; }
        public virtual List<string> RelationshipNeededPropertyNames{get; set;}
        public virtual List<ImportItem> ImportInstancesToCreate {get; set;}

        public virtual ImportItem CreateImportInstance(DataRow row, string instanceIdName)
        {
            string clsName = row.Table.TableName;

            ImportItem importItem = CreateInstance(clsName,instanceIdName);
            if (importItem == null)
            {
                //log
                return null;
            }

            foreach (DataColumn column in row.Table.Columns)
            {
                string propertyName = column.ColumnName;
                object propertValue = row[column];

                if (IsAPBS(propertyName))  //if (RelationshipNeededPropertyNames.Contains(propertyName))
                {
                    importItem.RelationClassValueCache.Add(propertyName, propertValue);
                }

                if (!importItem.PropertyExists(propertyName) || IgnorePropertyName.Contains(propertyName))
                    continue;

                importItem.SetValue(propertyName, propertValue);
            }

            //this means that the NAME property was not defined or is empty
            if (!importItem.Valid)
                return null;

            return importItem;
        }

        #endregion

        #region protected
       protected bool IsAPBS(string clsName)
        {
            if (string.IsNullOrEmpty(clsName))
                return false;

            IECClass cls = m_AppSchema.GetClass(clsName);
            return cls != null && cls.Is(PBSParentCls) ? true : false;
        }

        protected IECClass PBSParentCls
        {
            get
            {
                if (m_PBSParentCls == null)
                    m_PBSParentCls=m_AppSchema.GetClass(Constants.PBSClassName);
                return m_PBSParentCls;
            }
        }
        protected IECClass GetClass(string clsName)
        {
            if (string.IsNullOrEmpty(clsName))
                return null;

            return m_AppSchema.GetClass(clsName);
        }

        protected bool ClassExists(string clsName)
        {
            return GetClass(clsName) == null ? false : true;
        }

        protected bool PropertyExists(string clsName, string propertyName)
        {
            IECClass cls = GetClass(clsName) ;
            if (cls == null) return false;

            return cls.FindProperty(propertyName) == null ? false : true;
        }
        protected bool PropertyExists(IECClass cls, string propertyName)
        {
            if (cls == null) return false;

            return cls.FindProperty(propertyName) == null ? false : true;
        }

        protected ImportItem CreateInstance(string clsName,string instanceIdName)
        {
            IECClass cls = GetClass(clsName);
            if (cls == null) return null;
            return new ImportItem(cls,instanceIdName);
        }
        #endregion

        #region private
        private void SetIgnorePropertyNames()
        {
            IgnorePropertyName = new List<string>();
            IgnorePropertyName.Add("UNIT_NAME");
            IgnorePropertyName.Add("SERVICE_NAME");
            IgnorePropertyName.Add("SERVICE");
            IgnorePropertyName.Add("UNIT");
            IgnorePropertyName.Add("PLANT_AREA");
        }

        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (m_importReader != null)
                        m_importReader.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
