using System;
using System.Configuration;
using System.Data.SqlClient;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Jobs;
using Sitecore.Links;
using Sitecore;
using Sitecorian.LinkDatabaseContrib.Configuration;

namespace Sitecorian.LinkDatabaseContrib.Managers
{
    public class LinksDatabaseManager
    {
        public static Job UpdateReferencesAsync(Item item, string databaseName)
        {
            var options = new JobOptions("RemoveReferences", "links", Context.Site.Name, new LinksDatabaseManager(), "UpdateReferences", new object[] { item, databaseName })
            {
                AfterLife = TimeSpan.FromMinutes(1.0),
                AtomicExecution = true
            };
            var job = new Job(options);
            JobManager.Start(job);
            return job;
        }

        public static Job RemoveReferencesAsync(Item item, string databaseName)
        {
            var options = new JobOptions("RemoveReferences", "links", Context.Site.Name, new LinksDatabaseManager(), "RemoveReferences", new object[] { item, databaseName })
            {
                AfterLife = TimeSpan.FromMinutes(1.0),
                AtomicExecution = true
            };
            var job = new Job(options);
            JobManager.Start(job);
            return job;
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

        protected static void RemoveReferenceInTransaction(Item item, SqlConnection conn)
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

        protected static void UpdateReferenceInTransaction(Item item, SqlConnection conn)
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

        protected static void UpdateLinks(Item item, ItemLink[] links, SqlConnection conn, SqlTransaction tran)
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

        protected static void RemoveLinks(Item item, SqlConnection conn, SqlTransaction tran)
        {
            Assert.ArgumentNotNull(item, "item");
            var sql = "DELETE FROM Links WHERE SourceItemID = '{0}' AND SourceDatabase='{1}'";
            sql = string.Format(sql, item.ID.ToGuid(), StringUtil.GetString(item.Database.Name, 50));
            var command = new SqlCommand(sql, conn, tran);
            command.ExecuteNonQuery();
        }

        protected static void AddLink(Item item, ItemLink link, SqlConnection conn, SqlTransaction tran)
        {
            Assert.ArgumentNotNull(item, "item");
            Assert.ArgumentNotNull(link, "link");
            var sql = "INSERT INTO Links (SourceDatabase, SourceItemID, SourceFieldID, TargetDatabase, TargetItemID, TargetPath) values('{0}', '{1}', '{2}', '{3}','{4}','{5}')";
            sql = string.Format(sql, StringUtil.GetString(item.Database.Name, 50), item.ID.ToGuid(), link.SourceFieldID.ToGuid(), StringUtil.GetString(link.TargetDatabaseName, 50), link.TargetItemID.ToGuid(), link.TargetPath);
            var command = new SqlCommand(sql, conn, tran);
            command.ExecuteNonQuery();
        }

        protected static void LogError(string itemId, Exception exception)
        {
            Log.Error("LinkDatabaseManager: Error Rebuilding Link Database for item: " + itemId, exception);
        }
    }
}