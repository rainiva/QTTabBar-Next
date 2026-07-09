using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch2W7Tests {
        [Test]
        public void IDLCache_Field_Is_ConcurrentDictionary() {
            FieldInfo field = typeof(IDLWrapper).GetField(
                "dicCacheIDLs",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(field);
            Type fieldType = field.FieldType;
            Assert.IsTrue(fieldType.IsGenericType);
            Assert.AreEqual(typeof(ConcurrentDictionary<,>), fieldType.GetGenericTypeDefinition());
        }

        [Test]
        public void IDLCache_ConcurrentReadWrite_NoCrash() {
            byte[] sampleIdl = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var tasks = new List<Task>();
            for(int i = 0; i < 100; i++) {
                string path = @"C:\TestIDLCache\" + i + "???";
                tasks.Add(Task.Run(() => IDLWrapper.AddCache(path, sampleIdl)));
            }
            for(int i = 0; i < 100; i++) {
                string path = @"C:\TestIDLCache\" + i + "???";
                tasks.Add(Task.Run(() => {
                    IDLWrapper idlw;
                    IDLWrapper.TryGetCache(path, out idlw);
                }));
            }
            Assert.DoesNotThrow(() => Task.WaitAll(tasks.ToArray()));
        }
    }
}
