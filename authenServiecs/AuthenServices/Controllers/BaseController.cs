using AuthenServices.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using System.Text.Json.Serialization;
using Logger = Serilog.Log;

namespace AuthenServices.Controllers
{
    public abstract class BaseController : ControllerBase
    {
        protected IResult Ok(object data)
        {
            var response = new ApiResponse()
            {
                Data = data,
                Code = nameof(ErrorMessage.Success),
                Message = ErrorMessage.Success,
            };
            Logger.Debug(JsonConvert.SerializeObject(response));
            return TypedResults.Ok(response);
        }

        protected IResult Error(string code, string message)
        {
            var response = new ApiResponse()
            {
                Message = message,
                Code = code
            };
            Logger.Debug(JsonConvert.SerializeObject(response));
            return TypedResults.Ok(response);
        }

        protected IResult Error(string code, string message, object data)
        {
            var response = new ApiResponse()
            {
                Message = message,
                Code = code,
                Data = data
            };
            Logger.Debug(JsonConvert.SerializeObject(response));
            return TypedResults.Ok(response);
        }

        protected IResult ErrorUnauthorized => Error(nameof(ErrorMessage.Unauthorized), ErrorMessage.Unauthorized); 
        protected IResult OK => Ok(DateTime.Now);
    }
    class ApiResponse
    {
        public object? Data { get; set; }

        [JsonPropertyOrder(-1)]
        public string? Code { get; set; }

        [JsonPropertyOrder(-2)]
        public string? Message { get; set; }
    }

    public abstract class PagingModel
    {
        [JsonPropertyOrder(-6)]
        public int? CurrentPage { get; set; } = 1;

        [BindNever]
        [JsonPropertyOrder(-5)]
        public int? TotalPages => (TotalRecords / RowsPerPage) + (TotalRecords % RowsPerPage > 0 ? 1 : 0);

        [JsonPropertyOrder(-4)]
        public int? RowsPerPage { get; set; } = Const.ROWS_PER_PAGE;

        [BindNever]
        [JsonPropertyOrder(-3)]
        public int? TotalRecords { get; set; }
    }
    static class ErrorMessage
    {
        public static string Success => "Success";
        public static string Exception => "Internal Exception";
        public static string BadRequest => "Bad Request";
        public static string Error => "Error";
        
        public static string Unauthorized => "Unauthorized";
    }

}