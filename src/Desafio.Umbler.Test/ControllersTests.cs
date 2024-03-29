using Desafio.Umbler.Controllers;
using Desafio.Umbler.Data;
using Desafio.Umbler.DTOs;
using Desafio.Umbler.Interfaces;
using Desafio.Umbler.Models;
using DnsClient;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Desafio.Umbler.Test
{
    [TestClass]
    public class ControllersTest
    {
        [TestMethod]
        public void Home_Index_returns_View()
        {
            //arrange 
            var controller = new HomeController();

            //act
            var response = controller.Index();
            var result = response as ViewResult;

            //assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Home_Error_returns_View_With_Model()
        {
            //arrange 
            var controller = new HomeController();
            controller.ControllerContext = new ControllerContext();
            controller.ControllerContext.HttpContext = new DefaultHttpContext();

            //act
            var response = controller.Error();
            var result = response as ViewResult;
            var model = result.Model as ErrorViewModel;

            //assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(model);
        }
        
        [TestMethod]
        public void Domain_In_Database()
        {
            //arrange 
            var whoisClient = new Mock<IWhoisClient>().Object;
            var lookupClient = new Mock<ILookupClient>().Object;

            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(databaseName: "Find_searches_url")
                .Options;

            var domain = new Domain("test.com");
            domain.setDomain("192.168.0.1", "Ns.umbler.com", 60, "umbler.corp");

            // Insert seed data into the database using one instance of the context
            using (var db = new DatabaseContext(options))
            {
                db.Domains.Add(domain);
                db.SaveChanges();
            }

            // Use a clean instance of the context to run the test
            using (var db = new DatabaseContext(options))
            {
                var controller = new DomainController(db, lookupClient, whoisClient);

                //act
                var response = controller.Get("test.com");
                var result = response.Result as OkObjectResult;
                var obj = result.Value as DomainDTO;
                Assert.AreEqual(obj.Ip, domain.Ip);
                Assert.AreEqual(obj.Name, domain.Name);
                Assert.AreEqual(obj.HostedAt, domain.HostedAt);
                Assert.AreEqual(obj.WhoIs, domain.WhoIs);
            }
        }

        [TestMethod]
        public void Domain_Not_In_Database()
        {
            //arrange 
            var whoisClient = new Mock<IWhoisClient>().Object;
            var lookupClient = new Mock<ILookupClient>().Object;
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(databaseName: "Find_searches_url")
                .Options;

            // Use a clean instance of the context to run the test
            using (var db = new DatabaseContext(options))
            {
                var controller = new DomainController(db, lookupClient, whoisClient);

                //act
                var response = controller.Get("test.com");
                var result = response.Result as OkObjectResult;
                var obj = result.Value as DomainDTO;
                Assert.IsNotNull(obj);
            }
        }

        [TestMethod]
        public void Domain_Moking_LookupClient()
        {
            //arrange 
            var whoisClient = new Mock<IWhoisClient>();
            var lookupClient = new Mock<ILookupClient>();
            var domainName = "test.com";

            var dnsResponse = new Mock<IDnsQueryResponse>();
            lookupClient.Setup(l => l.QueryAsync(domainName, QueryType.ANY, QueryClass.IN, System.Threading.CancellationToken.None)).ReturnsAsync(dnsResponse.Object);

            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(databaseName: "Find_searches_url")
                .Options;

            // Use a clean instance of the context to run the test
            using (var db = new DatabaseContext(options))
            {
                //inject lookupClient in controller constructor
                var controller = new DomainController(db, lookupClient.Object , whoisClient.Object);

                //act
                var response = controller.Get("test.com");
                var result = response.Result as OkObjectResult;
                var obj = result.Value as DomainDTO;
                Assert.IsNotNull(obj);
            }
        }

        [TestMethod]
        public void Domain_Moking_WhoisClient()
        {
            //arrange
            //whois is a static class, we need to create a class to "wrapper" in a mockable version of WhoisClient
            var lookupClient = new Mock<ILookupClient>();
            var whoisClient = new Mock<IWhoisClient>();
            var domainName = "test.com";

            whoisClient.Setup(l => l.QueryAsync(domainName));

            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(databaseName: "Find_searches_url")
                .Options;

            // Use a clean instance of the context to run the test
            using (var db = new DatabaseContext(options))
            {
                //inject IWhoisClient in controller's constructor
                var controller = new DomainController(db, lookupClient.Object, whoisClient.Object);

                //act
                var response = controller.Get(domainName);
                var result = response.Result as OkObjectResult;
                var obj = result.Value as DomainDTO;
                Assert.IsNotNull(obj);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(BadHttpRequestException), "Nome de dom�nio inv�lido")]
        public void Domain_With_InvalidName()
        {
            new Domain("test");
        }

    }
}