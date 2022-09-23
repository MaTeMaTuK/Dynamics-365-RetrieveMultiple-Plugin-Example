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
    /// This is a simple example of Plugin on RetrieveMultiple message. Plugin works with classic & unified interfaces
    /// </summary>
    /// <remarks>
    /// Register this plug-in on the RetrieveMultiple message, on pre validation and account entity.
    /// </remarks>

    public class RetrieveMultipleSimpleExample : IPlugin
    {
        ITracingService tracingService;
        IPluginExecutionContext executionContext;

        public void Execute(IServiceProvider serviceProvider)
        {
            tracingService =
                (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            executionContext = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));

            if (!executionContext.InputParameters.Contains("Query"))
                return;

            if (executionContext.InputParameters["Query"] is QueryExpression)
            {
                tracingService.Trace("Context query is an Query Expression");
                SetFilterForQueryExpression();
            }
            else if (executionContext.InputParameters["Query"] is FetchExpression)
            {
                tracingService.Trace("Context query is a Fetch Expression");
                SetFilterForFetchExpression();
            }
        }

        /// <summary>
        /// Method sets filter for Legcay Interface
        /// </summary>
        /// <param name="executionContext"></param>
        void SetFilterForQueryExpression()
        {
            var queryExpression = (QueryExpression)executionContext.InputParameters["Query"];
            //All records must contain "Test" word in name field
            queryExpression.Criteria.AddCondition("name", ConditionOperator.Contains, "Test");
        }

        /// <summary>
        /// Method sets filter for Unified Interface
        /// </summary>
        /// <param name="executionContext"></param>
        void SetFilterForFetchExpression()
        {
            var fetchExpression = (FetchExpression)executionContext.InputParameters["Query"];

            var fetchXmlDoc = XDocument.Parse(fetchExpression.Query);

            //In Unified Interface Fetch Expression may contain filter with isquickfindfields attribute.
            //This attribute comes from customer search(lookup or quick find).
            //Details: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/quick-find-limit
            //In this example We use only view filter, if it exists
            var originalFilter = fetchXmlDoc
                .Descendants("filter")
                .Where(f => f.Attribute("isquickfindfields") == null)
                .FirstOrDefault();

            //If filter does not exist We may create a new.
            XElement filterElement;
            if (originalFilter != null)
            {
                filterElement = originalFilter;
                //Remove filter from main object
                filterElement.Remove();
            }
            else
            {
                filterElement = new XElement("filter", new XAttribute("type", "and"));
            }
           
            //All records must contain "Test" word in name field
            var newFilterCondition = new XElement(
                        "condition",
                        new XAttribute("attribute", "name"),
                        new XAttribute("operator", "like"),
                        new XAttribute("value", "%Test%")
                    );

            //Add new condition to filter
            filterElement.Add(newFilterCondition);
            tracingService.Trace("Filter result: {0}", filterElement.ToString());

            //Add new filter to main object
            var entityElement = fetchXmlDoc.Descendants("entity").FirstOrDefault();
            entityElement.Add(filterElement);

            var fetchResult = fetchXmlDoc.ToString();
            tracingService.Trace("Output result: {0}", fetchResult);
            fetchExpression.Query = fetchResult;
        }
    }
}
