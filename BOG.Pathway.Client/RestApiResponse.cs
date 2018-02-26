using System;
using System.Collections.Generic;
using System.Text;

namespace BOG.Pathway.Client
{
    public class RestApiResponse
    {
        public int StatusCode = 200;
        public string StatusDescription = "OK";
        public string ContentType = "text/plain";
        public string Body = "";
    }
}
