using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Routing;

namespace System.Web.Http.Routing.temp
{
    public static class TempHttpRoute
    {

        public static IHttpRouteData GetRouteData(string virtualPathRoot, HttpRequestMessage request)
        {
            if (virtualPathRoot == null)
            {
                throw new ArgumentNullException("virtualPathRoot");
            }
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }
            RoutingContext orCreateRoutingContext = GetOrCreateRoutingContext(virtualPathRoot, request);
            if (!orCreateRoutingContext.IsValid)
            {
                return null;
            }
            HttpRouteValueDictionary values = Match(orCreateRoutingContext, new HttpRouteValueDictionary());
            if (values == null)
            {
                return null;
            }
            //if (!ProcessConstraints(request, values, HttpRouteDirection.UriResolution))
            //{
            //    return null;
            //}
            return null;
            //return new HttpRouteData(this, values);
        }


        private static List<BatchSegment> thisPathSegments = new List<PathSegment>();


        #region match
        private static HttpRouteValueDictionary Match(RoutingContext context, HttpRouteValueDictionary defaultValues)
        {
            List<string> pathSegments = context.PathSegments;
            if (defaultValues == null)
            {
                defaultValues = new HttpRouteValueDictionary();
            }
            HttpRouteValueDictionary matchedValues = new HttpRouteValueDictionary();
            bool flag = false;
            bool flag2 = false;
            for (int i = 0; i < thisPathSegments.Count; i++)
            {
                PathSegment segment = thisPathSegments[i];
                if (pathSegments.Count <= i)
                {
                    flag = true;
                }
                string a = flag ? null : pathSegments[i];
                if (segment is PathSeparatorSegment)
                {
                    if (!flag && !string.Equals(a, "/", StringComparison.Ordinal))
                    {
                        return null;
                    }
                }
                else
                {
                    PathContentSegment contentPathSegment = segment as PathContentSegment;
                    if (contentPathSegment != null)
                    {
                        if (contentPathSegment.IsCatchAll)
                        {
                            MatchCatchAll(contentPathSegment, pathSegments.Skip<string>(i), defaultValues, matchedValues);
                            flag2 = true;
                        }
                        else if (!MatchContentPathSegment(contentPathSegment, a, defaultValues, matchedValues))
                        {
                            return null;
                        }
                    }
                }
            }
            if (!flag2 && (this.PathSegments.Count < pathSegments.Count))
            {
                for (int j = thisPathSegments.Count; j < pathSegments.Count; j++)
                {
                    if (!IsSeparator(pathSegments[j]))
                    {
                        return null;
                    }
                }
            }
            if (defaultValues != null)
            {
                foreach (KeyValuePair<string, object> pair in defaultValues)
                {
                    if (!matchedValues.ContainsKey(pair.Key))
                    {
                        matchedValues.Add(pair.Key, pair.Value);
                    }
                }
            }
            return matchedValues;
        }


        internal static bool IsSeparator(string s) =>
string.Equals(s, "/", StringComparison.Ordinal);


        private static void MatchCatchAll(PathContentSegment contentPathSegment, IEnumerable<string> remainingRequestSegments, HttpRouteValueDictionary defaultValues, HttpRouteValueDictionary matchedValues)
        {
            object obj2;
            string str = string.Join(string.Empty, remainingRequestSegments.ToArray<string>());
            PathParameterSubsegment subsegment = contentPathSegment.Subsegments[0] as PathParameterSubsegment;
            if (str.Length > 0)
            {
                obj2 = str;
            }
            else
            {
                defaultValues.TryGetValue(subsegment.ParameterName, out obj2);
            }
            matchedValues.Add(subsegment.ParameterName, obj2);
        }

        private static bool MatchContentPathSegment(PathContentSegment routeSegment, string requestPathSegment, HttpRouteValueDictionary defaultValues, HttpRouteValueDictionary matchedValues)
        {
            if (string.IsNullOrEmpty(requestPathSegment))
            {
                if (routeSegment.Subsegments.Count <= 1)
                {
                    object obj2;
                    PathParameterSubsegment subsegment3 = routeSegment.Subsegments[0] as PathParameterSubsegment;
                    if (subsegment3 == null)
                    {
                        return false;
                    }
                    if (defaultValues.TryGetValue(subsegment3.ParameterName, out obj2))
                    {
                        matchedValues.Add(subsegment3.ParameterName, obj2);
                        return true;
                    }
                }
                return false;
            }
            if (routeSegment.Subsegments.Count == 1)
            {
                return MatchSingleContentPathSegment(routeSegment.Subsegments[0], requestPathSegment, matchedValues);
            }
            int length = requestPathSegment.Length;
            int num2 = routeSegment.Subsegments.Count - 1;
            PathParameterSubsegment subsegment = null;
            PathLiteralSubsegment subsegment2 = null;
            while (num2 >= 0)
            {
                int num3 = length;
                PathParameterSubsegment subsegment4 = routeSegment.Subsegments[num2] as PathParameterSubsegment;
                if (subsegment4 != null)
                {
                    subsegment = subsegment4;
                }
                else
                {
                    PathLiteralSubsegment subsegment5 = routeSegment.Subsegments[num2] as PathLiteralSubsegment;
                    if (subsegment5 != null)
                    {
                        subsegment2 = subsegment5;
                        int startIndex = length - 1;
                        if (subsegment != null)
                        {
                            startIndex--;
                        }
                        if (startIndex < 0)
                        {
                            return false;
                        }
                        int num5 = requestPathSegment.LastIndexOf(subsegment5.Literal, startIndex, StringComparison.OrdinalIgnoreCase);
                        if (num5 == -1)
                        {
                            return false;
                        }
                        if ((num2 == (routeSegment.Subsegments.Count - 1)) && ((num5 + subsegment5.Literal.Length) != requestPathSegment.Length))
                        {
                            return false;
                        }
                        num3 = num5;
                    }
                }
                if ((subsegment != null) && (((subsegment2 != null) && (subsegment4 == null)) || (num2 == 0)))
                {
                    int num6;
                    int num7;
                    if (subsegment2 == null)
                    {
                        if (num2 == 0)
                        {
                            num6 = 0;
                        }
                        else
                        {
                            num6 = num3;
                        }
                        num7 = length;
                    }
                    else if ((num2 == 0) && (subsegment4 != null))
                    {
                        num6 = 0;
                        num7 = length;
                    }
                    else
                    {
                        num6 = num3 + subsegment2.Literal.Length;
                        num7 = length - num6;
                    }
                    string str = requestPathSegment.Substring(num6, num7);
                    if (string.IsNullOrEmpty(str))
                    {
                        return false;
                    }
                    matchedValues.Add(subsegment.ParameterName, str);
                    subsegment = null;
                    subsegment2 = null;
                }
                length = num3;
                num2--;
            }
            if (length != 0)
            {
                return (routeSegment.Subsegments[0] is PathParameterSubsegment);
            }
            return true;
        }



        #endregion

        private static RoutingContext GetOrCreateRoutingContext(string virtualPathRoot, HttpRequestMessage request)
        {
            RoutingContext context = null;
            if (request.Properties.ContainsKey("MS_RoutingContext"))
            {
                context = request.Properties["MS_RoutingContext"] as RoutingContext;
            }

            if (context == null)
            {
                context = CreateRoutingContext(virtualPathRoot, request);
                request.Properties["MS_RoutingContext"] = context;
            }
            return context;
        }




        private static RoutingContext CreateRoutingContext(string virtualPathRoot, HttpRequestMessage request)
        {
            string str = "/" + request.RequestUri.GetComponents(UriComponents.Path, UriFormat.Unescaped);
            if (!str.StartsWith(virtualPathRoot, StringComparison.Ordinal) && !str.StartsWith(virtualPathRoot, StringComparison.OrdinalIgnoreCase))
            {
                return RoutingContext.Invalid();
            }
            string uri = null;
            int length = virtualPathRoot.Length;
            if ((str.Length > length) && (str[length] == '/'))
            {
                uri = str.Substring(length + 1);
            }
            else
            {
                uri = str.Substring(length);
            }
            return RoutingContext.Valid(SplitUriToPathSegmentStrings(uri));
        }

        internal static List<string> SplitUriToPathSegmentStrings(string uri)
        {
            List<string> parts = new List<string>();

            if (String.IsNullOrEmpty(uri))
            {
                return parts;
            }

            int currentIndex = 0;

            // Split the incoming URI into individual parts
            while (currentIndex < uri.Length)
            {
                int indexOfNextSeparator = uri.IndexOf('/', currentIndex);
                if (indexOfNextSeparator == -1)
                {
                    // If there are no more separators, the rest of the string is the last part
                    string finalPart = uri.Substring(currentIndex);
                    if (finalPart.Length > 0)
                    {
                        parts.Add(finalPart);
                    }
                    break;
                }

                string nextPart = uri.Substring(currentIndex, indexOfNextSeparator - currentIndex);
                if (nextPart.Length > 0)
                {
                    parts.Add(nextPart);
                }

                Contract.Assert(uri[indexOfNextSeparator] == '/', "The separator char itself should always be a '/'.");
                parts.Add("/");
                currentIndex = indexOfNextSeparator + 1;
            }

            return parts;
        }





    }
    internal class RoutingContext
    {
        private static readonly RoutingContext CachedInvalid;

        static RoutingContext()
        {
            RoutingContext context1 = new RoutingContext
            {
                IsValid = false
            };
            CachedInvalid = context1;
        }

        private RoutingContext()
        {
        }

        public static RoutingContext Invalid()
        {
            return CachedInvalid;
        }

        public static RoutingContext Valid(List<string> pathSegments)
        {
            return new RoutingContext
            {
                PathSegments = pathSegments,
                IsValid = true
            };
        }

        public bool IsValid { get; private set; }

        public List<string> PathSegments { get; private set; }
    }

}







