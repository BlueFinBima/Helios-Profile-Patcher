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
    public class ControlItem
    {
        public string Name { get; init; } = string.Empty;
        public string Xml { get; set; } = string.Empty;
        public bool IsSelected { get; set; } = true;
        public XElement Element { get; init; } = null!;

    }
    public class InterfaceItem
    {
        public string Name { get; init; } = string.Empty;
        public XElement Element { get; init; } = null!;
        public bool IsSelected { get; set; } = true;
    }
}
