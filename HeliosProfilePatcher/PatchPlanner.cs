using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace HeliosProfilePatcher
{
    public class PatchPlanner
    {
        public List<InterfaceItem> Interfaces { get; } = new();
        public List<BindingItem> Bindings { get; } = new();
        public List<ControlItem> Controls { get; } = new();


        public PatchPlanner(XDocument target, XDocument source)
        {
            CollectInterfaces(target, source);
            CollectBindings(target, source);
            CollectControls(target, source);
        }


        private void CollectInterfaces(XDocument target, XDocument source)
        {
            XElement? targetInterfaces = target.Root?.Element("Interfaces");
            XElement? sourceInterfaces = source.Root?.Element("Interfaces");
            if (targetInterfaces == null || sourceInterfaces == null) return;


            HashSet<string> existing = new HashSet<string>(
            targetInterfaces.Elements("Interface")
            .Select(i => (string?)i.Attribute("Name"))
            .Where(n => !string.IsNullOrEmpty(n))!);


            foreach (XElement? iface in sourceInterfaces.Elements("Interface"))
            {
                string? name = (string?)iface.Attribute("Name");
                if (string.IsNullOrEmpty(name) || existing.Contains(name)) continue;


                Interfaces.Add(new InterfaceItem { Name = name, Element = iface });
                existing.Add(name);
            }
        }


        private void CollectBindings(XDocument target, XDocument source)
        {
            XElement? targetBindings = target.Root?.Element("Bindings");
            XElement? sourceBindings = source.Root?.Element("Bindings");
            if (targetBindings == null || sourceBindings == null) return;


            HashSet<string> existing = new HashSet<string>(
            targetBindings.Elements("Binding")
            .Select(b => b.ToString(SaveOptions.DisableFormatting)));


            foreach (XElement? binding in sourceBindings.Elements("Binding"))
            {
                string xml = binding.ToString(SaveOptions.DisableFormatting);
                if (existing.Contains(xml)) continue;

                Bindings.Add(new BindingItem { Xml = xml });
                existing.Add(xml);
            }
        }
        private void CollectControls(XDocument target, XDocument source)
        {
            XElement? targetMonitors = target.Root?.Element("Monitors");
            XElement? sourceMonitors = source.Root?.Element("Monitors");

            if (targetMonitors == null || sourceMonitors == null) return;

            HashSet<string> existing = new HashSet<string>(targetMonitors
                .Elements("Monitor")
                .SelectMany(m =>
                    m.Element("Children")?
                     .Elements("Control") ?? Enumerable.Empty<XElement>())
                .Where(c =>
                {
                    var children = c.Element("Children");
                    return children != null && !children.Elements().Any();
                }).Select(c => c.ToString(SaveOptions.DisableFormatting)));


            List<XElement> sourceControlsList = sourceMonitors
                .Elements("Monitor")
                .SelectMany(m =>
                    m.Element("Children")?
                     .Elements("Control") ?? Enumerable.Empty<XElement>())
                .Where(c =>
                {
                    var children = c.Element("Children");
                    return children != null && !children.Elements().Any();
                })
                .ToList();


            foreach (XElement control in sourceControlsList)
            {
                string xml = control.ToString(SaveOptions.DisableFormatting);
                if (existing.Contains(xml)) continue;

                Controls.Add(new ControlItem { Xml = xml, Element = control, Name = control.Attribute("Name")?.Value ?? string.Empty });
                existing.Add(xml);
            }
        }
    }
}
