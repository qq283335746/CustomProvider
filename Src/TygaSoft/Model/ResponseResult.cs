using System;
using System.Runtime.Serialization;
using System.Linq;
using Newtonsoft.Json;

namespace TygaSoft.Model
{
    public class ResponseResult
    {
        public static string Response(bool isOk, string msg, object data)
        {
            return ResJsonString(new ResponseResultModel { ResCode = isOk ? ResCodeOptions.Success : ResCodeOptions.Error, Msg = msg, Data = data });
        }

        public static string ResJsonString(bool isOk, string msg, params object[] data)
        {
            return JsonConvert.SerializeObject(new ResponseResultModel { ResCode = isOk ? ResCodeOptions.Success : ResCodeOptions.Error, Msg = msg, Data = data == null ? "" : data[0] });
        }

        public static string ResJsonString(ResponseResultModel model)
        {
            return JsonConvert.SerializeObject(model);
        }
    }

    [DataContract(Name = "ResponseResultModel")]
    public class ResponseResultModel
    {
        [DataMember]
        public ResCodeOptions ResCode { get; set; }

        [DataMember]
        public string Msg { get; set; }

        [DataMember]
        public object Data { get; set; }
    }
}
