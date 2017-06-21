using System;
using System.Collections.Generic;
using System.Reflection;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Converters;
using Sitecore.Diagnostics;

namespace Sitecore.Support.ContentSearch.Azure.Converters
{
    public class CloudIndexFieldStorageValueFormatter : Sitecore.ContentSearch.Azure.Converters.CloudIndexFieldStorageValueFormatter
    {
        private static readonly FieldInfo searchIndexFieldInfo;
        private static readonly MethodInfo convertToTypeMethodInfo;

        static CloudIndexFieldStorageValueFormatter()
        {
            searchIndexFieldInfo = typeof(Sitecore.ContentSearch.Azure.Converters.CloudIndexFieldStorageValueFormatter).GetField(
                "searchIndex", BindingFlags.Instance | BindingFlags.NonPublic);
            convertToTypeMethodInfo = typeof(Sitecore.ContentSearch.Azure.Converters.CloudIndexFieldStorageValueFormatter).GetMethod(
                "ConvertToType", BindingFlags.Instance | BindingFlags.NonPublic);
        }
        public override object FormatValueForIndexStorage(object value, string fieldName)
        {
            Assert.IsNotNullOrEmpty(fieldName, nameof(fieldName));

            var result = value;

            if (result == null)
            {
                return null;
            }

            var fieldSchema = ((Sitecore.ContentSearch.Azure.CloudSearchProviderIndex)searchIndexFieldInfo.GetValue(this)).SchemaBuilder.GetSchema().GetFieldByCloudName(fieldName);
            if (fieldSchema == null)
            {
                fieldSchema = ((Sitecore.ContentSearch.Azure.CloudSearchProviderIndex)searchIndexFieldInfo.GetValue(this)).SearchService.Schema.GetFieldByCloudName(fieldName);
            }
            //Quick fix for computed fields
            if (fieldSchema == null)
            {
                return value;
            }

            var cloudTypeMapper = ((Sitecore.ContentSearch.Azure.CloudSearchProviderIndex)searchIndexFieldInfo.GetValue(this)).CloudConfiguration.CloudTypeMapper;
            var fieldType = cloudTypeMapper.GetNativeType(fieldSchema.Type);

            var context = new IndexFieldConverterContext(fieldName);

            try
            {
                if (result is IIndexableId)
                {
                    result = this.FormatValueForIndexStorage(((IIndexableId)result).Value, fieldName);
                }
                else if (result is IIndexableUniqueId)
                {
                    result = this.FormatValueForIndexStorage(((IIndexableUniqueId)result).Value, fieldName);
                }
                else
                {
                    result = convertToTypeMethodInfo.Invoke(this, new object[] {result, fieldType, context});
                }

                if (result != null && !(result is string || fieldType.IsInstanceOfType(result) || (result is IEnumerable<string> && typeof(IEnumerable<string>).IsAssignableFrom(fieldType))))
                {
                    throw new InvalidCastException($"Converted value has type '{result.GetType()}', but '{fieldType}' is expected.");
                }
            }
            catch (Exception ex)
            {
                throw new NotSupportedException($"Field '{fieldName}' with value '{value}' of type '{value.GetType()}' cannot be converted to type '{fieldType}' declared for the field in the schema.", ex);
            }

            return result;
        }
    }
}