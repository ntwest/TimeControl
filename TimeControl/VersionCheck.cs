/*
 * Version Checker
 * Created by Vendan
 * License: MIT
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using MiniJSON;

namespace KSVersionCheck
{
    public struct Version
    {
        public string download_path;
        public string friendly_version;
        public string ksp_version;
        public string changelog;
    }

    public class VersionCheck
    {
        public static void CheckVersion(int ModID, Action<Version> action)
        {
            WebClient wc = new WebClient();

            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            wc.DownloadStringCompleted += (sender, e) =>
            {
                Dictionary<string, object> data = Json.Deserialize(e.Result) as Dictionary<string, object>;

                Version v = new Version { download_path = (string)data["download_path"], friendly_version = (string)data["friendly_version"], ksp_version = (string)data["ksp_version"], changelog = (string)data["changelog"] };

                action(v);
            };
            wc.DownloadStringAsync(new Uri("https://kerbalstuff.com/api/mod/" + ModID.ToString() + "/latest"));
        }
    }
}