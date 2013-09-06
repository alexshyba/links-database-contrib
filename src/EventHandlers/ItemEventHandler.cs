using System;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Events;
using Sitecore.LinkDatabaseContrib.Managers;
using Sitecore.Links;

namespace Sitecore.LinkDatabaseContrib.EventHandlers
{
    public class ItemEventHandler
    {
        protected void OnItemCopied(object sender, EventArgs args)
        {
            if (args == null || LinkDisabler.IsActive)
            {
                return;
            }

            var item = Event.ExtractParameter(args, 1) as Item;
            Assert.IsNotNull(item, "No item in parameters");

            LinksDatabaseManager.Instance.UpdateReferencesAsync(item, Config.DefaultDatabaseName);
        }

        protected void OnItemSaved(object sender, EventArgs args)
        {
            if (args == null || LinkDisabler.IsActive)
            {
                return;
            }

            var item = Event.ExtractParameter(args, 0) as Item;
            Assert.IsNotNull(item, "No item in parameters");

            LinksDatabaseManager.Instance.UpdateReferencesAsync(item, Config.DefaultDatabaseName);
        }

        protected void OnVersionRemoved(object sender, EventArgs args)
        {
            if (args == null)
            {
                return;
            }

            var item = Event.ExtractParameter(args, 0) as Item;
            Assert.IsNotNull(item, "No item in parameters");

            LinksDatabaseManager.Instance.UpdateReferencesAsync(item, Config.DefaultDatabaseName);
        }

        protected void OnItemDeleted(object sender, EventArgs args)
        {
            if (args == null)
            {
                return;
            }

            var item = Event.ExtractParameter(args, 0) as Item;
            Assert.IsNotNull(item, "No item in parameters");

            LinksDatabaseManager.Instance.RemoveReferencesAsync(item, Config.DefaultDatabaseName);
        }
    }
}