﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.AspNet.OData.Routing;
using Swashbuckle.Swagger;

namespace Swashbuckle.OData.Descriptions
{

    internal static class OperationExtensions
    {
        public static IDictionary<string, string> GenerateSamplePathParameterValues(this Operation operation)
        {
            Contract.Requires(operation != null);

            return operation.parameters?.Where(parameter => parameter.@in == "path")
                .ToDictionary(queryParameter => queryParameter.name, queryParameter => queryParameter.GenerateSamplePathParameterValue());
        }

        public static string GenerateSampleODataUri(this Operation operation, string serviceRoot, string pathTemplate, ODataRoute route)
        {
            Contract.Requires(operation != null);
            Contract.Requires(serviceRoot != null);

            var parameters = operation.GenerateSamplePathParameterValues();

            if (parameters != null && parameters.Any())
            {
                // if we have been supplied a versionPathResolver lets Use it.
               ODataSwaggerProvider.VersionPathParameterResolver?.Invoke(parameters, route);

                var prefix = new Uri(serviceRoot);

                return new UriTemplate(pathTemplate).BindByName(prefix, parameters).ToString();
            }
            return serviceRoot.AppendUriSegment(pathTemplate);
        }

        public static IList<Parameter> Parameters(this Operation operation)
        {
            Contract.Requires(operation != null);

            return operation.parameters ?? (operation.parameters = new List<Parameter>());
        }
    }
}