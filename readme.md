# Sitecore.Support.169122
**166318**: Azure Search provider cannot handle queries that use conditions based on DateTime objects, for example:
```c#
        using (var context = ContentSearchManager.GetIndex("sitecore_web_index").CreateSearchContext())
        {
            var date = DateTime.Now.AddMonths(-1);
            var results = context.GetQueryable<SearchResultItem>().Where(x => x.CreatedDate >= date)       
        }
```
**162451**: Search queries may use wrong syntax ignoring Azure Search schema settings, for example:
`$filter=template_1%20eq%20'%7BAB86861A-6030-46C5-B394-E8F99E8B87DB%7D'%20and%20(path_1/any(t:t%20eq%20'%7B3C1715FE-6A13-4FCF-845F-DE308BA9741D%7D'))` (wrong)  
instead of  
`search=template_1:(ab86861a603046c5b394e8f99e8b87db)&$filter=(path_1/any(t:t%20eq%20'3c1715fe6a134fcf845fde308ba9741d'))&queryType=full` (correct)

**169124**: Some index fields (latestversion, isclone) are not modified during index update

## License  
This patch is licensed under the [Sitecore Corporation A/S License for GitHub](https://github.com/sitecoresupport/Sitecore.Support.166318/blob/master/LICENSE).  

## Download  
Downloads are available via [GitHub Releases](https://github.com/sitecoresupport/Sitecore.Support.166318/releases).  
