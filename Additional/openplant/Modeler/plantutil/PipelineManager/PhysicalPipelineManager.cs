/*--------------------------------------------------------------------------------------+
|
|   $Source: PipelineManager/PhysicalPipelineManager.cs $
|
|   $Copyright: (c) 2019 Bentley Systems, Incorporated. All rights reserved. $
|
+--------------------------------------------------------------------------------------*/
using Bentley.OpenPlant.Modeler.Api;
using Bentley.ECObjects.Instance;
using Bentley.ECObjects.Schema;
using Bentley.Plant.Utilities;
using OPMCommon;
using System;
using System.Collections;
using System.Windows.Forms;
using Bentley.MstnPlatformNET;
using Bentley.Connect.Client.API.V2;
using Bentley.Plant.InstanceImporter;

namespace Bentley.Plant
    {
    namespace PipelineManager
        {

        [MstnPlatformNET.AddInAttribute (MdlTaskID = "PipelineManager"/*, KeyinTree = "PipelineManager.commands.xml"*/)]
        public sealed class PhysicalPipelineManager : AddIn
            {
            static DlgPipelineManager s_PipelineManagerDialog = null;
            static PipelineManager2 s_PipelineManager2 = null;
            static DlgConnetivtyReport s_ConnectivityDialog = null;
            static LogConnectivityReport s_ConnectivityLog = null;
            static DlgReplaceComponent s_ReplaceCompDialog = null;
            private static ConnectClientAPI connectApi = new ConnectClientAPI();
            //VANCOUVER_PORT_NEED_WORK
            //static OpenPlantRSSFeed         s_RSSFeedDialog         = null;
            /*------------------------------------------------------------------------------------**/
            /// <summary>
            /// PhysicalPipelineManager
            /// </summary>
            /// <param name="mdldesc"></param>
            /*--------------+---------------+---------------+---------------+---------------+------*/
            private PhysicalPipelineManager
            (
            IntPtr mdldesc
            ) : base (mdldesc)
                {
                }

            /*------------------------------------------------------------------------------------**/
            /// <summary>
            /// Run
            /// </summary>
            /// <param name="commandLine"></param>
            /// <returns></returns>
            /*--------------+---------------+---------------+---------------+---------------+------*/
            protected override int Run
            (
            string[] commandLine
            )
                {
                NewDesignFileEvent += new NewDesignFileEventHandler (Addin_NewDesignFileEvent);
                return 0; //SUCCESS
                }

            /*------------------------------------------------------------------------------------**/
            /// <summary>
            /// Addin_NewDesignFileEvent
            /// Used to reload Connectivity and/or Pipeline Manager dialogs
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="eventArgs"></param>
            /*--------------+---------------+---------------+---------------+---------------+------*/
            private void Addin_NewDesignFileEvent(AddIn sender, NewDesignFileEventArgs eventArgs)
                {
                if(eventArgs.WhenCode == NewDesignFileEventArgs.When.AfterDesignFileOpen)
                    {
                    //Reload contents on drawing reload
                    if(ReloadOnDGNChange)
                        {
                        //Reload Check Connectivity
                        if(s_ConnectivityDialog != null)
                            CheckConnectivity (string.Empty);

                        //Reload Pipeline manager 
                        if(s_PipelineManager2 != null)
                            ShowPipelineManager2 (string.Empty);
                        }
                    }
                }

            /*------------------------------------------------------------------------------------**/
            /// <summary>
            /// Variable used to disable reload Connectivity and/or Pipeline Manager dialogs
            /// By Default this option is on.
            /// </summary>
            /*--------------+---------------+---------------+---------------+---------------+------*/
            private static bool ReloadOnDGNChange
                {
                get
                    {
                    string cfg = WorkspaceUtilities.GetConfigVar ("OPMReloadOnDGNCHange");
                    switch (cfg.ToLower())
                        {
                        case "":
                        case "1":
                        case "yes":
                        case "on":
                            return true;
                        }
                    return false;
                    }
                }

            /*------------------------------------------------------------------------------------**/
            /// <summary>
            /// ShowPipelineManager
            /// </summary>
            /// <param name="unparsed"></param>
            /*--------------+---------------+---------------+---------------+---------------+------*/
            public static void ShowPipelineManager
            (
            string unparsed
            )
                {
                if(s_PipelineManagerDialog == null)
                    {
                    PipelineManagerMode.Mode = PipelineManagerAvailableModes.Piping;
                    if(unparsed == OPMCommon.ClassNames.TRAY_COMPONENT)
                        PipelineManagerMode.Mode = PipelineManagerAvailableModes.CableTray;
                    else if(unparsed == "HVAC_COMPONENT")
                        PipelineManagerMode.Mode = PipelineManagerAvailableModes.HVAC;

                    s_PipelineManagerDialog = new DlgPipelineManager ();
                    s_PipelineManagerDialog.FormClosed += new FormClosedEventHandler (PipelineManagerDialog_FormClosed);
                    PipelineUtilities.IsPipelineManagerRunning = true;
                    }
                s_PipelineManagerDialog.Show ();
                s_PipelineManagerDialog.BringToFront();
                }

            /*------------------------------------------------------------------------------------**/
            /// <summary>
            /// Maximize HVAC OR TRAY Manager 
            /// </summary>
            /// <param name="unparsed"></param>
            /*--------------+---------------+---------------+---------------+---------------+------*/
            public static void MaximizeHvacORTrayManager
            (
            string unparsed
            )
                {
                if (s_PipelineManagerDialog == null)
                    {
                    PipelineManagerMode.Mode = PipelineManagerAvailableModes.Piping;
                    if (unparsed == OPMCommon.ClassNames.TRAY_COMPONENT)
                        PipelineManagerMode.Mode = PipelineManagerAvailableModes.CableTray;
                    else if (unparsed == "HVAC_COMPONENT")
                        PipelineManagerMode.Mode = PipelineManagerAvailableModes.HVAC;

                    s_PipelineManagerDialog = new DlgPipelineManager();
                    s_PipelineManagerDialog.FormClosed += new FormClosedEventHandler(PipelineManagerDialog_FormClosed);
                    PipelineUtilities.IsPipelineManagerRunning = true;
                    }
                s_PipelineManagerDialog.Maximize();
                s_PipelineManagerDialog.Show();
                }

            /*------------------------------------------------------------------------------------**/
            /// <summary>
            /// ShowPipelineManager2
            /// </summary>
            /// <param name="unparsed"></param>
            /*--------------+---------------+---------------+---------------+---------------+------*/
            public static void ShowPipelineManager2
            (
            string unparsed
            )
                {
                PipelineManagerMode.Mode = PipelineManagerAvailableModes.Piping;
                if(s_PipelineManager2 == null)
                    {
                    s_PipelineManager2 = new PipelineManager2 ();
                    s_PipelineManager2.FormClosed += new FormClosedEventHandler (PipelineManagerDialog2_FormClosed);
                    PipelineUtilities.IsPipelineManagerRunning = true;
                    }
                else
                    {
                    PipelineUtilities.IsPipelineManagerRunning = false;
                    s_PipelineManager2.BringToFront();
                    s_PipelineManager2.CloseModeless ();
                    s_PipelineManager2 = null;
                    s_PipelineManager2 = new PipelineManager2 ();
                    s_PipelineManager2.FormClosed += new FormClosedEventHandler (PipelineManagerDialog2_FormClosed);
                    PipelineUtilities.IsPipelineManagerRunning = true;
                    }
                //this value is used with editting a value from the duplicate tag dialog
                s_PipelineManager2.SelectedInstanceId = unparsed;
                s_PipelineManager2.Show ();
                }

            /*------------------------------------------------------------------------------------**/
            /// <summary>
            ///  Maximize PipeLine Manager 2
            /// </summary>
            /// <param name="unparsed"></param>
            /*--------------+---------------+---------------+---------------+---------------+------*/
            public static void MaximizeManager
            (
            string unparsed
            )
                {
                PipelineManagerMode.Mode = PipelineManagerAvailableModes.Piping;
                if (s_PipelineManager2 == null)
                    {
                    s_PipelineManager2 = new PipelineManager2();
                    s_PipelineManager2.FormClosed += new FormClosedEventHandler(PipelineManagerDialog2_FormClosed);
                    PipelineUtilities.IsPipelineManagerRunning = true;
                    }
                s_PipelineManager2.Maximize();
                s_PipelineManager2.Show();
                }

            /*------------------------------------------------------------------------------------**/
            /// <summary>
            /// ShowJointAssocDlg
            /// </summary>
            /// <param name="unparsed"></param>
            /*--------------+---------------+---------------+---------------+---------------+------*/
            public static void ShowJointAssocDlg
            (
            string unparsed
            )
                {
                unparsed = unparsed.ToUpper ().Trim ();
                JointAssociationDialogType type = JointAssociationDialogType.Pipeline;

                if(String.Compare (unparsed, "ISOSHEET") == 0)
                    type = JointAssociationDialogType.ISOSheet;

                JointAssociationsDlg.LoadDialog (type, null);
                }

            /*------------------------------------------------------------------------------------**/
            /// <summary>
            /// IsImportFileValidFormat
            /// </summary>
            /// <param name="fileName"></param>
            /// <param name="ecClass"></param>
            /// <returns></returns>
            /*--------------+---------------+---------------+---------------+---------------+------*/
            private static bool IsImportFileValidFormat
            (
            string fileName,
            IECClass ecClass
            )
                {
                bool status = true;
                bool assoicationFound = false;
                bool instanceFound = false;
                int lineNumber = 0;

                ArrayList arr = SchemaUtilities.GetAssociatedItems (ecClass);
                bool lookForAssociations = (arr.Count > 1);
                System.IO.StreamReader sr = System.IO.File.OpenText (fileName);

                try
                    {
                    while(!sr.EndOfStream)
                        {
                        string lineRead = sr.ReadLine ().Trim ();
                        lineNumber++;

                        if(string.IsNullOrEmpty (lineRead))
                            continue;

                        string testString = lineRead.Replace ("[Associations]", "|");
                        testString = testString.Replace ("[InstanceData]", "|");
                        testString = testString.Replace ("=", "|");

                        if(!testString.Contains ("|"))
                            {
                            WorkspaceUtilities.DisplayErrorMessage (string.Format (LocalizableStrings.InvalidString, lineRead, lineNumber), "");
                            status = false;
                            break;
                            }

                        else if(lineRead == "[Associations]" && !lookForAssociations)
                            {
                            WorkspaceUtilities.DisplayErrorMessage (string.Format (LocalizableStrings.NoAssociationRequired, lineNumber), "");
                            status = false;
                            break;
                            }

                        else if(lineRead == "[InstanceData]" && lookForAssociations && !assoicationFound)
                            {
                            WorkspaceUtilities.DisplayErrorMessage (string.Format (LocalizableStrings.AssociationsNotDefined, ecClass.Name, lineNumber), "");
                            status = false;
                            break;
                            }

                        //Validate Associations
                        if(lineRead == "[Associations]")
                            {
                            while(!sr.EndOfStream)
                                {
                                lineRead = sr.ReadLine ().Trim ();
                                lineNumber++;
                                if(string.IsNullOrEmpty (lineRead))
                                    continue;

                                if(lineRead == "[InstanceData]")
                                    break;

                                string[] strs = lineRead.Split (new char[] { '=' });
                                IECClass ecAssocateClass = SchemaUtilities.PlantSchema.GetClass (strs[0]);
                                if(ecAssocateClass == null)
                                    {
                                    WorkspaceUtilities.DisplayErrorMessage (string.Format (LocalizableStrings.InvalidAssociationClass, lineNumber), "");
                                    status = false;
                                    break;
                                    }
                                }
                            assoicationFound = true;
                            }

                        if(status == false)
                            break;

                        //Validate Instance Data
                        if(lineRead == "[InstanceData]")
                            {
                            while(!sr.EndOfStream)
                                {
                                lineRead = sr.ReadLine ().Trim ();
                                lineNumber++;
                                if(lineRead == "[InstanceData]")
                                    break;

                                if(lineRead == "[Associations]")
                                    break;

                                if(string.IsNullOrEmpty (lineRead))
                                    continue;

                                string[] strs = lineRead.Split (new char[] { '=' });
                                IECProperty ecProp = ecClass[strs[0]];
                                if(ecProp == null)
                                    {
                                    WorkspaceUtilities.DisplayErrorMessage (string.Format (LocalizableStrings.InvalidPropertyName, lineNumber), "");
                                    status = false;
                                    break;
                                    }
                                }
                            instanceFound = true;
                            }
                        if(status == false)
                            break;
                        }
                    }
                finally
                    {
                    sr.Close ();
                    }

                return status && instanceFound;
                }

            /*------------------------------------------------------------------------------------**/
            /// <summary>
            /// RSSFeed
            /// </summary>
            /// <param name="unparsed"></param>
            /*--------------+---------------+---------------+---------------+---------------+------*/
            public static void RSSFeed
            (
            string unparsed
            )
            {
                //VANCOUVER_PORT_NEED_WORK
                //if (s_RSSFeedDialog == null)
                //{
                //    s_RSSFeedDialog = new OpenPlantRSSFeed();
                //    s_RSSFeedDialog.FormClosed += new FormClosedEventHandler(s_RSSFeedDialog_FormClosed);
                //}
                //s_RSSFeedDialog.Show();
            }

            public static string getProjectPortalUrl(Guid projectId)
            {
                try
                {
                    //Gets the url of Project portal from specified ProjectId
                    string url = connectApi.GetProjectHomepage(projectId.ToString());

                    // Replace the url in passive state to bypass the login screen if user is already login
                    url = connectApi.GetA2PUrl(url);

                    return url;
                }
                catch (Exception exp)
                {
                    string msg = exp.Message;
                }
                return "";
            }

            /*------------------------------------------------------------------------------------**/
            /// <summary>
            /// OpenProjectPortal
            /// </summary>
            /// <param name="unparsed"></param>
            /*--------------+---------------+---------------+---------------+---------------+------*/
            public static void OpenProjectPortal
            (
            string unparsed
            )
            {
                try
                {
                    string cfgProj = WorkspaceUtilities.GetConfigVar("CONNECTPROJECTGUID");
                    if (string.IsNullOrWhiteSpace(cfgProj))
                        return;
                    string url = getProjectPortalUrl(new System.Guid (cfgProj));
                    System.Diagnostics.Process.Start(url);
                }
                catch (Exception ex)
                {
                    BMECApi.Instance.
                        SendToMessageCenterD(Bentley.DgnPlatformNET.OutputMessagePriority.Error, ex.Message);

                }
            }

            /*------------------------------------------------------------------------------------**/
            /// <summary>
            /// OpenPlantSight
            /// </summary>
            /// <param name="unparsed"></param>
            /*--------------+---------------+---------------+---------------+---------------+------*/
            public static void OpenPlantSight
            (
            string unparsed
            )
                {
                try
                    {
                    System.Diagnostics.Process.Start(connectApi.GetBuddiUrl("PLANTSIGHT_PORTAL"));
                    }
                catch (Exception ex)
                {
                    BMECApi.Instance.
                        SendToMessageCenterD(Bentley.DgnPlatformNET.OutputMessagePriority.Error, ex.Message);

                }
            }

            /*------------------------------------------------------------------------------------**/
            /// <summary>
            /// s_RSSFeedDialog_FormClosed
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            /*--------------+---------------+---------------+---------------+---------------+------*/
            static void s_RSSFeedDialog_FormClosed(object sender, FormClosedEventArgs e)
                {
                //VANCOUVER_PORT_NEED_WORK
                //s_RSSFeedDialog.FormClosed -= new FormClosedEventHandler (s_RSSFeedDialog_FormClosed);
                //s_RSSFeedDialog = null;
                }
            /*------------------------------------------------------------------------------------**/
            /// <summary>
            /// Import
            /// </summary>
            /// <param name="unparsed"></param>
            /*--------------+---------------+---------------+---------------+---------------+------*/
            public static void Import
            (
            string unparsed
            )
                {
                if(string.IsNullOrEmpty (unparsed))
                    {
                    WorkspaceUtilities.DisplayErrorMessage (LocalizableStrings.BadImportCommand, LocalizableStrings.ImportCommandFormat);
                    return;
                    }

                BMECApi.Instance.ClearActivePlacementTool (); // exit current placement tool
                IECClass ecClass = SchemaUtilities.PlantSchema.GetClass (unparsed.Trim ());
                if(ecClass == null)
                    {
                    WorkspaceUtilities.DisplayErrorMessage (string.Format (LocalizableStrings.BadClassName, unparsed.Trim ()), "");
                    return;
                    }

                BMECInstanceManager instanceManager = BMECApi.Instance.InstanceManager;

                OpenFileDialog openFileDlg = new OpenFileDialog ();
                openFileDlg.Multiselect = false;
                openFileDlg.Filter = "Import data file|*.txt";
                openFileDlg.ShowDialog ();
                if(string.IsNullOrEmpty (openFileDlg.FileName))
                    return;

                if(!IsImportFileValidFormat (openFileDlg.FileName, ecClass))
                    return;

                System.IO.StreamReader sr = System.IO.File.OpenText (openFileDlg.FileName);

                ArrayList associations = new ArrayList ();
                ArrayList propertyValues = new ArrayList ();


                WaitDialog wtDlg = new WaitDialog ();
                wtDlg.SetTitleString (LocalizableStrings.ImportingInstances);
                wtDlg.SetInformationSting (LocalizableStrings.ImportingInstancesInfo);
                wtDlg.Show ();

                string lineRead = sr.ReadLine ().Trim ();
                while(!sr.EndOfStream)
                    {
                    Application.DoEvents ();
                    if(string.IsNullOrEmpty (lineRead))
                        {
                        lineRead = sr.ReadLine ().Trim ();
                        continue;
                        }
                    if(lineRead == "[Associations]")
                        {
                        associations.Clear ();
                        while(!sr.EndOfStream)
                            {
                            lineRead = sr.ReadLine ().Trim ();
                            if(string.IsNullOrEmpty (lineRead))
                                continue;

                            if(lineRead == "[InstanceData]")
                                break;

                            associations.Add (lineRead);
                            }
                        }

                    if(lineRead == "[InstanceData]")
                        {
                        propertyValues.Clear ();
                        while(!sr.EndOfStream)
                            {
                            lineRead = sr.ReadLine ().Trim ();
                            if(lineRead == "[InstanceData]")
                                break;

                            if(lineRead == "[Associations]")
                                break;

                            if(string.IsNullOrEmpty (lineRead))
                                continue;

                            propertyValues.Add (lineRead);
                            }

                        IECInstance ecInstance = instanceManager.CreateECInstance (ecClass.Name);
                        BMECApi.Instance.SpecProcessor.FillCurrentPreferences (ecInstance, true);
                        ecInstance.InstanceId = "mechanical";
                        ECInstanceDialog ecDlg = new ECInstanceDialog (ecInstance);
                        ecDlg.InitializeWithCustomProperties (associations, propertyValues, true);
                        ecDlg.SaveData ();
                        }
                    }
                wtDlg.Close ();
                sr.Close ();
                }

            /*------------------------------------------------------------------------------------**/
            /// <summary>
            /// Import
            /// </summary>
            /// <param name="unparsed"></param>
            /*--------------+---------------+---------------+---------------+---------------+------*/
            public static void ImportExcel
            (
            string unparsed
            )
                {

                BMECApi.Instance.ClearActivePlacementTool (); // exit current placement tool
                BMECInstanceManager instanceManager = BMECApi.Instance.InstanceManager;

                OpenFileDialog openFileDlg = new OpenFileDialog ();
                openFileDlg.Multiselect = false;
                openFileDlg.Filter = "Import file|*.xls;*.xlsx;*.mdb;*.accdb";
                //openFileDlg.Filter = "Import Excel file|*.xls;*.xlsx";
                openFileDlg.ShowDialog ();
                if(string.IsNullOrEmpty (openFileDlg.FileName))
                    return;

                try
                {
                    UI.Controls.WinForms.WaitForm.ShowForm(LocalizableStrings.ImportingInstances, LocalizableStrings.PleaseWaitImporting);
                    InstanceImporter opmAdp = new InstanceImporter(openFileDlg.FileName, SchemaUtilities.PlantSchema);
                    opmAdp.Import();
                }
                finally
                {
                    UI.Controls.WinForms.WaitForm.CloseForm();
                }
                }

            /*------------------------------------------------------------------------------------**/
            /// <summary>
            /// ReplaceComponents
            /// </summary>
            /// <param name="unparsed"></param>
            /*--------------+---------------+---------------+---------------+---------------+------*/
            public static void ReplaceComponents
            (
            string unparsed
            )
                {
                if(PipelineUtilities.IsReplaceDialogRunning)
                    return;

                BMECApi MechnanicalAPI = BMECApi.Instance;

                // Don't proceed for readonly files
                if(MechnanicalAPI.IsActiveModelReadOnly ())
                    {
                    MechnanicalAPI.SendToMessageCenterD (Bentley.DgnPlatformNET.OutputMessagePriority.Information, LocalizableStrings.DgnReadonly);
                    return;
                    }

                ECInstanceList ecList = MechnanicalAPI.GetSelectedInstances ();

                int count = 0;
                for(int i = 0 ; i < ecList.Count ; i++)
                    {
                    IECInstance ecInstanceTemp = ecList[i] as IECInstance;
                    if(ecInstanceTemp == null)
                        continue;

                    // Skip anything other than piping components or valve operators
                    if(!(SchemaUtilities.IsPipingComponent (ecInstanceTemp) || SchemaUtilities.IsComponentOfType (ecInstanceTemp, ClassNames.VALVE_OPERATING_DEVICE)))
                        continue;
                    //You can't replace fasteners
                    if(SchemaUtilities.IsComponentOfType (ecInstanceTemp, ClassNames.FASTENER))
                        continue;
                    //You can't replace seals
                    if(SchemaUtilities.IsComponentOfType (ecInstanceTemp, ClassNames.SEAL))
                        continue;

                    count++;
                    }

                ECInstanceList nozzleList = new ECInstanceList ();
                bool bHasFreeformNozzle = false;
                if(count == 0)
                    {
                    for(int i = 0 ; i < ecList.Count ; i++)
                        {
                        IECInstance ecInstanceTemp = ecList[i] as IECInstance;
                        if(ecInstanceTemp.ClassDefinition.Name == "NOZZLE")
                            {
                            IECPropertyValue propVal = ecInstanceTemp["TYPE_FOR_DATUM"];
                            if(propVal != null && propVal.StringValue == "Freeform")
                                {
                                bHasFreeformNozzle = true;
                                nozzleList.Add (ecInstanceTemp);
                                }
                            }
                        }
                    }

                if(count == 0 && !bHasFreeformNozzle)
                    {
                    MechnanicalAPI.SendToMessageCenterD (DgnPlatformNET.OutputMessagePriority.Information, LocalizableStrings.NoComponentsSelectedinDgn);
                    return;
                    }

                for(int i = 0 ; i < ecList.Count ; i++)
                    {
                    DgnUtilities dgnUtil = DgnUtilities.GetInstance ();
                    IECInstance ecInst = ecList[i];
                    if(dgnUtil.IsECInstanceFromReferenceFile (ecInst) || dgnUtil.IsECInstanceReferencedOut (ecInst))
                        {
                        MechnanicalAPI.SendToMessageCenterD (DgnPlatformNET.OutputMessagePriority.Information, LocalizableStrings.ReadonlyComps);
                        return;
                        }
                    }

                if(s_ReplaceCompDialog == null)
                    {
                    if(nozzleList.Count <= 0)
                        {
                        s_ReplaceCompDialog = new DlgReplaceComponent (ecList, ReplaceDialogMode.SimpleReplace, true);
                        s_ReplaceCompDialog.FormClosed += new FormClosedEventHandler (ReplaceCompDialog_FormClosed);
                        }
                    else
                        {
                        s_ReplaceCompDialog = new DlgReplaceComponent (nozzleList, ReplaceDialogMode.ChangeSpecSize, false);
                        s_ReplaceCompDialog.FormClosed += new FormClosedEventHandler (ReplaceCompDialog_FormClosed);
                        }
                    }
                s_ReplaceCompDialog.Show ();
                }

            /*------------------------------------------------------------------------------------**/
            /// <summary>
            /// CheckConnectivity
            /// </summary>
            /// <param name="unparsed"></param>
            /*--------------+---------------+---------------+---------------+---------------+------*/
            public static void CheckConnectivity
            (
            string unparsed
            )
                {

                BMECApi MechnanicalAPI = BMECApi.Instance;
                ECInstanceList ComponentList = null;

                DgnUtilities dgnUtil = DgnUtilities.GetInstance ();
                if(unparsed.ToUpper () == "HVAC")
                    {
                    PipelineManagerMode.Mode = PipelineManagerAvailableModes.HVAC;
                    ComponentList = dgnUtil.GetAllInstancesFromDgn ("HVAC_COMPONENT", true);
                    }
                else if (unparsed.ToUpper() == "TRAY")
                    {
                    PipelineManagerMode.Mode = PipelineManagerAvailableModes.CableTray;
                    ComponentList = dgnUtil.GetAllInstancesFromDgn("CABLETRAY_COMPONENT", true);
                    }
                else
                    {
                    PipelineManagerMode.Mode = PipelineManagerAvailableModes.Piping;
                    ComponentList = dgnUtil.GetAllInstancesFromDgn ("PIPING_COMPONENT", true);
                    }

                if(unparsed.ToUpper () == "FIXDISCONNECTS")
                    {
                    if(ComponentList.Count == 0)
                        {
                        return;
                        }

                    s_ConnectivityLog = new LogConnectivityReport ();
                    s_ConnectivityLog.AutoConnectivityCheck ();
                    s_ConnectivityLog.AutoDisconnectFix ();
                    PipelineUtilities.IsConnectivityCheckerRunning = false;
                    }
                else
                    {
                    if(ComponentList.Count == 0)
                        MechnanicalAPI.SendToMessageCenterD (DgnPlatformNET.OutputMessagePriority.Information, LocalizableStrings.NoComponentinDgn);

                if (s_ConnectivityDialog != null)
                    s_ConnectivityDialog.ExamineAndShow (true);

                    if(s_ConnectivityDialog == null)
                        {
                        s_ConnectivityDialog = new DlgConnetivtyReport ();
                        s_ConnectivityDialog.FormClosed += new FormClosedEventHandler (ConnectivityDialog_FormClosed);
                        }
                    s_ConnectivityDialog.Show ();
                    }
                }

            /*------------------------------------------------------------------------------------**/
            /// <summary>
            /// PipelineManagerDialog_FormClosed
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            /*--------------+---------------+---------------+---------------+---------------+------*/
            static void PipelineManagerDialog_FormClosed
            (
            object sender,
            FormClosedEventArgs e
            )
                {

                s_PipelineManagerDialog.FormClosed -= new FormClosedEventHandler (PipelineManagerDialog_FormClosed);
                PipelineUtilities.IsPipelineManagerRunning = false;
                s_PipelineManagerDialog = null;
                }

            /*------------------------------------------------------------------------------------**/
            /// <summary>
            /// PipelineManagerDialog2_FormClosed
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            /*--------------+---------------+---------------+---------------+---------------+------*/
            static void PipelineManagerDialog2_FormClosed
            (
            object sender,
            FormClosedEventArgs e
            )
                {
                PipelineUtilities.IsPipelineManagerRunning = false;

                s_PipelineManager2.FormClosed -= new FormClosedEventHandler (PipelineManagerDialog2_FormClosed);
                s_PipelineManager2 = null;
                }

            /*------------------------------------------------------------------------------------**/
            /// <summary>
            /// ConnectivityDialog_FormClosed
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            /*--------------+---------------+---------------+---------------+---------------+------*/
            static void ConnectivityDialog_FormClosed
            (
            object sender,
            FormClosedEventArgs e
            )
                {
                s_ConnectivityDialog = null;
                }

            /*------------------------------------------------------------------------------------**/
            /// <summary>
            /// ReplaceCompDialog_FormClosed
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            /*--------------+---------------+---------------+---------------+---------------+------*/
            static void ReplaceCompDialog_FormClosed
            (
            object sender,
            FormClosedEventArgs e
            )
                {
                s_ReplaceCompDialog.FormClosed -= new FormClosedEventHandler (ReplaceCompDialog_FormClosed);
                s_ReplaceCompDialog = null;
                }

            }
        }
    }
