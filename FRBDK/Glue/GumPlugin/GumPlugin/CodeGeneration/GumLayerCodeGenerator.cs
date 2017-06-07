﻿using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using Gum.DataTypes;
using GumPlugin.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GumPlugin.CodeGeneration
{
    class GumLayerCodeGenerator : ElementComponentCodeGenerator
    {
        bool ShouldGenerate
        {
            get
            {
                return AppState.Self.GumProjectSave != null;
            }
        }
        public override FlatRedBall.Glue.Plugins.Interfaces.CodeLocation CodeLocation
        {
            get
            {
                return FlatRedBall.Glue.Plugins.Interfaces.CodeLocation.AfterStandardGenerated;
            }
        }

        IEnumerable<NamedObjectSave> GetObjectsForGumLayers(IElement element)
        {
            return element.AllNamedObjects.Where(item => item.IsLayer &&
                NamedObjectSaveCodeGenerator.GetFieldCodeGenerationType(item) == CodeGenerationType.Full);
        }

        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, IElement element)
        {
            if (ShouldGenerate)
            {
                foreach (var layer in GetObjectsForGumLayers(element))
                {
                    codeBlock.Line("global::RenderingLibrary.Graphics.Layer " + layer.InstanceName + "Gum;");
                }
            }
            return codeBlock;
        }


        public override ICodeBlock GenerateAddToManagers(ICodeBlock codeBlock, IElement element)
        {
            if (ShouldGenerate)
            {
                bool wasAnythingMovedToALayer = false;
                // todo:  Need to register the layer here
                foreach (var item in element.AllNamedObjects.Where(item =>
                    GumPluginCodeGenerator.IsGue(item) &&
                    !string.IsNullOrEmpty(item.LayerOn) &&
                    NamedObjectSaveCodeGenerator.GetFieldCodeGenerationType(item) == CodeGenerationType.Full))
                {

                    codeBlock.Line($"{item.FieldName}.MoveToFrbLayer({item.LayerOn}, {item.LayerOn}Gum);");
                    wasAnythingMovedToALayer = true;
                }

                if(wasAnythingMovedToALayer && element is FlatRedBall.Glue.SaveClasses.ScreenSave)
                {
                    codeBlock.Line("FlatRedBall.Gui.GuiManager.SortZAndLayerBased();");
                }
            }
            return base.GenerateAddToManagers(codeBlock, element);
        }

    }
}
