﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Glue;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Utilities;
using FlatRedBall.Glue.TypeConversions;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins.AddVariablePlugin.ViewModels;

namespace FlatRedBall.Glue.Controls
{
    #region CustomVariableType enum

    public enum CustomVariableType
    {
        Exposed,
        Tunneled,
        New
    }

    #endregion

    public partial class AddVariableWindow : Form
    {
        #region Fields

        CreateNewVariableViewModel createNewVariableViewModel;
        IElement element;

        #endregion

        #region Properties

        public CustomVariableType DesiredVariableType
        {
            get
            {
                if (radExistingVariable.Checked)
                    return CustomVariableType.Exposed;
                else if (radTunnelVariable.Checked)
                    return CustomVariableType.Tunneled;
                else
                    return CustomVariableType.New;
            }
            set
            {
                switch (value)
                {
                    case CustomVariableType.Exposed:
                        radExistingVariable.Checked = true;
                        break;
                    case CustomVariableType.Tunneled:
                        radTunnelVariable.Checked = true;
                        break;
                    case CustomVariableType.New:
                        radCreateNewVariable.Checked = true;
                        break;
                }
            }
        }

        public string ResultName
		{
			get 
            {
                if (radExistingVariable.Checked)
                    return this.AvailableVariablesComboBox.Text;
                else if (radTunnelVariable.Checked)
                    return this.AlternativeNameTextBox.Text;
                else
                    return createNewVariableControl1.VariableName;
            }
		}

		public string ResultType
		{
			get 
            {
                if (radExistingVariable.Checked)
                {
                    if (EditorLogic.CurrentEntitySave != null)
                    {
                        string type = ExposedVariableManager.GetMemberTypeForEntity(ResultName, EditorLogic.CurrentEntitySave);

                        return TypeManager.ConvertToCommonType(type);

                    }
                    else
                    {
                        string type = ExposedVariableManager.GetMemberTypeForScreen(ResultName, EditorLogic.CurrentScreenSave);

                        return TypeManager.ConvertToCommonType(type);
                    }
                }
                else if (radTunnelVariable.Checked)
                {
                    NamedObjectSave nos = GlueState.Self.CurrentElement.GetNamedObjectRecursively(TunnelingObjectComboBox.Text);
                    string type = ExposedVariableManager.GetMemberTypeForNamedObject(nos, this.TunnelingVariableComboBox.Text);

                    return TypeManager.ConvertToCommonType(type);
                }
                else
                {
                    return createNewVariableControl1.SelectedType;
                }
            }
		}

        public string TunnelingObject
        {
            get
            {
                if (radTunnelVariable.Checked)
                    return TunnelingObjectComboBox.Text;
                else
                    return null;
            }
            set
            {
                TunnelingObjectComboBox.Text = value;
            }
        }

        public string TunnelingVariable
        {
            get
            {
                if (radTunnelVariable.Checked)
                    return TunnelingVariableComboBox.Text;
                else
                    return null;
            }
            set
            {
                TunnelingVariableComboBox.Text = value;
            }
        }

        public string OverridingType
        {
            get 
            {
                if (OverridingVariableTypeComboBox.SelectedIndex > 0)
                    return OverridingVariableTypeComboBox.Text;
                else
                    return null;
            }
        }

        public string TypeConverter
        {
            get { return TypeConverterComboBox.Text; }
        }

        public bool IsStatic
        {
            get
            {
                return createNewVariableControl1.IsStatic;
            }
        }

        #endregion

        #region Methods

        public AddVariableWindow(IElement element)
		{
			InitializeComponent();

            this.element = element;

            TypeConverterHelper.InitializeClasses();

			StartPosition = FormStartPosition.Manual;
			Location = new Point(MainGlueWindow.MousePosition.X - this.Width/2, 
                System.Math.Max(0,MainGlueWindow.MousePosition.Y - Height/2));
            

            FillExposableVariables();

            FillTunnelingObjects();

            FillOverridingTypesComboBox();

            FillTypeConverters();


            // force set visibility
            radCreateNewVariable_CheckedChanged(this, null);

            createNewVariableViewModel = new CreateNewVariableViewModel();
            createNewVariableControl1.DataContext = createNewVariableViewModel;
            createNewVariableViewModel.PropertyChanged += HandleCreateNewVariablePropertyChanged;
            FillNewVariableTypes();

            createNewVariableControl1.SelectedType = createNewVariableViewModel.AvailableVariableTypes.FirstOrDefault();
        }

        private void HandleCreateNewVariablePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(CreateNewVariableViewModel.IncludeStateCategories):
                    FillNewVariableTypes();
                    break;
            }
        }

        private void FillNewVariableTypes()
        {
            List<string> newVariableTypes = ExposedVariableManager.GetAvailableNewVariableTypes(allowNone:false, 
                includeStateCategories:createNewVariableViewModel.IncludeStateCategories);

            createNewVariableViewModel.AvailableVariableTypes.Clear();

            foreach (var type in newVariableTypes)
            {
                createNewVariableViewModel.AvailableVariableTypes.Add(type);
            }

            //createNewVariableControl1.FillAvailableTypes(newVariableTypes);
        }

        private List<string> GetNewVariableTypes()
        {
            throw new NotImplementedException();
        }

        private void FillTypeConverters()
        {
            List<string> converters = AvailableCustomVariableTypeConverters.GetAvailableConverters();

            foreach (string converter in converters)
                TypeConverterComboBox.Items.Add(converter);

            if (TypeConverterComboBox.Items.Count > 0)
                TypeConverterComboBox.SelectedIndex = 0;
        }

        private void FillOverridingTypesComboBox()
        {
            foreach (string propertyType in ExposedVariableManager.AvailablePrimitives)
            {
                OverridingVariableTypeComboBox.Items.Add(propertyType);
            }

            if (OverridingVariableTypeComboBox.Items.Count > 0)
            {
                OverridingVariableTypeComboBox.SelectedIndex = 0;
            }
        }

        private void FillExposableVariables()
        {
            List<string> availableVariables = null;

            if (EditorLogic.CurrentEntitySave != null)
            {
                availableVariables = ExposedVariableManager.GetExposableMembersFor(EditorLogic.CurrentEntitySave, true).Select(item=>item.Member).ToList();

            }
            else if (EditorLogic.CurrentScreenSave != null)
            {
                availableVariables = ExposedVariableManager.GetExposableMembersFor(EditorLogic.CurrentScreenSave, true).Select(item => item.Member).ToList();
            }

            if (availableVariables != null)
            {
                // We don't want to expose things like velocity an acceleration in Glue
                List<string> velocityAndAccelerationVariables = ExposedVariableManager.GetPositionedObjectRateVariables();
                // We also don't want to expose relative values - the user just simply sets the value and the state/variable handles
                // whether it sets relative or absolute depending on whether the Entity is attached or not.
                // This behavior used to not exist, but users never knew when to use relative or absolute, and
                // that level of control is not really needed...if it is, custom code can probably handle it.
                List<string> relativeVariables = ExposedVariableManager.GetPositionedObjectRelativeValues();

                foreach (string variableName in availableVariables)
                {
                    if (!velocityAndAccelerationVariables.Contains(variableName) && !relativeVariables.Contains(variableName))
                    {
                        AvailableVariablesComboBox.Items.Add(variableName);
                    }
                }

                if (AvailableVariablesComboBox.Items.Count > 0)
                    AvailableVariablesComboBox.SelectedIndex = 0;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void AddVariableWindow_Load(object sender, EventArgs e)
        {

            
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {

            }
        }

        private void AddVariableWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            // See if if the user is trying to create a reserved variable
            if (this.DialogResult == System.Windows.Forms.DialogResult.OK &&
                this.radCreateNewVariable.Checked && 
                EditorLogic.CurrentEntitySave != null &&
                ExposedVariableManager.IsMemberDefinedByPositionedObject(this.createNewVariableControl1.VariableName)
                )
            {
                System.Windows.Forms.MessageBox.Show("The variable " + this.createNewVariableControl1.VariableName + " is " +
                    "already defined by the engine.  You can expose this variable or select a different name.");
                e.Cancel = true;
            }
        }

        private void FillTunnelingObjects()
        {
            List<string> availableObjects = AvailableNamedObjectsAndFiles.GetAvailableObjects(false, true, GlueState.Self.CurrentElement, null);
            foreach (string availableObject in availableObjects)
            {
                this.TunnelingObjectComboBox.Items.Add(availableObject);
            }
        }



        private void TunnelingVariableComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            AlternativeNameTextBox.Text = 
                TunnelingObjectComboBox.Text + 
                TunnelingVariableComboBox.Text;
        }

        private void TunnelingObjectComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string nameOfNamedObject = TunnelingObjectComboBox.Text;

            NamedObjectSave nos = GlueState.Self.CurrentElement.GetNamedObjectRecursively(nameOfNamedObject);

            PopulateTunnelingVariables(nameOfNamedObject, nos);
        }

        private void PopulateTunnelingVariables(string nameOfNamedObject, NamedObjectSave nos)
        {
            List<string> availableVariables = null;
            if (nos != null)
            {
                availableVariables = ExposedVariableManager.GetExposableMembersFor(nos).Select(item=>item.Member).ToList();


                // We should remove any variables that are already tunneled into
                foreach (CustomVariable customVariable in GlueState.Self.CurrentElement.CustomVariables)
                {
                    if (customVariable.SourceObject == nameOfNamedObject)
                    {
                        // Reverse loop since we're removing things
                        for (int i = availableVariables.Count - 1; i > -1; i--)
                        {
                            if (availableVariables[i] == customVariable.SourceObjectProperty)
                            {
                                availableVariables.RemoveAt(i);
                                break;
                            }
                        }
                    }
                }
            }

            if (availableVariables != null)
            {
                availableVariables.Sort();

                this.TunnelingVariableComboBox.Items.Clear();
                if (availableVariables != null)
                {
                    // We don't want to expose things like velocity an acceleration in Glue
                    List<string> velocityAndAccelerationVariables = ExposedVariableManager.GetPositionedObjectRateVariables();
                    // We also don't want to expose relative values - the user just simply sets the value and the state/variable handles
                    // whether it sets relative or absolute depending on whether the Entity is attached or not.
                    // This behavior used to not exist, but users never knew when to use relative or absolute, and
                    // that level of control is not really needed...if it is, custom code can probably handle it.
                    List<string> relativeVariables = ExposedVariableManager.GetPositionedObjectRelativeValues();


                    foreach (string availableVariable in availableVariables)
                    {
                        if (!velocityAndAccelerationVariables.Contains(availableVariable) && !relativeVariables.Contains(availableVariable))
                        {
                            this.TunnelingVariableComboBox.Items.Add(availableVariable);
                        }
                    }

                }
            }
        }

        private void radCreateNewVariable_CheckedChanged(object sender, EventArgs e)
        {
            bool newVariablePanelVisible = false;
            TunnelVariablePanel.Visible = false;
            ExistingVariablePanel.Visible = false;

            if (radCreateNewVariable.Checked)
            {
                newVariablePanelVisible = true;

                createNewVariableControl1.FocusTextBox();

            }
            else if (radExistingVariable.Checked)
            {
                ExistingVariablePanel.Visible = true;
            }
            else if (radTunnelVariable.Checked)
            {
                TunnelVariablePanel.Visible = true;
            }

            NewVariablePanel.Visible = newVariablePanelVisible;
        }

        #endregion

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            this.EnsureOnScreen();
        }

        private void NewVariableTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void NewTypeListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            createNewVariableControl1.FocusTextBox();
        }

        private void NewTypeListBox_Click(object sender, EventArgs e)
        {
            createNewVariableControl1.FocusTextBox();
        }
    }
}
