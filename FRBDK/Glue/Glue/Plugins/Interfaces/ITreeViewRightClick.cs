﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FlatRedBall.Glue.Plugins.Interfaces
{
    public interface ITreeViewRightClick : IPlugin
    {
        void ReactToRightClick(TreeNode rightClickedTreeNode, ContextMenuStrip menuToModify);
    }
}
