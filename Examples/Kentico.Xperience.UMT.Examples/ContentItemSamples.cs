﻿using CMS.ContentEngine;
using Kentico.Xperience.UMT.Model;

namespace Kentico.Xperience.UMT.Examples;

public static class ContentItemSamples
{
    public static readonly Guid CONTENT_ITEM_GUID = new Guid("C354427D-3D02-4876-8ED4-4DE817FAE929");
    public static readonly Guid CONTENT_ITEM_FAQ_SAMPLE_GUID = new Guid("B28A0F09-9102-4E48-B6FE-3C405FEEAFB5");

    [Sample("contentitem.sample", "This sample describes how to create class inside XbyK to hold Content Item data", "ContentItem basic Sample")]
    public static ContentItemModel SampleContentItem => new()
    {
        ContentItemGUID = CONTENT_ITEM_GUID,
        ContentItemChannelGuid = new Guid("B186B5A3-F408-4E21-A2F9-E51D68ECAC38"),
        ContentItemDataClassGuid = new Guid("978B2CD4-C248-4317-86A1-3BDD17444267"),
        ContentItemIsSecured = true,
        ContentItemIsReusable = true,
        ContentItemName = "NewsLetterExampleName",
    };


    #region "Sample article"

    public static readonly Guid SampleArticleContentItemGuid = new Guid("DF81215E-1414-4D87-BEFD-AE123F4E5653");
    public static readonly Guid SampleArticleCommonDataGuidEnUs = new Guid("8F070195-2F39-463E-B7EB-C180C05FD5E0");
    public static readonly Guid SampleArticleCommonDataGuidEnGb = new Guid("49D2CAF6-2011-42D7-961D-02614D1B43F4");

    [Sample("ContentItemModel.Sample.Article", "This sample describes how to create content item data inside XbyK", "ContentItem basic Sample")]
    public static ContentItemModel SampleArticleContentItem => new()
    {
        ContentItemGUID = SampleArticleContentItemGuid,
        ContentItemChannelGuid = ChannelSamples.WEBSITE_CHANNEL_SAMPLE_GUID,
        ContentItemDataClassGuid = DataClassSamples.ARTICLE_SAMPLE_GUID,
        ContentItemIsSecured = true,
        ContentItemIsReusable = true,
        ContentItemName = "CreationOfUmtModel"
    };

    #region "EnUs version"

    [Sample("ContentItemCommonDataModel.Sample.Article.enUS", "This sample describes how to create content item common data inside XbyK", "ContentItemCommonData basic Sample")]
    public static ContentItemCommonDataModel SampleArticleContentItemCommonDataEnUs => new()
    {
        ContentItemCommonDataGUID = SampleArticleCommonDataGuidEnUs,
        ContentItemCommonDataContentItemGuid = SampleArticleContentItemGuid,
        ContentItemCommonDataContentLanguageGuid = ContentLanguageSamples.CONTENT_LANGUAGE_ENUS_SAMPLE_GUID,
        ContentItemCommonDataVersionStatus = VersionStatus.Published,
        ContentItemCommonDataIsLatest = true,
        ContentItemCommonDataPageBuilderWidgets = null,
        ContentItemCommonDataPageTemplateConfiguration = null
    };

    [Sample("ContentItemDataModel.Sample.Article.enUS", "This sample describes how to create content item data inside XbyK", "ContentItemData article sample (en-US)")]
    public static ContentItemDataModel SampleArticleDataEnUs => new()
    {
        ContentItemDataGUID = new Guid("9A5B10E0-D0E6-4DE9-9D82-6D8DEEEA1849"),
        ContentItemDataCommonDataGuid = SampleArticleCommonDataGuidEnUs,
        ContentItemContentTypeName = DataClassSamples.ARTICLE_SAMPLE_CLASS_NAME,
        CustomProperties = new Dictionary<string, object?> { ["ArticleTitle"] = "en-US UMT model creation", ["ArticleText"] = "This article is only example of creation UMT model for en-US language", }
    };


    [Sample("contentitemlanguagemetadata.sample.article.enus", "This sample describes how to create class inside XbyK to hold Content Item Language Metadata", "ContentItemLanguageMetadata Sample")]
    public static ContentItemLanguageMetadataModel SampleArticleContentItemLanguageMetadataEnUs => new()
    {
        ContentItemLanguageMetadataGUID = new Guid("192C63AC-E5BE-4B0F-B916-B8AF6C7E79A9"),
        ContentItemLanguageMetadataContentItemGuid = SampleArticleContentItemGuid,
        ContentItemLanguageMetadataContentLanguageGuid = ContentLanguageSamples.CONTENT_LANGUAGE_ENUS_SAMPLE_GUID,
        ContentItemLanguageMetadataDisplayName = "Creation of UMT model",
        ContentItemLanguageMetadataCreatedWhen = new DateTime(2023, 12, 10),
        ContentItemLanguageMetadataHasImageAsset = false,
        ContentItemLanguageMetadataCreatedByUserGuid = UserSamples.SampleAdminGuid,
        ContentItemLanguageMetadataModifiedByUserGuid = UserSamples.SampleAdminGuid,
        ContentItemLanguageMetadataLatestVersionStatus = VersionStatus.Published,
    };

    #endregion


    #region "EnGb version"

    [Sample("ContentItemCommonDataModel.Sample.Article.enGB", "This sample describes how to create content item common data inside XbyK", "ContentItemCommonData basic Sample")]
    public static ContentItemCommonDataModel SampleArticleContentItemCommonDataEnGb => new()
    {
        ContentItemCommonDataGUID = SampleArticleCommonDataGuidEnGb,
        ContentItemCommonDataContentItemGuid = SampleArticleContentItemGuid,
        ContentItemCommonDataContentLanguageGuid = ContentLanguageSamples.CONTENT_LANGUAGE_ENGB_SAMPLE_GUID,
        ContentItemCommonDataVersionStatus = VersionStatus.Published,
        ContentItemCommonDataIsLatest = true,
        ContentItemCommonDataPageBuilderWidgets = null,
        ContentItemCommonDataPageTemplateConfiguration = null
    };

    [Sample("ContentItemDataModel.Sample.Article.enGB", "This sample describes how to create content item data inside XbyK", "ContentItemData article sample (en-GB)")]
    public static ContentItemDataModel SampleArticleDataEnGb => new()
    {
        ContentItemDataGUID = new Guid("21380F91-279B-44BE-AAD8-2E62C345A0E9"),
        ContentItemDataCommonDataGuid = SampleArticleCommonDataGuidEnGb,
        ContentItemContentTypeName = DataClassSamples.ARTICLE_SAMPLE_CLASS_NAME,
        CustomProperties = new Dictionary<string, object?>
        {
            ["ArticleTitle"] = "en-GB UMT model creation", 
            ["ArticleText"] = "This article is only example of creation UMT model for en-GB language",
        }
    };

    [Sample("contentitemlanguagemetadata.sample.article.engb", "This sample describes how to create class inside XbyK to hold Content Item Language Metadata", "ContentItemLanguageMetadata Sample")]
    public static ContentItemLanguageMetadataModel SampleArticleContentItemLanguageMetadataEnGb => new()
    {
        ContentItemLanguageMetadataGUID = new Guid("7F6A0C0D-A2BB-454C-8E16-ADCFE0E38D17"),
        ContentItemLanguageMetadataContentItemGuid = SampleArticleContentItemGuid,
        ContentItemLanguageMetadataContentLanguageGuid = ContentLanguageSamples.CONTENT_LANGUAGE_ENGB_SAMPLE_GUID,
        ContentItemLanguageMetadataDisplayName = "Creation of UMT model GB",
        ContentItemLanguageMetadataCreatedWhen = new DateTime(2023, 12, 10),
        ContentItemLanguageMetadataHasImageAsset = false,
        ContentItemLanguageMetadataCreatedByUserGuid = UserSamples.SampleAdminGuid,
        ContentItemLanguageMetadataModifiedByUserGuid = UserSamples.SampleAdminGuid,
        ContentItemLanguageMetadataLatestVersionStatus = VersionStatus.Published,
    };

    #endregion

    [Sample("webpageitem.sample.article", "This sample describes how to create class inside XbyK to hold WebPage Item data", "ContentItem Sample")]
    public static WebPageItemModel SampleArticleWebPageItem => new()
    {
        WebPageItemGUID = new Guid("6E995319-77E7-475E-9EBB-607BDBF5AF9A"),
        WebPageItemContentItemGuid = SampleArticleContentItemGuid,
        WebPageItemName = "CreationOfUmtModelUs",
        WebPageItemOrder = 1,
        WebPageItemTreePath = "/creation-of-umt-model",
        WebPageItemWebsiteChannelGuid = WebSiteChannelSamples.WebsiteChannelGuid
    };

    #endregion
}
