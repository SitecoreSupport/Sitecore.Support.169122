# Sitecore.Support.166318
Azure Search provider cannot handle queries that use conditions based on DateTime objects, for example:
```c#
        using (var context = ContentSearchManager.GetIndex("sitecore_web_index").CreateSearchContext())
        {
            var date = DateTime.Now.AddMonths(-1);
            var results = context.GetQueryable<SearchResultItem>().Where(x => x.CreatedDate >= date)       
        }
```
## License  
This patch is licensed under the [Sitecore Corporation A/S License for GitHub](https://github.com/sitecoresupport/Sitecore.Support.166318/blob/master/LICENSE).  

## Download  
Downloads are available via [GitHub Releases](https://github.com/sitecoresupport/Sitecore.Support.166318/releases).  
