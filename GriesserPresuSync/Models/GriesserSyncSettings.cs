using System;
namespace GriesserPresuSync.Models
{
    public class GriesserSyncSettings
    {
        public string ApiUrl { get; set; }
        public int details = 1;
        public int regs_per_page = 5;
        public int page = 1;
        public GriesserSyncSettings()
        {
        }
    }
}
