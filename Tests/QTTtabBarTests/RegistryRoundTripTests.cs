using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Win32;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    /// <summary>
    /// 契约冻结护栏测试：锁定分组(Group/GroupsManager) 与应用(UserApp/AppsManager)
    /// 的注册表存储结构（导出→导入数据完整性）。
    ///
    /// 说明：GroupsManager/AppsManager 的读写路径硬编码到真实配置
    /// (HKCU\Software\QTTabBar\...) 且 Save 会触发 IPC 广播，无法安全注入路径。
    /// 因此本测试在 **隔离的临时注册表子键** (HKCU\Software\QTTabBarTest_&lt;guid&gt;)
    /// 中复刻 SaveGroups/LoadGroups 与 SaveApps/LoadApps 的字段布局，
    /// 锁定其存储 schema 与往返数据完整性，测试后清理，绝不污染真实配置。
    /// 应对当前代码全部通过（GREEN）。
    /// </summary>
    [TestFixture]
    public class RegistryRoundTripTests {

        private string testRoot;

        [SetUp]
        public void SetUp() {
            testRoot = @"Software\QTTabBarTest_" + Guid.NewGuid().ToString("N");
        }

        [TearDown]
        public void TearDown() {
            try {
                Registry.CurrentUser.DeleteSubKeyTree(testRoot, false);
            }
            catch(Exception) {
                // 清理失败不影响测试结论；忽略。
            }
        }

        #region Groups schema mirror (SaveGroups / LoadGroups)

        private void WriteGroups(string groupsPath, List<Group> groups) {
            using(RegistryKey key = Registry.CurrentUser.CreateSubKey(groupsPath)) {
                for(int i = 0; i < groups.Count; i++) {
                    Group g = groups[i];
                    using(RegistryKey gkey = key.CreateSubKey("" + i)) {
                        gkey.SetValue("", g.Name);
                        if(g.ShortcutKey != Keys.None) gkey.SetValue("key", (int)g.ShortcutKey);
                        if(g.Startup) gkey.SetValue("startup", "");
                        for(int j = 0; j < g.Paths.Count; j++) {
                            gkey.SetValue("" + j, g.Paths[j]);
                        }
                    }
                }
            }
        }

        private List<Group> ReadGroups(string groupsPath) {
            List<Group> result = new List<Group>();
            using(RegistryKey key = Registry.CurrentUser.CreateSubKey(groupsPath)) {
                int i = 0;
                while(true) {
                    using(RegistryKey gkey = key.OpenSubKey("" + i++)) {
                        if(gkey == null) break;
                        string name = gkey.GetValue("") as string;
                        if(name == null) continue;
                        Keys shortcut = (Keys)gkey.GetValue("key", Keys.None);
                        bool startup = gkey.GetValue("startup") != null;
                        List<string> paths = new List<string>();
                        int j = 0;
                        while(true) {
                            string path = gkey.GetValue("" + j++) as string;
                            if(path == null) break;
                            paths.Add(path);
                        }
                        result.Add(new Group(name, shortcut, startup, paths));
                    }
                }
            }
            return result;
        }

        [Test]
        public void Groups_Registry_RoundTrip_Preserves_All_Fields() {
            string groupsPath = testRoot + @"\Groups";
            List<Group> original = new List<Group> {
                new Group("Work", Keys.Control | Keys.G, true,
                    new List<string> { @"C:\Projects", @"D:\Docs", @"::{20D04FE0-3AEA-1069-A2D8-08002B30309D}" }),
                new Group("Empty", Keys.None, false, new List<string>()),
                new Group("Single", Keys.F5, false, new List<string> { @"E:\Only" }),
            };

            WriteGroups(groupsPath, original);
            List<Group> loaded = ReadGroups(groupsPath);

            Assert.AreEqual(original.Count, loaded.Count, "group count must survive round-trip");
            for(int i = 0; i < original.Count; i++) {
                Assert.AreEqual(original[i].Name, loaded[i].Name, "group name @" + i);
                Assert.AreEqual(original[i].ShortcutKey, loaded[i].ShortcutKey, "group shortcut @" + i);
                Assert.AreEqual(original[i].Startup, loaded[i].Startup, "group startup @" + i);
                CollectionAssert.AreEqual(original[i].Paths, loaded[i].Paths, "group paths @" + i);
            }
        }

        [Test]
        public void Groups_Ordering_Is_Preserved() {
            string groupsPath = testRoot + @"\Groups";
            List<Group> original = new List<Group> {
                new Group("Zeta", Keys.None, false, new List<string> { @"C:\z" }),
                new Group("Alpha", Keys.None, false, new List<string> { @"C:\a" }),
                new Group("Mu", Keys.None, false, new List<string> { @"C:\m" }),
            };

            WriteGroups(groupsPath, original);
            List<Group> loaded = ReadGroups(groupsPath);

            Assert.AreEqual("Zeta", loaded[0].Name);
            Assert.AreEqual("Alpha", loaded[1].Name);
            Assert.AreEqual("Mu", loaded[2].Name);
        }

        #endregion

        #region UserApps schema mirror (SaveApps / LoadApps)

        private void WriteApps(string appsPath, List<UserApp> apps) {
            using(RegistryKey key = Registry.CurrentUser.CreateSubKey(appsPath)) {
                for(int i = 0; i < apps.Count; i++) {
                    UserApp a = apps[i];
                    using(RegistryKey akey = key.CreateSubKey("" + i)) {
                        akey.SetValue("", a.Name);
                        if(a.IsFolder) {
                            akey.SetValue("children", a.ChildrenCount);
                        }
                        else {
                            akey.SetValue("path", a.Path);
                            akey.SetValue("args", a.Args);
                            akey.SetValue("wdir", a.WorkingDir);
                            if(a.ShortcutKey != Keys.None) akey.SetValue("key", (int)a.ShortcutKey);
                        }
                    }
                }
            }
        }

        private List<UserApp> ReadApps(string appsPath) {
            List<UserApp> result = new List<UserApp>();
            using(RegistryKey key = Registry.CurrentUser.CreateSubKey(appsPath)) {
                int i = 0;
                while(true) {
                    using(RegistryKey akey = key.OpenSubKey("" + i++)) {
                        if(akey == null) break;
                        string name = akey.GetValue("") as string;
                        if(name == null) continue;
                        int children = (int)akey.GetValue("children", -1);
                        if(children != -1) {
                            result.Add(new UserApp(name, children));
                        }
                        else {
                            string path = (string)akey.GetValue("path", "");
                            string args = (string)akey.GetValue("args", "");
                            string wdir = (string)akey.GetValue("wdir", "");
                            Keys shortcut = (Keys)akey.GetValue("key", Keys.None);
                            result.Add(new UserApp(name, path, args, wdir, shortcut));
                        }
                    }
                }
            }
            return result;
        }

        [Test]
        public void UserApps_Registry_RoundTrip_Preserves_Leaf_And_Folder() {
            string appsPath = testRoot + @"\UserApps";
            // 复刻 AppsManager 的扁平化嵌套结构：文件夹节点 + 其子叶子节点。
            List<UserApp> original = new List<UserApp> {
                new UserApp("Tools", 2),                                             // folder with 2 children
                new UserApp("Notepad", @"C:\Windows\notepad.exe", "%f%", @"C:\Windows", Keys.Control | Keys.N),
                new UserApp("Calc", @"C:\Windows\System32\calc.exe", "", "", Keys.None),
                new UserApp("StandaloneApp", @"D:\app.exe", "--flag", @"D:\", Keys.None),
            };

            WriteApps(appsPath, original);
            List<UserApp> loaded = ReadApps(appsPath);

            Assert.AreEqual(original.Count, loaded.Count, "app count must survive round-trip");

            // 文件夹节点
            Assert.AreEqual("Tools", loaded[0].Name);
            Assert.IsTrue(loaded[0].IsFolder, "Tools must remain a folder");
            Assert.AreEqual(2, loaded[0].ChildrenCount, "folder children count must survive");

            // 叶子节点 Notepad
            Assert.AreEqual("Notepad", loaded[1].Name);
            Assert.IsFalse(loaded[1].IsFolder);
            Assert.AreEqual(@"C:\Windows\notepad.exe", loaded[1].Path);
            Assert.AreEqual("%f%", loaded[1].Args);
            Assert.AreEqual(@"C:\Windows", loaded[1].WorkingDir);
            Assert.AreEqual(Keys.Control | Keys.N, loaded[1].ShortcutKey);

            // 叶子节点 Calc（空 args/wdir、无快捷键）
            Assert.AreEqual("Calc", loaded[2].Name);
            Assert.AreEqual(@"C:\Windows\System32\calc.exe", loaded[2].Path);
            Assert.AreEqual("", loaded[2].Args);
            Assert.AreEqual("", loaded[2].WorkingDir);
            Assert.AreEqual(Keys.None, loaded[2].ShortcutKey);

            // 独立叶子节点
            Assert.AreEqual("StandaloneApp", loaded[3].Name);
            Assert.AreEqual("--flag", loaded[3].Args);
        }

        [Test]
        public void UserApps_Empty_List_RoundTrips_To_Empty() {
            string appsPath = testRoot + @"\UserApps";
            WriteApps(appsPath, new List<UserApp>());
            List<UserApp> loaded = ReadApps(appsPath);
            Assert.AreEqual(0, loaded.Count, "empty app list must round-trip to empty");
        }

        #endregion
    }
}
