/*--------------------------------------------------------------------------------------+
|
|   $Source: PipelineManager/PhysicalPipelineManager.cs $
|
|   $Copyright: (c) 2019 Bentley Systems, Incorporated. All rights reserved. $
|
+--------------------------------------------------------------------------------------*/

using Bentley.ECObjects.Instance;
using Bentley.ECObjects.Schema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bentley.Plant.InstanceImporter
{
    public class ImportItem
    {

       
        public ImportItem(IECClass cls,string instanceIdName)
        {
            Instance = cls.CreateInstance();
            RelationClassValueCache = new Dictionary<string, object> ();
            Instance.InstanceId = instanceIdName;
            Valid = true;
        }

        public string Key { get; set; }
        public bool Valid { get; set; } = false;

        public IECInstance Instance { get; set; }

        public IDictionary<string, object> RelationClassValueCache { get; set; }

        public bool PropertyExists(string propertyName)
        {
            return Instance.ClassDefinition.FindProperty(propertyName) == null ? false : true;
        }

        public void SetValue(string propertyName, object val)
        {
            if (val == null) return;
            string propVal = val.ToString();
            try
            {
                IECType type = Instance[propertyName].Type;
                if (ECTypeHelper.IsDouble(type))
                {
                    double dd = 0.0;
                    if (double.TryParse(propVal, out dd))
                        Instance[propertyName].NativeValue = dd;
                }
                else if (ECTypeHelper.IsString(type))
                {
                    if (propertyName.Equals(Constants.KeyPropertyName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        //no NAME is set therefore Import item is Invalid
                        if (string.IsNullOrEmpty(propVal))
                            Valid = false;
                        Key = propVal;
                    }

                    //TODO check for NUMBER format
                    if (propertyName.Equals(Constants.NumberPropertyName, StringComparison.InvariantCultureIgnoreCase))
                    {

                    }

                    Instance[propertyName].NativeValue = propVal;
                }
                else if (ECTypeHelper.IsBoolean(type))
                    Instance[propertyName].NativeValue = Convert.ToBoolean(val);
                else if (ECTypeHelper.IsInteger(type))
                {
                    int ii = 0;
                    if (int.TryParse(propVal, out ii))
                        Instance[propertyName].NativeValue = ii;
                }
                else
                    Instance[propertyName].NativeValue = val;
            }
            catch
            {
            }
       }

    }
}
