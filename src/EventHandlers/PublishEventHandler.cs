using System;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Links;
using Sitecore.Publishing;
using Sitecore.Publishing.Pipelines.PublishItem;
using Sitecorian.LinkDatabaseContrib.Managers;

namespace Sitecorian.LinkDatabaseContrib.EventHandlers
{
    public class PublishEventHandler
    {
        protected static LinkDatabase FrontEndLinkDatabase
        {
            get
            {
                return (Factory.CreateObject("FrontEndLinkDatabase", true) as LinkDatabase);
            }
        }

        public string Database { get; set; }

        public void OnItemProcessed(object sender, EventArgs args)
        {
            var context = ((ItemProcessedEventArgs)args).Context;
            Assert.IsNotNull(context, "Cannot get PublishItem context");
            Assert.IsNotNull(FrontEndLinkDatabase, "Cannot resolve FrontEndLinkDatabase from config");

            if (context.PublishOptions.TargetDatabase.Name.Equals(Database))
            {
                var item = context.PublishHelper.GetTargetItem(context.ItemId);
                // if an item was not unpublished, 
                // the call below will reintroduce the reference
                // removed within OnItemProcessing method
                if (item != null)
                {
                    LinksDatabaseManager.UpdateReferencesAsync(item, Database);
                }
            }
        }

        public void OnItemProcessing(object sender, EventArgs args)
        {
            var context = ((ItemProcessingEventArgs)args).Context;
            Assert.IsNotNull(context, "Cannot get PublishItem context");
            Assert.IsNotNull(FrontEndLinkDatabase, "Cannot resolve FrontEndLinkDatabase from config");

            if (context.PublishOptions.TargetDatabase.Name.Equals(Database))
            {
                if (context.Action == PublishAction.DeleteTargetItem)
                {
                    var item = context.PublishHelper.GetTargetItem(context.ItemId);
                    Assert.IsNotNull(item, "Source item cannot be found");
                    LinksDatabaseManager.RemoveReferencesAsync(item, Database);
                }
            }
        }
    }
}