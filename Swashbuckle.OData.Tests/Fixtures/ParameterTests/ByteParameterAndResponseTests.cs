﻿using System;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using FluentAssertions;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.OData.Edm;
using Microsoft.Owin.Hosting;
using NUnit.Framework;
using Owin;
using Swashbuckle.Swagger;

namespace Swashbuckle.OData.Tests
{
    [TestFixture]
    public class ByteParameterAndResponseTests
    {
        [Test]
        public async Task It_supports_a_byte_parameter()
        {
            using (WebApp.Start(HttpClientUtils.BaseAddress, appBuilder => Configuration(appBuilder, typeof(ByteParametersController))))
            {
                // Arrange
                var httpClient = HttpClientUtils.GetHttpClient(HttpClientUtils.BaseAddress);
                // Verify that the OData route in the test controller is valid
                var result = await httpClient.GetJsonAsync<ByteParameter>("/odata/ByteParameters(1)");
                result.Should().NotBeNull();

                // Act
                var swaggerDocument = await httpClient.GetJsonAsync<SwaggerDocument>("swagger/docs/v1");

                // Assert
                PathItem pathItem;
                swaggerDocument.paths.TryGetValue("/odata/ByteParameters({Id})", out pathItem);
                pathItem.Should().NotBeNull();
                pathItem.get.Should().NotBeNull();

                await ValidationUtils.ValidateSwaggerJson();
            }
        }

        private static void Configuration(IAppBuilder appBuilder, Type targetController)
        {
            var config = appBuilder.GetStandardHttpConfig(targetController);

            // Define a route to a controller class that contains functions
            config.MapODataServiceRoute("ODataRoute", "odata", GetEdmModel());

            config.EnsureInitialized();
        }

        private static IEdmModel GetEdmModel()
        {
            ODataModelBuilder builder = new ODataConventionModelBuilder();

            builder.EntitySet<ByteParameter>("ByteParameters");

            var testType = builder.EntityType<ByteParameter>();

            testType.Collection.Function("ResponseTest").Returns<byte>().Parameter<byte>("param");

            return builder.GetEdmModel();
        }
    }

    public class ByteParameter
    {
        [Key]
        public byte Id { get; set; }
    }

    public class ByteParametersController : ODataController
    {
        private static readonly ConcurrentDictionary<byte, ByteParameter> Data;

        static ByteParametersController()
        {
            Data = new ConcurrentDictionary<byte, ByteParameter>();

            Enumerable.Range(0, 5).Select(i => new ByteParameter
            {
                Id = (byte)i
            }).ToList().ForEach(p => Data.TryAdd(p.Id, p));
        }

        [EnableQuery]
        public IHttpActionResult Get()
        {
            return Ok(Data.Values.AsQueryable());
        }

        [HttpGet]
        [ResponseType(typeof(byte))]
        public IHttpActionResult ResponseTest(byte param)
        {
            return Ok(param);
        }
    }
}