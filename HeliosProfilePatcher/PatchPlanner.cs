using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace HeliosProfilePatcher
{
    public class PatchPlanner
    {
        public List<InterfaceItem> Interfaces { get; } = new();
        public List<BindingItem> Bindings { get; } = new();


        public PatchPlanner(XDocument target, XDocument source)
        {
            CollectInterfaces(target, source);
            CollectBindings(target, source);
        }


        private void CollectInterfaces(XDocument target, XDocument source)
        {
            var targetInterfaces = target.Root?.Element("Interfaces");
            var sourceInterfaces = source.Root?.Element("Interfaces");
            if (targetInterfaces == null || sourceInterfaces == null) return;


            var existing = new HashSet<string>(
            targetInterfaces.Elements("Interface")
            .Select(i => (string?)i.Attribute("Name"))
            .Where(n => !string.IsNullOrEmpty(n))!);


            foreach (var iface in sourceInterfaces.Elements("Interface"))
            {
                var name = (string?)iface.Attribute("Name");
                if (string.IsNullOrEmpty(name) || existing.Contains(name)) continue;


                Interfaces.Add(new InterfaceItem { Name = name, Element = iface });
                existing.Add(name);
            }
        }


        private void CollectBindings(XDocument target, XDocument source)
        {
            var targetBindings = target.Root?.Element("Bindings");
            var sourceBindings = source.Root?.Element("Bindings");
            if (targetBindings == null || sourceBindings == null) return;


            var existing = new HashSet<string>(
            targetBindings.Elements("Binding")
            .Select(b => b.ToString(SaveOptions.DisableFormatting)));


            foreach (var binding in sourceBindings.Elements("Binding"))
            {
                var xml = binding.ToString(SaveOptions.DisableFormatting);
                if (existing.Contains(xml)) continue;


                Bindings.Add(new BindingItem { Xml = xml });
                existing.Add(xml);
            }
        }
    }
}
