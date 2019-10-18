/*--------------------------------------------------------------------------------------+
|
|   $Source: Utilities/ECInstanceDialog.cs $
|
|   $Copyright: (c) 2019 Bentley Systems, Incorporated. All rights reserved. $
|
+--------------------------------------------------------------------------------------*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Bentley.ECObjects;
using Bentley.ECObjects.Schema;
using Bentley.ECObjects.Instance;
using Bentley.ECObjects.UI;
using Bentley.EC.Persistence;
using Bentley.PlantBIMECPlugin;
using Bentley.EC.Persistence.Query;
using Bentley.Collections;
using Bentley.ECSystem;
using Bentley.Plant.CommonTools.BIMAdapter;
//using Bentley.Plant.TypeConverters;

namespace Bentley.Plant
{
namespace Utilities
{
public partial class ECInstanceDialog : Form
{
ECPropertyPane      m_propertyPane = null;
IECClass            m_ECClass = null;
ECInstanceList      m_ECInstanceList = new ECInstanceList ();
IECInstance         m_sourceInstance = null; //used with consistency manger

private bool m_DonotDisplayAssociatedItems = true;
private bool m_SavePressed = false;
private bool m_NewInstance = false;
private bool m_CheckForUniqueness = true;
private bool m_instanceModified = false;
private bool m_associatedItemModified = false;
private bool m_ValueChangeInProgress = false;

private void SetLocalizableStrings()
    {
    this.cmd_Browse.Text = LocalizableStrings.Browse;
    this.cmd_Save.Text          = LocalizableStrings.Save;
    this.cmd_Cancel.Text        = LocalizableStrings.Cancel;
    this.tpage_Plant.Text       = LocalizableStrings.Associations;
    this.tpage_Properties.Text  = LocalizableStrings.Properties;
    }

public string DialogTitle
    {
    set
        {
        Text = value;
        }
    }

public bool SavePressed
    {
    get
        {
        return m_SavePressed;
        }
    }

public bool AssociatedItemModified
    {
    get
        {
        return m_associatedItemModified;
        }
    }

public bool InstanceModified
    {
    get
        {
        return m_instanceModified;
        }
    }

public bool CheckForUniqueness
    {
    get
        {
        return m_CheckForUniqueness;
        }
    set
        {
        m_CheckForUniqueness = value;
        }
    }

public bool DonotDisplayAssociatedItems
    {
    set
        {
        m_DonotDisplayAssociatedItems = value;
        if (m_DonotDisplayAssociatedItems == true)
            tabControl1.TabPages.Remove (tpage_Plant);
        }
    }

/*------------------------------------------------------------------------------------**/
/// <summary>
/// Allow help topic to be passed in. This is done for the Pipe Bends dialog
/// An Example of this is done in btn_ModifyPipeBends_Click() in PipelineManger2.cs
/// The other helps are default constants. These are common help topics for Pipeline and
/// Associated Items creation
/// </summary>
/*--------------+---------------+---------------+---------------+---------------+------*/
public string HelpTopic
    {
    get
        {
        return m_helpTopic;
        }
    set
        {
        m_helpTopic = value;
        ModalHelpLaunch.Instance.SetHelpTopic = m_helpTopic;
        }
    }
private string m_helpTopic = string.Empty;
private const string DefaultAssociatedItemHelpTopic = "CreateAssoc";
private const string DefaultHelpTopic = "CreatePline";

/*------------------------------------------------------------------------------------**/
/// <summary>Constructor</summary>
/// <author>Yasir Bin Qaiser</author>                                <date>07/2008</date>
/*--------------+---------------+---------------+---------------+---------------+------*/
public ECInstanceDialog
(
String ClassName
)
    {
    InitializeComponent ();
    SetLocalizableStrings ();
    this.Scale (WorkspaceUtilities.UpdateDialogForDPI (this));
    Text = "";
    m_ECClass = SchemaUtilities.PlantSchema.GetClass (ClassName);

    IECInstance ecInst = SchemaUtilities.CreateECInstance (m_ECClass.Name);//  m_ECClass.CreateInstance();
    ecInst.InstanceId = "mechanical";
    m_ECInstanceList.Add (ecInst);
    m_NewInstance = true;
    HelpTopic = DefaultAssociatedItemHelpTopic;
    }

/*------------------------------------------------------------------------------------**/
/// <summary>Constructor</summary>
/// <author>Yasir Bin Qaiser</author>                                <date>07/2008</date>
/*--------------+---------------+---------------+---------------+---------------+------*/
public ECInstanceDialog
(
IECInstance ecInstance
)
    {
    InitializeComponent ();
    SetLocalizableStrings ();
    this.Scale (WorkspaceUtilities.UpdateDialogForDPI (this));
    Text = "";
    m_ECInstanceList.Add (ecInstance);
    m_ECClass = ecInstance.ClassDefinition;
    if (ecInstance.InstanceId == "mechanical" || string.IsNullOrEmpty (ecInstance.InstanceId))
        m_NewInstance = true;
    HelpTopic = DefaultHelpTopic;
    }

/*------------------------------------------------------------------------------------**/
/// <summary>Constructor</summary>
/// <author>Yasir Bin Qaiser</author>                                <date>07/2008</date>
/*--------------+---------------+---------------+---------------+---------------+------*/
public ECInstanceDialog
(
ECInstanceList ecInstanceList
)
    {
    InitializeComponent ();
    SetLocalizableStrings ();
    this.Scale (WorkspaceUtilities.UpdateDialogForDPI (this));
    Text = "";
    m_ECInstanceList = ecInstanceList;
    HelpTopic = DefaultHelpTopic;
    }

/*------------------------------------------------------------------------------------**/
/// <summary>
/// Constructor used with placement from Consistency manager
/// </summary>
/// <param name="ClassName">class name to create instance from</param>
/// <param name="sourceInstance">instance to copy values from</param>
/*--------------+---------------+---------------+---------------+---------------+------*/
public ECInstanceDialog
(
String ClassName,
IECInstance sourceInstance
)
    {
    InitializeComponent ();
    SetLocalizableStrings ();
    this.Scale (WorkspaceUtilities.UpdateDialogForDPI (this));
    Text = "";
    m_ECClass = SchemaUtilities.PlantSchema.GetClass (ClassName);
    IECInstance ecInst = SchemaUtilities.CreateECInstance (m_ECClass.Name);
    ecInst.InstanceId = "mechanical";
    DgnUtilities.GetInstance().CopyProperties(sourceInstance, ecInst, false);
    m_ECInstanceList.Add (ecInst);
    m_NewInstance = true;
    m_sourceInstance = sourceInstance;
    HelpTopic = DefaultAssociatedItemHelpTopic;
    }

/*------------------------------------------------------------------------------------**/
/// <summary>Help Requested (F1)</summary>
/*--------------+---------------+---------------+---------------+---------------+------*/
private void ECInstanceDialog_HelpRequested(object sender, HelpEventArgs hlpevent)
    {
    ModalHelpLaunch.Instance.LaunchHelp ();
    }

/*------------------------------------------------------------------------------------**/
/// <summary>Help button selected</summary>
/*--------------+---------------+---------------+---------------+---------------+------*/
private void ECInstanceDialog_HelpButtonClicked(object sender, CancelEventArgs e)
    {
    ModalHelpLaunch.Instance.LaunchHelp ();
    e.Cancel = true;
    }

public IECInstance GetECInstance()
    {
    if(m_ECInstanceList != null)
        return m_ECInstanceList[0];

    return null;
    }

private void InitializeDialog()
    {
    IECInstance ecFirstInstance = null;
    if (m_ECInstanceList.Count > 0)
        ecFirstInstance = m_ECInstanceList[0];

    SetOriginalWBSValues();
    SetTagValueBeforeEdit(m_ECInstanceList);

    _associatedItemsControl.Location = new Point(3, 3);
    _associatedItemsControl.Size = new Size(tpage_Plant.Width - 6, tpage_Plant.Height - 6);
    _associatedItemsControl.InstanceList = m_ECInstanceList;

    m_SavePressed = false;
    if (Text == "")
        {
        if (m_NewInstance)
            Text = LocalizableStrings.Create + " " + m_ECClass.DisplayLabel;
        else if (m_ECInstanceList.Count > 1)
            Text = LocalizableStrings.Edit + " " + ecFirstInstance.ClassDefinition.DisplayLabel + LocalizableStrings.PluralLiteral;
        else
            Text = LocalizableStrings.Edit + " " + SchemaUtilities.GetDisplayNameForECInstance (ecFirstInstance, false) + " (" + ecFirstInstance.ClassDefinition.DisplayLabel + ")";
        }

    //if (m_ECInstanceList.Count == 1)
        {
        if (!_associatedItemsControl.FillAssociatedItems(!m_NewInstance))
            tabControl1.TabPages.Remove(tpage_Plant);
        }
    EnableSaveButton ();

    InitializePropertyPane ();
    this.AcceptButton = this.cmd_Save;
    this.CancelButton = this.cmd_Cancel;

    //This is used with consistency manager placement (pipeline)
    ////Need to be after FillAssociatedItems() so that passed in instance get WBS items
    if(m_sourceInstance != null)
        InstanceFromBrowse (m_sourceInstance);

    SetBrowseVisibleState ();
}

/*------------------------------------------------------------------------------------**/
/// <summary>
/// Sets up a class object that is used to determine that validates status of a system class (PIPELINE, HVAC, TRAY)
/// </summary>
/// <param name="ecInstance">The ecinstance.</param>
/*--------------+---------------+---------------+---------------+---------------+------*/
private void SetTagValueBeforeEdit(ECInstanceList ecInstanceList)
{
    m_ValidateSavedSystemInstance = new ValidateAndReserveTagSavedSystemInstance(ecInstanceList);
}
private ValidateAndReserveTagSavedSystemInstance m_ValidateSavedSystemInstance = null;

/*------------------------------------------------------------------------------------**/
/// <summary>Sets the state of the browse visible.</summary>
/*--------------+---------------+---------------+---------------+---------------+------*/
private void SetBrowseVisibleState()
    {
    cmd_Browse.Visible = BIMAdapter.Connected || DgnUtilities.GetInstance ().IsValidPIDModelAttached ();
    }

/*------------------------------------------------------------------------------------**/
/// <summary>Load the form</summary>
/// <author>Yasir Bin Qaiser</author>                                <date>07/2008</date>
/*--------------+---------------+---------------+---------------+---------------+------*/
private void CreateECInstanceDlg_Load
(
object sender,
EventArgs e
)
    {
    InitializeDialog ();
    }

private void EnableSaveButton()
    {
    IECInstance ecFirstInstance = null;
    if (m_ECInstanceList.Count == 1)
        ecFirstInstance = m_ECInstanceList[0];
    else
        ecFirstInstance = null;


    if (ecFirstInstance != null)
        {
        if (ecFirstInstance.GetRelationshipInstances ().Count == _associatedItemsControl.RowCount || ecFirstInstance.GetRelationshipInstances ().Count >= 1)
            cmd_Save.Enabled = true;
        else
            cmd_Save.Enabled = false;
        }
    else
        cmd_Save.Enabled = true;
    }



private void InitializePropertyPane()
    {
    //IECInstance customExtendedType_Spec = ECPropertyPane.CreateExtendedType("SPECIFICATION");
    //ECPropertyPane.SetExtendedType(m_ECInstance["SPECIFICATION"].Property, customExtendedType_Spec);
    //ECPropertyPane.SetExtendedTypeTypeConverter(customExtendedType_Spec, typeof(SpecificationTypeConverter));

    ECInstanceListSet listSet = new ECInstanceListSet ();
    //Create a List set
    for (int i = 0; i < m_ECInstanceList.Count; i++)
        {
        ECInstanceList ecList = new ECInstanceList ();
        ecList.Add (m_ECInstanceList[i]);
        listSet.Add (ecList);
        }

    IECInstance ecFirstInstance = m_ECInstanceList[0];

    if (ecFirstInstance != null)
        {
        IECPropertyValue propValue = BusinessKeyUtility.GetPropertyValue (ecFirstInstance);
        if (null != propValue)
            ECPropertyPane.SetRequiresRefresh (propValue.Property);
        }

    // Initialize the property pane reference and add the pane to dialog
    m_propertyPane = new ECPropertyPane (listSet, Bentley.Properties.PropertyManager.Instance.PropertyFilters, null, "PlantUtilities.ECInstanceDialog", true, true, true, "OPM_ECPropertyUtil.State", 200);
    m_propertyPane.Location = new Point (3, 3);
    m_propertyPane.Size = new Size (tpage_Properties.Width - 6, tpage_Properties.Height - 6);
    m_propertyPane.Name = "ECPropertyDailog";
    m_propertyPane.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
    m_propertyPane.Dock = DockStyle.None;
    m_propertyPane.AllowGroupPanelReordering = false;
    
    

    m_propertyPane.Enabled = true;

    if (ecFirstInstance != null)
        ecFirstInstance.ECPropertyValueChanged += new ECPropertyValueChangedHandler (m_ECInstance_ECPropertyValueChanged);

    tpage_Properties.Controls.Add (m_propertyPane);

    FocusPropertyPane ();
    }

void m_ECInstance_ECPropertyValueChanged(object sender, ECPropertyValueChangedEventArgs args)
    {
    if (args.PropertyValue.Instance.ClassDefinition.Name == "PIPE_BEND")
        UpdateBendProperties(args.PropertyValue);

    m_propertyPane.RefreshProperties ();
    m_propertyPane.RefreshAllPropertyGrids ();
    m_propertyPane.Refresh ();

    //without this refresh call, when editing multiple (not sure how many but at least more than 2) instances, we get '**varies**'
    //for a property which is just edited and should have same value for all the instances.
    m_propertyPane.RefreshProperties();
    m_instanceModified = true;
    }

void UpdateBendProperties (IECPropertyValue updatedProperty)
    {
    //if PIPE_BEND class does not has BEND_RADIUS property (i.e. using older schemas) OR the updated property in
    //neither BEND_RADIUS nor BEND_POINT_RADIUS, return from the function.
    if (updatedProperty.Instance.ClassDefinition["BEND_RADIUS"] == null ||
        (updatedProperty.AccessString != "BEND_RADIUS" && updatedProperty.AccessString != "BEND_POINT_RADIUS"))
        return;

    //when updating a property, and we get an even due to that update, ignore it.
    if (m_ValueChangeInProgress)
        return;

    double dFactor = 1.0;
    System.String strCfgValue = WorkspaceUtilities.GetConfigVar("OPM_SPECPLUGIN_OPM_DIM_CONVERSIONFACTOR");
    if (!System.Double.TryParse(strCfgValue, out dFactor))
        dFactor = 1.0;

    m_ValueChangeInProgress = true;

    for (int i = 0; i < m_ECInstanceList.Count; i++)
        {
        IECInstance ecInst = m_ECInstanceList[i];

        double dND = ecInst["NOMINAL_DIAMETER"].DoubleValue;
        double dNewVal = updatedProperty.DoubleValue;
        double dResult = 0.0;
        IECPropertyValue pv = null;

        if (updatedProperty.AccessString == "BEND_RADIUS")
            {
            dResult = dNewVal / (dND * dFactor);
            pv = ecInst.FindPropertyValue("BEND_POINT_RADIUS", true, false, false);
            }
        else if (updatedProperty.AccessString == "BEND_POINT_RADIUS")
            {
            dResult = dNewVal * dND * dFactor;
            pv = ecInst.FindPropertyValue("BEND_RADIUS", true, false, false);
            }

        if (pv != null)
            pv.DoubleValue = dResult;
        }

    m_ValueChangeInProgress = false;
    }

private void FillAssociatedItems
(
)
    {
    if (!_associatedItemsControl.FillAssociatedItems (false))
        tabControl1.TabPages.Remove (tpage_Plant);
    }

private bool IsNewInstanceUnique(IECInstance ecInstance, bool showMessage)
    {
    return IsNewInstanceUnique(ecInstance, showMessage, true);
    }

private bool IsNewInstanceUnique(IECInstance ecInstance, bool showMessage, bool includeReferencedOut)
    {
    if (m_CheckForUniqueness == false)
        return true;

    DgnUtilities dgnUtil = DgnUtilities.GetInstance ();

    string newInstName = SchemaUtilities.GetDisplayNameForECInstance (ecInstance, false);
    // don't go to scan again. Use the hashtable from StdPref-->DgnUtilities
    if (dgnUtil.AllInstances.Count > 0 && dgnUtil.AllInstances.ContainsKey (ecInstance.ClassDefinition.Name))
        {
        Hashtable allInstances = dgnUtil.AllInstances[ecInstance.ClassDefinition.Name] as Hashtable;
        bool found = allInstances.ContainsKey (newInstName);
        if (found)
            {
            IECInstance foundInstance = allInstances[newInstName] as IECInstance;
            if (!includeReferencedOut && DgnUtilities.GetInstance().IsECInstanceReferencedOut(foundInstance))
                return true;

            if (null != foundInstance && foundInstance.InstanceId != ecInstance.InstanceId && !dgnUtil.IsECInstanceFromReferenceFile(foundInstance))
                {
                if (showMessage)
                    MessageBox.Show(string.Format(LocalizableStrings.DuplicateTag, newInstName),
                                    LocalizableStrings.DuplicateTagTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    WorkspaceUtilities.DisplayInfoMessage(string.Format(LocalizableStrings.DuplicateTag, newInstName), string.Empty);
                return false;
                }
            }
        }
    else
        {
        ECInstanceList ecList = dgnUtil.GetAllInstancesFromDgn (ecInstance.ClassDefinition.Name);
        for (int i = 0; i < ecList.Count; i++)
            {
            IECInstance existingInst = ecList[i];
            string existingInstName = SchemaUtilities.GetDisplayNameForECInstance (existingInst, true);

            if (existingInstName.ToLower () == newInstName.ToLower () && ecInstance.InstanceId != existingInst.InstanceId && !dgnUtil.IsECInstanceFromReferenceFile (existingInst))
                {
                if (showMessage)
                    MessageBox.Show (string.Format (LocalizableStrings.DuplicateTag, existingInstName),
                                        LocalizableStrings.DuplicateTagTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    WorkspaceUtilities.DisplayInfoMessage(string.Format(LocalizableStrings.DuplicateTag, existingInstName), string.Empty);

                if (!includeReferencedOut && DgnUtilities.GetInstance().IsECInstanceReferencedOut(ecList[i]))
                    return true;
                else
                    return false;
                }
            }
        }

    return true;
    }

private bool SaveMultipleECIsntances()
    {
    ArrayList arr = new ArrayList ();
    DgnUtilities dgnUtil = DgnUtilities.GetInstance ();
    bool status = false;
    for (int i = 0; i < m_ECInstanceList.Count; i++)
        {
        if (IsNewInstanceUnique (m_ECInstanceList[i], false))
            dgnUtil.SaveModifiedInstance (m_ECInstanceList[i], dgnUtil.GetDGNConnection ());
        else
            status = true;
        }

    if (status)
        MessageBox.Show (LocalizableStrings.DuplicateTags,
                         LocalizableStrings.DuplicateTagTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);

    return true;
    }

public bool isUniqueInstance(IECInstance ecInstance, bool includeReferencedOut)
    {
    return IsNewInstanceUnique(ecInstance, false, includeReferencedOut);
    }

public bool SaveData()
    {
    return SaveData(true);
    }

private void RefreshPipelines(bool status)
    {
    if (status)
        WorkspaceUtilities.SetToolState ("PIPELINEMANAGER_REFRESH_STDPREFS", "true");
    else
        WorkspaceUtilities.SetToolState ("PIPELINEMANAGER_REFRESH_STDPREFS", "false");
    }

private bool Tagged3DInstanceExistsInServer (ref IECInstance ecInst)
    {
    // Check if 3D line exists on iModelHub
    IExtendedParameters extendedParameters = ExtendedParameters.Create();
    ProviderFilter provFilter = new ProviderFilter(true, new string[] { "Bentley.PlantBIMECPlugin" });
    ProviderFilter.SetIn(extendedParameters, provFilter);
    ECQuerySettings ecQuerySettings = new ECQuerySettings(-1, extendedParameters);

    //ECQuery query = new ECQuery();
    ECQuery query = QueryHelper.CreateQuery(SchemaUtilities.PlantSchema, ecInst.ClassDefinition.Name);

    IECPropertyValue prop = ecInst["NAME"];
    PropertyExpression exp = new PropertyExpression(RelationalOperator.EQ, prop.Property, ecInst["NAME"].StringValue);
    query.WhereClause.Add(exp);
    query.SelectClause.SelectAllProperties = true;

    ECSystem.Repository.RepositoryConnection connection = PlantBimConnectionHelper.GetRepositoryConnection();

    bool isCurrentTransformationModeFunctional = PlantBIMECPluginUtilities.IsCurrentTransformationModeFunctoinal(connection);
    if ( isCurrentTransformationModeFunctional )
        PlantBIMECPluginUtilities.SetCurrentTransformationMode(connection, false);

    QueryResults queryResults = PersistenceServiceFactory.GetService().ExecuteQuery(connection, query, ecQuerySettings);

    if ( isCurrentTransformationModeFunctional )
        PlantBIMECPluginUtilities.SetCurrentTransformationMode(connection, true);

    if ( queryResults.Count > 0 )
        return false;

    return true;
    }

public bool SaveData(bool displayDialogs)
    {
    bool bRetVal = true;
    DgnUtilities dgnUtil = DgnUtilities.GetInstance ();

    string defaultRestrictClasses = "PIPE_BEND;ISO_SHEET";
    string restrictClassNames = "";
    if (Bentley.DgnPlatformNET.ConfigurationManager.IsVariableDefined ("OPM_RESTRICT_CALLS_TO_TAGGING_SERVICE"))
        {
        string value = Bentley.DgnPlatformNET.ConfigurationManager.GetVariable("OPM_RESTRICT_CALLS_TO_TAGGING_SERVICE");
        if (value.Length == 1)
            restrictClassNames = (value == "0") ? "" : defaultRestrictClasses;
        else
            restrictClassNames = (value.Length > 1) ? value : defaultRestrictClasses;
        }
    else
        restrictClassNames = defaultRestrictClasses;

    for (int i = 0; i < m_ECInstanceList.Count; i++)
    {
        bool bStatus = true;
        //get instance from the list
        IECInstance ecInst = m_ECInstanceList[i];

        bool isUnique = false;
        bool createAdditionalLink = false;
        bool bCheckTaggingService = true;
        if (restrictClassNames.Length > 0)
            bCheckTaggingService = !restrictClassNames.Contains (ecInst.ClassDefinition.Name);

        if (bCheckTaggingService && CommonTools.BIMAdapter.BIMAdapter.Connected)
            {
            string newTagCodeValue = ecInst["NAME"].StringValue;

            //m_ValidateSavedSystemInstance should never be null, but just in case check.
            BusinessKeyStatus status = m_ValidateSavedSystemInstance != null ?
                                       m_ValidateSavedSystemInstance.GetStatus(ecInst, newTagCodeValue, m_NewInstance) :
                                       BIMAdapter.ReserveTag(ecInst, newTagCodeValue);
            switch (status)
                {
                case BusinessKeyStatus.Success:
                    isUnique = true;
                    break;
                case BusinessKeyStatus.CodeUsed:
                    if ( SchemaUtilities.IsAWBSItem(ecInst.ClassDefinition) )
                        {
                        isUnique = IsNewInstanceUnique(ecInst, displayDialogs);
                        }
                    else if (SchemaUtilities.IsComponentOfType(ecInst, OPMCommon.ClassNames.PIPELINE) ||
                            SchemaUtilities.IsComponentOfType(ecInst, OPMCommon.ClassNames.TRAYLINE) ||
                            SchemaUtilities.IsComponentOfType(ecInst, OPMCommon.ClassNames.HVACLINE))
                        {
                        isUnique = IsNewInstanceUnique(ecInst, displayDialogs);
                        // does not exit in local dgn:
                        if(isUnique)
                            {
                            // Check if exists on Server
                            isUnique = Tagged3DInstanceExistsInServer(ref ecInst);
                            if (!isUnique)
                                {                                    
                                string errormsg = string.Format(LocalizableStrings.DuplicateTags, newTagCodeValue);
                                if ( displayDialogs )
                                    MessageBox.Show(errormsg, LocalizableStrings.DuplicateTagTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                WorkspaceUtilities.DisplayErrorMessage(errormsg, string.Empty);
                                break;
                                }
                            }
                        }
                    else
                        {
                        createAdditionalLink = true;
                        }
                    break;
                default:
                case BusinessKeyStatus.CodeUnavailable:
                    isUnique = false;
                    string msg = string.Format (LocalizableStrings.TagCodeUnavailable, newTagCodeValue);
                    if (displayDialogs)
                        MessageBox.Show (msg, LocalizableStrings.DuplicateTagTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    WorkspaceUtilities.DisplayErrorMessage (msg, string.Empty);
                    break;
                }
            }
        else
            {
            isUnique = IsNewInstanceUnique(ecInst, displayDialogs);
            }

        //check if this instance is unique or additional link...
        if(isUnique || createAdditionalLink)
            {
            if(m_NewInstance)
                {
                //if m_NewInstance flag is set, verify the instance id after the save
                dgnUtil.WriteECInstanceToDgn (ecInst, dgnUtil.GetDGNConnection ());
                if(ecInst.InstanceId == "mechanical") //means it didn't get persisted.
                    bStatus = false;
                }
            }
        else//if its not unique, set status flag to false.
            bStatus = false;

        if (!bStatus)
            {
            bRetVal = false;
            continue;
            }

        if (_associatedItemsControl.InstanceModified)
            m_associatedItemModified = true;

        for (int j = 0; j < _associatedItemsControl.Rows.Count; j++)
            {
            ECClass ecClass = _associatedItemsControl.getECClassInformationForCurrentSelection (j) as ECClass;
            string ecInstanceValue = _associatedItemsControl[AssociatedItemsControl.s_WBSInstanceKey, j].Value as string;
            if (ecInstanceValue == "**Varies**")
                continue;

            IECRelationshipClass ecRel = _associatedItemsControl.getECRelationshipClassInformationForCurrentSelection (j);
            if (!m_NewInstance)//delete existing relationships to parent since we will be creating new later on
                dgnUtil.DeleteAllRelationshipsToParent (ecInst, ecRel, ecClass, dgnUtil.GetDGNConnection ());

            IECRelationshipInstance ecRelInst = ecRel.CreateInstance () as IECRelationshipInstance;
            // get from DropDown instance
            DataGridViewComboBoxCell itemInstanceCell = _associatedItemsControl[AssociatedItemsControl.s_WBSInstanceKey, j] as DataGridViewComboBoxCell;
            IECInstance ecParentInstance = null;

            Dictionary<string, IECInstance> cacheList = itemInstanceCell.Tag as Dictionary<string, IECInstance>;
            if (null != cacheList && cacheList.Count > 0 && cacheList.ContainsKey (ecInstanceValue) && !dgnUtil.IsECInstanceFromReferenceFile (cacheList[ecInstanceValue]))
                ecParentInstance = cacheList[ecInstanceValue];
            else
                ecParentInstance = dgnUtil.GetSourceInstance (ecClass, ecInstanceValue);

            if (ecParentInstance == null)
                continue;

            if (dgnUtil.IsECInstanceFromReferenceFile (ecParentInstance))
                {
                MessageBox.Show (string.Format (LocalizableStrings.ReadOnlyInstance, ecParentInstance.ClassDefinition.DisplayLabel, ecInst.ClassDefinition.DisplayLabel),
                            LocalizableStrings.ReadOnlyInstanceTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                continue;
                }

            ecRelInst.Source = ecParentInstance;
            ecRelInst.Target = ecInst;
            dgnUtil.WriteECInstanceToDgn (ecRelInst, dgnUtil.GetDGNConnection ());
            dgnUtil.SaveModifiedInstance (ecParentInstance, dgnUtil.GetDGNConnection ());
            }


        if (i == (m_ECInstanceList.Count - 1))
            RefreshPipelines (true);
        else
            RefreshPipelines (false);

            if (!m_NewInstance)
            {
                dgnUtil.SaveModifiedInstance(ecInst, dgnUtil.GetDGNConnection());
                WBSHelper.Instance.UpdateRelatedWBSInstances(ecInst);
            }
        }

    RefreshPipelines (true);
    return bRetVal;
    }


/// <summary>Sets the original WBS values.</summary>
/// <remarks>
/// Used to updated WBS related items when name is changed
/// Currently only used to change the name in a PIPING_NETWORK_SEGMENT and relationship that have SEGMENT_HAS_PIPING_COMPONENT
/// </remarks>
private void SetOriginalWBSValues()
    {
        if (!DgnUtilities.GetInstance().AllowWBSEdit) return;
        IECInstance ecInstance = null;
        if (m_ECInstanceList.Count > 0) ecInstance = m_ECInstanceList[0];
        if (ecInstance == null) return;
        if (!WBSHelper.Instance.IsAWBSItem(ecInstance.ClassDefinition)) return;

        //pass in the WBS item to create list of Pipelines before an edit
        WBSHelper.Instance.SetListOfPipelinesValuesBeforeAWBSEdit(ecInstance);
    }


/// <summary>Gets the current ec instance list.</summary>
/// <value>The current ECInstance list.</value>
public ECInstanceList CurrentECInstanceList { get { return m_ECInstanceList; } }

public bool AddAssociationToChangeSet(ChangeSet changeSet)
    {
    DgnUtilities dgnUtil = DgnUtilities.GetInstance ();

    for (int i = 0; i < _associatedItemsControl.Rows.Count; i++)
        {
        ECClass ecClass = _associatedItemsControl[AssociatedItemsControl.s_WBSClassKey, i].Tag as ECClass;
        string ecInstanceValue = _associatedItemsControl[AssociatedItemsControl.s_WBSInstanceKey, i].Value as string;
        IECRelationshipClass ecRel = _associatedItemsControl[AssociatedItemsControl.s_WBSInstanceKey, i].Tag as IECRelationshipClass;

        IECRelationshipInstance ecRelInst = ecRel.CreateInstance () as IECRelationshipInstance;
        IECInstance ecParentInstance = dgnUtil.GetSourceInstance (ecClass, ecInstanceValue);
        ecRelInst.Source = ecParentInstance;
        ecRelInst.Target = GetECInstance();
        changeSet.Add (ecRelInst);
        changeSet.MarkNew (ecRelInst);
        }

    return true;
    }
///*------------------------------------------------------------------------------------**/
/// <summary>Setup the ecinstance for save in Consistency Manager.</summary>
/// <returns>true if instance is valid</returns>
///*--------------+---------------+---------------+---------------+---------------+------*/
public bool SaveNonGraphicalECInstance ()
    {
    if (m_ECInstanceList == null || m_ECInstanceList.Count == 0)
        return false;
    InitializeDialog();
    DecrementEvent();
    IECInstance ecIsnt = m_ECInstanceList[0];
    string businesskey = SchemaUtilities.GetDisplayNameForECInstance (ecIsnt, true);
    //need to copy all the properties on mass import
    if (businesskey == "" || businesskey == ecIsnt.InstanceId || businesskey == (ecIsnt.ClassDefinition.Name + "(" + ecIsnt.InstanceId + ")"))
        {
        MessageBox.Show (string.Format (LocalizableStrings.TagNotSet, ecIsnt.ClassDefinition.DisplayLabel),
                            LocalizableStrings.TagNotSetTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
        m_SavePressed = false;
        return false;
        }
    m_SavePressed = true;
    return true;
    }

/*------------------------------------------------------------------------------------**/
/// <summary>Decrements the event.</summary>
/*--------------+---------------+---------------+---------------+---------------+------*/
private void DecrementEvent()
{
    if (m_ECInstanceList != null && m_ECInstanceList.Count > 0)
    {
        IECInstance ecFirstInstance = m_ECInstanceList[0];
        ecFirstInstance.ECPropertyValueChanged -= new ECPropertyValueChangedHandler(m_ECInstance_ECPropertyValueChanged);
    }
}

/*------------------------------------------------------------------------------------**/
/// <summary>Writes the EC isntacne to current dgn</summary>
/// <author>Yasir Bin Qaiser</author>                                <date>07/2008</date>
/*--------------+---------------+---------------+---------------+---------------+------*/
private void cmd_Save_Click
(
object sender,
EventArgs e
)
    {
    DecrementEvent();

    //We know there is just one object in the ECPane, get it
    //m_ECInstance = m_propertyPane.InstanceListSet[0][0];
    if (m_ECInstanceList.Count > 1)
        m_SavePressed = true;
    else
        {
        IECInstance ecIsnt = m_ECInstanceList[0];

        string businesskey = SchemaUtilities.GetDisplayNameForECInstance (ecIsnt, true);
        if (businesskey == "" || businesskey == ecIsnt.InstanceId || businesskey == (ecIsnt.ClassDefinition.Name + "(" + ecIsnt.InstanceId + ")"))
            {
            MessageBox.Show (string.Format (LocalizableStrings.TagNotSet, ecIsnt.ClassDefinition.DisplayLabel),
                             LocalizableStrings.TagNotSetTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
            }
        }
    m_SavePressed = true;
    m_propertyPane.SaveSettings ();
    m_propertyPane.SavePersistentSettings ();
    this.Close ();
    }

private void cmd_Cancel_Click
(
object sender,
EventArgs e
)
    {
    DecrementEvent();
    m_ECInstanceList    = null;
    m_SavePressed       = false;
    m_propertyPane.SaveSettings ();
    m_propertyPane.SavePersistentSettings ();
    this.Close ();
    }

/*------------------------------------------------------------------------------------**/
/// <summary>
/// Browse to a select instance
/// </summary>
/// <param name="passedInstance">instance to used get values from</param>
/*--------------+---------------+---------------+---------------+---------------+------*/
private void Cmd_Browse_Click(object sender, System.EventArgs e)
    {
    InstanceFromBrowse (null);
    }

/*------------------------------------------------------------------------------------**/
/// <summary>Sets the browse button status.</summary>
/// <param name="status">if set to <c>true</c> [status].</param>
/*--------------+---------------+---------------+---------------+---------------+------*/
public void SetBrowseButtonStatus(bool status)
    {
    if(m_ECClass == null) //this indicates multiple instances
        status = false;
    else if(m_ECClass != null && !status)
        status = true;
    this.cmd_Browse.Enabled = status;
    }
/*------------------------------------------------------------------------------------**/
/// <summary>
/// Use instance from a browsed instance or from instance passed from consistence manager
/// </summary>
/// <param name="passedInstance">instance to used get values from</param>
/*--------------+---------------+---------------+---------------+---------------+------*/
private void InstanceFromBrowse(IECInstance passedInstance)
    {
    if(m_ECClass == null)
        return;

    IECInstance ecFirstInstance = m_ECInstanceList[0];
    if(ecFirstInstance == null)
        return;

    DgnUtilities dgnUtil = DgnUtilities.GetInstance ();
    IDictionary<string, object> overrides = null;
    ArrayList assocItems = null;
    if(SchemaUtilities.IsAssociatedComponentClass (m_ECClass.Name))
        {
        overrides = dgnUtil.AssoicatedItemsTagBrowserOverrides;
        }
    else if(SchemaUtilities.IsComponentOfType (ecFirstInstance, OPMCommon.ClassNames.HVAC_COMPONENT) ||
            SchemaUtilities.IsComponentOfType (ecFirstInstance, OPMCommon.ClassNames.TRAY_COMPONENT))
        {
        overrides = dgnUtil.CommonTagBrowserOverrides;
        }
    else 
        {
        overrides = dgnUtil.PipelineTagBrowserOverrides;
        assocItems = SchemaUtilities.GetAssociatedItemInformation (m_ECClass);
        }

    IECInstance selectedECInstance = passedInstance;
    if(selectedECInstance == null)
        {
        object instance = dgnUtil.TagBrowser (ecFirstInstance, overrides);
        if(instance == null || !(instance is IECInstance))
            return;
        selectedECInstance = instance as IECInstance;
        }

    dgnUtil.CopyProperties (selectedECInstance, ecFirstInstance, false);
    dgnUtil.UpdateFunctionalGuidProperty (selectedECInstance, ecFirstInstance);

    try
        {
        if(SchemaUtilities.IsComponentOfType (ecFirstInstance, OPMCommon.ClassNames.PIPELINE))
            _associatedItemsControl.UpdateWBSParts (assocItems, selectedECInstance);
        }
    catch(Exception ex)
        {
        WorkspaceUtilities.DisplayErrorMessage (ex.Message, string.Empty);
        }
    }


private void ECInstanceDialog_FormClosing
(
object sender,
FormClosingEventArgs e
)
    {
    if (m_SavePressed == false)
        m_ECInstanceList = null;
    }

private void dataGrid_AssociatedItems_CellMouseClick
(
object sender,
DataGridViewCellMouseEventArgs e
)
    {

    if (e.ColumnIndex != 2)
        return;



    if (e.RowIndex == -1)
        return;

    EnableSaveButton ();
    }

private void dataGrid_AssociatedItems_CellValueChanged
(
object sender,
DataGridViewCellEventArgs e
)
    {
    if (e.ColumnIndex != 1)
        return;

    if (e.RowIndex < 0 || e.ColumnIndex < 0)
        return;

    if (m_propertyPane != null)
        {
        m_propertyPane.RefreshProperties ();
        m_propertyPane.RefreshAllPropertyGrids ();
        m_propertyPane.Refresh ();
        }
    EnableSaveButton ();
    }

void FocusPropertyPane
(
)
    {
    if (m_propertyPane == null)
        return;

    if (m_ECInstanceList == null)
        return;

    if (m_ECInstanceList.Count > 1)
        return;

    m_propertyPane.Focus ();

    m_propertyPane.RefreshProperties ();
    m_propertyPane.RefreshAllPropertyGrids ();
    m_propertyPane.Refresh ();

    m_propertyPane.ActivateGroup (0);
    m_propertyPane.Focus ();

    if (m_ECClass["NUMBER"] != null)
        m_propertyPane.SelectProperty (m_ECClass.Name, "NUMBER");
    else
    {
        try
        {
            m_propertyPane.SelectProperty(m_ECClass.Name, BusinessKeyUtility.GetPropertyName(m_ECClass));
        }
        catch (NullReferenceException)
        {
            MessageBox.Show("Please add custom attribute \"Business Key Specification\" for the associated class " + m_ECClass.Name, "Customer Attribute Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }



    }

void tabControl1_SelectedIndexChanged
(
object sender,
System.EventArgs e
)
    {
    FocusPropertyPane ();
    SetBrowseVisibleState ();
    }


public bool InitializeWithCustomProperties(ArrayList associations, ArrayList propertyValues)
    {
    return InitializeWithCustomProperties (associations,propertyValues,false);
    }

public bool InitializeWithCustomProperties (ArrayList associations,ArrayList propertyValues, bool resetStandardPrefOnOnImport)
    {

    InitializeDialog ();

    bool status = true;

    //need resetStandardPrefOnOnImport for import because
    //LoadSettings was not using associations, but the first item in the list
    //(Get2D3DInstance()) is returning true, which sets strUseStandPref = "1"
    //when strUseStandPref == "1", str = ReadStandPref (ecClass.Name) is used and THAT gets first item in list
    //setting strUseStandPref == "" forces the passed in associations to be used.
    status = _associatedItemsControl.LoadSettings (associations, resetStandardPrefOnOnImport);
    for (int i = 0; i < propertyValues.Count; i++)
        {
        string equalSeperatedPropVal = propertyValues[i] as string;
        string[] strs = equalSeperatedPropVal.Split (new char[] {'='});
        IECPropertyValue ecPropVal = m_ECInstanceList[0].GetPropertyValue (strs[0].Trim ());
        if (ecPropVal == null || ecPropVal.IsNull || ecPropVal.IsReadOnly)
            {
            status = false;
            continue;
            }
        ecPropVal.StringValue = strs[1].Trim ();
        }

    m_propertyPane.RefreshProperties ();
    m_propertyPane.RefreshAllPropertyGrids ();
    m_propertyPane.Refresh ();

    return (status && cmd_Save.Enabled);
    }

}
}
}