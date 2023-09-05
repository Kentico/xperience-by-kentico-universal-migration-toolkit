using Kentico.Xperience.UMT.Model;

namespace Kentico.Xperience.UMT;

public static class TreeNodeSamples
{
    public static Guid SiteRootNodeGuid => new Guid("ACDD2058-BDE0-4C9D-8332-45F417220571");
    
    #region Event samples

    [Sample("treenode.yearlyevent", "", "Instance of dataclass UMT.Event")]
    public static TreeNodeModel YearlyEvent => new()
    {
        NodeGUID = new Guid("E2B53C6C-020D-45D3-ACB7-C2651BF7171A"),
        NodeParentGuid = SiteRootNodeGuid,
        NodeClassGuid = DataClassSamples.EventDataClass.ClassGuid,
        NodeOwnerGuid = UserSamples.SampleAdministrator.UserGUID,
        DocumentCreatedByUserGuid = UserSamples.SampleAdministrator.UserGUID,
        DocumentCulture = "en-US",
        DocumentGUID = new Guid("34B4DF1B-6261-4546-9382-941809E1936F"),
        //DocumentName = $"Yearly barbeque for customers",
        NodeAlias = "yearly-barbecue-for-customers",
        NodeName = $"Yearly barbeque for customers",
        CustomProperties = new Dictionary<string, object?>
        {
            { "EventTitle", $"Yearly barbeque for customers" },
            { "EventText", $"<h1>Barbecue is here!</h1><p>let us invite You to our yearly friendly meeting with You - our customers</p>" },
            { "EventDate", $"2023-06-01T11:00:00" },
            { "EventRecurrentYearly", true }
        }
    };
    
    [Sample("treenode.singleevent", "", "Instance of dataclass UMT.Event")]
    public static TreeNodeModel SingleOccurenceEvent => new()
    {
        NodeGUID = new Guid("50CF01D4-1C39-4910-82CA-298C9F7A4840"),
        NodeParentGuid = SiteRootNodeGuid,
        NodeClassGuid = DataClassSamples.EventDataClass.ClassGuid,
        NodeOwnerGuid = UserSamples.SampleAdministrator.UserGUID,
        DocumentGUID = new Guid("15F0EFD6-6961-4640-AF6D-9C06B3A0A24D"),
        DocumentCreatedByUserGuid = UserSamples.SampleAdministrator.UserGUID,
        DocumentCulture = "en-US",
        DocumentName = $"Announcement of economic results",
        NodeAlias = "announcement-of-economic-results",
        NodeName = $"Announcement of economic results",
        CustomProperties = new Dictionary<string, object?>
        {
            { "EventTitle", $"Announcement of economic results" },
            { "EventText", $"<h1>Announcement of economic results</h1>" },
            { "EventDate", $"2023-07-01T11:00:00" },
            { "EventRecurrentYearly", false }
        }
    };

    #endregion
}
