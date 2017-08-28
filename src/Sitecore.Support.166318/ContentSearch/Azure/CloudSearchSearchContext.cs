namespace Sitecore.Support.ContentSearch.Azure
{
    using System.Reflection;
    using System.Linq;
    using Sitecore.ContentSearch.Azure.Query;
    using Sitecore.ContentSearch.Diagnostics;
    using Sitecore.ContentSearch.Linq.Common;
    using Sitecore.ContentSearch.Utilities;
    using Sitecore.ContentSearch;
    using Sitecore.ContentSearch.Security;
    public class CloudSearchSearchContext : Sitecore.ContentSearch.Azure.CloudSearchSearchContext, IProviderSearchContext
    {
        public CloudSearchSearchContext(Sitecore.ContentSearch.Azure.CloudSearchProviderIndex index, SearchSecurityOptions options = SearchSecurityOptions.EnableSecurityCheck) : base(index, options)
        {

        }

        IQueryable<TItem> IProviderSearchContext.GetQueryable<TItem>()
        {
            return ((IProviderSearchContext)this).GetQueryable<TItem>(new IExecutionContext[0]);
        }
        IQueryable<TItem> IProviderSearchContext.GetQueryable<TItem>(IExecutionContext executionContext)
        {
            return ((IProviderSearchContext)this).GetQueryable<TItem>(new IExecutionContext[]
            {
        executionContext
            });
        }

        IQueryable<TItem> IProviderSearchContext.GetQueryable<TItem>(params IExecutionContext[] executionContexts)
        {
            queryMapperFieldInfo = typeof(CloudIndex<TItem>).GetField("queryMapper",
                BindingFlags.Instance | BindingFlags.NonPublic);
            LinqToCloudIndex<TItem> linqToCloudIndex = new Sitecore.Support.ContentSearch.Azure.Query.LinqToCloudIndex<TItem>(this, executionContexts);
            if (queryMapperFieldInfo != null)
            {
                queryMapperFieldInfo.SetValue(linqToCloudIndex, new Sitecore.Support.ContentSearch.Azure.Query.CloudQueryMapper(linqToCloudIndex.Parameters));
            }
            if (this.Index.Locator.GetInstance<IContentSearchConfigurationSettings>().EnableSearchDebug())
            {
                ((IHasTraceWriter)linqToCloudIndex).TraceWriter = new LoggingTraceWriter(SearchLog.Log);
            }
            return linqToCloudIndex.GetQueryable();
        }

        private FieldInfo queryMapperFieldInfo;
    }
}