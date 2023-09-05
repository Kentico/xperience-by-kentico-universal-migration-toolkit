using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.UMT;
using Kentico.Xperience.UMT.UIPages.CustomTemplate;

/*
 * This file demonstrates a UI page based on a custom template (CustomLayoutTemplate.tsx).
 * The page defines a single page command that returns the server's DateTime.Now value.
 */

// Defines a new application and registers its root page under <webappdomain>/admin/CustomTemplate
[assembly: UIApplication("UMT.index", typeof(UniversalMigrationTemplate), "universal-migration-toolkit", "Universal migration toolkit", UmtWebAdminModule.CustomCategory, Icons.Clock, "@umt/web-admin/CustomLayout")]

namespace Kentico.Xperience.UMT.UIPages.CustomTemplate
{
    internal class UniversalMigrationTemplate : Page<CustomLayoutProperties>
    {
        public override Task<CustomLayoutProperties> ConfigureTemplateProperties(CustomLayoutProperties properties)
        {
            properties.Label = "Migrate data from anywhere";
            return Task.FromResult(properties);
        }
    }

    internal class CustomLayoutProperties : TemplateClientProperties
    {
        public string Label { get; set; } = null!;
    }
}
