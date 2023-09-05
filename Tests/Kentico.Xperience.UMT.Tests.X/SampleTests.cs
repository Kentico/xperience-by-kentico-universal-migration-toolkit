using CMS.DataEngine;
using CMS.Membership;
using FluentAssertions;
using Kentico.Xperience.UMT.Model;
using Kentico.Xperience.UMT.ProviderProxy;
using Kentico.Xperience.UMT.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kentico.Xperience.UMT;

public class SampleTests
{
    [Fact(Skip = "This test needs to be run using https://docs.xperience.io/custom-development/writing-automated-tests")]
    public async Task SampleTest()
    {
        var sp = KenticoFixture.FakeDiContainer(new ProviderProxyContext("boilerplate", "en-US")).BuildServiceProvider();
        var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger<SampleTests>();
        var importService = sp.GetRequiredService<IImportService>();

        var importObserver = new ImportStateObserver();
        var results = new List<BaseInfo>();
        importObserver.ImportedInfo += info =>
        {
            results.Add(info);
        };
        importObserver.Exception += (model, id, exception) =>
        {
            log.LogError("error occured {Exception}", exception);
        };
        importObserver.ValidationError += (model, id, validationResults) =>
        {
            foreach (var validationResult in validationResults)
            {
                log.LogError("validation error {ErrorMessage}", validationResult.ErrorMessage);
            }
        };

        await importService.StartImportAsync(new UmtModel[]
        {
            UserSamples.SampleAdministrator,
            DataClassSamples.ArticleClassSample,
            DataClassSamples.EventDataClass,
            TreeNodeSamples.YearlyEvent,
            TreeNodeSamples.SingleOccurenceEvent
        }.AsAsyncEnumerable(), new ImporterContext("boilerplate", "en-US"), importObserver);

        await importObserver.ImportCompletedTask;


        results.Should().SatisfyRespectively(first =>
        {
            if (first.Should().BeOfType<UserInfo>().Subject is { } user)
            {
                user.Should().BeEquivalentTo(UserSamples.SampleAdministrator, options => options
                    .Including(ui => ui.Email)
                    .Including(ui => ui.FirstName)
                    .Including(ui => ui.LastName)
                    .Including(ui => ui.UserCreated)
                    .Including(ui => ui.UserEnabled)
                    .Including(ui => ui.UserPassword)
                    .Including(ui => ui.UserName)
                    .Including(ui => ui.UserAdministrationAccess)
                    .Including(ui => ui.UserIsPendingRegistration)
                    .Including(ui => ui.UserIsExternal)
                    .Including(ui => ui.UserGUID)
                );
            }
        });
    }
}
