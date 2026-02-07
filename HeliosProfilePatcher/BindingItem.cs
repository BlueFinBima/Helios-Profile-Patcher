using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace HeliosProfilePatcher
{
    public class BindingItem
    {
        public string Xml { get; set; } = string.Empty;
        public bool IsSelected { get; set; } = true;


        public XElement ToElement()
        => XElement.Parse(Xml);
    }
}
