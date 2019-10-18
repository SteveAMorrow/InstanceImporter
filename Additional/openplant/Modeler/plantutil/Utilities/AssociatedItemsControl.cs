/*--------------------------------------------------------------------------------------+
|
|   $Source: Utilities/AssociatedItemsControl.cs $
|
|   $Copyright: (c) 2019 Bentley Systems, Incorporated. All rights reserved. $
|
+--------------------------------------------------------------------------------------*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Bentley.ECObjects.Instance;
using Bentley.ECObjects.Schema;
using Bentley.PlantBIMECPlugin;

namespace Bentley.Plant.Utilities
{
public partial class AssociatedItemsControl : DataGridView
{
private ECInstanceList  _instList       = null;
private IECClass        _ClassDef       = null;
private bool            _bMultiInst = false;
private bool            _bInstanceModified = false;

private const string _cstr_Varies       = "**Varies**";

private const int ClassColumn          = 0;
private const int ComboBoxColumn       = 1;
private const int AddButtonColumn      = 2;
private const int EditButtonColumn     = 3;
private const int DeleteButtonColumn   = 4;
private const int BrowseButtonColumn   = 5;

public const string s_WBSClassKey = "_associatedClass";
public const string s_WBSInstanceKey = "_associatedInstance";
public const string s_WBSName = "NAME";


        public bool InstanceModified
    {
    get
        {
        return _bInstanceModified;
        }
    }

private IECInstance GetAssociatedItemFromInstance
(
IECInstance             ecInstance,
IECRelationshipClass    ecRelClass,
IECClass                ecParentClass
)
    {
    DgnUtilities dgnUtil    = DgnUtilities.GetInstance();
    if (ecInstance.InstanceId == "mechanical")
        return null;

    ECInstanceList ecList = dgnUtil.GetParentInstances(ecInstance, ecRelClass, ecParentClass, dgnUtil.GetDGNConnection());

    return  (ecList.Count > 0) ? ecList[0] : null;
    }

public IECInstance getInstanceForCurrentSelection
(
int     rowIndex,
string  dropDownValue
)
    {
    DataGridViewComboBoxCell itemInstanceCell = this[s_WBSInstanceKey, rowIndex] as DataGridViewComboBoxCell;
    if (itemInstanceCell.Tag == null)
        return null;

    IECClass ecClass = itemInstanceCell.Tag as IECClass;
    if (null != ecClass)
        return null;

    Dictionary<string, IECInstance> cachedInstances = itemInstanceCell.Tag as Dictionary<string, IECInstance>;
    if (null == cachedInstances)
        return null;

    if ( cachedInstances.ContainsKey(dropDownValue) )
        return cachedInstances[dropDownValue];

    return null;
    }

public IECClass getECClassInformationForCurrentSelection
(
int rowIndex
)
    {
    DataGridViewComboBoxCell itemInstanceCell = this[s_WBSInstanceKey, rowIndex] as DataGridViewComboBoxCell;
    IECClass ecClass = itemInstanceCell.Tag as IECClass;
    if (null != ecClass)
        return ecClass;

    if (itemInstanceCell.Tag == null)
        return null;

    Dictionary<string, IECInstance> cachedInstances = itemInstanceCell.Tag as Dictionary<string, IECInstance>;
    if (null == cachedInstances)
        return null;

    foreach (KeyValuePair<string, IECInstance> entry in cachedInstances)
        {
        ecClass = entry.Value.ClassDefinition;
        break;
        }

    return ecClass;
    }

public IECRelationshipClass getECRelationshipClassInformationForCurrentSelection
(
int rowIndex
)
    {
    DataGridViewTextBoxCell itemClassCell = this[s_WBSClassKey, rowIndex] as DataGridViewTextBoxCell;
    IECRelationshipClass ecClass = itemClassCell.Tag as IECRelationshipClass;

    return ecClass;
    }

private void RefillItems
(
int rowIndex,
string className
)
    {
    DgnUtilities dgnUtil = DgnUtilities.GetInstance();

    //ArrayList arr = dgnUtil.GetECInstanceDisplayLabelsFromDgn (className);
    ECInstanceList arr = DgnUtilities.GetInstance().GetAllInstancesFromDgn(className);

    DataGridViewComboBoxCell itemInstanceCell = this[s_WBSInstanceKey, rowIndex] as DataGridViewComboBoxCell;
    itemInstanceCell.Items.Clear();
    itemInstanceCell.Tag = SchemaUtilities.PlantSchema.GetClass (className);

    itemInstanceCell.Value = null;

    if (_bMultiInst)
        itemInstanceCell.Items.Add(_cstr_Varies);

    itemInstanceCell.Items.Add (LocalizableStrings.None);

    Dictionary<string, IECInstance> dropDownCache = new Dictionary<string, IECInstance> ();
    foreach (IECInstance returnedInstance in arr)
    //for (int j = 0; j < arr.Count; j++)
        {
        string key = SchemaUtilities.GetDisplayNameForECInstance(returnedInstance, true);
        if (dropDownCache.ContainsKey (key))
            dropDownCache[key] = returnedInstance;
        else
            {
            // insert
            dropDownCache.Add(key, returnedInstance);
            itemInstanceCell.Items.Add (key);
            }

        }
    if (dropDownCache.Count >0)
        itemInstanceCell.Tag = dropDownCache;

    //for (int j = 0; j < arr.Count; j++)
    //    itemInstanceCell.Items.Add (arr[j]);

    if (arr.Count > 0)
        itemInstanceCell.Value = SchemaUtilities.GetDisplayNameForECInstance(arr[0], true);
    else
        itemInstanceCell.Value = LocalizableStrings.None;
    }


private void OnResizeGrid
(
object sender,
EventArgs e
)
    {
    //this._associatedInstance.Width = this.Width - 155;
    int iWidthUsed = this._associatedClass.Width + 5;
    if (this.optionAdd.Visible)
        iWidthUsed += this.optionAdd.Width;
    if (this.optionDelete.Visible)
        iWidthUsed += this.optionDelete.Width;
    if (this.optionEdit.Visible)
        iWidthUsed += this.optionEdit.Width;
    if(this.optionBrowse.Visible)
        iWidthUsed += this.optionBrowse.Width;

    this._associatedInstance.Width = this.Width - iWidthUsed;
    }

private void OnCellMouseClick
(
object sender,
DataGridViewCellMouseEventArgs e
)
    {
    if (e.RowIndex == -1)
        return;

    if (e.ColumnIndex == 1)
        {
        this.BeginEdit (true);
        ComboBox comboBox = (ComboBox)this.EditingControl;
        if(null != comboBox)
            comboBox.DroppedDown = true;
        }

    if (e.ColumnIndex < AddButtonColumn)
        return;

    DgnUtilities dgnUtil = DgnUtilities.GetInstance();
    IECClass ecClass = getECClassInformationForCurrentSelection (e.RowIndex);
    //this[ClassColumn, e.RowIndex].Tag as IECClass;
    IECRelationshipClass ecRelClass = getECRelationshipClassInformationForCurrentSelection (e.RowIndex);
    //this[ComboBoxColumn, e.RowIndex].Tag as IECRelationshipClass;
    DataGridViewComboBoxCell itemInstanceCell = this[s_WBSInstanceKey, e.RowIndex] as DataGridViewComboBoxCell;

    string strVal = this[s_WBSInstanceKey, e.RowIndex].Value as string;
    if (strVal == LocalizableStrings.None && (e.ColumnIndex != AddButtonColumn && e.ColumnIndex != BrowseButtonColumn))
        {
        this[e.ColumnIndex, e.RowIndex].Selected = false;
        return;
        }

    //ADD///////////////////////////////////////////////////////////////////////////////////////////////////////////
    if (e.ColumnIndex == AddButtonColumn)
        {
        // If we are connected to iModelHub we should acquire a a new tag for WBS
        
        ECInstanceDialog ecDlg = new ECInstanceDialog(ecClass.Name);
        ecDlg.ShowDialog();
        //User didn't save the new instance
        if (!ecDlg.SavePressed)
            return;
        //We were not able to save the new instance
        if (!ecDlg.SaveData())
            return;

        RefillItems (e.RowIndex, ecClass.Name);

        if (itemInstanceCell.Items.Count > 0)
            itemInstanceCell.Value = SchemaUtilities.GetDisplayNameForECInstance (ecDlg.GetECInstance(), false);

        }
    //EDIT//////////////////////////////////////////////////////////////////////////////////////////////////////////
    else if (e.ColumnIndex == EditButtonColumn)
        {
        IECInstance ecInst = this[e.ColumnIndex, e.RowIndex].Tag as IECInstance;
        if (ecInst == null)
            MessageBox.Show (LocalizableStrings.NoEditDesc, LocalizableStrings.NoEditTitle, MessageBoxButtons.OK, MessageBoxIcon.Stop);
        else
            {
            IECInstance ecInstance = EditECInstance (ecInst);
            if (ecInstance == null)
                return;

            RefillItems (e.RowIndex, ecClass.Name);

            if (itemInstanceCell.Items.Count > 0)
                itemInstanceCell.Value = SchemaUtilities.GetDisplayNameForECInstance (ecInstance, false);

            _bInstanceModified = true;
            }

        }
    //DELETE////////////////////////////////////////////////////////////////////////////////////////////////////////
    else if (e.ColumnIndex == DeleteButtonColumn)
        {
        IECInstance ecInst = this[e.ColumnIndex, e.RowIndex].Tag as IECInstance;
        if (ecInst == null)
            MessageBox.Show (LocalizableStrings.NoDeleteDescRefOut, LocalizableStrings.NoDeleteTitle, MessageBoxButtons.OK, MessageBoxIcon.Stop);
        else
            {
            //.GetRelationshipInstances (ecClass.Name, _instance.ClassDefinition.Name, ecRelClass.Name, dgnUtil.GetDGNConnection());
            ECInstanceList ecList = dgnUtil.GetRelatedInstances (ecInst, ecRelClass, _ClassDef, dgnUtil.GetDGNConnection ());
            if (ecList.Count > 0)
                {
                MessageBox.Show(string.Format(LocalizableStrings.NoDeleteDescAssocCompFound, itemInstanceCell.Value), LocalizableStrings.NoDeleteTitle, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
                }

            if (MessageBox.Show (String.Format (LocalizableStrings.AreYouSureDelete, SchemaUtilities.GetDisplayNameForECInstance (ecInst, true)),
                                 String.Format (LocalizableStrings.DeleteInstanceTitle, ecInst.ClassDefinition.DisplayLabel), MessageBoxButtons.YesNo, MessageBoxIcon.Stop) == DialogResult.No)
                return;

            dgnUtil.DeleteECInstanceFromDgn (ecInst, dgnUtil.GetDGNConnection ());

            RefillItems (e.RowIndex, ecClass.Name);

            if (itemInstanceCell.Items.Count > 0)
                itemInstanceCell.Value = itemInstanceCell.Items[0];
            else
                itemInstanceCell.Value = null;
            }
        _bInstanceModified = true;

        }
        //BROWSE////////////////////////////////////////////////////////////////////////////////////////////////////////
        else if(e.ColumnIndex == BrowseButtonColumn)
            {

            IECInstance newEcinstance = ecClass.CreateInstance () as IECInstance;
            if(newEcinstance == null)
                return;

            object instance = dgnUtil.TagBrowser (newEcinstance, dgnUtil.AssoicatedItemsTagBrowserOverrides);
            if(instance == null || !(instance is IECInstance))
                return;
            IECInstance selectedECInstance = instance as IECInstance;

                //CommonTools.BIMAdapter.BIMAdapter.ReleaseTag (newEcinstance);
            dgnUtil.CopyProperties (selectedECInstance, newEcinstance, false);
            //BusinessKeyUtility.Release (newEcinstance);
            BusinessKeyUtility.SetIsReserved (newEcinstance, true);
            dgnUtil.UpdateFunctionalGuidProperty (selectedECInstance, newEcinstance);
            dgnUtil.WriteECInstanceToDgn (newEcinstance, dgnUtil.GetDGNConnection ());

                //CommonTools.BIMAdapter
                //IECInstance inst = EditECInstance (ecInstance);
                //if(inst == null)
                //    return;
                //    inst = ecInstance;

            RefillItems (e.RowIndex, ecClass.Name);

            if(itemInstanceCell.Items.Count > 0)
                itemInstanceCell.Value = SchemaUtilities.GetDisplayNameForECInstance (newEcinstance, false);

            _bInstanceModified = true;
            }
        this[e.ColumnIndex, e.RowIndex].Selected = false;

        }

        private IECInstance EditECInstance (IECInstance ecInst)
            {
            ECInstanceDialog ecDlg = new ECInstanceDialog (ecInst);

            ecDlg.DialogTitle = string.Format (LocalizableStrings.EditingInstanceTitle, 
                ecInst.ClassDefinition.DisplayLabel, SchemaUtilities.GetDisplayNameForECInstance (ecInst, false));
            ecDlg.ShowDialog ();
            //User didn't save the new instance
            if(!ecDlg.SavePressed)
                return null;

            //We were not able to save the new instance
            if(!ecDlg.SaveData ())
                return null;

            return ecDlg.GetECInstance();
            }


private void OnCellValueChanged
(
object sender,
DataGridViewCellEventArgs e
)
    {
    if (e.ColumnIndex != 1)
        return;

    if (e.RowIndex < 0 || e.ColumnIndex < 0)
        return;

    IECClass ecClass = getECClassInformationForCurrentSelection (e.RowIndex);
    //this[ClassColumn, e.RowIndex].Tag as IECClass;
    IECRelationshipClass ecRelClass = getECRelationshipClassInformationForCurrentSelection(e.RowIndex);
    //this[ClassColumn, e.RowIndex].Tag as IECRelationshipClass;
    string strVal = LocalizableStrings.None;
    if (null != this[ComboBoxColumn, e.RowIndex].Value)
        strVal = this[ComboBoxColumn, e.RowIndex].Value.ToString();

    if (strVal == _cstr_Varies)
        return;

    DgnUtilities dgnUtil = DgnUtilities.GetInstance();

    IECInstance newECIsnt = getInstanceForCurrentSelection (e.RowIndex, strVal);

    // get from Tag
    //dgnUtil.GetSourceInstance (ecClass, strVal);

    foreach (IECInstance ecInstance in _instList)
    {
        IECRelationshipInstanceCollection ecRelCol = ecInstance.GetRelationshipInstances();
        IEnumerator<IECRelationshipInstance> ecRelInstEnum = ecRelCol.GetEnumerator();

        bool newSourceSet = false;
        while (ecRelInstEnum.MoveNext())
            {
            IECRelationshipInstance ecRelInst = ecRelInstEnum.Current;
            if (!ecRelInst.ClassDefinition.Is(ecRelClass))
                continue;

            if (strVal == LocalizableStrings.None || string.IsNullOrEmpty(strVal))
                ecRelCol.Remove(ecRelInst);
            else
                ecRelInst.Source = newECIsnt;
            newSourceSet = true;
            break;
            }

        if (!newSourceSet && newECIsnt != null)
            {
            IECRelationshipInstance temporaryRelInst = ecRelClass.CreateInstance() as IECRelationshipInstance;
            temporaryRelInst.Source = newECIsnt;
            temporaryRelInst.Target = ecInstance;
            ecInstance.GetRelationshipInstances().Add(temporaryRelInst);
            }

        IECInstance ecInst = newECIsnt;
        if (ecInst != null)
            {
            if ((dgnUtil.IsECInstanceFromReferenceFile(ecInst) || dgnUtil.IsECInstanceReferencedOut(ecInst) || ecInst.IsReadOnly))
                ecInst = null;
            }

        DataGridViewImageCell tempImageCell = this["optionEdit", e.RowIndex] as DataGridViewImageCell;
        tempImageCell.Tag = ecInst;

        tempImageCell = this["optionDelete", e.RowIndex] as DataGridViewImageCell;
        tempImageCell.Tag = ecInst;

        //Mark the property (having business key CA/or NAME property if no Business key is defined) as changed and set the event which will reset the property pane.
        //This is required to update UNIT and SERVICE property listed in property pane on the fly whenever user changes the UNIT or SERVICE in ASSOCIATIONS pane.
        IECPropertyValue propVal = BusinessKeyUtility.GetPropertyValue (ecInstance);
        if (propVal != null)
            ecInstance.OnECPropertyValueChanged (propVal);
        }

    SaveSettings();
    }

public void DisableAssociationModifications(bool bDisable)
    {
    this.optionAdd.Visible = !bDisable;
    this.optionEdit.Visible = !bDisable;
    this.optionDelete.Visible = !bDisable;
    this.optionBrowse.Visible = !bDisable;
    this.OnResize(new EventArgs());
    }

/// <summary>
/// Constructor
/// </summary>
public AssociatedItemsControl() : base ()
    {
    InitializeComponent();
    //I10N
    this._associatedInstance.HeaderText = LocalizableStrings.AvailableItems;
    this._associatedClass.HeaderText = LocalizableStrings.Item;    
    this.ShowCellToolTips = true;

    this.optionBrowse.Visible = CommonTools.BIMAdapter.BIMAdapter.Connected;
    this.optionEdit.Visible = DgnUtilities.GetInstance().AllowWBSEdit;
    }

void OnCurrentCellDirtyStateChanged(object sender, EventArgs e)
    {
    //This event was added to fix the problem mention in the defect 'D-104966'
    //OP3D SS5_OPM: Equipment entries incorrectly appear under unit instead of respective service under Service node in Item Browser
    //Normal workflow: User would first change/set Unit in the Place Equipment window and then Service and will go on to place the equipment. Since we get
    //notification that something has changed in the event "OnCellValueChanged()", and this event would only get fired when focus would be lost, so no value
    //got set for the Service before it was placed resulting in the defect mentioned above. To overcome this, this event was added so that control will monitor
    //the change in dirty state of the cell, and once we get the event and we can tell the control to commit the change, which will result in firing of
    //'OnCellValueChanged' event before the focus is lost from combobox.
    if (this.IsCurrentCellDirty)
        this.CommitEdit(DataGridViewDataErrorContexts.Commit);
    }

public bool FillAssociatedItems(bool isModifyTool)
    {
    ArrayList classRelationshipStrs = SchemaUtilities.GetAssociatedItems (_ClassDef);
    if (classRelationshipStrs.Count == 0)
        return false;

    if (_bMultiInst)
        {//when editing associations of multiple instances, edit and delete options should not be available.
        this.optionDelete.Visible = false;
        this.optionEdit.Visible = false;

        //refresh/redraw the grid
        this.OnResize(new EventArgs());
        }

    for (int i = 0; i < classRelationshipStrs.Count; i++)
        {
        string strData = classRelationshipStrs[i] as string;
        string[] strArr = strData.Split(new char[]{'|'});

        IECClass ecAssociatedClass = SchemaUtilities.PlantSchema.GetClass(strArr[0]);
        int rowIndex = this.Rows.Add();
        DataGridViewTextBoxCell itemTypeCell = this[s_WBSClassKey, rowIndex] as DataGridViewTextBoxCell;
        itemTypeCell.Value = ecAssociatedClass.DisplayLabel;
        //itemTypeCell.Tag = ecAssociatedClass;

        DataGridViewComboBoxCell itemInstanceCell = this[s_WBSInstanceKey, rowIndex] as DataGridViewComboBoxCell;
        //ArrayList arr = DgnUtilities.GetInstance().GetECInstanceDisplayLabelsFromDgn (strArr[0]);
        ECInstanceList arr = DgnUtilities.GetInstance().GetAllInstancesFromDgn(strArr[0]);
        IECRelationshipClass ecAssociatedRel = SchemaUtilities.PlantSchema.GetClass (strArr[1]) as IECRelationshipClass;
        // move class to 1st column tag, to start with,  later changed

        itemInstanceCell.Tag = ecAssociatedClass;
        itemTypeCell.Tag = ecAssociatedRel;

        if (_bMultiInst)
            itemInstanceCell.Items.Add(_cstr_Varies);

        itemInstanceCell.Items.Add (LocalizableStrings.None);

        Dictionary<string, IECInstance> dropDownCache = new Dictionary<string, IECInstance> ();

        foreach (IECInstance returnedInstance in arr)
        //for (int j = 0; j < arr.Count; j++)
            {
            // insert only if this is not from a referenced file
            if(DgnUtilities.GetInstance ().IsECInstanceFromReferenceFile (returnedInstance))
                continue;

            string key = SchemaUtilities.GetDisplayNameForECInstance(returnedInstance, true);
            if (dropDownCache.ContainsKey (key))
                dropDownCache[key] = returnedInstance;
            else
                {
                // insert
                    dropDownCache.Add(key, returnedInstance);
                    itemInstanceCell.Items.Add (key);
                }

            }
        if (dropDownCache.Count >0)
            itemInstanceCell.Tag = dropDownCache;

        if (isModifyTool)
            {
            string strLastInstID = null;
            string key = "";
            for (int j = 0; j < _instList.Count; j++)
                {
                IECInstance parentInstance = GetAssociatedItemFromInstance(_instList[j], ecAssociatedRel, ecAssociatedRel.Source.GetClasses()[0].Class);
                string strCurrInstID = "";

                // If we have a parent instance, then we must be using an existing instance. We need to make sure we select the existing values.
                if (null != parentInstance)
                    {
                    key = SchemaUtilities.GetDisplayNameForECInstance(parentInstance, true);
                    strCurrInstID = parentInstance.InstanceId;
                    }
                else
                    {
                    key = LocalizableStrings.None;
                    strCurrInstID = LocalizableStrings.None;
                    }

                if (String.IsNullOrEmpty(strLastInstID))
                    strLastInstID = strCurrInstID;
                else
                    {
                    if (strCurrInstID != strLastInstID)
                        {
                        key = _cstr_Varies;
                        break;
                        }
                    }
                } // end for loop

            itemInstanceCell.Value = key;
            }

        itemInstanceCell.Sorted = true;

        DataGridViewImageCell tempImageCell = this["optionAdd", rowIndex] as DataGridViewImageCell;
        tempImageCell.Tag = ecAssociatedClass;
        }

    if (!isModifyTool)
        {
        ArrayList AssociatonList = WorkspaceUtilities.GetSettings("Associations." + _ClassDef.Name);
        LoadSettings(AssociatonList, false);
        }

    return true;
    }

private void SaveSettings()
    {
    ArrayList arr = new ArrayList();

    for (int i = 0; i < this.Rows.Count; i++)
        {
        //DataGridViewTextBoxCell itemTypeCell = this[s_wbsClass, i] as DataGridViewTextBoxCell;
        //IECClass ecClass = itemTypeCell.Tag as IECClass;

        DataGridViewComboBoxCell itemInstanceCell = this[s_WBSInstanceKey, i] as DataGridViewComboBoxCell;
        string selectedInstanceString = itemInstanceCell.Value as string;
        IECClass ecClass = getECClassInformationForCurrentSelection (i);
        //itemInstanceCell.Tag as IECClass;

        arr.Add (ecClass.Name + "=" + selectedInstanceString);
        }

    WorkspaceUtilities.SaveSettings ("Associations." + _ClassDef.Name, arr);
    }


public bool LoadSettings
(
ArrayList arr,
bool resetStandardPrefOnOnImport
)
    {
    Hashtable hTable    = new Hashtable ();
    bool status = true;

    string strUseStandPref = GetMSConfigVariable("OPM_EQUIP_ASSOC_USE_STANDPREF");
    if (Get2D3DInstance())
        strUseStandPref = "1";
    if (resetStandardPrefOnOnImport)
        strUseStandPref = "";

    for (int i = 0; i < arr.Count; i++)
        {
        string      strLine = arr[i] as string;
        string[]    strs    = strLine.Split (new char[]{'='});
        if(!hTable.ContainsKey(strs[0]))
            hTable.Add (strs[0], strs[1]);
        }

    if (this == null)
        return status;

    for (int i = 0; i < this.Rows.Count; i++)
        {
        DataGridViewTextBoxCell     itemTypeCell        = this[s_WBSClassKey, i] as DataGridViewTextBoxCell;
        IECClass                    ecClass             = getECClassInformationForCurrentSelection (i);
        DataGridViewComboBoxCell    itemInstanceCell    = this[s_WBSInstanceKey, i] as DataGridViewComboBoxCell;

        if (hTable.Contains (ecClass.Name))
            {
            if (!string.IsNullOrEmpty (strUseStandPref) && strUseStandPref == "1")
                {
                string ret = ReadStandPref (ecClass.Name);
                if (itemInstanceCell.Items.Contains (ret))
                    {
                    itemInstanceCell.Value = ret;
                    continue;
                    }
                }
            if (itemInstanceCell.Items.Contains (hTable[ecClass.Name]))
                itemInstanceCell.Value = hTable[ecClass.Name];
            else
                {
                string ret = ReadStandPref(ecClass.Name);
                if (itemInstanceCell.Items.Contains(ret))
                    {
                    itemInstanceCell.Value = ret;
                    }
                else
                    {
                    itemInstanceCell.Value = LocalizableStrings.None;
                    status = false;
                    }
                }
            }
        else
            {
            string ret = ReadStandPref(ecClass.Name);

            if (itemInstanceCell.Items.Contains(ret))
                {
                itemInstanceCell.Value = ret;
                }
            else
                {
                itemInstanceCell.Value = LocalizableStrings.None;
                status = false;
                }
            }
        }
    return status;
    }

    private string GetMSConfigVariable
    (
    string Var
     )
    {
        string ret = string.Empty;
        try
        {
            Assembly _apiAssembly = Assembly.Load ("Bentley.OpenPlant.Modeler.Api");
            Type bmInstanceManager = _apiAssembly.GetType ("Bentley.OpenPlant.Modeler.Api.BMECInstanceManager");
            MethodInfo n = bmInstanceManager.GetMethod ("FindConfigVariableName");
            Object[] param = { Var };
            return n.Invoke (null, param).ToString ();
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
        }
        return ret;
    }
    
private static string ReadStandPref(string ecClassName)
    {
    try
        {
        System.Reflection.Assembly assembly = System.Reflection.Assembly.Load("Bentley.Plant.StandardPreferences");
        System.Reflection.MethodInfo m =
            assembly.GetType("Bentley.Plant.StandardPreferences.DlgStandardPreference")
                .GetMethod("GetPreferenceValue");
        Object[] param = {ecClassName};
        System.Object ret = m.Invoke(null, param);
        return (string) ret;
        }
    catch (Exception ex)
        {
        string msg = ex.Message;
        }
    return string.Empty;
    }

private bool Get2D3DInstance()
    {
    try
        {
        System.Reflection.Assembly assembly = System.Reflection.Assembly.Load("MechAddin"); 
        System.Reflection.MethodInfo m = assembly.GetType("Bentley.Building.Mechanical.MechAddIn").GetMethod("GetIs2D3DInstance");
        System.Object ret = m.Invoke(null, null);
        return (bool) ret;
        }
    catch (Exception ex)
        {
        string msg = ex.Message;
        }
    return false;
    }

public ECInstanceList InstanceList
    {
    get {return _instList;}
    set {
        _instList = value;
        if (_instList != null && _instList.Count > 0)
            {
            _ClassDef = _instList[0].ClassDefinition;
            _bMultiInst = (_instList.Count > 1) ? true : false;
            }
        }
    }

public IECInstance Instance
    {
    get {
        if (null != _instList && _instList.Count > 0)
            return _instList[0];
        else
            return null;
        }

    set {
        if (null == _instList)
            _instList = new ECInstanceList();
        else
            _instList.Clear();

        _instList.Add(value);
        _ClassDef = value.ClassDefinition;
        _bMultiInst = false;
        }
    }

void OnEditingControlShowing(object sender, System.Windows.Forms.DataGridViewEditingControlShowingEventArgs e)
    {
    if (this.Columns[this.CurrentCell.ColumnIndex].Name.Equals(s_WBSInstanceKey))
        {
        ComboBox cbb = e.Control as ComboBox;

        //first remove any existing handlers
        cbb.DropDown -= new EventHandler(OnComboDropDown);
        cbb.DropDownClosed -= new EventHandler(OnComboDropDownClosed);

        //add new handlers
        cbb.DropDown += new EventHandler(OnComboDropDown);
        cbb.DropDownClosed += new EventHandler(OnComboDropDownClosed);
        }
    }

void OnComboDropDownClosed(object sender, EventArgs e)
    {
    ComboBox cbb = sender as ComboBox;
    if (_bMultiInst)
        cbb.Items.Add (_cstr_Varies);
    }

void OnComboDropDown(object sender, EventArgs e)
    {
    ComboBox cbb = sender as ComboBox;
    if (_bMultiInst)
        cbb.Items.Remove(_cstr_Varies);
    }

public void UpdateWBSParts(IECInstance selectedECInstance)
    {
    try
        {
        if(selectedECInstance == null)
            return;
        ArrayList assocItems = SchemaUtilities.GetAssociatedItemInformation (selectedECInstance.ClassDefinition);
        UpdateWBSParts (assocItems, selectedECInstance);
        }
    catch ( Exception ex)
        {
        WorkspaceUtilities.DisplayErrorMessage (ex.Message, string.Empty);
        }
    }

public void UpdateWBSParts(ArrayList assocItems, IECInstance selectedECInstance)
    {
    if(assocItems == null || assocItems.Count == 0 || selectedECInstance == null)
        return;

    //set cache of WBS items (classname, relationship name)
    IDictionary<string, string> aitems = new Dictionary<string, string> ();
    foreach(string item in assocItems)
        {
        string[] part = item.Split (new char[] { '|' });
        if(part.Length < 1)
            continue;

        string className = part[0];
        string relClassName = part[1];
        if(!aitems.ContainsKey (className))
            aitems.Add (className, relClassName);
        }

    DgnUtilities dgnUtil = DgnUtilities.GetInstance ();
    IECRelationshipInstanceCollection ecList = selectedECInstance.GetRelationshipInstances ();
    if(ecList == null || ecList.Count == 0)
        return;

    bool needWBS = false;
    string msg = string.Empty;
    //iterate thru relationship instances
    IEnumerator en = ecList.GetEnumerator ();
    while(en.MoveNext ())
        {
        IECRelationshipInstance rel = en.Current as IECRelationshipInstance;
        if(rel == null)
            continue;

        string wbsRelationshipName = rel.ClassDefinition.Name;
        IECInstance wbsSource = rel.Source;
        if(wbsSource == null)
            continue;

        string wbsSourceClassName = wbsSource.ClassDefinition.Name;
        //match wbs type and pass to UpdateWBSComboxValue()
        if(!aitems.ContainsKey (wbsSourceClassName))
            continue;

        if(UpdateWBSComboxValue (wbsSource, wbsRelationshipName) == WBSUpdateType.NeedToCreate)
            {
            IECPropertyValue sourceVal = wbsSource.FindPropertyValue (s_WBSName, false, false, false);
            string val = string.Empty;
            if(sourceVal != null  && !sourceVal.IsNull)
                val = sourceVal.StringValue;

            msg = msg + "\n" + string.Format (LocalizableStrings.MissingWbs, wbsSourceClassName, val);
            needWBS = true;
            }
        }
    if (needWBS && !string.IsNullOrEmpty(msg))
        {
        string val = WorkspaceUtilities.GetMSConfigVariable ("AllowPlacementOnMissingWBSItems");
        if (string.IsNullOrEmpty(val) || val != "1")
            dgnUtil.RunKeyin ("BMECH DEFERDEFAULTCMD");

        //this value is used in consistency manager placement. 
        //setting to false allows CM to display instances correctly
        dgnUtil.ConsistencyPlacementValidState = false;

        string message = string.Format (LocalizableStrings.AlertMissingWbs, msg);
        dgnUtil.ConsistencyPlacementErrorMessage = message;
        MessageBox.Show (message, LocalizableStrings.WBSAlert, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        WorkspaceUtilities.DisplayWarningMessage (LocalizableStrings.WBSAlert, message);
        }
    }

    private int GetRowIdFromClassName(string clsName)
    {
    try
        {
        for(int i = 0 ; i < this.Rows.Count ; i++)
            {
            ECClass ecClass = this[s_WBSClassKey, i].Tag as ECClass;
            if(ecClass.Name.Equals (clsName))
                return i;
            }
        }
    catch { } // catch if s_WBSClassKey is not valid
    return -1;
    }

//update the WBS specific item based on the wbs ecinstance and relationship name
private WBSUpdateType UpdateWBSComboxValue(IECInstance sourceWBS, string WBSRrelationshipName)
    {

    //set source WBS values
    //these values will be matched against values in existing datagrid combo box
    string sourceName = sourceWBS.ClassDefinition.Name;
    IECPropertyValue sourceVal = sourceWBS.FindPropertyValue (s_WBSName, false, false, false);
    if(sourceVal == null || sourceVal.IsNull)
        return WBSUpdateType.Error;

    string sourceWbsValue = sourceVal.StringValue;
    DataGridViewCell dataGridClassCell = null;
    DataGridViewCell dataGridInstanceCell = null;

    //iterate items and try match passed in value against associated datagrid combox items
    for(int i = 0 ; i < this.Rows.Count ; i++)
        {
        ECClass ecClass = this[s_WBSClassKey, i].Tag as ECClass;
        if(!ecClass.Name.Equals (WBSRrelationshipName))
            continue;

        string currentValue = this[s_WBSInstanceKey, i].Value as string;
        //values are the same no need to update
        if(currentValue.Equals (sourceWbsValue))
            return WBSUpdateType.ExistingFoundInDgn;

        //list of existing WBS items are stored in tag
        Dictionary<string, IECInstance> cachedInstances = this[s_WBSInstanceKey, i].Tag as Dictionary<string, IECInstance>;
        if(cachedInstances == null || cachedInstances.Count == 0)
            continue;

        foreach(IECInstance wbsInst in cachedInstances.Values)
            {
            IECPropertyValue wbsPropval = wbsInst.FindPropertyValue (s_WBSName, false, false, false);
            if(wbsPropval == null || wbsPropval.IsNull)
                return WBSUpdateType.Error;

            //if match is found set datagrid values
            if(wbsPropval.StringValue.Equals (sourceWbsValue))
                {
                dataGridClassCell = this[s_WBSClassKey, i];
                dataGridInstanceCell = this[s_WBSInstanceKey, i];
                break;
                }
            }

        if(dataGridInstanceCell != null && dataGridClassCell != null)
            break;
        }

    //if all those values are found, set datagrid values
    if(dataGridInstanceCell != null && dataGridClassCell != null)
        {
        dataGridClassCell.Selected = true;
        dataGridInstanceCell.Selected = true;
        dataGridInstanceCell.Value = sourceWbsValue;
        return WBSUpdateType.ExistingFoundInDgn;
        }
    return WBSUpdateType.NeedToCreate;
    }
private enum WBSUpdateType
    {
    Error,
    ExistingFoundInDgn,
    NeedToCreate
    }

private void AssociatedItemsControl_CellFormatting(object sender, System.Windows.Forms.DataGridViewCellFormattingEventArgs e)
    {
    var cell = this.Rows[e.RowIndex].Cells[e.ColumnIndex];
    cell.ToolTipText = this.Columns[e.ColumnIndex].ToolTipText;
    }


        }
    }

