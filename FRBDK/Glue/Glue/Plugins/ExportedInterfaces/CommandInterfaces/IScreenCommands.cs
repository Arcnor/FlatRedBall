﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public interface IScreenCommands
    {
        ScreenSave AddScreen(string screenName);

        void AddScreen(ScreenSave screenSave, bool suppressAlreadyExistingFileMessage = false);
    }
}
