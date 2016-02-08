﻿/* Copyright (c) 2013, HotDocs Limited
   Use, modification and redistribution of this source is subject
   to the New BSD License as set out in LICENSE.TXT. */

using System;
using System.IO;

namespace HotDocs.Sdk
{
    /// <summary>
    ///     This class represents a template that is managed by the host application, and
    ///     (optionally) some assembly parameters (as specified by switches) for that template.
    ///     The location of the template is defined by Template.Location.
    /// </summary>
    public class Template
    {
        private string _title; //A cached title when non-null.

        //Constructors
        /// <summary>
        ///     Construct a Template object.
        /// </summary>
        /// <param name="fileName">The template file name.</param>
        /// <param name="location">The location of the template.</param>
        /// <include file="../Shared/Help.xml" path="Help/string/param[@name='switches']"></include>
        /// <include file="../Shared/Help.xml" path="Help/string/param[@name='key']"></include>
        public Template(string fileName, TemplateLocation location, string switches = "", string key = "")
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("fileName");

            if (location == null)
                throw new ArgumentException("location");

            FileName = fileName;
            Location = location;
            Switches = string.IsNullOrEmpty(switches) ? "" : switches;
            Key = string.IsNullOrEmpty(key) ? "" : key;
        }

        /// <summary>
        ///     Construct a Template object for the main template in a package.
        /// </summary>
        /// <param name="location">The template location as a package location.</param>
        /// <include file="../Shared/Help.xml" path="Help/string/param[@name='switches']"></include>
        /// <include file="../Shared/Help.xml" path="Help/string/param[@name='key']"></include>
        public Template(PackageTemplateLocation location, string switches = "", string key = "")
        {
            if (location == null)
                throw new ArgumentNullException("location");

            var ti = location.GetPackageManifest().MainTemplate;
            FileName = ti.FileName;
            Location = location;
            Switches = string.IsNullOrEmpty(switches) ? "" : switches;
            Key = string.IsNullOrEmpty(key) ? "" : key;
        }

        //Public methods.

        /// <summary>
        ///     The template title, which comes from the template's manifest file by default.
        /// </summary>
        public string Title
        {
            get
            {
                if (_title == null)
                {
                    try
                    {
                        var manifest = GetManifest(ManifestParseFlags.ParseTemplateInfo);
                        _title = manifest.Title;
                    }
                    catch (Exception)
                    {
                        _title = "";
                    }
                }
                return _title;
            }
            set { _title = value; }
        }

        //Public properties.
        /// <summary>
        ///     The file name (including extension) of the template. No path information is included.
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        ///     Defines the location of the template.
        /// </summary>
        public TemplateLocation Location { get; }

        /// <summary>
        ///     Command line switches that may be applicable when assembling the template, as provided to the ASSEMBLE instruction.
        /// </summary>
        public string Switches { get; set; }

        /// <summary>
        ///     A key identifying the template. When using a template management scheme where the template file itself is temporary
        ///     (such as with a DMS) set this key to help HotDocs Server to keep track of which server files are for which
        ///     template.
        ///     If not empty, this key is also used internally by HotDocs Server for caching purposes.
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        ///     If the host app wants to know, this property does what's necessary to
        ///     tell you the type of template you're dealing with.
        /// </summary>
        public TemplateType TemplateType
        {
            get
            {
                switch (Path.GetExtension(FileName).ToLowerInvariant())
                {
                    case ".cmp":
                        return TemplateType.InterviewOnly;
                    case ".docx":
                        return TemplateType.WordDOCX;
                    case ".rtf":
                        return TemplateType.WordRTF;
                    case ".hpt":
                        return TemplateType.HotDocsPDF;
                    case ".hft":
                        return TemplateType.HotDocsHFT;
                    case ".wpt":
                        return TemplateType.WordPerfect;
                    case ".ttx":
                        return TemplateType.PlainText;
                    default:
                        return TemplateType.Unknown;
                }
            }
        }

        /// <summary>
        ///     Parses command-line switches to inform the host app whether or not
        ///     an interview should be displayed for this template.
        /// </summary>
        public bool HasInterview
        {
            get
            {
                var switches = string.IsNullOrEmpty(Switches) ? string.Empty : Switches.ToLower();
                return !switches.Contains("/nw") && !switches.Contains("/naw") && !switches.Contains("/ni");
            }
        }

        /// <summary>
        ///     Based on TemplateType, tells the host app whether this type of template
        ///     generates a document or not (although regardless, ALL template types
        ///     need to be assembled in order to participate in assembly queues)
        /// </summary>
        public bool GeneratesDocument
        {
            get
            {
                var type = TemplateType;
                return type != TemplateType.InterviewOnly && type != TemplateType.Unknown;
            }
        }

        /// <summary>
        ///     Based on the template file extension, get the document type native to the template type.
        /// </summary>
        public DocumentType NativeDocumentType
        {
            get
            {
                var ext = Path.GetExtension(FileName);
                if (string.Compare(ext, ".docx", true) == 0)
                    return DocumentType.WordDOCX;
                if (string.Compare(ext, ".rtf", true) == 0)
                    return DocumentType.WordRTF;
                if (string.Compare(ext, ".hpt", true) == 0)
                    return DocumentType.PDF;
                if (string.Compare(ext, ".hft", true) == 0)
                    return DocumentType.HFD;
                if (string.Compare(ext, ".wpt", true) == 0)
                    return DocumentType.WordPerfect;
                if (string.Compare(ext, ".ttx", true) == 0)
                    return DocumentType.PlainText;
                return DocumentType.Unknown;
            }
        }

        /// <summary>
        ///     Returns a locator string to recreate the template object at a later time.
        ///     Use the Locate method to recreate the object.
        /// </summary>
        /// <returns></returns>
        public string CreateLocator()
        {
            var locator = FileName + "|" + Switches + "|" + Key + "|" + Location.CreateLocator();
            return Util.EncryptString(locator);
        }

        /// <summary>
        ///     Returns a Template created from a locator string generated by CreateLocator.
        /// </summary>
        /// <param name="locator">A locator string provided by CreateLocator.</param>
        /// <returns></returns>
        public static Template Locate(string locator)
        {
            if (string.IsNullOrEmpty(locator))
                throw new ArgumentNullException("locator");

            var decryptedLocator = Util.DecryptString(locator);
            var tokens = decryptedLocator.Split('|');
            if (tokens.Length != 4)
                throw new Exception("Invalid template locator.");

            var fileName = tokens[0];
            var switches = tokens[1];
            var key = tokens[2];
            var locationLocator = tokens[3];

            var template = new Template(fileName, TemplateLocation.Locate(locationLocator), switches);
            template.Key = key;
            template.UpdateFileName();
            return template;
        }

        /// <summary>
        ///     Gets the template manifest for this template. Can optionally parse an entire template manifest spanning tree.
        ///     See <see cref="ManifestParseFlags" /> for details.
        /// </summary>
        /// <param name="parseFlags">See <see cref="ManifestParseFlags" />.</param>
        /// <returns></returns>
        public TemplateManifest GetManifest(ManifestParseFlags parseFlags)
        {
            return TemplateManifest.ParseManifest(FileName, Location, parseFlags);
        }

        /// <summary>
        ///     Request that the Template.Location update the file name as needed.
        /// </summary>
        /// <remarks>Call this method to request that the file name</remarks>
        public void UpdateFileName()
        {
            string updatedFileName;
            if (Location.GetUpdatedFileName(this, out updatedFileName))
            {
                if (updatedFileName == null || updatedFileName == "")
                    throw new Exception("Invalid file name.");
                FileName = updatedFileName;
            }
        }

        /// <summary>
        ///     Returns the full path (based on the directory specified by Location.GetTemplateDirectory) of the template.
        /// </summary>
        /// <returns></returns>
        public string GetFullPath()
        {
            // Note: As the code is currently written, Location will never be null, but in the sake of defensive programming, 
            // we are checking it anyway lest we try to access its members if it were in fact a null object.
            if (Location == null)
                throw new Exception("No location has been specified.");
            return Path.Combine(Location.GetTemplateDirectory(), FileName);
        }

        /// <summary>
        ///     Returns the assembled document extension associated with the NativeDocumentType property.
        /// </summary>
        /// <returns></returns>
        public string GetDocExtension()
        {
            return GetDocExtension(NativeDocumentType, this);
        }

        /// <summary>
        ///     Returns the assembled document extension for a specific document type.
        /// </summary>
        /// <param name="docType">The document type to find an extension for.</param>
        /// <param name="template">The template to to derive an extension from if docType is DocumentType.Native.</param>
        /// <returns></returns>
        public static string GetDocExtension(DocumentType docType, Template template)
        {
            var ext = "";
            switch (docType)
            {
                case DocumentType.HFD:
                    ext = ".hfd";
                    break;
                case DocumentType.HPD:
                    ext = ".hpd";
                    break;
                case DocumentType.HTML:
                case DocumentType.HTMLwDataURIs:
                case DocumentType.MHTML:
                    ext = ".htm";
                    break;
                case DocumentType.Native:
                {
                    if (template == null)
                        throw new ArgumentNullException("template",
                            "The template cannot be null if the DocumentType is Native.");

                    var templateExt = Path.GetExtension(template.FileName);
                    if (templateExt == ".hpt")
                        ext = ".pdf";
                    else if (templateExt == ".hft")
                        ext = ".hfd";
                    else if (templateExt == ".ttx")
                        ext = ".txt";
                    else if (templateExt == ".wpt")
                        ext = ".wpd";
                    else
                        ext = templateExt;
                    break;
                }
                case DocumentType.PDF:
                    ext = ".pdf";
                    break;
                case DocumentType.PlainText:
                    ext = ".txt";
                    break;
                case DocumentType.WordDOC: //Note that DOC files are not supported on a server.
                    ext = ".doc";
                    break;
                case DocumentType.WordDOCX:
                    ext = ".docx";
                    break;
                case DocumentType.WordPerfect:
                    ext = ".wpd";
                    break;
                case DocumentType.WordRTF:
                    ext = ".rtf";
                    break;
                //For XML, use plain text.
                default:
                    throw new Exception("Unsupported document type.");
            }
            return ext;
        }
    }
}