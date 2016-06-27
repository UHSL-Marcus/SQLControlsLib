// <copyright file="UpdateTest.cs">Copyright ©  2016</copyright>
using System;
using Microsoft.Pex.Framework;
using Microsoft.Pex.Framework.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SQLControlsLib;

namespace SQLControlsLib.Tests
{
    /// <summary>This class contains parameterized unit tests for Update</summary>
    [PexClass(typeof(Update))]
    [PexAllowedExceptionFromTypeUnderTest(typeof(InvalidOperationException))]
    [PexAllowedExceptionFromTypeUnderTest(typeof(ArgumentException), AcceptExceptionSubtypes = true)]
    [TestClass]
    public partial class UpdateTest
    {
        /// <summary>Test stub for doUpdateByID(!!0)</summary>
        [PexGenericArguments(typeof(DatabaseTableObject))]
        [PexMethod]
        public bool doUpdateByIDTest<TYPE>(TYPE ob)
            where TYPE : DatabaseTableObject
        {
            bool result = Update.doUpdateByID<TYPE>(ob);
            return result;
            // TODO: add assertions to method UpdateTest.doUpdateByIDTest(!!0)
        }
    }
}
