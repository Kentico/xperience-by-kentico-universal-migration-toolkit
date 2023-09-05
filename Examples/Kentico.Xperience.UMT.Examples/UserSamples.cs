using Kentico.Xperience.UMT.Model;

namespace Kentico.Xperience.UMT;

public static class UserSamples
{
    [Sample("userinfo.sampleadmin", "Sample demonstrates how to create administrator user", "Instance of dataclass UserInfo - Sample admin")]
    public static UserInfoModel SampleAdministrator => new()
    {
        UserGUID = new Guid("DBFCC244-2CB9-4934-857F-9D75404C1553"),
        
        Email = $"admin@sample.localhost",
        FirstName = "Sample",
        LastName = "Admin",
        UserCreated = new DateTime(1990, 01, 01),
        UserEnabled = true,
        UserPassword = "[sample hash]",
        UserName = "sadmin",
        UserAdministrationAccess = true,
        UserIsPendingRegistration = false,
        UserIsExternal = false
    };
}
