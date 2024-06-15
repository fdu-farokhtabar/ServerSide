namespace Application.SeedWork
{
    public interface IApplicationSettings
    {
        public string WwwRootPath { get; }
        public string CatalogEmailSetting { get; set; }
        public string ContactUsEmailSetting { get; set; }
        public string OrderEmailSetting { get; set; }
        public string SigningKey { get; set; }
        public int UserAuthorizationTokenExpireTimeInMin { get; set; }
        public string ImportPath { get; set; }
        //public string PoDataPath { get; set; }
        public int StartIndexOfImageForUsingInCatalog { get; set; }
    }
}
