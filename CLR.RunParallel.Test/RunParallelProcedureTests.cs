using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Imato.CLR.ParallelRunner.Test
{
    [TestClass]
    public class RunParallelProcedureTests : RunParallelProcedure
    {
        [TestMethod]
        public void TestParseXmlParameter()
        {
            var xml = @"<Root>
  <Procedure>
    <Id>1</Id>
    <SqlText>waitfor delay '00:00:02';</SqlText>
  </Procedure>
  <Procedure>
    <Id>2</Id>
    <SqlText>waitfor delay '00:00:02';</SqlText>
  </Procedure>
  <Procedure>
    <Id>3</Id>
    <SqlText>waitfor delay '00:00:02'; select 1/0;</SqlText>
  </Procedure>
</Root>";

            var resutl = ParseXmlParameter(xml);
            Assert.AreEqual(3, resutl.Count);
            Assert.AreEqual(2, resutl[1].Id);
            Assert.AreEqual("waitfor delay '00:00:02';", resutl[1].Text);
        }
    }
}