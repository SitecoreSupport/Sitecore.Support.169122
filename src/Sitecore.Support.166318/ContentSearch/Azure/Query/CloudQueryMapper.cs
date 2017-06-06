namespace Sitecore.Support.ContentSearch.Azure.Query
{
    using Sitecore.ContentSearch.Azure.Models;
    using Sitecore.ContentSearch.Azure.Query;
    using Sitecore.ContentSearch.Azure.Schema;
    public class CloudQueryMapper : Sitecore.ContentSearch.Azure.Query.CloudQueryMapper
    {
        public CloudQueryMapper(CloudIndexParameters parameters) : base(parameters)
        {
        }

        protected ICloudSearchIndexSchema Schema
        {
            get
            {
                return this.Parameters.Schema as ICloudSearchIndexSchema;
            }
        }

        protected override string HandleGreaterThan(string srcFieldName, object srcValue, float boost)
        {
            string indexFieldName = this.Parameters.FieldNameTranslator.GetIndexFieldName(srcFieldName, this.Parameters.IndexedFieldType);
            IndexedField fieldByCloudName = this.Schema.GetFieldByCloudName(indexFieldName);
            if (fieldByCloudName == null)
            {
                return Sitecore.ContentSearch.Azure.Query.CloudQueryBuilder.Search.Operations.Equal(null, "This_Is_Equal_ConstNode_Return_Nothing", boost);
            }
            object value = base.ValueFormatter.FormatValueForIndexStorage(srcValue, indexFieldName);
            return CloudQueryBuilder.Filter.Operations.GreaterThan(indexFieldName, value, fieldByCloudName.Type);
        }

        protected override string HandleGreaterThanOrEqual(string srcFieldName, object value, float boost)
        {
            string indexFieldName = this.Parameters.FieldNameTranslator.GetIndexFieldName(srcFieldName, this.Parameters.IndexedFieldType);
            IndexedField fieldByCloudName = this.Schema.GetFieldByCloudName(indexFieldName);
            if (fieldByCloudName == null)
            {
                return Sitecore.ContentSearch.Azure.Query.CloudQueryBuilder.Search.Operations.Equal(null, "This_Is_Equal_ConstNode_Return_Nothing", boost);
            }
            object value2 = base.ValueFormatter.FormatValueForIndexStorage(value, indexFieldName);
            return CloudQueryBuilder.Filter.Operations.GreaterOrEqualThan(indexFieldName, value2, fieldByCloudName.Type);
        }

        protected override string HandleLessThan(string srcFieldName, object fieldValue)
        {
            string indexFieldName = this.Parameters.FieldNameTranslator.GetIndexFieldName(srcFieldName, this.Parameters.IndexedFieldType);
            IndexedField fieldByCloudName = this.Schema.GetFieldByCloudName(indexFieldName);
            if (fieldByCloudName == null)
            {
                return Sitecore.ContentSearch.Azure.Query.CloudQueryBuilder.Search.Operations.Equal(null, "This_Is_Equal_ConstNode_Return_Nothing", 1f);
            }
            object value = base.ValueFormatter.FormatValueForIndexStorage(fieldValue, indexFieldName);
            return CloudQueryBuilder.Filter.Operations.LessThan(indexFieldName, value, fieldByCloudName.Type);
        }

        protected override string HandleLessThanOrEqual(string srcFieldName, object srcValue)
        {
            string indexFieldName = this.Parameters.FieldNameTranslator.GetIndexFieldName(srcFieldName, this.Parameters.IndexedFieldType);
            IndexedField fieldByCloudName = this.Schema.GetFieldByCloudName(indexFieldName);
            if (fieldByCloudName == null)
            {
                return Sitecore.ContentSearch.Azure.Query.CloudQueryBuilder.Search.Operations.Equal(null, "This_Is_Equal_ConstNode_Return_Nothing", 1f);
            }
            object value = base.ValueFormatter.FormatValueForIndexStorage(srcValue, indexFieldName);
            return CloudQueryBuilder.Filter.Operations.LessOrEqualThan(indexFieldName, value, fieldByCloudName.Type);
        }
    }
}