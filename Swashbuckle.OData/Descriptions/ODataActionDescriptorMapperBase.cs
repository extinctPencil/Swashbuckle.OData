using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Web.Http.Controllers;
using System.Web.Http.Description;
using System.Web.Http.Routing;
using System.Web.Http.Services;
using System.Web.OData.Formatter;

namespace Swashbuckle.OData.Descriptions
{
    internal class ODataActionDescriptorMapperBase
    {
        protected void PopulateApiDescriptions(ODataActionDescriptor oDataActionDescriptor, List<ApiParameterDescription> parameterDescriptions, string apiDocumentation, List<ApiDescription> apiDescriptions)
        {
            // request formatters
            var bodyParameter = default(ApiParameterDescription);
            if (parameterDescriptions != null)
            {
                bodyParameter = parameterDescriptions.FirstOrDefault(description => description.Source == ApiParameterSource.FromBody);
            }

            var httpConfiguration = oDataActionDescriptor.ActionDescriptor.Configuration;
            Contract.Assume(httpConfiguration != null);
            var mediaTypeFormatterCollection = httpConfiguration.Formatters;
            var responseDescription = oDataActionDescriptor.ActionDescriptor.CreateResponseDescription();
            IEnumerable<MediaTypeFormatter> supportedRequestBodyFormatters = new List<MediaTypeFormatter>();
            IEnumerable<MediaTypeFormatter> supportedResponseFormatters = new List<MediaTypeFormatter>();
            if (mediaTypeFormatterCollection != null)
            {
                supportedRequestBodyFormatters = bodyParameter != null ? mediaTypeFormatterCollection.Where(f => f is ODataMediaTypeFormatter && f.CanReadType(bodyParameter.ParameterDescriptor.ParameterType)) : Enumerable.Empty<MediaTypeFormatter>();

                // response formatters
                var returnType = responseDescription.ResponseType ?? responseDescription.DeclaredType;
                supportedResponseFormatters = returnType != null && returnType != typeof (void) ? mediaTypeFormatterCollection.Where(f => f is ODataMediaTypeFormatter && f.CanWriteType(returnType)) : Enumerable.Empty<MediaTypeFormatter>();


                // Replacing the formatter tracers with formatters if tracers are present.
                supportedRequestBodyFormatters = SwaggerOperationMapper.GetInnerFormatters(supportedRequestBodyFormatters);
                supportedResponseFormatters = SwaggerOperationMapper.GetInnerFormatters(supportedResponseFormatters);
            }

            var supportedHttpMethods = GetHttpMethodsSupportedByAction(oDataActionDescriptor.Route, oDataActionDescriptor.ActionDescriptor);
            foreach (var supportedHttpMethod in supportedHttpMethods)
            {
                var apiDescription = new ApiDescription
                {
                    Documentation = apiDocumentation,
                    HttpMethod = supportedHttpMethod,
                    RelativePath = oDataActionDescriptor.RelativePathTemplate.TrimStart('/'),
                    ActionDescriptor = oDataActionDescriptor.ActionDescriptor,
                    Route = oDataActionDescriptor.Route
                };

                apiDescription.SupportedResponseFormatters.AddRange(supportedResponseFormatters);
                apiDescription.SupportedRequestBodyFormatters.AddRange(supportedRequestBodyFormatters.ToList());
                if (parameterDescriptions != null)
                {
                    apiDescription.ParameterDescriptions.AddRange(parameterDescriptions);
                }

                // Have to set ResponseDescription because it's internal!??
                apiDescription.GetType().GetProperty("ResponseDescription").SetValue(apiDescription, responseDescription);

                apiDescription.RelativePath = apiDescription.GetRelativePathForSwagger();

                apiDescriptions.Add(apiDescription);
            }
        }

        /// <summary>
        ///     Gets a collection of HttpMethods supported by the action. Called when initializing the
        ///     <see cref="ApiExplorer.ApiDescriptions" />.
        /// </summary>
        /// <param name="route">The route.</param>
        /// <param name="actionDescriptor">The action descriptor.</param>
        /// <returns>A collection of HttpMethods supported by the action.</returns>
        private static IEnumerable<HttpMethod> GetHttpMethodsSupportedByAction(IHttpRoute route, HttpActionDescriptor actionDescriptor)
        {
            Contract.Requires(route != null);
            Contract.Requires(actionDescriptor != null);
            Contract.Requires(route.Constraints != null);

            IList<HttpMethod> actionHttpMethods = actionDescriptor.SupportedHttpMethods;
            var httpMethodConstraint = route.Constraints.Values.FirstOrDefault(c => c is HttpMethodConstraint) as HttpMethodConstraint;

            return httpMethodConstraint?.AllowedMethods?.Intersect(actionHttpMethods).ToList() ?? actionHttpMethods;
        }

        private static IEnumerable<MediaTypeFormatter> GetInnerFormatters(IEnumerable<MediaTypeFormatter> mediaTypeFormatters)
        {
            Contract.Requires(mediaTypeFormatters != null);

            return mediaTypeFormatters.Select(Decorator.GetInner);
        }
    }
}