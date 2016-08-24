﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using WikiClientLibrary.Client;
using WikiClientLibrary.Generators;

namespace WikiClientLibrary
{
    internal static class Utility
    {
        // http://stackoverflow.com/questions/36186276/is-the-json-net-jsonserializer-threadsafe
        private static readonly JsonSerializerSettings WikiJsonSerializerSettings =
            new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                NullValueHandling = NullValueHandling.Include,
                ContractResolver = new DefaultContractResolver {NamingStrategy = new WikiJsonNamingStrategy()},
                Converters =
                {
                    new WikiBooleanJsonConverter()
                },
            };

        public static readonly JsonSerializer WikiJsonSerializer =
            JsonSerializer.CreateDefault(WikiJsonSerializerSettings);

        /// <summary>
        /// Convert name-value paris to URL query format.
        /// This overload handles <see cref="ExpandoObject"/> as well as anonymous objects.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The key-value pair with null value will be excluded. To specify a key with empty value,
        /// consider using <see cref="string.Empty"/> .
        /// </para>
        /// <para>
        /// For <see cref="bool"/> values, if the value is true, a pair with key and empty value
        /// will be generated; otherwise the whole pair will be excluded. 
        /// </para>
        /// <para>
        /// If <paramref name="values"/> is <see cref="IEnumerable{T}"/> of <see cref="KeyValuePair{TKey,TValue}"/>
        /// of strings, the values will be returned with no further processing.
        /// </para>
        /// </remarks>
        public static IEnumerable<KeyValuePair<string, string>> ToWikiStringValuePairs(object values)
        {
            var pc = values as IEnumerable<KeyValuePair<string, string>>;
            if (pc != null) return pc;
            return IterateWikiStringValuePairs(values);
        }

        private static string ToWikiQueryValue(object value)
        {
            if (value == null)
                return null;
            if (value is bool)
            {
                if ((bool) value) value = "";
                else return null;
            }
            else if (value is AutoWatchBehavior)
            {
                switch ((AutoWatchBehavior)value)
                {
                    case AutoWatchBehavior.Default:
                        value = "preferences";
                        break;
                    case AutoWatchBehavior.None:
                        value = "nochange";
                        break;
                    case AutoWatchBehavior.Watch:
                        value = "watch";
                        break;
                    case AutoWatchBehavior.Unwatch:
                        value = "unwatch";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value), value, null);
                }
            }
            else if (value is DateTime)
            {
                // ISO 8601
                value = ((DateTime)value).ToString("yyyy-MM-ddTHH:mm:ssK");
            }
            return Convert.ToString(value);
        }

        private static IEnumerable<KeyValuePair<string, string>> IterateWikiStringValuePairs(object values)
        {
            Debug.Assert(!(values is IEnumerable<KeyValuePair<string, string>>));
            var dict = values as IDictionary<string, object>;
            if (dict != null)
                return from p in dict
                    let value = ToWikiQueryValue(p.Value)
                    where value != null
                    select new KeyValuePair<string, string>(p.Key, value);
            return from p in values.GetType().GetRuntimeProperties()
                   let value = ToWikiQueryValue(p.GetValue(values))
                   where value != null select new KeyValuePair<string, string>(p.Name, value);
        }

        /// <summary>
        /// Partitions <see cref="IEnumerable{T}"/> into a sequence of <see cref="IEnumerable{T}"/>,
        /// each child <see cref="IEnumerable{T}"/> having the same length, except the last one.
        /// </summary>
        public static IEnumerable<IReadOnlyCollection<T>> Partition<T>(this IEnumerable<T> source, int partitionSize)
        {
            if (partitionSize <= 0) throw new ArgumentOutOfRangeException(nameof(partitionSize));
            var list = new List<T>(partitionSize);
            foreach (var item in source)
            {
                list.Add(item);
                if (list.Count == partitionSize)
                {
                    yield return list;
                    list.Clear();
                }
            }
            if (list.Count > 0) yield return list;
        }

        public static string ToString(this PropertyFilterOption value,
            string withValue, string withoutValue, string allValue = "all")
        {
            switch (value)
            {
                case PropertyFilterOption.Disable:
                    return allValue;
                case PropertyFilterOption.WithProperty:
                    return withValue;
                case PropertyFilterOption.WithoutProperty:
                    return withoutValue;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }
    }
}
