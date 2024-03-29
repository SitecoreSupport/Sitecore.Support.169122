# Sitecore.Support.169122
**166318 (Fixed in 9.1.0)**: Azure Search provider cannot handle queries that use conditions based on DateTime objects, for example:
```c#
        using (var context = ContentSearchManager.GetIndex("sitecore_web_index").CreateSearchContext())
        {
            var date = DateTime.Now.AddMonths(-1);
            var results = context.GetQueryable<SearchResultItem>().Where(x => x.CreatedDate >= date)       
        }
```
**162451 (Fixed in 8.2.4)**: Search queries may use wrong syntax ignoring Azure Search schema settings, for example:
`$filter=template_1%20eq%20'%7BAB86861A-6030-46C5-B394-E8F99E8B87DB%7D'%20and%20(path_1/any(t:t%20eq%20'%7B3C1715FE-6A13-4FCF-845F-DE308BA9741D%7D'))` (wrong)  
instead of  
`search=template_1:(ab86861a603046c5b394e8f99e8b87db)&$filter=(path_1/any(t:t%20eq%20'3c1715fe6a134fcf845fde308ba9741d'))&queryType=full` (correct)

**169124 (Fixed in 8.2.4)**: Some index fields (latestversion, isclone) are not modified during index update

**147386 (Fixed in 8.2.7)**: Search should return documents which match all terms in a phrase.  

**147550 (Fixed in 8.2.7)**: Search result contains 50 items by default

**164633 (Fixed in 8.2.4)**: Index document delete operation may fail with an exception if the document is not present in the index

**299679 (Fixed in 9.2.0)**: All item-specific documents may be deleted instead of deleting a document for a specific item version

**136614 (Fixed in 8.2.6)**: Implementation of SchemaBuilder and SearchService properties of a search index do not allow to customize Azure Search provider

**362127 (Fixed in 9.2.0)**: The Final Rendering field was not excluded from indexing which could led to skipping some items during indexing

## License  
This patch is licensed under the [Sitecore Corporation A/S License for GitHub](https://github.com/sitecoresupport/Sitecore.Support.166318/blob/master/LICENSE).  

## Download  
Downloads are available via [GitHub Releases](https://github.com/sitecoresupport/Sitecore.Support.166318/releases).  
