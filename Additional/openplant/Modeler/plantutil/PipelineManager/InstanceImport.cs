/*--------------------------------------------------------------------------------------+
|
|   $Source: PipelineManager/PhysicalPipelineManager.cs $
|
|   $Copyright: (c) 2019 Bentley Systems, Incorporated. All rights reserved. $
|
+--------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bentley.Plant.InstanceImporter;
using Bentley.ECObjects.Schema;
using Bentley.ECObjects.Instance;
using Bentley.ECObjects;
using Bentley.OpenPlant.Modeler.Api;
using Bentley.Plant.Utilities;
using System.Collections;

namespace Bentley.Plant.PipelineManager
{
    internal class InstanceImporter : InstanceImportAdapter
    {
        
        public InstanceImporter(string filetoImport, IECSchema AppSchema)
            : base(filetoImport,AppSchema)
        {

        }

        public override bool Import()
        {
            if (!Valid)
                return false;

            m_importReader.ReadAndCreateDataSet();
            bool status = DoOPMImport();

            ApplicationExceptions = m_importReader.ApplicationExceptions;
            this.Dispose();
            return status;
        }

        public override List<ImportItem> ImportInstancesToCreate {get; set;}

        private bool DoOPMImport()
        {
            //ensure PBS items are at top
            Sort();

            //
            ImportInstancesToCreate = new List<ImportItem>();
            foreach (DataTable dt in m_sortedDataTables)
            {
                List<ImportItem> importInsts = CreateImportInstances(dt);
                CreateImportItem(importInsts);
            }

            return true;
        }

        private void CreateImportItem(List<ImportItem> importItems)
        {
            BMECInstanceManager instanceManager = BMECApi.Instance.InstanceManager;
            foreach (ImportItem importItem in importItems)
            {
                if (string.IsNullOrEmpty(importItem.Key) || !importItem.Valid)
                {
                    //log-- this means there are blank records
                    continue;
                }
                System.Windows.Forms.Application.DoEvents ();
                ArrayList associations = new ArrayList();
                ArrayList propertyValues = new ArrayList();

                //use the instance to set the properties
                foreach (IECPropertyValue propertyValue in importItem.Instance)
                {
                    if (propertyValue.IsNull)
                        continue;
                    if (propertyValue.StringValue == null)
                        continue;
                    string pval = string.Format("{0}={1}", propertyValue.AccessString, propertyValue.StringValue);
                    propertyValues.Add(pval);
                }

                foreach (string rItem in importItem.RelationClassValueCache.Keys)
                {
                    string relatedVal = string.Format("{0}={1}", rItem, importItem.RelationClassValueCache[rItem]);
                    associations.Add(relatedVal);
                }
                string clsName = importItem.Instance.ClassDefinition.Name;
                if (associations.Count > 0)
                    WorkspaceUtilities.SaveSettings ("Associations." + clsName, associations);
                
                IECInstance ecInstance = instanceManager.CreateECInstance (clsName);
                BMECApi.Instance.SpecProcessor.FillCurrentPreferences(ecInstance, true);
                ECInstanceDialog ecDlg = new ECInstanceDialog(importItem.Instance);
                ecDlg.InitializeWithCustomProperties(associations, propertyValues, true);
                ecDlg.SaveData(false);
            }

        }

        private List<ImportItem> CreateImportInstances(DataTable dt)
        {
            string clsName = dt.TableName;
            if (!ClassExists(clsName))
                return null;

            List<ImportItem> instancesToCreate = new List<ImportItem>();
            foreach (DataRow row in dt.Rows)
            {

                ImportItem importItem = base.CreateImportInstance(row, "mechanical");
                if (importItem != null)
                    instancesToCreate.Add(importItem);
            }
            return instancesToCreate;
        }

    }
}
