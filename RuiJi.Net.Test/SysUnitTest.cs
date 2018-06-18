﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuiJi.Net.Owin.Controllers;

namespace RuiJi.Net.Test
{
    [TestClass]
    public class SysUnitTest
    {
        [TestMethod]
        public void TestGetIfTable()
        {
            while (true)
            {
                var c = new InfoApiController();
                var info = c.System();
            }

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestGitPulse()
        {
            var api = new InfoApiController();
            var r = api.Pulse();

            Assert.IsTrue(r.ToString().Length > 0);
        }
    }
}
