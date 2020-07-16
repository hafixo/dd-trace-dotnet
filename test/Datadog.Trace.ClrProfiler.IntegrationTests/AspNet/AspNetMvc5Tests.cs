#if NET461

using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    [Collection("IisTests")]
    public class AspNetMvc5Tests : TestHelper, IClassFixture<IisFixture>
    {
        private readonly IisFixture _iisFixture;

        public AspNetMvc5Tests(IisFixture iisFixture, ITestOutputHelper output)
            : base("AspNetMvc5", "samples-aspnet", output)
        {
            SetServiceVersion("1.0.0");

            _iisFixture = iisFixture;
            _iisFixture.TryStartIis(this);
        }

        [Theory]
        [Trait("Category", "EndToEnd")]
        [Trait("Integration", nameof(Integrations.AspNetMvcIntegration))]
        [InlineData("/Home/Index", "GET", "/home/index", HttpStatusCode.OK)]
        [InlineData("/delay/0", "GET", "/delay/{seconds}", HttpStatusCode.OK)]
        [InlineData("/delay-async/0", "GET", "/delay-async/{seconds}", HttpStatusCode.OK)]
        [InlineData("/badrequest", "GET", "/badrequest", HttpStatusCode.InternalServerError)]
        [InlineData("/statuscode/201", "GET", "/statuscode/{value}", HttpStatusCode.Created)]
        public async Task SubmitsTraces(
            string path,
            string expectedVerb,
            string expectedResourceSuffix,
            HttpStatusCode expectedStatusCode)
        {
            await AssertWebServerSpan(
                path,
                _iisFixture.Agent,
                _iisFixture.HttpPort,
                expectedStatusCode,
                "web",
                "aspnet-mvc.request",
                $"{expectedVerb} {expectedResourceSuffix}",
                "1.0.0");
        }
    }
}

#endif
