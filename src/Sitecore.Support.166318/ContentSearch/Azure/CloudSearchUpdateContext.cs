namespace Sitecore.Support.ContentSearch.Azure
{
    using Sitecore.ContentSearch.Azure;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Sitecore.Abstractions;
    using Sitecore.ContentSearch;
    using Sitecore.ContentSearch.Azure.Config;
    using Sitecore.ContentSearch.Azure.Http;
    using Sitecore.ContentSearch.Diagnostics;
    using Sitecore.ContentSearch.Linq.Common;
    using Sitecore.ContentSearch.Sharding;
    using Sitecore.ContentSearch.Utilities;

    public class CloudSearchUpdateContext : IProviderUpdateContext, ITrackingIndexingContext, IProviderUpdateContextEx2
    {
        private readonly AbstractSearchIndex index;
        private readonly ISearchResultsDeserializer deserializer;
        private readonly ConcurrentBag<CloudSearchDocument> documents;
        private ContextOperationStatistics statistics = new ContextOperationStatistics();
        private readonly IEvent events;

        private object @lock = new object();

        internal CloudSearchUpdateContext(AbstractSearchIndex index, ISearchResultsDeserializer deserializer, ICommitPolicyExecutor commitPolicyExecutor)
        {
            this.index = index;
            this.deserializer = deserializer;
            this.documents = new ConcurrentBag<CloudSearchDocument>();
            this.ParallelOptions = new ParallelOptions();
            this.IsParallel = ContentSearchConfigurationSettings.IsParallelIndexingEnabled;
            var limitCores = ContentSearchConfigurationSettings.ParallelIndexingCoreLimit;
            this.events = this.index.Locator.GetInstance<IEvent>();

            if (limitCores > 0)
            {
                this.ParallelOptions.MaxDegreeOfParallelism = limitCores;
            }

            this.CommitPolicyExecutor = commitPolicyExecutor ?? new NullCommitPolicyExecutor();
            this.Processed = new ConcurrentDictionary<IIndexableUniqueId, object>();
        }

        public ConcurrentDictionary<IIndexableUniqueId, object> Processed { get; set; }

        public void Dispose()
        {
        }

        public void Commit()
        {
            CrawlingLog.Log.Debug(string.Format("[Index={0}] Committing: {1}", this.index.Name, this.statistics));
            this.events.RaiseEvent("indexing:committing", this.index.Name);

            var searchIndex = this.Index as CloudSearchProviderIndex;

            if (searchIndex != null && this.documents.Count > 0)
            {
                var factory = searchIndex.Locator.GetInstance<IFactoryWrapper>();
                var batchBuilder = factory.CreateObject<ICloudBatchBuilder>("contentSearch/cloudBatchBuilder", true);

                try
                {
                    while (!this.documents.IsEmpty)
                    {
                        CloudSearchDocument document;

                        if (this.documents.TryTake(out document))
                        {
                            batchBuilder.AddDocument(document);
                        }

                        if (batchBuilder.IsFull || this.documents.IsEmpty)
                        {
                            var batch = batchBuilder.Release();
                            searchIndex.SearchService.PostDocuments(batch);
                            batchBuilder.Clear();
                        }
                    }
                }
                catch (Exception exception)
                {
                    foreach (var document in batchBuilder)
                    {
                        this.documents.Add(document);
                    }
                    CrawlingLog.Log.Error(string.Format("[Index={0}] Commit failed", this.index.Name), exception);

                    throw;
                }

                CrawlingLog.Log.Debug(string.Format("[Index={0}] Committed", this.index.Name));
                this.events.RaiseEvent("indexing:committed", this.index.Name);

                this.statistics = new ContextOperationStatistics();
                this.CommitPolicyExecutor.Committed();
            }
        }

        public void Optimize()
        {
            //Not supported by Cloud Search provider
        }

        public void AddDocument([NotNull] object itemToAdd, [NotNull] IExecutionContext executionContext)
        {
            if (itemToAdd == null) throw new ArgumentNullException("itemToAdd");

            this.AddDocument(itemToAdd, new[] { executionContext });
        }

        public void AddDocument([NotNull] object itemToAdd, [NotNull] params IExecutionContext[] executionContexts)
        {
            if (itemToAdd == null) throw new ArgumentNullException("itemToAdd");

            var concurrentDic = itemToAdd as IDictionary<string, object>;

            if (concurrentDic != null)
            {
                lock (this.@lock)
                {
                    this.documents.Add(new CloudSearchDocument(concurrentDic, SearchAction.Upload));
                    this.CommitPolicyExecutor.IndexModified(this, concurrentDic, IndexOperation.Add);
                    this.statistics.IncrementAddCounter();
                }
            }
        }

        public void UpdateDocument(object itemToUpdate, object criteriaForUpdate, IExecutionContext executionContext)
        {
            this.UpdateDocument(itemToUpdate, criteriaForUpdate, new[] { executionContext });
        }

        public void UpdateDocument([NotNull] object itemToUpdate, object criteriaForUpdate, params IExecutionContext[] executionContexts)
        {
            if (itemToUpdate == null) throw new ArgumentNullException("itemToUpdate");

            var concurrentDic = itemToUpdate as IDictionary<string, object>;
            if (concurrentDic != null)
            {
                lock (this.@lock)
                {
                    this.documents.Add(new CloudSearchDocument(concurrentDic, SearchAction.Upload));
                    this.CommitPolicyExecutor.IndexModified(this, concurrentDic, IndexOperation.Update);
                    this.statistics.IncrementUpdateCounter();
                }
            }
        }

        public void Delete([NotNull] IIndexableUniqueId id)
        {
            if (id == null) throw new ArgumentNullException("id");

            this.Delete(Utils.CloudIndexParser.HashUniqueId(id.Value.ToString()));
        }

        public void Delete([NotNull] IIndexableId id)
        {
            if (id == null)
                throw new ArgumentNullException("id");

            var searchIndex = this.Index as CloudSearchProviderIndex;

            // Find azure unique id in index first
            var fieldName = this.Index.FieldNameTranslator.GetIndexFieldName(BuiltinFields.ID);
            var formattedValue = this.Index.Configuration.IndexFieldStorageValueFormatter.FormatValueForIndexStorage(id.Value, fieldName);
            var expression = $"&$filter=({fieldName} eq '{formattedValue}')&$select={CloudSearchConfig.VirtualFields.CloudUniqueId}";

            var search = searchIndex.SearchService.Search(expression);
            if (search == null)
            {
                // null result means index is not created yet or no search service is available.
                return;
            }

            var searchResults = this.deserializer.Deserialize(search);

            if (searchResults.Documents == null || searchResults.Documents.Count == 0)
            {
                CrawlingLog.Log.Info($"[Index={this.index.Name}] Failed to delete document with id= {id.Value}, document not found");
                return;
            }

            this.Delete(searchResults.Documents.First().AzureUniqueId);
        }

        public void Delete([NotNull] FieldCriteria[] criteriaForDelete)
        {
            if (criteriaForDelete == null)
            {
                throw new ArgumentNullException(nameof(criteriaForDelete));
            }

            using (var searchContext = this.Index.CreateSearchContext())
            {
                IQueryable<GenericDocument> documents = searchContext.GetQueryable<GenericDocument>();
                documents = criteriaForDelete.Aggregate(documents, (q, criterion) => q.Where(doc => doc[criterion.FieldName] == criterion.FieldValue));

                documents.Select(doc => doc.AzureUniqueId).ForEach(this.Delete);
            }
        }

        private void Delete(string cloudUniqueId)
        {
            var document = new ConcurrentDictionary<string, object>();
            document.TryAdd(CloudSearchConfig.VirtualFields.CloudUniqueId, cloudUniqueId);
            this.documents.Add(new CloudSearchDocument(document, SearchAction.Delete));
            lock (this.@lock)
            {
                this.CommitPolicyExecutor.IndexModified(this, cloudUniqueId, IndexOperation.Delete);
                this.statistics.IncrementDeleteUniqueCounter();
            }
        }

        public bool IsParallel { get; private set; }
        public ParallelOptions ParallelOptions { get; private set; }
        public ISearchIndex Index
        {
            get
            {
                return this.index;
            }
        }
        public ICommitPolicyExecutor CommitPolicyExecutor { get; private set; }

        public IEnumerable<Shard> ShardsWithPendingChanges { get; private set; }

        #region Nested class GenericDocument

        /// <summary>
        /// This class is used to build generic queries when removing documents in <see cref="CloudSearchUpdateContext.Delete(FieldCriteria[])"/>.
        /// </summary>
        private class GenericDocument
        {
            public object this[[UsedImplicitly] string fieldName] { get { return null; } set { MainUtil.Nop(value); } }
            public string AzureUniqueId { get; set; }
        }

        #endregion
    }
}