﻿using FlatRedBall.Glue;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using OfficialPlugins.TreeViewPlugin.Logic;
using OfficialPlugins.TreeViewPlugin.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OfficialPlugins.TreeViewPlugin.ViewModels
{
    #region Enums

    public enum SearchVisibility
    {
        MatchExplicitly,
        HaveVisibileChildren,
        MatchOrHaveVisibleChildren
    }

    #endregion

    public class NodeViewModel : ViewModel, ITreeNode
    {
        #region External DllImport
        [DllImport("Shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern int StrCmpLogicalW(string x, string y);

        #endregion

        #region Static ImageSource Members

        public static ImageSource CodeIcon;
        public static ImageSource CollisionsIcon;
        public static ImageSource CollisionIcon;
        public static ImageSource EntityIcon;
        public static ImageSource EntityInstanceIcon;
        public static ImageSource EventIcon;
        public static ImageSource FileIcon;
        public static ImageSource FolderClosedIcon;
        public static ImageSource FolderOpenIcon;
        public static ImageSource LayersIcon;
        public static ImageSource LayerIcon;
        public static ImageSource ScreenIcon;
        public static ImageSource ScreenStartupIcon;
        public static ImageSource StateIcon;
        public static ImageSource VariableIcon;

        #endregion

        #region Fields/Properties

        public object Tag { get; set; }
        
        // Not sure if we should have the setter be private or if it's okay to assign this. I think 
        // the amount that interacts with the NodeViewModel is very limited so for now we can leave it as public
        public NodeViewModel Parent { get; set; }

        ITreeNode ITreeNode.Parent => this.Parent;

        public bool HasItems
        {
            get
            {
                //this.LoadChildren();
                return this.children.Count > 0;
            }
        }

        public void Detach()
        {
            this.Parent.Children.Remove(this);
            this.Parent = null;
        }

        public FontWeight FontWeight
        {
            get => Get<FontWeight>();
            set => Set(value);
        }

        public ImageSource ImageSource
        {
            get => Get<ImageSource>();
            set => Set(value);
        }

        public Visibility Visibility
        {
            get => Get<Visibility>();
            set => Set(value);
        }


        private ObservableCollection<NodeViewModel> children = new ObservableCollection<NodeViewModel>();

        public ObservableCollection<NodeViewModel> Children
        {
            get => children;
        }

        public ObservableCollection<NodeViewModel> VisibleChildren
        {
            get; set;
        } = new ObservableCollection<NodeViewModel>();

        IEnumerable<ITreeNode> ITreeNode.Children => children;

        public string Text 
        {
            get => Get<string>();
            set
            {
                //this.Node.Name = value;
                Set(value);
            }
        }

        public bool IsExpanded
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsSelected
        {
            get => Get<bool>();
            set
            {
                if (Set(value) && value)
                {

                    SelectionLogic.HandleSelected(this);
                }
            }
        }

        public int Level
        {
            get => Get<int>();
            set => Set(value);
        }

        public bool DirectlyMatchesSearch { get; set; }
        public bool IndirectlyMatchesSearch { get; set; }

        #endregion

        #region Constructors

        static NodeViewModel()
        {
            CodeIcon = LoadIcon("icon_code");
            CollisionIcon = LoadIcon("icon_collision");
            CollisionsIcon = LoadIcon("icon_collisions");
            EntityIcon = LoadIcon("icon_entity");
            EntityInstanceIcon = LoadIcon("icon_entity_instance");
            EventIcon = LoadIcon("icon_event");
            FileIcon = LoadIcon("icon_file_standard");
            FolderClosedIcon = LoadIcon("icon_folder");
            FolderOpenIcon = LoadIcon("icon_folder_open");
            LayersIcon = LoadIcon("icon_layers");
            LayerIcon = LoadIcon("icon_layer");
            ScreenIcon = LoadIcon("icon_screen");
            ScreenStartupIcon = LoadIcon("icon_screen_startup");
            StateIcon = LoadIcon("icon_state");
            VariableIcon = LoadIcon("icon_variable");

            ImageSource LoadIcon(string iconName)
            {
                var location = $"/OfficialPluginsCore;component/TreeViewPlugin/Content/{iconName}.png";
                var bitmapImage = new BitmapImage(new Uri(location, UriKind.Relative));
                return bitmapImage;
            }

        }


        public NodeViewModel(NodeViewModel parent)
        {
            Visibility = Visibility.Visible;
            //this.Node = Node;
            this.Parent = parent;
            this.IsExpanded = false;

            FontWeight = FontWeights.Normal;

            ImageSource = FolderClosedIcon;

            children.CollectionChanged += (not, used) => UpdateToSearch();
        }

        #endregion

        public virtual void RefreshTreeNodes()
        {
            UpdateToSearch();
        }


        internal void CollapseRecursively()
        {
            this.IsExpanded = false;
            foreach(var child in this.Children)
            {
                child.CollapseRecursively();
            }
        }

        #region Parent-based Methods

        internal void ExpandParentsRecursively()
        {
            if(Parent != null)
            {
                Parent.IsExpanded = true;
                Parent.ExpandParentsRecursively();
            }
        }

        public NodeViewModel Root() => Parent == null ? this : Parent.Root();

        #endregion

        #region Children-based methods

        public NodeViewModel AddChild()
        {
            //var cn = this.Node as CompositeNode;
            //if (cn == null)
            //{
            //    return null;
            //}

            //var newChild = new CompositeNode() { Name = "New node" };
            //cn.Children.Add(newChild);
            var vm = new NodeViewModel(this);
            this.Children.Add(vm);
            return vm;
        }


        void ITreeNode.SortByTextConsideringDirectories() => this.SortByTextConsideringDirectories();
        public void SortByTextConsideringDirectories(ObservableCollection<NodeViewModel> treeNodeCollection = null, bool recursive = false)
        {
            if(treeNodeCollection == null)
            {
                treeNodeCollection = Children;
            }

            int lastObjectExclusive = treeNodeCollection.Count;
            int whereObjectBelongs;
            for (int i = 0 + 1; i < lastObjectExclusive; i++)
            {
                var first = treeNodeCollection[i];
                var second = treeNodeCollection[i - 1];
                if (TreeNodeComparer(first, second) < 0)
                {
                    if (i == 1)
                    {
                        var treeNode = treeNodeCollection[i];
                        treeNodeCollection.RemoveAt(i);

                        treeNodeCollection.Insert(0, treeNode);
                        continue;
                    }

                    for (whereObjectBelongs = i - 2; whereObjectBelongs > -1; whereObjectBelongs--)
                    {
                        second = treeNodeCollection[whereObjectBelongs];
                        if (TreeNodeComparer(treeNodeCollection[i], second) >= 0)
                        {
                            var treeNode = treeNodeCollection[i];

                            treeNodeCollection.RemoveAt(i);
                            treeNodeCollection.Insert(whereObjectBelongs + 1, treeNode);
                            break;
                        }
                        else if (whereObjectBelongs == 0 && TreeNodeComparer(treeNodeCollection[i], treeNodeCollection[0]) < 0)
                        {
                            var treeNode = treeNodeCollection[i];
                            treeNodeCollection.RemoveAt(i);
                            treeNodeCollection.Insert(0, treeNode);
                            break;
                        }
                    }
                }
            }

            if (recursive)
            {
                foreach (var node in treeNodeCollection)
                {
                    if (((ITreeNode)node).IsDirectoryNode())
                    {
                        SortByTextConsideringDirectories(node.Children, recursive);
                    }
                }
            }

        }

        private static int TreeNodeComparer(NodeViewModel first, NodeViewModel second)
        {
            bool isFirstDirectory = ((ITreeNode)first).IsDirectoryNode();
            bool isSecondDirectory = ((ITreeNode)second).IsDirectoryNode();

            if (isFirstDirectory && !isSecondDirectory)
            {
                return -1;
            }
            else if (!isFirstDirectory && isSecondDirectory)
            {
                return 1;
            }
            else
            {

                //return first.Text.CompareTo(second.Text);
                // This will put Level9 before Level10
                return StrCmpLogicalW(first.Text, second.Text);
            }
        }

        internal NodeViewModel GetNodeByTag(object tag)
        {
            if(tag == this.Tag)
            {
                return this;
            }
            else
            {
                foreach(var child in Children)
                {
                    var node = child.GetNodeByTag(tag);

                    if(node != null)
                    {
                        return node;
                    }
                }
            }
            return null;
        }

        public void Remove(ITreeNode child)
        {
            var childAsViewModel = child as NodeViewModel;

            this.Children.Remove(childAsViewModel);
            childAsViewModel.Parent = null;
        }

        public void Add(ITreeNode child)
        {
            var childAsViewModel = child as NodeViewModel;
            this.Children.Add(childAsViewModel);
            childAsViewModel.Parent = this;
        }

        public ITreeNode FindByName(string name)
        {
            return this.Children.FirstOrDefault(item => item.Text == name);
        }

        public void RemoveGlobalContentTreeNodesIfDoesntExist(ITreeNode treeNode)
        {
            var vm = treeNode as NodeViewModel;
            if (((ITreeNode)treeNode).IsDirectoryNode())
            {
                string directory = treeNode.GetRelativePath();

                directory = ProjectManager.MakeAbsolute(directory, true);


                if (!Directory.Exists(directory))
                {
                    // The directory isn't here anymore, so kill it!
                    treeNode.Parent.Remove(treeNode);

                }
                else
                {
                    // The directory is valid, but let's check subdirectories
                    for (int i = vm.Children.Count - 1; i > -1; i--)
                    {
                        RemoveGlobalContentTreeNodesIfDoesntExist(vm.Children[i]);
                    }
                }
            }
            else // assume content for now
            {

                ReferencedFileSave referencedFileSave = treeNode.Tag as ReferencedFileSave;

                if (!ProjectManager.GlueProjectSave.GlobalFiles.Contains(referencedFileSave))
                {
                    treeNode.Parent.Remove(treeNode);
                }
                else
                {
                    // The RFS may be contained, but see if the file names match
                    string rfsName = FileManager.Standardize(referencedFileSave.Name, null, false).ToLower();
                    string treeNodeFile = FileManager.Standardize(treeNode.GetRelativePath(), null, false).ToLower();

                    // We first need to make sure that the file is part of GlobalContentFiles.
                    // If it is, then we may have tree node in the wrong folder, so let's get rid
                    // of it.  If it doesn't start with globalcontent/ then we shouldn't remove it here.
                    if (rfsName.StartsWith("globalcontent/") && rfsName != treeNodeFile)
                    {
                        treeNode.Parent.Remove(treeNode);
                    }
                }
            }
        }

        #endregion

        #region Search-based

        public void UpdateToSearch()
        {
            IndirectlyMatchesSearch = false;
            DirectlyMatchesSearch = false;

            DirectlyMatchesSearch = string.IsNullOrEmpty(MainTreeViewViewModel.SearchText) || Text.ToLowerInvariant().Contains(MainTreeViewViewModel.SearchText);

            if (DirectlyMatchesSearch && !string.IsNullOrEmpty(MainTreeViewViewModel.PrefixText))
            {
                if (Tag != null)
                {
                    switch (MainTreeViewViewModel.PrefixText)
                    {
                        case "f":
                            DirectlyMatchesSearch = Tag is ReferencedFileSave;
                            break;
                        case "e":
                            DirectlyMatchesSearch = Tag is EntitySave;
                            break;
                        case "s":
                            DirectlyMatchesSearch = Tag is ScreenSave;
                            break;
                        case "o":
                            DirectlyMatchesSearch = Tag is NamedObjectSave;
                            break;
                        case "v":
                            DirectlyMatchesSearch = Tag is CustomVariable;
                            break;
                    }
                }
            }

            bool forceExclude = false;
            if ((IndirectlyMatchesSearch || DirectlyMatchesSearch) && !string.IsNullOrEmpty(MainTreeViewViewModel.PrefixText))
            {
                if (Tag == null)
                {
                    var asITreeNode = this as ITreeNode;
                    switch (MainTreeViewViewModel.PrefixText)
                    {
                        case "f":
                            if(asITreeNode.IsRootCodeNode() || asITreeNode.IsRootObjectNode() || asITreeNode.IsRootCustomVariablesNode() || asITreeNode.IsRootStateNode() || asITreeNode.IsRootEventsNode())
                            {
                                forceExclude = true;
                            }
                            break;
                        case "e":
                            if(asITreeNode.IsRootScreenNode() || asITreeNode.IsGlobalContentContainerNode())
                            {
                                forceExclude = true;
                            }
                            break;
                        case "s":
                            if (asITreeNode.IsRootEntityNode() || asITreeNode.IsGlobalContentContainerNode())
                            {
                                forceExclude = true;
                            }
                            break;
                        case "o":
                            if (asITreeNode.IsRootCodeNode() || asITreeNode.IsFilesContainerNode() || asITreeNode.IsRootCustomVariablesNode() || asITreeNode.IsRootStateNode() || asITreeNode.IsRootEventsNode())
                            {
                                forceExclude = true;
                            }
                            break;
                        case "v":
                            if (asITreeNode.IsRootCodeNode() || asITreeNode.IsRootObjectNode() || asITreeNode.IsFilesContainerNode() || asITreeNode.IsRootStateNode() || asITreeNode.IsRootEventsNode())
                            {
                                forceExclude = true;
                            }
                            break;
                    }
                }
            }

            if(forceExclude)
            {
                DirectlyMatchesSearch = false;
                IndirectlyMatchesSearch = false;
            }
            else
            {
                var forceExpand = !string.IsNullOrWhiteSpace(MainTreeViewViewModel.SearchText);

                bool childIndirectlyMatches = false;
                foreach(var child in Children)
                {
                    child.UpdateToSearch();
                    childIndirectlyMatches |= child.IndirectlyMatchesSearch;
                }

                IndirectlyMatchesSearch = DirectlyMatchesSearch || childIndirectlyMatches;

                if(IndirectlyMatchesSearch && forceExpand)
                {
                    IsExpanded = true;
                }

                VisibleChildren.Clear();

                int expectedVisibleChildrenCount = 0;

                for(int i = 0; i < children.Count; i++)
                {
                    var child = children[i];
                    var shouldBeIncluded = child.IndirectlyMatchesSearch;
                    if(shouldBeIncluded)
                    {
                        expectedVisibleChildrenCount++;

                        if (i >= VisibleChildren.Count)
                        {
                            // it's being added at the end:
                            VisibleChildren.Add(child);
                        }
                        else if(VisibleChildren[i] != child)
                        {
                            var oldIndex = VisibleChildren.IndexOf(child);
                            if(oldIndex > -1)
                            {
                                VisibleChildren.Move(oldIndex, i);
                            }
                            else
                            {
                                VisibleChildren.Insert(i, child);
                            }

                            //if(VisibleChildren.Contains(child))
                            //{
                            //    VisibleChildren.Remove(child);
                            //}
                        }
                    }
                    else
                    {
                        if(VisibleChildren.Contains(child))
                        {
                            VisibleChildren.Remove(child);
                        }
                        child.IsExpanded = false;
                    }
                }

                // At this point all the visible children should match the normal Children, at least up to the
                // Children.Count. If there are any extra VisibleTreeNodes, then they've been removed so let's get rid of them:
                while(VisibleChildren.Count > expectedVisibleChildrenCount)

                if(Tag == null && VisibleChildren.Count == 0 && !string.IsNullOrWhiteSpace(MainTreeViewViewModel.PrefixText))
                {
                    IndirectlyMatchesSearch = false;
                    DirectlyMatchesSearch = false;
                }
            }

        }


        #endregion

        public override string ToString()
        {
            return Text;
        }

        ITreeNode ITreeNode.FindByTagRecursive(object tag) => this.GetNodeByTag(tag);

    }
}
