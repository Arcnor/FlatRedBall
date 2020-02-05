using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using FlatRedBall.Glue;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Plugins;
using System.Windows.Forms;
using FlatRedBall.Glue.Controls;
//using FlatRedBall.AnimationEditorForms;
using FlatRedBall.IO;
//using FlatRedBall.AnimationEditorForms.CommandsAndState;
using FlatRedBall.Content.AnimationChain;


namespace AnimationEditorPlugin
{

    [Export(typeof(PluginBase))]
    public class AnimationEditorPlugin : PluginBase
    {
        #region Fields

        //MainControl mAchxControl;
        //TextureCoordinateSelectionWindow mTextureCoordinateControl;
        TabControl mContainer; // This is the tab control for all tabs on the left
        PluginTab mTab; // This is the tab that will hold our control
        string mLastFile;
        TextureCoordinateSelectionLogic textureCoordinateSelectionLogic;

        int mReloadsToIgnore = 0;

        #endregion

        #region Properties


        [Import("GlueCommands")]
        public IGlueCommands GlueCommands
        {
            get;
            set;
        }

        [Import("GlueState")]
        public IGlueState GlueState
        {
            get;
            set;
        }

        public override string FriendlyName
        {
            get { return "Animation Editor"; }
        }

        public override Version Version
        {
            // 2.0 adds lots of improvements to the texture coordinate selection window.
            // 2.0.2 fixes a crash reported by users with the selection rectangle and improves
            // the region selection to show the full texture when no coordinates are specified.
            // 2.1 
            // - No longer shows the + move in all directions arrows when the user has the magic
            //   wand selected and is over a region.
            // - Camera now focuses on selected region when it's off screen when an animation chain
            //   or frame is selected.
            // - .aeproperties file now includes list of expanded nodes. This is applied whenever the
            //   .ach file is loaded
            // 2.2 - Added CTRL drag+drop file on AnimationChain to add a frame
            // 2.2.1 - Dropped frames now default to a frame length of .1 seconds
            // 2.2.2 - Fixed bug with texture selection with magic wand
            // 2.2.3 - Fixed (maybe) a null reference exception in sprite sheet mode
            // 2.2.4
            // - Fixed possible crashes when changing texture coord modes with no frame/chain selected
            // - Editor remembers coordinate mode per .achx
            // 2.2.5
            // - Derived entities entity will now show the texture in the texture selection window when a Sprite uses the file from the base
            // 3.0 
            // - Added texture selection combo box which populates with textures in animation frame, file, and Gum context
            // - Added preview of magic wand 
            // - Added CTRL+click showing the + icon next to the cursor
            // - Magic wand is now a checkbox instead of a toggle button - reads better
            // 3.0.1
            // - Fixed lots of little issues with preview/wireframe window not refreshing when it should
            // 3.0.2
            // - Fixed a few null ref exceptions
            // 3.0.3
            // - Fixed crash when clicking on texture coordinate window
            // 3.1
            // - Large reduction in complexity of camera positioning - now it's simply based on the selected texture
            //   and previous position for that texture
            // - Texture dropdown now reflects the selected AnimaitonFrame.
            // 3.1.1
            // - Fixed snapping size not applying until checkbox is checked/unchecked.
            // 3.1.2
            // - Removed text showing up when no animation chains are selected, overlapping the texture and making % selection not work.
            // 3.2.0
            // - Added CTRL+click when snapping is on to create animation 
            // frames based on the grid size
            // 3.2.1
            // - Added frame reordering through right-click and alt arrows.
            get { return new Version(3, 2, 1); }
        }

        public bool IsSelectedItemSprite
        {
            get
            {
                NamedObjectSave nos = GlueState.CurrentNamedObjectSave;

                return nos != null && nos.SourceType == SourceType.FlatRedBallType &&
                    nos.SourceClassType == "Sprite";

                
            }
        }

        #endregion

        #region Methods

        public AnimationEditorPlugin() : base()
        {
            textureCoordinateSelectionLogic = new TextureCoordinateSelectionLogic();
        }

        public override void StartUp()
        {
            // Do anything your plugin needs to do when it first starts up
            this.InitializeCenterTabHandler += HandleInitializeTab;

            this.ReactToItemSelectHandler += HandleItemSelect;

            //this.ReactToFileChangeHandler += HandleFileChange;

            this.ReactToLoadedGlux += HandleGluxLoad;

            this.ReactToChangedPropertyHandler += HandleChangedProperty;

            //this.SelectItemInCurrentFile += HandleSelectItemInCurrentFile;
        }

        //private void HandleSelectItemInCurrentFile(string objectName)
        //{
        //    if(mAchxControl != null)
        //    {
        //        var found = 
        //            mAchxControl.AnimationChainList.AnimationChains.FirstOrDefault(item => item.Name == objectName);

        //        if(found != null)
        //        {
        //            SelectedState.Self.SelectedChain = found;
        //        }
        //    }
        //}

        bool ignoringChanges = false;
        private void HandleChangedProperty(string changedMember, object oldValue)
        {
            if (!ignoringChanges)
            {

                if (IsSelectedItemSprite)
                {
                    //if (changedMember == "Texture" ||

                    //    changedMember == "LeftTextureCoordinate" ||
                    //    changedMember == "RightTextureCoordinate" ||
                    //    changedMember == "TopTextureCoordinate" ||
                    //    changedMember == "BottomTextureCoordinate" ||

                    //    changedMember == "LeftTexturePixel" ||
                    //    changedMember == "RightTexturePixel" ||
                    //    changedMember == "TopTexturePixel" ||
                    //    changedMember == "BottomTexturePixel"
                    //    )
                    //{
                    //    textureCoordinateSelectionLogic.RefreshSpriteDisplay(mTextureCoordinateControl);
                    //}
                }
            }
        }

        private void HandleGluxLoad()
        {
            //ApplicationState.Self.ProjectFolder =
            //    FlatRedBall.Glue.ProjectManager.ContentDirectory;
        }
        
        public override bool ShutDown(PluginShutDownReason reason)
        {
            // Do anything your plugin needs to do to shut down
            // or don't shut down and return false
            if (mTab != null)
            {
                mContainer.Controls.Remove(mTab);
            }
            mContainer = null;
            mTab = null;
            //mAchxControl = null;
            //mTextureCoordinateControl = null;
            return true;
        }

        //void HandleFileChange(string fileName)
        //{
        //    string standardizedChangedFile = FileManager.Standardize(fileName, null, false);
        //    string standardizedCurrent = 
        //        FileManager.Standardize(FlatRedBall.AnimationEditorForms.ProjectManager.Self.FileName, null, false);

        //    if (standardizedChangedFile == standardizedCurrent)
        //    {
        //        if (mReloadsToIgnore == 0)
        //        {
        //            mAchxControl.LoadAnimationChain(standardizedCurrent);
        //        }
        //        else
        //        {
        //            mReloadsToIgnore--;
        //        }
        //    }
        //}

        void HandleItemSelect(TreeNode selectedTreeNode)
        {
            HandleIfAchx(selectedTreeNode);

            HandleIfSprite(selectedTreeNode);
        }

        private void HandleIfSprite(TreeNode selectedTreeNode)
        {
            if (IsSelectedItemSprite)
            {
                if (!mContainer.Controls.Contains(mTab))
                {
                    mContainer.Controls.Add(mTab);

                    //mContainer.SelectTab(mContainer.Controls.Count - 1);
                }

                mTab.Text = "  Texture Coordinates"; // add spaces to make room for the X to close the plugin

                //if (mTextureCoordinateControl == null)
                //{
                //    mTextureCoordinateControl = new TextureCoordinateSelectionWindow();
                //    mTextureCoordinateControl.Dock = DockStyle.Fill;
                //    mTextureCoordinateControl.EndRegionChanged += HandleRegionChanged;
                //}

                //if (!mTab.Controls.Contains(mTextureCoordinateControl))
                //{
                //    mTab.Controls.Add(mTextureCoordinateControl);
                //}
                //if (mTab.Controls.Contains(mAchxControl))
                //{
                //    mTab.Controls.Remove(mAchxControl);
                //}

                //textureCoordinateSelectionLogic.RefreshSpriteDisplay(mTextureCoordinateControl);
            }
        }



        private void HandleRegionChanged()
        {
            ignoringChanges = true;

            //textureCoordinateSelectionLogic.HandleCoordinateChanged(
            //    mTextureCoordinateControl, GlueState.CurrentNamedObjectSave);

            ignoringChanges = false;
        }


        private void HandleIfAchx(TreeNode selectedTreeNode)
        {
            ReferencedFileSave rfs = selectedTreeNode?.Tag as ReferencedFileSave;

            bool shouldShowAnimationChainUi = rfs != null && rfs.Name != null && FileManager.GetExtension(rfs.Name) == "achx";

            if (shouldShowAnimationChainUi)
            {
                if (!mContainer.Controls.Contains(mTab))
                {
                    mContainer.Controls.Add(mTab);

                    mContainer.SelectTab(mContainer.Controls.Count - 1);
                }

                //if (mAchxControl == null)
                //{
                //    mAchxControl = new MainControl();

                //    ToolStripMenuItem saveToolStripItem = new ToolStripMenuItem("Force Save", null, HandleSaveClick);
                //    mAchxControl.AddToolStripMenuItem(saveToolStripItem, "File");

                //    ToolStripMenuItem forceSaveAllItem = new ToolStripMenuItem("Re-Save all .achx files in this Glue project", null, HandleForceSaveAll);
                //    mAchxControl.AddToolStripMenuItem(forceSaveAllItem, "File");


                //    mAchxControl.AnimationChainChange += new EventHandler(HandleAnimationChainChange);
                //    mAchxControl.AnimationChainSelected += HandleAnimationChainInFileSelected;
                //    mAchxControl.Dock = DockStyle.Fill;
                //}


                mTab.Text = "  Animation"; // add spaces to make room for the X to close the plugin

                //if (!mTab.Controls.Contains(mAchxControl))
                //{
                //    mTab.Controls.Add(mAchxControl);
                //}
                //if (mTab.Controls.Contains(mTextureCoordinateControl))
                //{
                //    mTab.Controls.Remove(mTextureCoordinateControl);
                //}

                //string fullFileName = FlatRedBall.Glue.ProjectManager.MakeAbsolute(rfs.Name);
                //mLastFile = fullFileName;

                //if (System.IO.File.Exists(fullFileName))
                //{
                //    mAchxControl.LoadAnimationChain(fullFileName);
                //}
            }
            else if (mContainer.Controls.Contains(mTab))
            {
                mContainer.Controls.Remove(mTab);
            }
        }

        private void AddAdditionalTextureFilesToDropDown()
        {
            var currentElement = GlueState.CurrentElement;

            if (currentElement != null)
            {

                // add any files here from
                // the entity or global content
                // if they're not already added.
                //var viewModel =
                //    mAchxControl.WireframeEditControlsViewModel;

                //var pngFiles = currentElement.ReferencedFiles.Concat(GlueState.CurrentGlueProject.GlobalFiles)
                //    .Select(item => new ToolsUtilities.FilePath(GlueCommands.GetAbsoluteFileName(item)))
                //    .Where(item => item.Extension == "png")
                //    .Distinct()
                //    .Except(viewModel.AvailableTextures)
                //    .ToArray();

                //foreach (var file in pngFiles)
                //{
                //    viewModel.AvailableTextures.Add(file);
                //}

                //if (viewModel.SelectedTextureFilePath == null && viewModel.AvailableTextures.Any())
                //{
                //    viewModel.SelectedTextureFilePath = viewModel.AvailableTextures.First();
                //}
            }
        }

        private void HandleForceSaveAll(object sender, EventArgs e)
        {
            foreach(var rfs in FlatRedBall.Glue.Elements.ObjectFinder.Self.GetAllReferencedFiles()
                .Where(item=>FileManager.GetExtension(item.Name) == "achx"))
            {
                string fullFileName = FlatRedBall.Glue.ProjectManager.MakeAbsolute(rfs.Name);

                if (System.IO.File.Exists(fullFileName))
                {
                    try
                    {
                        AnimationChainListSave acls = AnimationChainListSave.FromFile(fullFileName);

                        acls.Save(fullFileName);

                        PluginManager.ReceiveOutput("Re-saved " + rfs.ToString());
                    }
                    catch (Exception exc)
                    {
                        PluginManager.ReceiveError(exc.ToString());
                    }
                }

            }
        }

        private void HandleSaveClick(object sender, EventArgs e)
        {
            //mReloadsToIgnore++;
            //mAchxControl.SaveCurrentAnimationChain();
        }

        void HandleInitializeTab(TabControl tabControl)
        {
            mTab = new PluginTab();
            mContainer = tabControl;

            mTab.ClosedByUser += new PluginTab.ClosedByUserDelegate(OnClosedByUser);

        
        
        }

        void HandleAnimationChainChange(object sender, EventArgs e)
        {
            mReloadsToIgnore++;
            //mAchxControl.SaveCurrentAnimationChain();
        }

        void HandleAnimationChainInFileSelected(object sender, EventArgs e)
        {
            AddAdditionalTextureFilesToDropDown();
        }

        void OnClosedByUser(object sender)
        {
            PluginManager.ShutDownPlugin(this);
        }

        #endregion
    }
}
