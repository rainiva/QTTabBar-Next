using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    /// <summary>
    /// Characterization tests for Task 2.2 SessionState container extraction.
    /// Verifies that the session-state true source (ITEMIDLIST_Dic_Session,
    /// NoCapturePathsList, WindowAlpha) has been moved into an internal static
    /// QTTabBarLib.SessionState container, while QTUtility keeps equivalent
    /// facade members so existing consumers keep compiling and behaving
    /// (same collection instances, same single syncRoot lock).
    /// </summary>
    [TestFixture]
    public class SessionStateExtractionTests {

        private const BindingFlags AnyStatic =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        private static Type SessionStateType {
            get { return typeof(QTUtility).Assembly.GetType("QTTabBarLib.SessionState"); }
        }

        private static bool HasStaticMember(Type type, string name) {
            if(type == null) return false;
            return type.GetField(name, AnyStatic) != null
                || type.GetProperty(name, AnyStatic) != null;
        }

        private static object GetStaticMemberValue(Type type, string name) {
            FieldInfo f = type.GetField(name, AnyStatic);
            if(f != null) return f.GetValue(null);
            PropertyInfo p = type.GetProperty(name, AnyStatic);
            if(p != null) return p.GetValue(null);
            return null;
        }

        #region SessionState container exists with migrated members

        [Test]
        public void SessionState_Type_Exists() {
            Assert.IsNotNull(SessionStateType, "QTTabBarLib.SessionState type should exist");
        }

        [Test]
        public void SessionState_Is_Internal_Static_Class() {
            Type type = SessionStateType;
            Assert.IsNotNull(type, "SessionState type should exist");
            Assert.IsTrue(type.IsAbstract && type.IsSealed,
                "SessionState should be a static class (abstract + sealed)");
            Assert.IsTrue(type.IsNotPublic, "SessionState should be internal (not public)");
        }

        [Test]
        public void SessionState_Owns_ITEMIDLIST_Dic_Session() {
            Assert.IsTrue(HasStaticMember(SessionStateType, "ITEMIDLIST_Dic_Session"),
                "SessionState should own ITEMIDLIST_Dic_Session");
        }

        [Test]
        public void SessionState_Owns_NoCapturePathsList() {
            Assert.IsTrue(HasStaticMember(SessionStateType, "NoCapturePathsList"),
                "SessionState should own NoCapturePathsList");
        }

        [Test]
        public void SessionState_Owns_WindowAlpha() {
            Assert.IsTrue(HasStaticMember(SessionStateType, "WindowAlpha"),
                "SessionState should own WindowAlpha");
        }

        #endregion

        #region QTUtility facade still present (consumers keep compiling)

        [Test]
        public void QTUtility_Still_Exposes_ITEMIDLIST_Dic_Session_Facade() {
            Assert.IsTrue(HasStaticMember(typeof(QTUtility), "ITEMIDLIST_Dic_Session"),
                "QTUtility should still expose ITEMIDLIST_Dic_Session facade");
        }

        [Test]
        public void QTUtility_Still_Exposes_NoCapturePathsList_Facade() {
            Assert.IsTrue(HasStaticMember(typeof(QTUtility), "NoCapturePathsList"),
                "QTUtility should still expose NoCapturePathsList facade");
        }

        [Test]
        public void QTUtility_Still_Exposes_WindowAlpha_Facade() {
            Assert.IsTrue(HasStaticMember(typeof(QTUtility), "WindowAlpha"),
                "QTUtility should still expose WindowAlpha facade");
        }

        #endregion

        #region Semantic equivalence: same instance / same lock (no behavior change)

        [Test]
        public void QTUtility_ITEMIDLIST_Dic_Session_Facade_Is_SameInstance_As_SessionState() {
            Type ss = SessionStateType;
            Assert.IsNotNull(ss, "SessionState type should exist");
            object viaSession = GetStaticMemberValue(ss, "ITEMIDLIST_Dic_Session");
            object viaFacade = GetStaticMemberValue(typeof(QTUtility), "ITEMIDLIST_Dic_Session");
            Assert.IsNotNull(viaFacade, "facade dictionary should not be null");
            Assert.AreSame(viaSession, viaFacade,
                "QTUtility.ITEMIDLIST_Dic_Session must return the same instance as SessionState (index write equivalence)");
        }

        [Test]
        public void QTUtility_NoCapturePathsList_Facade_Is_SameInstance_As_SessionState() {
            Type ss = SessionStateType;
            Assert.IsNotNull(ss, "SessionState type should exist");
            object viaSession = GetStaticMemberValue(ss, "NoCapturePathsList");
            object viaFacade = GetStaticMemberValue(typeof(QTUtility), "NoCapturePathsList");
            Assert.IsNotNull(viaFacade, "facade list should not be null");
            Assert.AreSame(viaSession, viaFacade,
                "QTUtility.NoCapturePathsList must return the same instance as SessionState (Add/iteration equivalence)");
        }

        [Test]
        public void SessionState_And_QTUtility_Share_The_Same_SyncRoot() {
            Type ss = SessionStateType;
            Assert.IsNotNull(ss, "SessionState type should exist");
            object ssLock = GetStaticMemberValue(ss, "SyncRoot");
            object quLock = GetStaticMemberValue(typeof(QTUtility), "syncRoot");
            Assert.IsNotNull(quLock, "QTUtility.syncRoot should exist");
            Assert.AreSame(quLock, ssLock,
                "SessionState must reuse QTUtility.syncRoot (no second lock, global lock semantics preserved)");
        }

        #endregion
    }
}
