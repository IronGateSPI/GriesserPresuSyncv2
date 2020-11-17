using System;
using System.Collections.Generic;
using System.Text;

namespace GriesserPresuSync.Models
{
    public class MiGriesserDataStore
    {
        private static readonly MiGriesserDataStore instance = new MiGriesserDataStore();

        public GriesserSyncSettings SyncSettings { get; set; }

        private MiGriesserDataStore()
        {

        }

        public static MiGriesserDataStore Instance => instance;
    }
}
