﻿namespace BidService.Settings
{
    public class BidDatabaseSettings : IBidDatabaseSettings
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        public string CollectionName { get; set; }
    }
}
