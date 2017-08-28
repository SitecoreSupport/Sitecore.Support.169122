

namespace Sitecore.Support.ContentSearch.Azure.Query
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Diagnostics;
    using Newtonsoft.Json.Linq;
    using Sitecore.ContentSearch.Azure;
    using Sitecore.ContentSearch.Azure.Query;
    using Sitecore.ContentSearch.Azure.Utils;
    using Sitecore.ContentSearch.Diagnostics;
    using Sitecore.ContentSearch.Linq;
    using Sitecore.ContentSearch.Linq.Common;
    using Sitecore.ContentSearch.Linq.Methods;
    using Sitecore.ContentSearch.Linq.Nodes;

    public class LinqToCloudIndex<TItem> : Sitecore.ContentSearch.Azure.Query.LinqToCloudIndex<TItem>
    {
        private readonly CloudSearchSearchContext context;

        private static readonly MethodInfo miApplyScalarMethods;

        private static readonly MethodInfo miOptimizeQueryExpression;

        static LinqToCloudIndex()
        {
            var t = typeof(Sitecore.ContentSearch.Azure.Query.LinqToCloudIndex<TItem>);

            miApplyScalarMethods = t.GetMethod("ApplyScalarMethods", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(miApplyScalarMethods, "miApplyScalarMethods is null...");

            miOptimizeQueryExpression = t.GetMethod("OptimizeQueryExpression",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(miOptimizeQueryExpression, "miOptimizeQueryExpression is null...");

        }

        public LinqToCloudIndex(CloudSearchSearchContext context, IExecutionContext executionContext)
            : base(context, executionContext)
        {
            this.context = context;
        }

        public LinqToCloudIndex(CloudSearchSearchContext context, IExecutionContext[] executionContexts)
            : base(context, executionContexts)
        {
            this.context = context;
        }

        public override TResult Execute<TResult>(CloudQuery query)
        {
            var selectMethod = GetSelectMethod(query);
            int totalDoc, countDoc;
            Dictionary<string, object> facetResult;

            var results = this.Execute(query, out countDoc, out totalDoc, out facetResult);

            Type documentType = null;
            object processedResults;

            if (typeof(TResult).IsGenericType && typeof(TResult).GetGenericTypeDefinition() == typeof(SearchResults<>))
            {
                documentType = typeof(TResult).GetGenericArguments()[0];

                // Construct CloudSearchResults<> instance
                var cloudSearchResultsGenericType = typeof(CloudSearchResults<>).MakeGenericType(documentType);
                processedResults = Activator.CreateInstance(cloudSearchResultsGenericType, this.context, results,
                    selectMethod, countDoc, facetResult, this.Parameters, query.VirtualFieldProcessors);
            }
            else
            {
                documentType = typeof(TItem);
                processedResults = new CloudSearchResults<TItem>(this.context, results, selectMethod, countDoc,
                    facetResult, this.Parameters, query.VirtualFieldProcessors);
            }

            // Since the ApplyScalarMethods is private, in any case reflection has to be used 
            // return this.ApplyScalarMethods<TResult, TItem>(query, processedResults, totalDoc);
            var applyScalarMethodsGenericMethod = miApplyScalarMethods.MakeGenericMethod(typeof(TResult), documentType);

            TResult result;

            try
            {
                result =
                    (TResult) applyScalarMethodsGenericMethod.Invoke(this, new[] {query, processedResults, totalDoc});
            }
            catch (TargetInvocationException ex)
            {
                // Required for First or Single methdos otherwise TargetInvocationException is thrown instead of InvalidOperationException
                throw ex.InnerException;
            }

            return result;
        }


        public override IEnumerable<TElement> FindElements<TElement>(CloudQuery query)
        {
            SearchLog.Log.Debug("Executing query: " + query.Expression);

            if (EnumerableLinq.ShouldExecuteEnumerableLinqQuery(query))
            {
                return EnumerableLinq.ExecuteEnumerableLinqQuery<IEnumerable<TElement>>(query);
            }

            int totalDoc, countDoc;
            Dictionary<string, object> facetResult;
            var valueList = this.Execute(query, out countDoc, out totalDoc, out facetResult);
            var selectMethod = GetSelectMethod(query);
            var processedResults = new CloudSearchResults<TElement>(this.context, valueList, selectMethod, countDoc,
                facetResult, this.Parameters, query.VirtualFieldProcessors);
            return processedResults.GetSearchResults();
        }

        internal List<Dictionary<string, object>> Execute(CloudQuery query, out int countDoc, out int totalDoc,
            out Dictionary<string, object> facetResult)
        {
            countDoc = 0;
            totalDoc = 0;
            facetResult = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(query.Expression) || query.Methods.Count > 0)
            {
                var searchIndex = this.context.Index as CloudSearchProviderIndex;
                if (searchIndex == null)
                {
                    return new List<Dictionary<string, object>>();
                }

                if (
                    query.Expression.Contains(
                        Sitecore.ContentSearch.Azure.Query.CloudQueryBuilder.Search.SearchForNothing))
                {
                    return new List<Dictionary<string, object>>();
                }

                // Finalize the query expression
                var expression = this.OptimizeQueryExpression(query, searchIndex) + "&$count=true";

                // append $count=true to find out total found document, but has performance impact;

                try
                {
                    var results = searchIndex.SearchService.Search(expression);

                    if (string.IsNullOrEmpty(results))
                    {
                        return new List<Dictionary<string, object>>();
                    }

                    // TODO Replace this handmade deserialization with SearchResultsDeserializer.
                    var valueList =
                        JObject.Parse(results)
                            .SelectToken("value")
                            .Select(x => JsonHelper.Deserialize(x.ToString()) as Dictionary<string, object>)
                            .ToList();
                    if (valueList.Count != 0)
                    {
                        totalDoc = JObject.Parse(results)["@odata.count"].ToObject<int>();
                        countDoc = totalDoc;
                        var skipFields =
                            query.Methods.Where(m => m.MethodType == QueryMethodType.Skip)
                                .Select(m => (SkipMethod) m)
                                .ToList();
                        if (skipFields.Any())
                        {
                            var start = skipFields.Sum(skipMethod => skipMethod.Count);
                            countDoc = countDoc - start;
                        }

                        // If take method is defined, total doc will based on that
                        var takeFields =
                            query.Methods.Where(m => m.MethodType == QueryMethodType.Take)
                                .Select(m => (TakeMethod) m)
                                .ToList();
                        if (takeFields.Any())
                        {
                            countDoc = takeFields.Sum(takeMethod => takeMethod.Count);
                            if (valueList.Count < countDoc)
                            {
                                countDoc = valueList.Count;
                            }
                        }

                        var facetData = JObject.Parse(results).GetValue("@search.facets");
                        if (facetData != null)
                        {
                            facetResult =
                                JObject.Parse(results).GetValue("@search.facets").ToObject<Dictionary<string, object>>();
                        }

                        return valueList;
                    }
                }
                catch (Exception ex)
                {
                    SearchLog.Log.Error(
                        string.Format("Azure Search Error [Index={0}] ERROR:{1} Search expression:{2}", searchIndex.Name,
                            ex.Message, query.Expression));

                    throw;
                }
            }

            return new List<Dictionary<string, object>>();
        }

        private string OptimizeQueryExpression(CloudQuery query, CloudSearchProviderIndex index)
        {
            return (string)miOptimizeQueryExpression.Invoke(this, new object[] { query, index });
        }

        private static SelectMethod GetSelectMethod(CloudQuery compositeQuery)
        {
            var selectMethods =
                compositeQuery.Methods.Where(m => m.MethodType == QueryMethodType.Select)
                    .Select(m => (SelectMethod) m)
                    .ToList();

            return selectMethods.Count() == 1 ? selectMethods[0] : null;
        }

    }
}