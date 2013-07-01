namespace Sitecorian.LinkDatabaseContrib.Configuration
{
    public static class Config
    {
        public static string DefaultDatabaseName
        {
            get { return Sitecore.Configuration.Settings.GetSetting("LinkDatabaseManager.DefaultDatabaseName"); }
        }
    }
}