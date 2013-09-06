using System;
using Sitecore.Configuration;

namespace Sitecore.LinkDatabaseContrib
{
    public static class Config
    {
        public static string DefaultDatabaseName
        {
            get
            {
                return Settings.GetSetting("LinkDatabase.DefaultDatabaseName");
            }
        }

        public static int MaxConcurrentThreads
        {
            get
            {
                return Settings.GetIntSetting("LinkDatabase.MaxConcurrentThreads", Math.Max(Environment.ProcessorCount, 1));
            }
        }
    }
}