using System.Collections.Generic;
using beyondnations.web;
using Xunit;

namespace beyondnationstests.web {

    public class TestRulerWebOptions {

        [Fact]
        public void theSuccessionStageAndTheLawsTabCanBeAskedFor() {
            List<string> args = WebOptions.toArguments("?stage-succession&start-screen=laws", w => { });
            Assert.Equal(new[] { "--stage-succession", "--start-screen", "laws" }, args);
        }
    }
}
