using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.ServiceModel.Description;
using System.Text;

namespace FciSharePointUpload.Core
{
    public enum UploadSourceAction
    {
        Keep,
        Delete,
        Url
    }

    public enum UploadTargetAction
    {
        Overwrite,
        Skip,
        Fail
    }

    public enum PropertyAction
    {
        Copy,
        Ignore
    }

    public enum PropertyType
    {
        OrderedList = 1,
        MultiChoiceList = 2,
        String = 4,
        MultiString = 5,
        Int = 6,
        Bool = 7,
        Date = 8
    }

    public class Uploader
    {
        private const string UrlSuffix = ".uploaded.url";
        private const string VersionHandler = "_vti_bin/versions.asmx";
        private const string CopyHandler = "_vti_bin/copy.asmx";

        private static bool IsUploadedFile(string name)
        {
            return name.EndsWith(UrlSuffix, StringComparison.CurrentCultureIgnoreCase);
        }

        private static string CombineUrls(string a, string b)
        {
            if (string.IsNullOrEmpty(a))
            {
                return b;
            }
            if (string.IsNullOrEmpty(b))
            {
                return a;
            }
            int count = 0;
            if (a[a.Length - 1] == '/')
            {
                ++count;
            }
            if (b[0] == '/')
            {
                ++count;
            }
            switch (count)
            {
                case 0:
                    return a + '/' + b;
                case 1:
                    return a + b;
                case 2:
                    return a + b.Substring(1);
            }
            return null;
        }

        private static FieldInformation TranslateProperty(FsrmLib.IFsrmProperty property,
            FsrmLib.IFsrmPropertyDefinition propDef)
        {
            FieldInformation field = new FieldInformation();
            field.DisplayName = property.Name;
            field.Value = property.Value;
            PropertyType propType = (PropertyType)propDef.Type;
            switch (propType)
            {
                case PropertyType.OrderedList:
                    field.Type = FieldType.Choice;
                    break;
                case PropertyType.MultiChoiceList:
                    field.Type = FieldType.MultiChoice;
                    field.Value = property.Value.Replace("|", ";#");
                    break;
                case PropertyType.String:
                    field.Type = FieldType.Text;
                    break;
                case PropertyType.MultiString:
                    field.Type = FieldType.Note;
                    break;
                case PropertyType.Int:
                    field.Type = FieldType.Number;
                    break;
                case PropertyType.Bool:
                    field.Type = FieldType.Boolean;
                    field.Value = (property.Value == "1").ToString();
                    break;
                case PropertyType.Date:
                    field.Type = FieldType.DateTime;

                    //Convert to ISO8601
                    DateTime dateValue = DateTime.FromFileTimeUtc(long.Parse(property.Value));
                    field.Value = dateValue.ToString("o");

                    break;
            }
            return field;
        }

        private static FieldInformation[] ReadProperties(string fileName)
        {
            FsrmLib.FsrmClassificationManager fsrmMgr = new FsrmLib.FsrmClassificationManager();
            //TODO: Load the list of FSRM properties of the file that will be uploaded
            FsrmLib.IFsrmCollection properties = null;

            List<FieldInformation> fields = new List<FieldInformation>();

            for (int propertyIndex = 0; propertyIndex != properties.Count; propertyIndex++)
            {
                FsrmLib.IFsrmProperty property = properties[propertyIndex + 1];
                FsrmLib.IFsrmPropertyDefinition propDef;
                try
                {
                    //TODO: Get the definition of the current property
                    propDef = null;
                }
                catch
                {
                    // Most probably not an FSRM property, just skip
                    continue;
                }
                FieldInformation info = TranslateProperty(property, propDef);
                if (info != null)
                {
                    fields.Add(info);
                }
            }
            return fields.ToArray();
        }

        private static void CreateUrl(string src, string dest)
        {
            src += UrlSuffix;
            StreamWriter writer = null;
            try
            {
                writer = new StreamWriter(src);
                writer.WriteLine("[InternetShortcut]");
                writer.WriteLine("URL=" + dest);
            }
            finally
            {
                if (writer != null)
                {
                    writer.Dispose();
                }
            }
        }

        public static void UploadToLibrary(string file, string url, string libPath, string name,
            UploadSourceAction sourceAction, UploadTargetAction targetAction, PropertyAction propertyAction)
        {
            UploadToLibrary(file, url, libPath, name, sourceAction, targetAction, propertyAction, null, null);
        }

        public static void UploadToLibrary(string file, string url, string libPath, string name,
            UploadSourceAction sourceAction, UploadTargetAction targetAction, PropertyAction propertyAction,
            string user, string password)
        {
            // Skip prevously uploaded files
            if (IsUploadedFile(file))
            {
                return;
            }
            if (string.IsNullOrEmpty(name))
            {
                // No rename
                name = Path.GetFileName(file);
            }
            string relPath = CombineUrls(libPath, name);
            if (targetAction != UploadTargetAction.Overwrite)
            {
                // See if the target exists
                bool targetPresent = false;
                try
                {
                    string urlVer = CombineUrls(url, VersionHandler);
                    Versions versionsClient = new Versions(urlVer);
                    if (!string.IsNullOrEmpty(user))
                    {
                        versionsClient.Credentials = new NetworkCredential(user, password);
                    }
                    else
                    {
                        versionsClient.Credentials = CredentialCache.DefaultNetworkCredentials;
                    }
                    versionsClient.GetVersions(relPath);
                    targetPresent = true;
                }
                catch
                {
                    targetPresent = false;
                }
                if (targetPresent)
                {
                    if (targetAction == UploadTargetAction.Skip)
                    {
                        return;
                    }
                    throw new Exception("Target file \"" + relPath + "\" already exists");
                }
            }
            string urlCopy = CombineUrls(url, CopyHandler);
            
            //TODO: Copy does not exit. Add a class that references the Copy web service from Sharepoint.
            Copy copyClient = new Copy(urlCopy);
            
            if (!string.IsNullOrEmpty(user))
            {
                copyClient.Credentials = new NetworkCredential(user, password);
            }
            else
            {
                copyClient.Credentials = CredentialCache.DefaultNetworkCredentials;
            }

            byte[] data = File.ReadAllBytes(file);
            string[] dest = new[] { CombineUrls(url, relPath) };
            FieldInformation[] fields = new FieldInformation[0];

            if (propertyAction == PropertyAction.Copy)
            {
                fields = ReadProperties(file);
            }

            //TODO: Call Copy Web Service in Sharepoint to copy the file over
            CopyResult[] results;
            uint ret = 0;

            if (ret != 0)
            {
                throw new Exception("Copy request did not complete");
            }

            if (results[0].ErrorCode != 0)
            {
                throw new Exception(results[0].ErrorMessage);
            }

            switch (sourceAction)
            {
                case UploadSourceAction.Delete:
                    File.Delete(file);
                    break;
                case UploadSourceAction.Url:
                    File.Delete(file);
                    CreateUrl(file, dest[0]);
                    break;
            }
        }
    }
}
