using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Links;
using Sitecore.Security.Accounts;
using Sitecore.Threading;

namespace Sitecore.LinkDatabaseContrib.Managers
{
    public class LinksDatabaseManager
    {
        private readonly AutoResetEvent alldone = new AutoResetEvent(false);

        private static LinksDatabaseManager instance;

        private readonly int allowedThreads = 1;

        private int threadCount = 0;

        private LinksDatabaseManager()
        {
            allowedThreads = Config.MaxConcurrentThreads;
        }

        public static LinksDatabaseManager Instance
        {
            get { return instance ?? (instance = new LinksDatabaseManager()); }
        }
        public void UpdateReferencesAsync(Item item, string databaseName)
        {
            var callContext = new
            {
                Context.Site,
                Context.User
            };

            if (TakeThread())
            {
                ManagedThreadPool.QueueUserWorkItem(x =>
                {
                    try
                    {
                        if (callContext.Site != null)
                        {
                            Context.SetActiveSite(callContext.Site.Name);
                        }

                        using (new UserSwitcher(callContext.User))
                        {
                            UpdateReferences(item, databaseName);
                        }
                    }
                    finally
                    {
                        ReleaseThread();
                    }
                });
            }
            else
            {
                UpdateReferences(item, databaseName);
            }
        }

        public void RemoveReferencesAsync(Item item, string databaseName)
        {
            var callContext = new
            {
                Context.Site,
                Context.User
            };

            if (TakeThread())
            {
                ManagedThreadPool.QueueUserWorkItem(x =>
                {
                    try
                    {
                        if (callContext.Site != null)
                        {
                            Context.SetActiveSite(callContext.Site.Name);
                        }

                        using (new UserSwitcher(callContext.User))
                        {
                            RemoveReferences(item, databaseName);
                        }
                    }
                    finally
                    {
                        ReleaseThread();
                    }
                });
            }
            else
            {
                RemoveReferences(item, databaseName);
            }
        }

        public void RemoveReferences(Item item)
        {
            RemoveReferences(item, Config.DefaultDatabaseName);
        }

        public void RemoveReferences(Item item, string databaseName)
        {
            Assert.IsNotNull(item, "item");
            Assert.IsNotNull(Factory.GetDatabase(databaseName), "database does not exist");

            var name = Factory.GetDatabase(databaseName).ConnectionStringName;

            SqlConnection connection = null;
            try
            {
                connection = new SqlConnection(ConfigurationManager.ConnectionStrings[name].ConnectionString);
                connection.Open();
                RemoveReferenceInTransaction(item, connection);
            }
            catch (Exception ex)
            {
                LogError(item.ID.ToString(), ex);
            }
            finally
            {
                if (connection != null && connection.State == System.Data.ConnectionState.Open)
                {
                    connection.Close();
                    connection.Dispose();
                }
            }
        }

        public void UpdateReferences(Item item)
        {
            UpdateReferences(item, Config.DefaultDatabaseName);
        }

        public void UpdateReferences(Item item, string databaseName)
        {
            Assert.IsNotNull(item, "item");
            Assert.IsNotNull(Factory.GetDatabase(databaseName), "database does not exist");

            var name = Factory.GetDatabase(databaseName).ConnectionStringName;
            SqlConnection connection = null;
            try
            {
                connection = new SqlConnection(ConfigurationManager.ConnectionStrings[name].ConnectionString);
                connection.Open();
                UpdateReferenceInTransaction(item, connection);
            }
            catch (Exception ex)
            {
                LogError(item.ID.ToString(), ex);
            }
            finally
            {
                if (connection != null && connection.State == System.Data.ConnectionState.Open)
                {
                    connection.Close();
                    connection.Dispose();
                }
            }
        }

        private void RemoveReferenceInTransaction(Item item, SqlConnection conn)
        {
            var tran = conn.BeginTransaction();
            try
            {
                Assert.ArgumentNotNull(item, "item");
                RemoveLinks(item, conn, tran);
                tran.Commit();
            }
            catch (Exception ex)
            {
                LogError(item.ID.ToString(), ex);
                tran.Rollback();
            }
        }

        private void UpdateReferenceInTransaction(Item item, SqlConnection conn)
        {
            var tran = conn.BeginTransaction();
            try
            {
                Assert.ArgumentNotNull(item, "item");
                var allLinks = item.Links.GetAllLinks();
                UpdateLinks(item, allLinks, conn, tran);
                tran.Commit();
            }
            catch (Exception ex)
            {
                LogError(item.ID.ToString(), ex);
                tran.Rollback();
            }
        }

        private void UpdateLinks(Item item, ItemLink[] links, SqlConnection conn, SqlTransaction tran)
        {
            Assert.ArgumentNotNull(item, "item");
            Assert.ArgumentNotNull(links, "links");

            RemoveLinks(item, conn, tran);

            foreach (var link in links)
            {
                if (!link.SourceItemID.IsNull)
                {
                    AddLink(item, link, conn, tran);
                }
            }
        }

        private void RemoveLinks(Item item, SqlConnection conn, SqlTransaction tran)
        {
            Assert.ArgumentNotNull(item, "item");
            var sql = "DELETE FROM Links WHERE SourceItemID = '{0}' AND SourceDatabase='{1}'";
            sql = string.Format(sql, item.ID.ToGuid(), StringUtil.GetString(item.Database.Name, 50));
            var command = new SqlCommand(sql, conn, tran);
            command.ExecuteNonQuery();
        }

        private void AddLink(Item item, ItemLink link, SqlConnection conn, SqlTransaction tran)
        {
            Assert.ArgumentNotNull(item, "item");
            Assert.ArgumentNotNull(link, "link");
            var sql = "INSERT INTO Links (SourceDatabase, SourceItemID, SourceFieldID, TargetDatabase, TargetItemID, TargetPath) values('{0}', '{1}', '{2}', '{3}','{4}','{5}')";
            sql = string.Format(sql, StringUtil.GetString(item.Database.Name, 50), item.ID.ToGuid(), link.SourceFieldID.ToGuid(), StringUtil.GetString(link.TargetDatabaseName, 50), link.TargetItemID.ToGuid(), link.TargetPath);
            var command = new SqlCommand(sql, conn, tran);
            command.ExecuteNonQuery();
        }

        private void LogError(string itemId, Exception exception)
        {
            Log.Error(string.Format("LinkDatabaseManager: Error Rebuilding Link Database for item: {0}. Exception: {1}. Details {2}", itemId, exception, exception.StackTrace), new object());
        }

        private void ReleaseThread()
        {
            lock (this)
            {
                --threadCount;
                if (threadCount == 0)
                {
                    alldone.Set();
                }
            }
        }

        private bool TakeThread()
        {
            lock (this)
            {
                if (threadCount >= allowedThreads)
                    return false;

                ++threadCount;
                return true;
            }
        }
    }
}