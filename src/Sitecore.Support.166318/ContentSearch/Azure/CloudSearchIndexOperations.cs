namespace Sitecore.Support.ContentSearch.Azure
{
  using Sitecore.ContentSearch;
  public class CloudSearchIndexOperations : Sitecore.ContentSearch.Azure.CloudSearchIndexOperations, IIndexOperations
  {
    public CloudSearchIndexOperations(ISearchIndex index) : base(index)
    {
    }

    void IIndexOperations.Delete(IIndexable indexable, IProviderUpdateContext context)
    {
      context.Delete(indexable.UniqueId);
    }
  }
}