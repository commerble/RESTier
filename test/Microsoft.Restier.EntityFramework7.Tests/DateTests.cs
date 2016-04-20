﻿using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Microsoft.Restier.Core;
using Microsoft.Restier.EntityFramework.Tests.Models.Primitives;
using Microsoft.Restier.Tests;
using Microsoft.Restier.WebApi.Batch;
using Microsoft.Restier.WebApi.Routing;
using Newtonsoft.Json;
using System;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Xunit;

namespace Microsoft.Restier.EntityFramework.Tests
{
    public class DateTests
    {
        [Fact]
        public async Task VerifyMetadataPropertyType()
        {
            var api = new PrimitivesApi();
            var edmModel = await api.GetModelAsync();

            var entityType = (IEdmEntityType)
                edmModel.FindDeclaredType(@"Microsoft.Restier.EntityFramework.Tests.Models.Primitives.DateItem");
            Assert.NotNull(entityType);

            Assert.True(entityType.FindProperty("DTProperty").Type.IsDateTimeOffset());
            Assert.True(entityType.FindProperty("DateProperty").Type.IsDate());
            Assert.True(entityType.FindProperty("TODProperty").Type.IsTimeOfDay());
            Assert.True(entityType.FindProperty("TSProperty").Type.IsDuration());
        }

        [Fact]
        public async Task VerifyQuery()
        {
            await PopulateData(1);

            using (var response = await ODataTestHelpers.GetResponse(@"http://local/api/Prim/Dates(1)", HttpMethod.Get, null, RegisterApi))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var s = await response.Content.ReadAsStringAsync();
                Assert.NotNull(s);
            }
        }

        [Fact]
        public async Task VerifyChange()
        {
            await PopulateData(42);

            {
                dynamic newObj = new ExpandoObject();
                newObj.DateProperty = "2016-01-04";
                newObj.DTProperty = DateTime.UtcNow;
                newObj.DTOProperty = DateTimeOffset.Now;
                newObj.TODProperty = "08:09:10";
                newObj.TSProperty = "PT4H12M";

                string newOrderContent = JsonConvert.SerializeObject(newObj);
                StringContent content = new StringContent(newOrderContent, UTF8Encoding.Default, "application/json");
                using (var response = await ODataTestHelpers.GetResponse(@"http://local/api/Prim/Dates(42)", HttpMethod.Put, content, RegisterApi))
                {
                    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
                    using (var ctx = new PrimitivesContext())
                    {
                        var item42 = ctx.Dates.First(e => e.RowId == 42);

                        Assert.Equal(new DateTime(2016, 1, 4), item42.DateProperty);
                        Assert.Equal(new TimeSpan(8, 9, 10), item42.TODProperty);
                        Assert.Equal(new TimeSpan(4, 12, 0), item42.TSProperty);
                    }
                }
            }

            {
                dynamic newObj = new ExpandoObject();
                newObj.DateProperty = "2016-01-04";
                newObj.DTProperty = DateTime.UtcNow;
                newObj.DTOProperty = DateTimeOffset.Now;
                newObj.RowId = 1024;
                newObj.TODProperty = "08:09:10";
                newObj.TSProperty = "PT4H12M";

                string newOrderContent = JsonConvert.SerializeObject(newObj);
                StringContent content = new StringContent(newOrderContent, UTF8Encoding.Default, "application/json");
                using (var response = await ODataTestHelpers.GetResponse(@"http://local/api/Prim/Dates", HttpMethod.Post, content, RegisterApi))
                {
                    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                    var ret = await response.Content.ReadAsAsync<DateItem>();
                    Assert.Equal(new DateTime(2016, 1, 4), ret.DateProperty);
                    Assert.Equal(new TimeSpan(8, 9, 10), ret.TODProperty);
                    Assert.Equal(new TimeSpan(4, 12, 0), ret.TSProperty);

                    using (var ctx = new PrimitivesContext())
                    {
                        ret = ctx.Dates.First(e => e.RowId == 1024);

                        Assert.Equal(new DateTime(2016, 1, 4), ret.DateProperty);
                        Assert.Equal(new TimeSpan(8, 9, 10), ret.TODProperty);
                        Assert.Equal(new TimeSpan(4, 12, 0), ret.TSProperty);
                    }
                }
            }

            {
                dynamic newObj = new ExpandoObject();
                newObj.DateProperty = "2017-01-04";
                newObj.TODProperty = "10:09:08";
                newObj.TSProperty = "PT4H32M";

                string newOrderContent = JsonConvert.SerializeObject(newObj);
                StringContent content = new StringContent(newOrderContent, UTF8Encoding.Default, "application/json");
                using (var response = await ODataTestHelpers.GetResponse(@"http://local/api/Prim/Dates(1024)", new HttpMethod("Patch"), content, RegisterApi))
                {
                    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

                    using (var ctx = new PrimitivesContext())
                    {
                        var ret = ctx.Dates.First(e => e.RowId == 1024);

                        Assert.NotNull(ret.DTProperty);
                        Assert.NotEqual(default(DateTimeOffset), ret.DTOProperty);

                        Assert.Equal(new DateTime(2017, 1, 4), ret.DateProperty);
                        Assert.Equal(new TimeSpan(10, 9, 8), ret.TODProperty);
                        Assert.Equal(new TimeSpan(4, 32, 0), ret.TSProperty);
                    }
                }
            }
        }

        private static async Task PopulateData(int rowId)
        {
            using (var ctx = new PrimitivesContext())
            {
                ctx.Add(new DateItem()
                {
                    DateProperty = DateTime.Now,
                    DTProperty = DateTime.UtcNow,
                    DTOProperty = DateTimeOffset.Now,
                    RowId = rowId,
                    TODProperty = TimeOfDay.Now,
                    TSProperty = TimeSpan.FromDays(.3),
                });

                await ctx.SaveChangesAsync();
            }
        }

        private static async void RegisterApi(HttpConfiguration config, HttpServer server)
        {
            await config.MapRestierRoute<PrimitivesApi>(
                "PrimitivesApi", "api/Prim",
                new RestierBatchHandler(server));
        }
    }
}
