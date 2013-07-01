using System;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Events;
using Sitecore.Links;
using Sitecorian.LinkDatabaseContrib.Configuration;
using Sitecorian.LinkDatabaseContrib.Managers;

namespace Sitecorian.LinkDatabaseContrib.EventHandlers
{
    public class ItemEventHandler
    {
        protected void OnItemSaved(object sender, EventArgs args)
        {
            if (args == null || LinkDisabler.IsActive) return;

            var item = Event.ExtractParameter(args, 0) as Item;
            Assert.IsNotNull(item, "No item in parameters");

            LinksDatabaseManager.UpdateReferencesAsync(item, Config.DefaultDatabaseName);
        }

        protected void OnVersionRemoved(object sender, EventArgs args)
        {
            if (args == null) return;

            var item = Event.ExtractParameter(args, 0) as Item;
            Assert.IsNotNull(item, "No item in parameters");

            LinksDatabaseManager.UpdateReferencesAsync(item, Config.DefaultDatabaseName);
        }

        protected void OnItemDeleted(object sender, EventArgs args)
        {
            if (args == null) return;

            var item = Event.ExtractParameter(args, 0) as Item;
            Assert.IsNotNull(item, "No item in parameters");

            LinksDatabaseManager.RemoveReferencesAsync(item, Config.DefaultDatabaseName);
        }
    }
}