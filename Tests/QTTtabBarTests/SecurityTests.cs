using NUnit.Framework;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
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

        // ---- Batch 1 (P0-1): deserialization whitelist hardening ----

        // A serializable, non-whitelisted type living OUTSIDE QTTabBarLib.
        // It records whether it was ever instantiated during deserialization,
        // acting as a stand-in for a dangerous gadget (System.Diagnostics.Process
        // itself is not [Serializable], so it cannot be placed into a payload).
        [Serializable]
        private class NonWhitelistedGadget : ISerializable
        {
            [ThreadStatic] public static bool Instantiated;
            public NonWhitelistedGadget() { }
            protected NonWhitelistedGadget(SerializationInfo info, StreamingContext context)
            {
                Instantiated = true;
            }
            public void GetObjectData(SerializationInfo info, StreamingContext context) { }
        }

        [Test]
        public void BindToType_ReturnsNull_ForDangerousNonWhitelistedType()
        {
            // Test 1: the binder must refuse to resolve a dangerous, non-whitelisted type.
            var binder = new PreMergeToMergedDeserializationBinder();
            Type resolved = binder.BindToType("System", "System.Diagnostics.Process");
            Assert.IsNull(resolved,
                "Non-whitelisted dangerous type must not be resolved by the binder.");
        }

        [Test]
        public void ByteArrayToObject_Rejects_NonWhitelisted_DangerousPayload()
        {
            // Test 2: a payload carrying a non-whitelisted type must be rejected:
            // ByteArrayToObject returns null and the dangerous object is never instantiated.
            NonWhitelistedGadget.Instantiated = false;
            byte[] payload;
            using (var ms = new MemoryStream())
            {
                new BinaryFormatter().Serialize(ms, new NonWhitelistedGadget());
                payload = ms.ToArray();
            }

            object result = QTUtility.ByteArrayToObject(payload);

            Assert.IsNull(result,
                "Deserializing a non-whitelisted type must return null (graceful degradation).");
            Assert.IsFalse(NonWhitelistedGadget.Instantiated,
                "Non-whitelisted dangerous type must NOT be instantiated during deserialization.");
        }
    }
}
