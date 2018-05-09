﻿using System.Collections.Generic;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.Glue.Events;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces
{
    public interface IGlueState
    {
        #region Properties

        IElement CurrentElement
        {
            get;
        }

#if GLUE
        System.Windows.Forms.TreeNode CurrentTreeNode
        {
            get;
        }

        Managers.IFindManager Find { get; }
#endif

        EntitySave CurrentEntitySave
        {
            get;
        }
        ProjectBase CurrentMainProject
        {
            get;
        }
        ProjectBase CurrentMainContentProject { get; }

        IEnumerable<ProjectBase> SyncedProjects { get; }

        ScreenSave CurrentScreenSave
        {
            get;
        }

        NamedObjectSave CurrentNamedObjectSave
        {
            get;
        }

        EventResponseSave CurrentEventResponseSave
        {
            get;
        }

        CustomVariable CurrentCustomVariable
        {
            get;
        }

        StateSave CurrentStateSave
        {
            get;
        }

        StateSaveCategory CurrentStateSaveCategory
        {
            get;
        }

        ReferencedFileSave CurrentReferencedFileSave
        {
            get;
        }

        GlueProjectSave CurrentGlueProject
        {
            get;
        }

        string CurrentGlueProjectDirectory { get; }

        string ContentDirectory
        {
            get;
        }

        string ProjectNamespace { get; }

        string ProjectSpecificSettingsFolder { get; }



        #endregion


        IElement GetElement(string name);
        NamedObjectSave GetNamedObjectSave(string containerName, string name);
        CustomVariable GetCustomVariable(string containerName, string name);
        StateSave GetState(string containerName, string name);
        StateSaveCategory GetStateCategory(string containerName, string name);
        List<ProjectBase> GetProjects();
        IEnumerable<ReferencedFileSave> GetAllReferencedFiles();

    }

    public class GlueStateSnapshot : IGlueState
    {
        public IElement CurrentElement
        {
            get;
            set;
        }

#if GLUE
        public System.Windows.Forms.TreeNode CurrentTreeNode
        {
            get;
            set;
        }
        public Managers.IFindManager Find { get; }
#endif


        public EntitySave CurrentEntitySave
        {
            get;
            set;
        }

        public ScreenSave CurrentScreenSave
        {
            get;
            set;
        }

        public NamedObjectSave CurrentNamedObjectSave
        {
            get;
            set;
        }

        public EventResponseSave CurrentEventResponseSave
        {
            get;
            set;
        }

        public CustomVariable CurrentCustomVariable
        {
            get;
            set;
        }

        public StateSave CurrentStateSave
        {
            get;
            set;
        }

        public StateSaveCategory CurrentStateSaveCategory
        {
            get;
            set;
        }

        public ReferencedFileSave CurrentReferencedFileSave
        {
            get;
            set;
        }

        public GlueProjectSave CurrentGlueProject
        {
            get;
            set;
        }

        public string ContentDirectory
        {
            get;
            set;
        }

        public ProjectBase CurrentMainProject
        {
            get;
            set;
        }

        public IEnumerable<ProjectBase> SyncedProjects { get; set; }

        public ProjectBase CurrentMainContentProject
        {
            get;
            set;
        }

        public string ProjectNamespace
        {
            get;
            set;
        }

        public string ProjectSpecificSettingsFolder
        {
            get;
            set;
        }



        public string CurrentGlueProjectDirectory { get; set; }
        // STOP!  If adding here be sure to add to SetFrom too

        public void SetFrom(IGlueState glueState)
        {
            this.CurrentElement = glueState.CurrentElement;

#if GLUE
            this.CurrentTreeNode = glueState.CurrentTreeNode;
#endif

            this.CurrentEntitySave = glueState.CurrentEntitySave;

            this.CurrentScreenSave = glueState.CurrentScreenSave;

            this.CurrentNamedObjectSave = glueState.CurrentNamedObjectSave;

            this.CurrentEventResponseSave = glueState.CurrentEventResponseSave;

            this.CurrentCustomVariable = glueState.CurrentCustomVariable;

            this.CurrentStateSave = glueState.CurrentStateSave;

            this.CurrentStateSaveCategory = glueState.CurrentStateSaveCategory;

            this.CurrentReferencedFileSave = glueState.CurrentReferencedFileSave;

            this.CurrentGlueProject = glueState.CurrentGlueProject;

            // do we need to foreach it instead?
            this.SyncedProjects = glueState.SyncedProjects;

            this.ContentDirectory = glueState.ContentDirectory;

            this.CurrentMainProject = glueState.CurrentMainProject;

            this.CurrentMainContentProject = glueState.CurrentMainContentProject;

            this.ProjectNamespace = glueState.ProjectNamespace;

            this.ProjectSpecificSettingsFolder = glueState.ProjectSpecificSettingsFolder;

            this.CurrentGlueProjectDirectory = glueState.CurrentGlueProjectDirectory;
        }

        public IElement GetElement(string name)
        {
            throw new System.NotImplementedException();
        }

        public NamedObjectSave GetNamedObjectSave(string containerName, string name)
        {
            throw new System.NotImplementedException();
        }

        public CustomVariable GetCustomVariable(string containerName, string name)
        {
            throw new System.NotImplementedException();
        }

        public StateSave GetState(string containerName, string name)
        {
            throw new System.NotImplementedException();
        }

        public StateSaveCategory GetStateCategory(string containerName, string name)
        {
            throw new System.NotImplementedException();
        }

        public List<ProjectBase> GetProjects()
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<ReferencedFileSave> GetAllReferencedFiles()
        {
            throw new System.NotImplementedException();
        }
    }
}
