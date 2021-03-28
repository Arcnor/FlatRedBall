﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.ComponentModel;

using System.IO;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;

namespace FlatRedBall.Glue.Events
{
    public delegate string EventResponseSaveToString(EventResponseSave eventResponseSave);

    public class EventResponseSave
    {
        [XmlIgnore]
        public static EventResponseSaveToString ToStringDelegate;

        public string EventName
        {
            get;
            set;
        }

        public string Contents
        {
            get;
            set;
        }

        public string SourceObject
        {
            get;
            set;
        }

        public string SourceObjectEvent
        {
            get;
            set;
        }

        public string SourceVariable
        {
            get;
            set;
        }

        public BeforeOrAfter BeforeOrAfter
        {
            get;
            set;
        }

        public static string GetEventFileNameForElement(IElement element)
        {
            string fileName = element.Name + ".Event.cs";
            return fileName;
        }

        public static string GetSharedCodeFullFileName(IElement container, string baseProjectDirectory)
        {

            string fileName = GetEventFileNameForElement(container);
            return baseProjectDirectory + fileName;
        
        }

        public string DelegateType
        {
            get;
            set;
        }

        public override string ToString()
        {
            if (ToStringDelegate != null)
            {
                return ToStringDelegate(this);
            }
            else
            {
                return EventName + "(Event Response)";
            }

        }
    }
}
