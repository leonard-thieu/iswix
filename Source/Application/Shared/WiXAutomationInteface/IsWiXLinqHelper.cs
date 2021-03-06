﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace IsWiXAutomationInterface
{
    public enum IsWiXDocumentType { Product, Module, Fragment, None };

    public static class IsWiXExtensionQueries
    {

        public static Dictionary<string, XNamespace> NameSpaces( this XDocument Document )
        {
                var schemas = new Dictionary<string, XNamespace>();
                try
                {
                    var result = Document.Root.Attributes().
                            Where(a => a.IsNamespaceDeclaration).
                            GroupBy(a => a.Name.Namespace == XNamespace.None ? String.Empty : a.Name.LocalName,
                                    a => XNamespace.Get(a.Value)).
                                    ToDictionary(g => g.Key, g => g.First());
                    foreach (var item in result)
                    {
                        schemas.Add(item.Key, item.Value);
                    }
                }
                catch (Exception)
                {
                    schemas.Add("", XNamespace.None);
                }
                return schemas;
        }

        public static string GetOptionalAttribute(this XElement Element, string AttributeName)
        {
            string value = string.Empty;
            try
            {
                value = Element.Attribute(AttributeName).Value;
            }
            catch (Exception)
            {
            }
            return value;
        }

        public static bool GetOptionalYesNoAttribute(this XElement Element, string AttributeName, bool DefaultValue)
        {
            bool finalValue = DefaultValue;

            string attribute = GetOptionalAttribute(Element, AttributeName).ToLower();

            switch (attribute)
            {
                case "yes":
                    finalValue = true;
                    break;

                case "no":
                    finalValue = false;
                    break;
            }

            return finalValue;
        }

        public static XNamespace GetWiXNameSpace(this XDocument Document)
        {
            XNamespace nameSpace = NameSpaces(Document)[""];
            XNamespace finalNameSpace;
            if(
                nameSpace=="http://schemas.microsoft.com/wix/2006/wi"||
                nameSpace=="http://wixtoolset.org/schemas/v4/wxs")
            {
                finalNameSpace = nameSpace;
            }
            else
            {
                finalNameSpace = ""; 
            }

            return finalNameSpace;
        }

        public static IsWiXDocumentType GetDocumentType(this XDocument Document)
        {
            IsWiXDocumentType docType = IsWiXDocumentType.None;

            if (NameSpaces(Document)[""] == GetWiXNameSpace(Document))
            {
                switch (Document.Root.Elements().First().Name.LocalName)
                {
                    case "Product":
                        docType = IsWiXDocumentType.Product;
                        break;
                    case "Module":
                        docType = IsWiXDocumentType.Module;
                        break;
                    case "Fragment":
                        docType = IsWiXDocumentType.Fragment;
                        break;
                    default:
                        docType = IsWiXDocumentType.None;
                        break;
                }
            }
            return docType;
        }

        public static XElement GetProductOrModuleElement(this XDocument Document)
        {
            XElement element;
            XNamespace ns = GetWiXNameSpace(Document);
            try
            {
                element = (from myitem in Document.Root.Elements()
                           where myitem.Name == ns + "Module" || myitem.Name == ns + "Product"
                           select myitem).First();
            }
            catch (Exception ex)
            {
                throw new Exception("IsWix Query Error: " + ex.Message);
            }
            return element;
        }

        public static XElement GetProductOrFragmentElement(this XDocument Document)
        {
            XElement element;
            XNamespace ns = Document.GetWiXNameSpace();
            try
            {
                element = (from myitem in Document.Root.Elements()
                           where myitem.Name == ns + "Product" || myitem.Name == ns + "Fragment"
                           select myitem).First();
            }
            catch (Exception ex)
            {
                throw new Exception("IsWix Query Error: " + ex.Message);
            }
            return element;
        }

        public static XElement GetProductModuleOrFragmentElement(this XDocument Document)
        {
            XElement element;
            XNamespace ns = Document.GetWiXNameSpace();
            
            try
            {
                element = (from myitem in Document.Root.Elements()
                           where myitem.Name == ns + "Module" || myitem.Name == ns + "Product" || myitem.Name == ns + "Fragment"
                           select myitem).First();
            }
            catch (Exception ex)
            {
                throw new Exception("IsWix Query Error: " + ex.Message);
            }
            return element;
        }

        public static XElement GetElementToAddAfterSelf(this XDocument Document, string ElementName)
        {
            XElement element = null;
            string elementsInOrder = string.Empty;
            XNamespace ns = Document.GetWiXNameSpace();
            
            switch (Document.GetDocumentType())
            {
                case IsWiXDocumentType.Module:
                    elementsInOrder =
                        "Package,AppId,Binary,Component,ComponentGroupRef,ComponentRef,Configuration,CustomAction,CustomActionRef,CustomTable,Dependency," +
                        "Directory,DirectoryRef,EmbeddedChainer,EmbeddedChainerRef,EnsureTable,Exclusion,Icon,IgnoreModularization,IgnoreTable,Property,PropertyRef,SetDirectory," +
                        "SetProperty,SFPCatalog,Substitution,UI,UIRef,WixVariable,InstallExecuteSequence,InstallUISequence,AdminExecuteSequence,AdminUISequence,AdvertiseExecuteSequence";
                    break;

                case IsWiXDocumentType.Product:
                    elementsInOrder =
                        "Package,AppId,Binary,ComplianceCheck,Component,ComponentGroup,Condition,CustomAction,CustomActionRef,CustomTable,Directory,DirectoryRef," +
                        "EmbeddedChainer,EmbeddedChainerRef,EnsureTable,Feature,FeatureGroupRef,FeatureRef,Icon,InstanceTransforms,Media,PackageCertificates," +
                        "PatchCertificates,Property,PropertyRef,SetDirectory,SetProperty,SFPCatalog,SymbolPath,UI,UIRef,Upgrade,WixVariable,InstallExecuteSequence," +
                        "InstallUISequence,AdminExecuteSequence,AdminUISequence,AdvertiseExecuteSequence";
                    break;

                case IsWiXDocumentType.Fragment:
                    elementsInOrder =
                        "AppId,Binary,ComplianceCheck,Component,ComponentGroup,Condition,CustomAction,CustomActionRef,CustomTable,Directory,DirectoryRef,EmbeddedChainer,EmbeddedChainerRef," +
                        "EnsureTable,Feature,FeatureGroup,FeatureRef,Icon,IgnoreModularization,Media,PackageCertificates,PatchCertificates,PatchFamily,Property,PropertyRef,SetDirectory," +
                        "SetProperty,SFPCatalog,UI,UIRef,Upgrade,WixVariable,Sequence,InstallExecuteSequence,InstallUISequence,AdminExecuteSequence,AdminUISequence,AdvertiseExecuteSequence";
                    break;
            }

            List<string> elements = new List<string>(elementsInOrder.Split(new char[] { ',' }).ToArray());
            var index = elements.IndexOf(ElementName);
            elements.RemoveRange(index + 1, elements.Count - index - 1);
            elements.Reverse();

            foreach (var item in elements)
            {
                try
                {
                    var test = Document.GetProductModuleOrFragmentElement();
                    element = test.Elements(ns + item).Last();
                    break;
                }
                catch (Exception)
                {
                }
            }

            return element;
        }

        public static string GetDestinationFilePath(this XElement file)
        {
            char[] delimiter = {'\\'};
            string filePath = file.Attribute("Source").Value.Split(delimiter).Last();
            XNamespace ns = file.Name.Namespace;

            foreach (var ancestor in file.Ancestors(ns+"Directory"))
            {
                string directoryName = GetParentDirectoryName(ancestor);
                if(!string.IsNullOrEmpty(directoryName))
                {
                    filePath = Path.Combine(directoryName + filePath);
                }
            }

            int firstSlashLocation = filePath.IndexOf('\\');
            string almostDone = "[" + filePath.Insert(firstSlashLocation, "]");
            return almostDone.Replace("]\\", "]");

        }

        private static string GetParentDirectoryName(XElement element)
        {
            string result = string.Empty;
            string id = element.GetOptionalAttribute("Id");
            string name = element.GetOptionalAttribute("Name");

            if(!string.IsNullOrEmpty(name) && !name.Equals("SourceDir", StringComparison.InvariantCultureIgnoreCase))
            {
                result = name + @"\";
            }
            else if (!string.IsNullOrEmpty(id) && !id.Equals("TARGETDIR", StringComparison.InvariantCultureIgnoreCase))
            {
                result = id + @"\";
            }
            
            return result;
        }
    }
}
