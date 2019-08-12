using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Helpers
{
    public static class ObjectExtensions
    {
        public static List<PropertyInfo> GetPropertiesNames(this object obj)
        {
            var props = new PropertyInfo[] { };

            if(obj is IList)
            {
                var listType = ((IList)obj).GetType().GetGenericArguments().Single();

                props = listType.GetProperties();
            }
            else
            {
                props = obj.GetType().GetProperties();
            }

            return props.ToList();
        }

        public static String ToCSV(this object obj, 
            String separator = ",", 
            bool suppressHeader = default,
            object translatedColumns = default,
            List<String> ignoredProperties = default,
            String datesFormat = default,
            object formattedProperties = default,
            bool surroundWithQuotes = default,
            List<String> surroundedWithQuotes = default)
        {
            var csvOutput = "";
            var csvValues = new List<String>();

            //  make sure ignoredProperties is not null
            ignoredProperties = ignoredProperties ?? new List<String>();
            //  make sure markedWithQuotes is not null
            surroundedWithQuotes = surroundedWithQuotes ?? new List<String>();
            //  make sure formattedProperties is not null
            formattedProperties = formattedProperties ?? new { };
            //  make sure translatedColumns is not null
            translatedColumns = translatedColumns ?? new { };

            //  force ignoredProperties to be lower case
            ignoredProperties = ignoredProperties.Select(ip => ip.ToLower()).ToList();
            //  force markedWithQuotes to be lower case
            surroundedWithQuotes = surroundedWithQuotes.Select(mwq => mwq.ToLower()).ToList();

            //  build a KeyValuePair list based on dynamic formattedProperties
            //  this will allow to use LINQ through the property list
            List<PropertyInfo> piFormattedPropertiesNames = formattedProperties.GetPropertiesNames();
            var kvFormattedProperties = new List<KeyValuePair<String, String>>();
            foreach (var fp in piFormattedPropertiesNames)
            {
                var pValue = fp.GetValue(formattedProperties);
                kvFormattedProperties.Add(new KeyValuePair<string, string>(fp.Name.ToLower(), (string)pValue));
            }

            //  build a KeyValuePair list based on dynamic translatedColumns
            //  this will allow to use LINQ through the property list
            List<PropertyInfo> piTranslatedColumns = translatedColumns.GetPropertiesNames();
            var kvTranslatedColumns = new List<KeyValuePair<string, string>>();
            foreach(var fp in piTranslatedColumns)
            {
                var pValue = fp.GetValue(translatedColumns);
                kvTranslatedColumns.Add(new KeyValuePair<string, string>(fp.Name, (string)pValue));
            }

            //  decide to add a header to the csvOutput
            if (!suppressHeader)
            {
                var header = obj.GetPropertiesNames();
                header = header.Where(h => !ignoredProperties.Contains(h.Name.ToLower())).ToList();

                var headerString = string.Join(separator, header.Select(h => h.Name));

                if(translatedColumns != default)
                {
                    foreach (var name in kvTranslatedColumns)
                        headerString = headerString
                            .Replace(name.Key, name.Value);
                }

                csvOutput += headerString;
                csvOutput += Environment.NewLine;
            }

            //  needs refactoring
            if (obj is IList)
            {
                foreach (var item in (IList)obj)
                {
                    csvValues.Clear();

                    var props = item.GetType().GetProperties();
                    if(ignoredProperties != default(List<String>))
                    {
                        props = props.Where(p => !ignoredProperties.Contains(p.Name.ToLower())).ToArray();
                    }
                    foreach (var p in props)
                    {
                        var v = "";

                        //  general date format
                        if(datesFormat != default)
                        {
                            if(p.PropertyType == typeof(DateTime))
                            {
                                if (!kvFormattedProperties.Any(fp => fp.Key.ToLower() == p.Name.ToLower()))
                                {
                                    var date = (DateTime)p.GetValue(item);
                                    v = date.IsDateValid() ? String.Format("{0:" + datesFormat + "}", date) : "";

                                    if(surroundWithQuotes || surroundedWithQuotes.Contains(p.Name.ToLower()))
                                    {
                                        v = $"\"{v}\"";
                                    }

                                    csvValues.Add(v);

                                    continue;
                                }
                            }
                        }

                        //  specific optional formatting <'PropertyName','FormatString'>
                        var vFormatted = kvFormattedProperties.Where(fp => fp.Key.ToLower() == p.Name.ToLower()).FirstOrDefault();
                        if (!String.IsNullOrEmpty(vFormatted.Key) && !String.IsNullOrEmpty(vFormatted.Value))
                        {
                            //  is property is a date, its value will only show if valid
                            if (p.PropertyType == typeof(DateTime))
                            {
                                var date = (DateTime)p.GetValue(item);
                                v = date.IsDateValid() ? String.Format("{0:" + vFormatted.Value + "}", date) : "";
                            }
                            else
                            {
                                v = String.Format("{0:" + vFormatted.Value + "}", p.GetValue(item));
                            }
                        }

                        if (string.IsNullOrEmpty(v))
                        {
                            var vTemp = p.GetValue(item);
                            if (vTemp != null)
                                v = vTemp.ToString();
                            else
                                v = String.Empty;
                        }

                        if (surroundWithQuotes || surroundedWithQuotes.Contains(p.Name.ToLower()))
                        {
                            v = $"\"{v}\"";
                        }

                        csvValues.Add(v);
                    }
                    csvOutput += string.Join(separator, csvValues);
                    csvOutput += Environment.NewLine;
                }
            }
            else
            {
                var props = obj.GetType().GetProperties();
                if (ignoredProperties != default(List<String>))
                {
                    props = props.Where(p => !ignoredProperties.Contains(p.Name.ToLower())).ToArray();
                }
                foreach (var p in props)
                {
                    var v = "";

                    //  general date format
                    if (datesFormat != default)
                    {
                        if (p.PropertyType == typeof(DateTime))
                        {
                            if (!kvFormattedProperties.Any(fp => fp.Key.ToLower() == p.Name.ToLower()))
                            {
                                var date = (DateTime)p.GetValue(obj);
                                v = date.IsDateValid() ? String.Format("{0:" + datesFormat + "}", date) : "";

                                if (surroundWithQuotes || surroundedWithQuotes.Contains(p.Name.ToLower()))
                                {
                                    v = $"\"{v}\"";
                                }

                                csvValues.Add(v);
                                continue;
                            }
                        }
                    }

                    //  specific optional formatting <'PropertyName','FormatString'>
                    var vFormatted = kvFormattedProperties.Where(fp => fp.Key.ToLower() == p.Name.ToLower()).FirstOrDefault();

                        if (!String.IsNullOrEmpty(vFormatted.Key) && !String.IsNullOrEmpty(vFormatted.Value))
                        {
                            //  is property is a date, its value will only show if valid
                            if (p.PropertyType == typeof(DateTime))
                            {
                                var date = (DateTime)p.GetValue(obj);
                                v = date.IsDateValid() ? String.Format("{0:" + vFormatted.Value + "}", date) : "";
                            }
                            else
                            {
                                v = String.Format("{0:" + vFormatted.Value + "}", p.GetValue(obj));
                            }
                        }

                    if (string.IsNullOrEmpty(v))
                    {
                        var vTemp = p.GetValue(obj);
                        if (vTemp != null)
                            v = vTemp.ToString();
                        else
                            v = String.Empty;
                    }
                    if (surroundWithQuotes || surroundedWithQuotes.Contains(p.Name.ToLower()))
                    {
                        v = $"\"{v}\"";
                    }

                    csvValues.Add(v);
                }
                csvOutput += string.Join(separator, csvValues);
                csvOutput += Environment.NewLine;
            }

            return csvOutput;
        }
    }
}
