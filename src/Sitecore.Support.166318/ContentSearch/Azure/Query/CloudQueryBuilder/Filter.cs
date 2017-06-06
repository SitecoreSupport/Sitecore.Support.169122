namespace Sitecore.Support.ContentSearch.Azure.Query.CloudQueryBuilder
{
    using System;
    using Sitecore.ContentSearch.Azure.Http;
    using Sitecore.ContentSearch.Linq;
    public static class Filter
    {
        public static string IsMatchScoring(string expression)
        {
            return string.Format("&$filter=search.ismatchscoring('{0}', null, 'full', null)", expression);
        }
        public static class Operations
        {
            public static string Equal(string field, object value, string edmType)
            {
                return Equal(field, value, edmType, 1f);
            }

            public static string Equal(string field, object value, string edmType, float boost)
            {
                if (Sitecore.ContentSearch.Azure.Query.CloudQueryBuilder.Settings.UseIsMatchScoring && boost > 1f)
                {
                    string expression = $"{field}:{value}^{boost}";
                    return IsMatchScoring(expression);
                }

                return string.Format("&$filter={0} eq {1}", field, PrepareValue(value, edmType));
            }

            public static string NotEqual(string field, object value, string edmType)
            {
                return string.Format("&$filter={0} ne {1}", field, PrepareValue(value, edmType));
            }

            public static string GreaterThan(string field, object value, string edmType)
            {
                return string.Format("&$filter={0} gt {1}", field, PrepareValue(value, edmType));
            }

            public static string GreaterOrEqualThan(string field, object value, string edmType)
            {
                return string.Format("&$filter={0} ge {1}", field, PrepareValue(value, edmType));
            }

            public static string LessThan(string field, object value, string edmType)
            {
                return string.Format("&$filter={0} lt {1}", field, PrepareValue(value, edmType));
            }

            public static string LessOrEqualThan(string field, object value, string edmType)
            {
                return string.Format("&$filter={0} le {1}", field, PrepareValue(value, edmType));
            }

            public static string Between(string field, object from, object to, string edmType,
                Inclusion inclusion = Inclusion.None)
            {
                var includeLower = inclusion == Inclusion.Both || inclusion == Inclusion.Lower;
                var includeUpper = inclusion == Inclusion.Both || inclusion == Inclusion.Upper;

                from = PrepareValue(from, edmType);
                to = PrepareValue(to, edmType);

                var left = includeLower
                    ? string.Format("{0} ge {1}", field, from)
                    : string.Format("{0} gt {1}", field, from);

                var right = includeUpper
                    ? string.Format("{0} le {1}", field, to)
                    : string.Format("{0} lt {1}", field, to);

                return string.Format("&$filter=({0} and {1})", left, right);
            }

            public static string Is(string field, bool value)
            {
                if (!value)
                {
                    return string.Format("&$filter=not {0}", field);
                }

                return string.Format("&$filter={0}", field);
            }

            private static object PrepareValue(object value, string edmType)
            {
                if (value == null)
                {
                    return "null";
                }

                switch (edmType)
                {
                    case EdmTypes.DateTimeOffset:
                        {
                            if (value is DateTimeOffset)
                                return ((DateTimeOffset)value).ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                            if (value is DateTime)
                                return ((DateTime)value).ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                            return value.ToString();
                        }
                    case EdmTypes.String:
                    case EdmTypes.StringCollection:
                        {
                            if (value is Guid)
                            {
                                return "'" + ((Guid)value).ToString("N") + "'";
                            }

                            return "'" + value + "'";
                        }
                    case EdmTypes.Boolean:
                        {
                            return value.ToString().ToLower();
                        }
                    default:
                        {
                            return value;
                        }
                }
            }
        }
    }
}