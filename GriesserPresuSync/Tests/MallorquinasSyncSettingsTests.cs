using GriesserPresuSync.Models;
using Xunit;

namespace GriesserPresuSync.Tests
{
    public class MallorquinasSyncSettingsTests
    {
        [Fact]
        public void DefaultsAreSane()
        {
            var s = new MallorquinasSyncSettings();
            Assert.Equal("https://www.migriesser.com/es/mallorquinas_budgets/api", s.ApiUrl);
            Assert.Equal(1, s.details);
            Assert.Equal(100, s.regs_per_page);
            Assert.Equal(1, s.page);
            Assert.Equal(1, s.id_from);
            Assert.Equal("IG_GriesserSyncMallorquinas", s.DestinationTable);
        }

        [Fact]
        public void IdFromCanBeOverridden()
        {
            var s = new MallorquinasSyncSettings { id_from = 4321 };
            Assert.Equal(4321, s.id_from);
        }
    }
}
