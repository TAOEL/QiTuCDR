using Microsoft.VisualStudio.TestTools.UnitTesting;
using QiTuCDR.Bridge.Serialization;

namespace QiTuCDR.Tests
{
    [TestClass]
    public sealed class SerializationTests
    {
        [TestMethod]
        public void DeserializeRequestReadsStandardEnvelope()
        {
            var serializer = new BridgeJsonSerializer();
            var request = serializer.DeserializeRequest("{\"version\":\"1.0\",\"requestId\":\"abc\",\"action\":\"echo\",\"payload\":{\"value\":42}}");

            Assert.AreEqual("1.0", request.Version);
            Assert.AreEqual("abc", request.RequestId);
            Assert.AreEqual("echo", request.Action);
            Assert.AreEqual(42, request.Payload.Value<int>("value"));
        }

        [TestMethod]
        public void TryDeserializeRequestReturnsFalseForBrokenJson()
        {
            var serializer = new BridgeJsonSerializer();

            var success = serializer.TryDeserializeRequest("{ broken json", out var request, out var errorMessage);

            Assert.IsFalse(success);
            Assert.IsNull(request);
            Assert.IsFalse(string.IsNullOrWhiteSpace(errorMessage));
        }

        [TestMethod]
        public void DeserializeRequestNormalizesMissingPayload()
        {
            var serializer = new BridgeJsonSerializer();

            var request = serializer.DeserializeRequest("{\"version\":\"1.0\",\"requestId\":\"abc\",\"action\":\"echo\"}");

            Assert.IsNotNull(request.Payload);
            Assert.AreEqual(0, request.Payload.Count);
        }
    }
}
