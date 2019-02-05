using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace NationStatesAPIBot
{
    public class NationStaatesApiController
    {
        public HttpWebRequest CreateApiRequest(string parameters)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"http://www.nationstates.net/cgi-bin/api.cgi?{parameters}");
            request.Method = "GET";
            request.UserAgent = ActionManager.NationStatesAPIUserAgent;
            return request;
        }
    }
}
