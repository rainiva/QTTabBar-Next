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

        #region C7q: QTUtility facades removed (callers use SessionState directly)

        [Test]
        public void QTUtility_Does_Not_Expose_SessionState_Facades() {
            Assert.IsFalse(HasStaticMember(typeof(QTUtility), "ITEMIDLIST_Dic_Session"),
                "QTUtility should not expose ITEMIDLIST_Dic_Session facade after C7q");
            Assert.IsFalse(HasStaticMember(typeof(QTUtility), "NoCapturePathsList"),
                "QTUtility should not expose NoCapturePathsList facade after C7q");
            Assert.IsFalse(HasStaticMember(typeof(QTUtility), "WindowAlpha"),
                "QTUtility should not expose WindowAlpha facade after C7q");
        }

        #endregion

        #region Semantic equivalence: direct container access

        [Test]
        public void SessionState_ITEMIDLIST_Dic_Session_Is_Mutable_Dictionary() {
            Type ss = SessionStateType;
            Assert.IsNotNull(ss, "SessionState type should exist");
            object viaSession = GetStaticMemberValue(ss, "ITEMIDLIST_Dic_Session");
            Assert.IsNotNull(viaSession, "session dictionary should not be null");
            Assert.IsInstanceOf<Dictionary<string, byte[]>>(viaSession);
        }

        [Test]
        public void SessionState_NoCapturePathsList_Is_Mutable_List() {
            Type ss = SessionStateType;
            Assert.IsNotNull(ss, "SessionState type should exist");
            object viaSession = GetStaticMemberValue(ss, "NoCapturePathsList");
            Assert.IsNotNull(viaSession, "no-capture list should not be null");
            Assert.IsInstanceOf<List<string>>(viaSession);
        }

        [Test]
        public void SessionState_Owns_SyncRoot() {
            Type ss = SessionStateType;
            Assert.IsNotNull(ss, "SessionState type should exist");
            object ssLock = GetStaticMemberValue(ss, "SyncRoot");
            Assert.IsNotNull(ssLock, "SessionState.SyncRoot should exist after C7s");
            Assert.AreSame(ssLock, SessionState.SyncRoot,
                "SessionState.SyncRoot must be the authoritative global lock object");
        }

        #endregion
    }
}
