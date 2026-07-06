using NUnit.Framework;
using System;
using System.Runtime.Serialization;
using QTTabBarLib;

namespace QTTtabBarTests
{
    [TestFixture]
    public class SecurityTests
    {
        [Test]
        public void PreMergeToMergedDeserializationBinder_Exists_And_IsSerializationBinder()
        {
            // Verify the security Binder type exists and is a proper SerializationBinder
            Type binderType = typeof(PreMergeToMergedDeserializationBinder);
            Assert.IsNotNull(binderType, "PreMergeToMergedDeserializationBinder type should exist");
            Assert.IsTrue(typeof(SerializationBinder).IsAssignableFrom(binderType),
                "PreMergeToMergedDeserializationBinder should inherit from SerializationBinder");
        }

        [Test]
        public void ByteArrayToObject_Roundtrips_SerializeDelegate_Successfully()
        {
            // Verify that ByteArrayToObject still works after enabling the security Binder.
            // SerializeDelegate is the type used by ObjectToByteArray.
            Action testAction = () => { };
            SerializeDelegate original = new SerializeDelegate(testAction);
            byte[] bytes = QTUtility.ObjectToByteArray(original);
            Assert.IsNotNull(bytes, "Serialized bytes should not be null");
            Assert.Greater(bytes.Length, 0, "Serialized bytes should have content");

            object result = QTUtility.ByteArrayToObject(bytes);
            Assert.IsNotNull(result, "Deserialized object should not be null");
            Assert.IsInstanceOf<SerializeDelegate>(result, "Deserialized object should be a SerializeDelegate");
        }

        [Test]
        public void ByteArrayToObject_Handles_Null_Input()
        {
            // Verify null/empty input is handled gracefully
            Assert.IsNull(QTUtility.ByteArrayToObject(null));
            Assert.IsNull(QTUtility.ByteArrayToObject(new byte[0]));
        }
    }
}
