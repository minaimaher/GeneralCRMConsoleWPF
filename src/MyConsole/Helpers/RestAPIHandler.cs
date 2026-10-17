using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using static MyConsole.Applications;

namespace MyConsole.Helpers
{
    public class RestAPIHandler
    {
        public static string CurrentUrl { get; set; }
        public async Task<Response<T>> RunAsync<T>(StringBuilder url_, MethodTypes methodType, object _Object, CreateLogRequest createLogRequest, IOrganizationService service, ITracingService tracingService, string ApplicationCode = null, string AppSecretKey = null)
        {
            try
            {
                using (var client = new System.Net.Http.HttpClient())
                {
                    JavaScriptSerializer js = new JavaScriptSerializer();

                    string jsonMessage = js.Serialize(_Object);
                    jsonMessage = "1122510728";
                    // Logger.LogInformation("Call API Url : " + url_);
                    var content = new System.Net.Http.StringContent(jsonMessage, Encoding.UTF8, "application/json-patch+json");
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json-patch+json");
                    client.DefaultRequestHeaders.Add("AppCode", ApplicationCode);
                    client.DefaultRequestHeaders.Add("AppSecretKey", AppSecretKey);
                    client.DefaultRequestHeaders.Add("RequestGUID", _Object.ToString());

                    System.Net.Http.HttpResponseMessage result = new System.Net.Http.HttpResponseMessage();
                    try
                    {
                        CurrentUrl = url_.ToString();
                        if (methodType == MethodTypes.post)
                            result = client.PostAsync(url_.ToString(), content).Result;
                        else if (methodType == MethodTypes.get)
                            result = client.GetAsync(CurrentUrl.ToString()).Result;

                        //Logger.LogInformation("Call API result : " + result.ToString());
                        if (result.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            #region Log on PFP Create Loan on Waseet
                            createLogRequest.Name = createLogRequest.Name;
                            createLogRequest.Request = jsonMessage;
                            createLogRequest.Response = "Unauthorized";
                            createLogRequest.Status = 556310001;
                            CreateLog(service, createLogRequest);
                            #endregion
                            //--------------------
                            return new Response<T>()
                            {
                                status = "Failed",
                                IsSuccess = false,
                                //error =  "Unauthorized" 
                            };
                        }
                        if (result.IsSuccessStatusCode)
                        {
                            //Logger.LogInformation("Call API Response : " + System.Text.Json.JsonSerializer.Serialize<object>(response1));
                            //T data = await result.Content.ReadAsAsync<T>();
                            var jsonResponseString = await result.Content.ReadAsStringAsync();
                            Console.WriteLine("json response" + jsonResponseString);

                            var jsonContent = js.Deserialize<Response<T>>(jsonResponseString);
                            Console.WriteLine("jsonContent" + jsonContent);
                            #region Log on PFP Create Loan on Waseet
                            createLogRequest.Name = createLogRequest.Name;
                            createLogRequest.Request = jsonMessage;
                            createLogRequest.Response = jsonResponseString;
                            createLogRequest.Status = 556310000;
                            CreateLog(service, createLogRequest);
                            #endregion
                            //--------------------
                            return jsonContent;

                        }
                        else
                        {
                            //Logger.LogInformation("Exception at Call API  : " + response);
                            var details = await result.Content.ReadAsStringAsync();
                            #region Log on PFP Create Loan on Waseet
                            createLogRequest.Name = createLogRequest.Name;
                            createLogRequest.Request = jsonMessage;
                            createLogRequest.Response = details;
                            createLogRequest.Status = 556310001;
                            CreateLog(service, createLogRequest);
                            #endregion
                            //--------------------
                            Response<T> response = new Response<T>()
                            {
                                status = "Failed",
                                IsSuccess = false,
                                // error = details 
                            };
                            return response;
                        }
                    }
                    catch (Exception ex)
                    {
                        var details = await result.Content.ReadAsStringAsync();
                        #region Log on PFP Create Loan on Waseet
                        createLogRequest.Name = createLogRequest.Name;
                        createLogRequest.Request = jsonMessage;
                        createLogRequest.Response = details;
                        createLogRequest.Status = 556310001;
                        CreateLog(service, createLogRequest);
                        #endregion
                        //--------------------
                        Response<T> response = new Response<T>()
                        {
                            status = "Failed",
                            IsSuccess = false,
                            //error =details 
                        };
                        return response;
                    }
                    finally
                    {
                        if (result != null)
                            result.Dispose();
                    }

                }
            }
            catch (Exception ex)
            {
                //Logger.LogInformation("Exception at Call API  : " + response);

                Response<T> response = new Response<T>()
                {
                    status = "Failed",
                    IsSuccess = false,
                    // error = new Error() { ErrorCode = System.Net.HttpStatusCode.BadRequest, ErrorDescription = ex.Message + Environment.NewLine + ex.StackTrace }
                };
                return response;
            }
        }
        public Response<T> Run<T>(StringBuilder url_, MethodTypes methodType, object _Object, CreateLogRequest createLogRequest, IOrganizationService service, ITracingService tracingService, string ApplicationCode = null, string AppSecretKey = null)
        {
            try
            {
                using (var client = new System.Net.Http.HttpClient())
                {
                    JavaScriptSerializer js = new JavaScriptSerializer();
                    var jsonMessage = System.Text.Json.JsonSerializer.Serialize(_Object);
                    var content = new System.Net.Http.StringContent(jsonMessage, Encoding.UTF8, "application/json-patch+json");
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json-patch+json");
                    client.DefaultRequestHeaders.Add("AppCode", ApplicationCode);
                    client.DefaultRequestHeaders.Add("AppSecretKey", AppSecretKey);
                    client.DefaultRequestHeaders.Add("RequestGUID", _Object.ToString());

                    System.Net.Http.HttpResponseMessage result = new System.Net.Http.HttpResponseMessage();
                    try
                    {
                        CurrentUrl = url_.ToString();
                        if (methodType == MethodTypes.post)
                            result = client.PostAsync(url_.ToString(), content).Result;
                        else if (methodType == MethodTypes.get)
                            result = client.GetAsync(CurrentUrl.ToString()).Result;

                        if (result.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            #region Log on PFP Create Loan on Waseet
                            createLogRequest.Name = createLogRequest.Name;
                            createLogRequest.Request = jsonMessage;
                            createLogRequest.Response = "Unauthorized";
                            createLogRequest.Status = 556310001;
                            CreateLog(service, createLogRequest);
                            #endregion
                            //--------------------
                            return new Response<T>()
                            {
                                status = "Failed",
                                IsSuccess = false
                            };
                        }
                        if (result.IsSuccessStatusCode)
                        {
                            var jsonResponseString = result.Content.ReadAsStringAsync().Result;
                            Console.WriteLine("json response" + jsonResponseString);

                            var jsonContent = JsonSerializer.Deserialize<Response<T>>(jsonResponseString);
                            Console.WriteLine("jsonContent" + jsonContent);
                            #region Log on PFP Create Loan on Waseet
                            createLogRequest.Name = createLogRequest.Name;
                            createLogRequest.Request = jsonMessage;
                            createLogRequest.Response = jsonResponseString;
                            createLogRequest.Status = 556310000;
                            CreateLog(service, createLogRequest);
                            #endregion
                            //--------------------
                            return jsonContent;
                        }
                        else
                        {
                            var details = result.Content.ReadAsStringAsync().Result;
                            #region Log on PFP Create Loan on Waseet
                            createLogRequest.Name = createLogRequest.Name;
                            createLogRequest.Request = jsonMessage;
                            createLogRequest.Response = details;
                            createLogRequest.Status = 556310001;
                            CreateLog(service, createLogRequest);
                            #endregion
                            //--------------------
                            Response<T> response = new Response<T>()
                            {
                                status = "Failed",
                                IsSuccess = false
                            };
                            return response;
                        }
                    }
                    catch (Exception ex)
                    {
                        var details = result.Content.ReadAsStringAsync().Result;
                        #region Log on PFP Create Loan on Waseet
                        createLogRequest.Name = createLogRequest.Name;
                        createLogRequest.Request = jsonMessage;
                        createLogRequest.Response = details;
                        createLogRequest.Status = 556310001;
                        CreateLog(service, createLogRequest);
                        #endregion
                        //--------------------
                        Response<T> response = new Response<T>()
                        {
                            status = "Failed",
                            IsSuccess = false
                        };
                        return response;
                    }
                    finally
                    {
                        if (result != null)
                            result.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                Response<T> response = new Response<T>()
                {
                    status = "Failed",
                    IsSuccess = false
                };
                return response;
            }
        }
        public Response<T> Runjs<T>(StringBuilder url_, MethodTypes methodType, object _Object, CreateLogRequest createLogRequest, IOrganizationService service, ITracingService tracingService, string ApplicationCode = null, string AppSecretKey = null)
        {
            try
            {
                using (var client = new System.Net.Http.HttpClient())
                {
                    JavaScriptSerializer js = new JavaScriptSerializer();

                    string jsonMessage = js.Serialize(_Object);

                    var content = new System.Net.Http.StringContent(jsonMessage, Encoding.UTF8, "application/json-patch+json");
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json-patch+json");
                    client.DefaultRequestHeaders.Add("AppCode", ApplicationCode);
                    client.DefaultRequestHeaders.Add("AppSecretKey", AppSecretKey);
                    client.DefaultRequestHeaders.Add("RequestGUID", _Object.ToString());

                    System.Net.Http.HttpResponseMessage result = new System.Net.Http.HttpResponseMessage();
                    try
                    {
                        CurrentUrl = url_.ToString();
                        if (methodType == MethodTypes.post)
                            result = client.PostAsync(url_.ToString(), content).Result;
                        else if (methodType == MethodTypes.get)
                            result = client.GetAsync(CurrentUrl.ToString()).Result;

                        if (result.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            #region Log on PFP Create Loan on Waseet
                            createLogRequest.Name = createLogRequest.Name;
                            createLogRequest.Request = jsonMessage;
                            createLogRequest.Response = "Unauthorized";
                            createLogRequest.Status = 556310001;
                            CreateLog(service, createLogRequest);
                            #endregion
                            //--------------------
                            return new Response<T>()
                            {
                                status = "Failed",
                                IsSuccess = false
                            };
                        }
                        if (result.IsSuccessStatusCode)
                        {
                            var jsonResponseString = result.Content.ReadAsStringAsync().Result;
                            Console.WriteLine("json response" + jsonResponseString);

                            var jsonContent = js.Deserialize<Response<T>>(jsonResponseString);
                            Console.WriteLine("jsonContent" + jsonContent);
                            #region Log on PFP Create Loan on Waseet
                            createLogRequest.Name = createLogRequest.Name;
                            createLogRequest.Request = jsonMessage;
                            createLogRequest.Response = jsonResponseString;
                            createLogRequest.Status = 556310000;
                            CreateLog(service, createLogRequest);
                            #endregion
                            //--------------------
                            return jsonContent;
                        }
                        else
                        {
                            var details = result.Content.ReadAsStringAsync().Result;
                            #region Log on PFP Create Loan on Waseet
                            createLogRequest.Name = createLogRequest.Name;
                            createLogRequest.Request = jsonMessage;
                            createLogRequest.Response = details;
                            createLogRequest.Status = 556310001;
                            CreateLog(service, createLogRequest);
                            #endregion
                            //--------------------
                            Response<T> response = new Response<T>()
                            {
                                status = "Failed",
                                IsSuccess = false
                            };
                            return response;
                        }
                    }
                    catch (Exception ex)
                    {
                        var details = result.Content.ReadAsStringAsync().Result;
                        #region Log on PFP Create Loan on Waseet
                        createLogRequest.Name = createLogRequest.Name;
                        createLogRequest.Request = jsonMessage;
                        createLogRequest.Response = details;
                        createLogRequest.Status = 556310001;
                        CreateLog(service, createLogRequest);
                        #endregion
                        //--------------------
                        Response<T> response = new Response<T>()
                        {
                            status = "Failed",
                            IsSuccess = false
                        };
                        return response;
                    }
                    finally
                    {
                        if (result != null)
                            result.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                Response<T> response = new Response<T>()
                {
                    status = "Failed",
                    IsSuccess = false
                };
                return response;
            }
        }
        public static string GetWaseetAPIhFromSDBConfiguration(IOrganizationService service, string code)
        {
            var URLValue = "";
            QueryExpression query = new QueryExpression("ntw_sdbconfiguratin");
            query.ColumnSet = new ColumnSet("ntw_textvalue");
            query.Criteria.AddCondition("ntw_configurationcode", ConditionOperator.Equal, code);
            var queryResult = service.RetrieveMultiple(query);
            if (queryResult.Entities != null && queryResult.Entities.Count != 0)
            {
                var sdbConfigurationRecord = queryResult.Entities[0];
                URLValue = sdbConfigurationRecord.GetAttributeValue<string>("ntw_textvalue");
            }
            return URLValue;
        }
        public static void CreateLog(IOrganizationService service, CreateLogRequest createLogRequest)
        {
            if (createLogRequest != null)
            {
                Entity newRecord = new Entity("ntw_pfpcreateloanlog");
                // Set attribute values for the new record
                newRecord["ntw_name"] = createLogRequest.Name;
                newRecord["ntw_response"] = createLogRequest.Response;
                newRecord["ntw_request"] = createLogRequest.Request;
                newRecord["ntw_createnewpaymentrequest"] = createLogRequest.CreateNewPaymentRequest;
                newRecord["statuscode"] = new OptionSetValue(createLogRequest.Status);
                // Create the record in CRM
                Guid createdRecordId = service.Create(newRecord);
            }
        }
    }
    public class Response<T>
    {
        public bool IsSuccess { get; set; }
        public Error error { get; set; }
        public string status { get; set; }
        public string Message { get; set; }
        public T data { get; set; }
    }
    public class Error
    {
        public string errorCode { get; set; }
        public string errorDescription { get; set; }
    }
}
    public enum MethodTypes
    {
        get = 1,
        post = 2
    }

