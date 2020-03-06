using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;
using System.Xml.Linq;
/// <summary>
/// This code was made by Maksym Vikarii 
/// https://github.com/MaTeMaTuK
/// </summary>
namespace RetrieveMultipleDemo
{
    /// <summary>
    /// This is an example of Plugin on RetrieveMultiple message with quick find using. Plugin works with Unified Interface
    /// </summary>
    /// <remarks>
    /// Register this plug-in on the RetrieveMultiple message, on pre validation and account entity.
    /// </remarks>
    public class RetrieveMultipleWithQuickFindExample : IPlugin
    {
        ITracingService tracingService;
        IPluginExecutionContext executionContext;
        public void Execute(IServiceProvider serviceProvider)
        {
            tracingService =
               (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            executionContext = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));
            if (executionContext.InputParameters["Query"] is FetchExpression)
            {
                tracingService.Trace("Context query is a Fetch Expression");
                SetFilterForFetchExpression();
            }
        }

        private void SetFilterForFetchExpression()
        {
            var fetchExpression = executionContext.InputParameters["Query"] as FetchExpression;
            tracingService.Trace("Original query: {0}", fetchExpression.Query);

            var fetchXmlDoc = XDocument.Parse(fetchExpression.Query);
            var findValue = GetQuickFindValue(fetchXmlDoc);
            if (string.IsNullOrEmpty(findValue))
                return;

            var newFilter = new XElement(
                    "filter",
                    new XAttribute("type", "and"),
                    new XElement(
                        "condition",
                        new XAttribute("attribute", "address1_fax"),
                        new XAttribute("operator", "like"),
                        new XAttribute("value", $"%{findValue}%")
                    ));

            var quickFindFieldsFilter = fetchXmlDoc.Descendants("filter")
                .Where(f => f.Attribute("isquickfindfields") != null)
                .FirstOrDefault();
            //Attaching to quick find filter
            quickFindFieldsFilter.Add(newFilter);

            var newQuery = fetchXmlDoc.ToString();
            tracingService.Trace("New Query with products: {0}", newQuery);

            fetchExpression.Query = newQuery;
        }

        protected string GetQuickFindValue(XDocument xDocument)
        {
            var filters = xDocument.Descendants("filter");
            var quickFindFilter = filters.Where(f => f.Attribute("isquickfindfields") != null).FirstOrDefault();

            if (quickFindFilter == null)
                return string.Empty;

            var firstElement = quickFindFilter.FirstNode as XElement;
            var quickFindValue = firstElement.Attribute("value").Value;
            quickFindValue = quickFindValue.Replace("%", "");

            return quickFindValue;
        }
    }
}
