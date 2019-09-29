using System.Reflection;
using System.Reflection.Emit;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Azure.Http;
using Sitecore.ContentSearch.Azure.Schema;
using Sitecore.ContentSearch.Maintenance;
using Sitecore.ContentSearch.Security;

namespace Sitecore.Support.ContentSearch.Azure
{
    public class CloudSearchProviderIndex : Sitecore.ContentSearch.Azure.CloudSearchProviderIndex
    {
        public CloudSearchProviderIndex(string name, string connectionStringName, string totalParallelServices, IIndexPropertyStore propertyStore) : base(name, connectionStringName, totalParallelServices, propertyStore)
        {
        }

        public CloudSearchProviderIndex(string name, string connectionStringName, string totalParallelServices, IIndexPropertyStore propertyStore, string @group) : base(name, connectionStringName, totalParallelServices, propertyStore, @group)
        {
        }

        public override IProviderSearchContext CreateSearchContext(SearchSecurityOptions options = SearchSecurityOptions.EnableSecurityCheck)
        {
            if (EnsureInitializedMi != null)
            {
                EnsureInitializedMi.Invoke(this, new object[] {});
            }
            return new Sitecore.Support.ContentSearch.Azure.CloudSearchSearchContext(this, options);
        }

        public override IIndexOperations Operations => new Sitecore.Support.ContentSearch.Azure.CloudSearchIndexOperations(this);

        public override IProviderUpdateContext CreateUpdateContext()
        {
            if (EnsureInitializedMi != null)
            {
                EnsureInitializedMi.Invoke(this, new object[] { });
            }
            ICommitPolicyExecutor commitPolicyExecutor = (ICommitPolicyExecutor)this.CommitPolicyExecutor.Clone();
            commitPolicyExecutor.Initialize(this);
            return new CloudSearchUpdateContext(this, (ISearchResultsDeserializer)deserializerFi.GetValue(this), commitPolicyExecutor);
        }

        private static readonly MethodInfo EnsureInitializedMi;
        private static readonly FieldInfo deserializerFi;
        static CloudSearchProviderIndex()
        {
            EnsureInitializedMi =
                typeof(Sitecore.ContentSearch.Azure.CloudSearchProviderIndex).GetMethod("EnsureInitialized",
                    BindingFlags.Instance | BindingFlags.NonPublic);
            deserializerFi = typeof(Sitecore.ContentSearch.Azure.CloudSearchProviderIndex).GetField("deserializer",
                    BindingFlags.Instance | BindingFlags.NonPublic);
        }
    }
}