using System.ComponentModel;

namespace Kraken
{
    internal enum enmMethod
    {
        upload,
        url,
    }

    public enum EnmIsKrakable
    {
        MissingCredentials = 1,
        Unkrakable = 2,
        Krakable = 3,
        Kraked = 4,
        Original = 5,
    }

    internal enum EnmKrakStatus
    {
        None,
        Compressed,
        Original,
    }

    public enum enmStatus
    {
        [Description("Success")]
        Ok = 200,
        [Description("Incoming request body does not contain a valid JSON object")]
        BadRequest = 400,
        [Description("Unnknown API Key. Please check your API key and try again")]
        Unauthorized = 401,
        [Description("Your account has been temporarily suspended")]
        Forbidden = 403,
        [Description("File size too large")]
        FileTooLarge = 413,
        [Description("File type not supported")]
        UnsupportedMediaType = 415,
        [Description("You need to specify either callback_url or wait flag")]
        UnprocessableEntity = 422,
        [Description("Request limit reached")]
        RequestLimitReached = 429,
        [Description("Kraken has encountered an unexpected error and cannot fulfill your request")]
        UnexpectedError = 500,
    }

    internal static class Constants
    {
        public const string UmbracoMediaTabnameKraken = "Image optimizer";
        public const string UmbracoPackageName = "Official Kraken Image Optimizer plugin for Umbraco";
        public const string KrakenApiEndpoint = "https://api.kraken.io/v1/"; // upload is de API method
        public const string ConfigFile = "/config/Kraken.config"; // De config waar de instellingen staan voor PerplexKraken

        // STANDAARD UMBRACO PROEPRTIES
        public const string UmbracoPropertyAliasFile = "umbracoFile"; // Standaard Umbraco property
        public const string UmbracoPropertyAliasSize = "umbracoBytes"; // Standaard Umbraco property
        public const string UmbracoPropertyAliasWidth = "umbracoWidth"; // Standaard Umbraco property
        public const string UmbracoPropertyAliasHeight = "umbracoHeight"; // Standaard Umbraco property
        public const string UmbracoPropertyAliasExtension = "umbracoExtension"; // Standaard Umbraco property
        // STATUS
        public const string UmbracoPropertyTextStatus = "Status"; // Nieuwe PerplexKraken status property
        public const string UmbracoPropertyAliasStatus = "status"; // Nieuwe PerplexKraken status property
        // ORIGINAL SIZE
        public const string UmbracoPropertyTextOriginalSize = "Original size"; // Nieuwe PerplexKraken filesize property
        public const string UmbracoPropertyAliasOriginalSize = "originalSize"; // Nieuwe PerplexKraken filesize property
        // COMPRESSION DATE
        public const string UmbracoPropertyAliasCompressionDate = "krakDate"; // Nieuwe PerplexKraken filesize property
        public const string UmbracoPropertyTextCompressionDate = "Compression date"; // Nieuwe PerplexKraken status property
        // STATUS
        public const string UmbracoPropertyAliasSaved = "saved"; // Nieuwe PerplexKraken filesize property
        public const string UmbracoPropertyTextSaved = "Percent saved"; // Nieuwe PerplexKraken filesize property
        // GUIDS
        public const string UmbracoDatatypeGuidStatus = "c62255d7-9a61-4733-9cb1-b6db77d857a5"; // Dit is een random GUID. Niet te diep over nadenken. Dient ook voor te komen in de package.manifest in de status property editor voor umbraco 7
        public const string UmbracoDatatypeGuidStatus_7 = "294f71db-0aa7-48fb-bc7c-7f1c40f7bb7b"; // Umbraco 7 variant
        public const string UmbracoDataTypeGuidImageCropper = "556d6272-6163-6f2e-496d-61676543726f"; // Umbraco 7 Image Editor GUID (Standaard)
        public const string UmbracoDataTypeGuidImageCropper_new = "7a2d436c-34c2-410f-898f-4a23b3d79f54"; // Alternatieve image cropper GUID (?)
        
        public const string UmbracoPropertyEditorGuidStatus = "f0b6a952-e8a2-4974-b345-4954456f1d4c"; // Umbraco 7 Property Editor GUID voor de status ==> Zie manifest bestand
        
        public const string UmbracoDatatypeDefinitionGuidUsercontrolGrapper = "d15e1281-e456-4b24-aa86-1dda3e4299d5"; // Dit is de standaard Umbraco user control grapper data type definition GUID. Deze gebruiken we in alle pre-V7 umbraco installaties
        
        public const string UmbracoDatatypeTextStatus = "Status"; // TODO: Deze is eigenlijk overbodig met de GUID
        public const string UmbracoDatatypeTextLabe = "Label"; // TODO: Deze is eigenlijk overbodig met de GUID
        public const string DefaultUmbracoMediaType = "Image"; // Default media type die gebruikt wordt om PerplexKraken toe te voegen
    }
}
