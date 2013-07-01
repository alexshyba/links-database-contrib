namespace Sitecorian.LinkDatabaseContrib.Managers
{
    public class StringUtil
    {
        public static string GetString(string value, int maxLength)
        {
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}