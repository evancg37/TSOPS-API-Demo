// Evan Greavu - egreavu@osisoft.com
// TSOPS API Demo

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using OSIsoft.AF.PI;
using OSIsoft.AF.Asset;

namespace APITest.Controllers
{
    public class tsopsController : ApiController
    {
        PIServer pisrv;
        public tsopsController()
        {
            pisrv = PIServers.GetPIServers().DefaultPIServer;
            pisrv.Connect(); 
        }

        // Read Snapshot value
        // GET /api/tsops/{tag}
        public HttpResponseMessage Get(string tag)
        {
            PIPoint point;
            if (PIPoint.TryFindPIPoint(pisrv, tag, out point) == false) // PI Point not found 
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, $"Tag '{tag}' not found on PI Server '{pisrv.Name}'.");
            }

            AFValue value = point.CurrentValue(); 
            string response;
            if (value.IsGood) 
            {
                response = $"{value.Timestamp}: {value.ValueAsDouble()}"; // For good values, show the Timestamp and Value
            }
            else
            {
                response = $"{value.Timestamp}: {value.Status}"; // For bad values, show the Timestamp and State
            }

            return Request.CreateResponse(HttpStatusCode.OK, response); 
        }

        // Create PI Point
        // POST /api/tsops/{tag}
        public HttpResponseMessage Post(string tag)
        {
            if (PIPoint.TryFindPIPoint(pisrv, tag, out _)) // PI Point is found
            {
                return Request.CreateResponse(HttpStatusCode.Conflict, $"Tag '{tag}' already exists on PI Server '{pisrv.Name}'.");
            }

            try
            {
                pisrv.CreatePIPoint(tag); // Create PI Point
                return Request.CreateResponse(HttpStatusCode.Created, $"Point '{tag}' created on PI Server '{pisrv.Name}'.");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Failed to create tag '{tag}' on PI Server '{pisrv.Name}': {ex.Message}");
            }
        }

        // Write Snapshot value
        // PATCH /api/tsops/{tag}?value={value}
        public HttpResponseMessage Patch(string tag, double value)
        {
            PIPoint point;
            if (PIPoint.TryFindPIPoint(pisrv, tag, out point) == false) // PI Point not found
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, $"Tag '{tag}' not found on PI Server '{pisrv.Name}'.");
            }

            try
            {
                var currTime = new OSIsoft.AF.Time.AFTime("*");
                point.UpdateValue(new AFValue(value, currTime), OSIsoft.AF.Data.AFUpdateOption.Insert); // Insert value at current time
                return Request.CreateResponse(HttpStatusCode.OK, $"Value '{value}' written at '{currTime}' for tag '\\\\{pisrv.Name}\\{tag}'.");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"Failed to write value to tag '{tag}' on PI Server '{pisrv.Name}': {ex.Message}");
            }
        }

        // Missing tag for Read Snapshot Value
        // GET /api/tsops/{?}
        public HttpResponseMessage Get()
        {
            return Request.CreateResponse(HttpStatusCode.BadRequest, "Please specify a tag. Example: '/api/tsops/Sinusoid'");
        }

        // Missing tag for Create Tag
        // POST /api/tsops/{?}
        public HttpResponseMessage Post()
        {
            return Request.CreateResponse(HttpStatusCode.BadRequest, "Please specify a tag. Example: '/api/tsops/Sinusoid'");
        }

        // Missing value for Write Snapshot Value
        // PATCH /api/tsops/{tag}?value={?}
        public HttpResponseMessage Patch(string tag)
        {
            return Request.CreateResponse(HttpStatusCode.BadRequest, "Please specify a value. Example: '/api/tsops/Sinusoid?value=123'");
        }
    }
}
