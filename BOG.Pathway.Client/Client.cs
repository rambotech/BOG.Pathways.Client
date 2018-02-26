using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BOG.Pathway.Client
{
    public class Client
    {
        Dictionary<int, string> RestApiUris = new Dictionary<int, string>();
        int RestApiUriUseIndex = -1;
        string SuperAccessToken = string.Empty;
        string AdminAccessToken = string.Empty;
        string UserAccessToken = string.Empty;
        string EncryptKey = string.Empty;
        string EncryptSalt = string.Empty;

        Dictionary<string, Pathway> Pathways = new Dictionary<string, Pathway>();

        public Client(IEnumerable<string> uris, string superAccessToken, string adminAccessToken, string userAccessToken)
        {
            Setup(uris, superAccessToken, adminAccessToken, userAccessToken, string.Empty, string.Empty);
        }

        public Client(IEnumerable<string> uris, string superAccessToken, string adminAccessToken, string userAccessToken, string encryptKey, string encrypSalt)
        {
            Setup(uris, superAccessToken, adminAccessToken, userAccessToken, encryptKey, encrypSalt);
        }

        private void Setup(IEnumerable<string> uris, string superAccessToken, string adminAccessToken, string userAccessToken, string encryptKey, string encrypSalt)
        {
            foreach (string uri in uris)
            {
                RestApiUris.Add(RestApiUris.Count, uri);
                RestApiUriUseIndex++;
            }
            this.SuperAccessToken = superAccessToken;
            this.AdminAccessToken = adminAccessToken;
            this.UserAccessToken = userAccessToken;
            this.EncryptKey = encryptKey;
            this.EncryptSalt = encrypSalt;
            if (!string.IsNullOrWhiteSpace(this.EncryptKey) && string.IsNullOrWhiteSpace(this.EncryptSalt))
                throw new ArgumentException("When an encrypt key is used, a value for salt must also be specified.");
        }

        public void AddPathway(Pathway p)
        {
            if (Pathways.ContainsKey(p.ID))
            {
                throw new ArgumentException("Pathway already registered");
            }
            Pathways.Add(p.ID, p);
        }

        private RestApiResponse MakeTheCall(string url, string method, string[] headers, string body)
        {
            var result = new RestApiResponse();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = null;
            try
            {
                request.Credentials = CredentialCache.DefaultCredentials;
                request.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                ((HttpWebRequest)request).UserAgent = ".NET BOG.Pathway.Client";
                request.Method = method.ToUpper();
                foreach (string headerLine in headers)
                {
                    var breakpoint = headerLine.IndexOf(":");
                    var key = breakpoint == -1 ? headerLine.Trim() : headerLine.Substring(0, breakpoint);
                    var value = (breakpoint == -1 || breakpoint == (headerLine.Length - 1)) ? string.Empty : headerLine.Substring(breakpoint + 1).Trim();
                    if (key.ToLower() == "accept")
                    {
                        request.Accept = value;
                    }
                    else if (key.ToLower() == "content-type")
                    {
                        request.ContentType = value;
                    }
                    else
                    {
                        request.Headers.Set(key, value);
                    }
                }
                if (method.ToUpper() == "POST" || method.ToUpper() == "PUT")
                {
                    request.ContentLength = body.Length;
                    using (var sw = new StreamWriter(request.GetRequestStream()))
                    {
                        sw.Write(body);
                    }
                }

                using (response = (HttpWebResponse)request.GetResponse())
                {
                    result.StatusCode = (int)response.StatusCode;
                    result.StatusDescription = response.StatusDescription;
                    for (int index = 0; index < response.Headers.Count; index++)
                    {
                        var key = response.Headers.GetKey(index);
                        var values = response.Headers.GetValues(index);
                        if (key.ToLower() == "content-type")
                        {
                            result.ContentType = values[0];
                        }
                    }
                    result.Body = new StreamReader(response.GetResponseStream()).ReadToEnd().Replace("\r", string.Empty).Replace("\n", "\r\n");
                }
            }
            catch (WebException webex)
            {
                if (webex.Status == WebExceptionStatus.ProtocolError && webex.Response != null)
                {
                    var resp = (HttpWebResponse)webex.Response;
                    result.StatusCode = (int)resp.StatusCode;
                    result.StatusDescription = resp.StatusDescription;
                }
                else
                {
                    result.StatusCode = 599;
                    result.StatusDescription = webex.Message;
                }
            }
            catch (Exception err)
            {
                result.StatusCode = 999;
                result.StatusDescription = err.Message;
            }
            return result;
        }


        #region API methods - Admin
        public string ServerClients()
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("{");
            int index = 0;
            foreach (var key in RestApiUris.Keys)
            {
                RestApiResponse r = MakeTheCall(
                    RestApiUris[key] + "/api/admin/clients",
                    "GET",
                    new string[] { $"Access-Token: {this.AdminAccessToken}" },
                    string.Empty
                    );
                switch (r.StatusCode)
                {
                    case 200:
                        Uri u = new Uri(RestApiUris[key]);
                        result.Append(r.Body);
                        break;
                    default:
                        result.Append($"\"{key}\": {{ \"statusCode\": \"{r.StatusCode}\", \"statusDescription\": \"{r.StatusDescription}\" }}");
                        break;
                }
                index++;
                if (index != RestApiUris.Keys.Count)
                {
                    result.Append(",");
                }
                result.AppendLine();
            }
            result.AppendLine("}");
            return result.ToString();
        }

        public string ServerSummary()
        {
            StringBuilder result = new StringBuilder();
            int index = 0;
            foreach (var key in RestApiUris.Keys)
            {
                result.Append($"  \"{key}\": ");
                RestApiResponse r = MakeTheCall(
                    RestApiUris[key] + "/api/admin/pathways/summary",
                    "GET",
                    new string[] { $"Access-Token: {this.AdminAccessToken}" },
                    string.Empty
                    );
                switch (r.StatusCode)
                {
                    case 200:
                        result.Append(r.Body);
                        break;
                    default:
                        result.Append($"{{ \"statusCode\": \"{r.StatusCode}\", \"statusDescription\": \"{r.StatusDescription}\" }}");
                        break;
                }
                index++;
                if (index != RestApiUris.Count)
                {
                    result.Append(",");
                }
                result.AppendLine();
            }
            result.AppendLine("}");
            return result.ToString();
        }

        public void ServerReset()
        {
            foreach (var key in RestApiUris.Keys)
            {
                RestApiResponse r = MakeTheCall(
                    RestApiUris[key] + "/api/admin/reset",
                    "GET",
                    new string[] { $"Access-Token: {this.SuperAccessToken}" },
                    string.Empty
                    );
                switch (r.StatusCode)
                {
                    case 200:
                        break;
                    default:
                        throw new IOException(r.StatusDescription);
                }
            }
        }

        public void ServerShutdown()
        {
            foreach (var key in RestApiUris.Keys)
            {
                RestApiResponse r = MakeTheCall(
                    RestApiUris[key] + "/api/admin/shutdown",
                    "GET",
                    new string[] { $"Access-Token: {this.SuperAccessToken}" },
                    string.Empty
                    );
                switch (r.StatusCode)
                {
                    case 200:
                        break;
                    default:
                        throw new IOException(r.StatusDescription);
                }
            }
        }
        #endregion

        #region API methods - Pathway
        public void CreatePathway(string id)
        {
            if (!Pathways.ContainsKey(id))
            {
                throw new ArgumentException($"Pathway id not found: {id}");
            }
            foreach (var key in RestApiUris.Keys)
            {
                RestApiResponse r = MakeTheCall(
                    RestApiUris[key] + $"/api/pathways/create/{id}?readToken={Pathways[id].ReadToken}&writeToken={Pathways[id].WriteToken}&maxPayloads={Pathways[id].maxPayloads}&maxReferences={Pathways[id].maxReferences}",
                    "GET",
                    new string[] { $"Access-Token: {this.AdminAccessToken}" },
                    string.Empty
                    );
                switch (r.StatusCode)
                {
                    case 401:
                    case 409:
                    case 500:
                    case 599:
                    case 999:
                        throw new IOException(r.StatusDescription);
                    default:
                        break;
                }
            }
        }

        public void DeletePathway(string id)
        {
            if (!Pathways.ContainsKey(id))
            {
                throw new ArgumentException($"Pathway id not found: {id}");
            }
            foreach (var key in RestApiUris.Keys)
            {
                RestApiResponse r = MakeTheCall(
                    RestApiUris[key] + $"/api/pathways/delete/{id}",
                    "GET",
                    new string[] { $"Access-Token: {this.AdminAccessToken}" },
                    string.Empty
                    );
                switch (r.StatusCode)
                {
                    case 200:
                        break;
                    default:
                        throw new IOException($"{r.StatusCode}: {r.StatusDescription}");
                }
            }
        }

        public bool WritePayload(string pathwayId, string payload)
        {
            bool completed = false;
            if (!Pathways.ContainsKey(pathwayId))
            {
                throw new ArgumentException($"Pathway id not found: {pathwayId}");
            }

            string sendPayload = null;
            if (!string.IsNullOrWhiteSpace(this.EncryptKey))
            {
                sendPayload = MakeEncryptedPayload(payload);
            }
            else
            {
                sendPayload = Base64EncodeString(payload, true);
            }

            int fullCircleIndex = RestApiUriUseIndex;
            int serversLeft = RestApiUris.Count;
            while (serversLeft > 0 && !completed)
            {
                serversLeft--;
                var baseUri = RestApiUris[RestApiUriUseIndex];
                RestApiUriUseIndex++;
                if (RestApiUriUseIndex == RestApiUris.Count)
                {
                    RestApiUriUseIndex = 0;
                }
                RestApiResponse r = MakeTheCall(
                    baseUri + $"/api/pathways/{pathwayId}/payloads/write",
                    "POST",
                    new string[] { $"Access-Token: {this.UserAccessToken}", $"Pathway-Token: {Pathways[pathwayId].WriteToken}", "content-type: text/plain" },
                    sendPayload
                    );
                switch (r.StatusCode)
                {
                    case 200:
                        completed = true;
                        break;
                    case 429:
                        continue; // try the next server
                    case 409:
                        continue; // try the next server
                    default:
                        throw new IOException($"{r.StatusCode}: {r.StatusDescription}");
                }
            }
            return completed;
        }

        public string ReadPayload(string pathwayId)
        {
            string result = string.Empty;
            if (!Pathways.ContainsKey(pathwayId))
            {
                throw new ArgumentException($"Pathway id not found: {pathwayId}");
            }
            int fullCircleIndex = RestApiUriUseIndex;
            bool completed = false;
            int serversLeft = RestApiUris.Count;
            while (serversLeft > 0 && !completed)
            {
                serversLeft--;
                var baseUri = RestApiUris[RestApiUriUseIndex];
                RestApiUriUseIndex++;
                if (RestApiUriUseIndex == RestApiUris.Count)
                {
                    RestApiUriUseIndex = 0;
                }
                RestApiResponse r = MakeTheCall(
                baseUri + $"/api/pathways/{pathwayId}/payloads/read",
                "GET",
                new string[] { $"Access-Token: {this.UserAccessToken}", $"Pathway-Token: {Pathways[pathwayId].ReadToken}", "Accept: tet/plain" },
                string.Empty
                );
                switch (r.StatusCode)
                {
                    case 204:
                        // no payload at this Uri: try the next one.
                        continue;
                    case 200:
                        if (!string.IsNullOrWhiteSpace(this.EncryptKey))
                        {
                            result = ReadEncryptedPayload(r.Body);
                        }
                        else
                        {
                            result = Base64DecodeString(r.Body);
                        }
                        completed = true;
                        break;
                    default:
                        throw new IOException($"{r.StatusCode}: {r.StatusDescription}");
                }
            }
            return result;
        }

        public void SetReference(string pathwayId, string referenceKey, string referenceValue)
        {
            if (!Pathways.ContainsKey(pathwayId))
            {
                throw new ArgumentException($"Pathway id not found: {pathwayId}");
            }
            string sendReference = null;
            if (!string.IsNullOrWhiteSpace(this.EncryptKey))
            {
                sendReference = MakeEncryptedPayload(referenceValue);
            }
            else
            {
                sendReference = Base64EncodeString(referenceValue, true);
            }
            foreach (var k in RestApiUris.Keys)
            {
                RestApiResponse r = MakeTheCall(
                    RestApiUris[k] + $"/api/pathways/{pathwayId}/references/set/{referenceKey}",
                    "POST",
                    new string[] { $"Access-Token: {this.UserAccessToken}", $"Pathway-Token: {Pathways[pathwayId].WriteToken}", "Content-Type: text/plain" },
                    sendReference
                    );
                switch (r.StatusCode)
                {
                    case 200:
                        break;
                    default:
                        throw new IOException($"{r.StatusCode}: {r.StatusDescription}");
                }
            }
        }

        public string GetReference(string pathwayId, string referenceKey)
        {
            string result = string.Empty;
            if (!Pathways.ContainsKey(pathwayId))
            {
                throw new ArgumentException($"Pathway id not found: {pathwayId}");
            }
            foreach (var key in RestApiUris.Keys)
            {
                RestApiResponse r = MakeTheCall(
                    RestApiUris[key] + $"/api/pathways/{pathwayId}/references/get/{referenceKey}",
                    "GET",
                    new string[] { $"Access-Token: {this.UserAccessToken}", $"Pathway-Token: {Pathways[pathwayId].ReadToken}" },
                    string.Empty
                    );
                switch (r.StatusCode)
                {
                    case 204:
                        // no reference at this Uri: try the next one.
                        continue;
                    case 200:
                        if (!string.IsNullOrWhiteSpace(this.EncryptKey))
                        {
                            result = ReadEncryptedPayload(r.Body);
                        }
                        else
                        {
                            result = Base64DecodeString(r.Body);
                        }
                        break;
                    default:
                        throw new IOException($"{r.StatusCode}: {r.StatusDescription}");
                }
            }
            return result;
        }

        public string ListReferences(string pathwayId)
        {
            string result = string.Empty;
            if (!Pathways.ContainsKey(pathwayId))
            {
                throw new ArgumentException($"Pathway id not found: {pathwayId}");
            }
            foreach (var key in RestApiUris.Keys)
            {
                RestApiResponse r = MakeTheCall(
                    RestApiUris[key] + $"/api/pathways/{pathwayId}/references/list",
                    "GET",
                    new string[] { $"Access-Token: {this.UserAccessToken}", $"Pathway-Token: {Pathways[pathwayId].ReadToken}" },
                    string.Empty
                    );
                switch (r.StatusCode)
                {
                    case 200:
                        result = r.Body;
                        break;
                    default:
                        throw new IOException($"{r.StatusCode}: {r.StatusDescription}");
                }
            }
            return result;
        }

        public void DeleteReference(string pathwayId, string referenceKey)
        {
            if (!Pathways.ContainsKey(pathwayId))
            {
                throw new ArgumentException($"Pathway id not found: {pathwayId}");
            }
            foreach (var key in RestApiUris.Keys)
            {
                RestApiResponse r = MakeTheCall(
                    RestApiUris[key] + $"/api/pathways/{pathwayId}/references/delete/{referenceKey}",
                    "GET",
                    new string[] { $"Access-Token: {this.UserAccessToken}", $"Pathway-Token: {Pathways[pathwayId].WriteToken}" },
                    string.Empty
                    );
                switch (r.StatusCode)
                {
                    case 200:
                        break;
                    case 204:
                        break;
                    default:
                        throw new IOException($"{r.StatusCode}: {r.StatusDescription}");
                }
            }
        }
        #endregion

        #region Helpers (Private)
        private string SHA256toString(byte[] array)
        {
            var result = new StringBuilder();
            int saltIndex = 0;
            for (int index = 0; index < array.Length; index++)
            {
                switch (index)
                {
                    case 0:
                    case 1:
                        result.Append(String.Format("{0:x2}", (byte)array[index]));
                        break;

                    default:
                        result.Append(String.Format("{0:x2}", (byte)array[index] % (byte)this.EncryptSalt[saltIndex]));
                        saltIndex = (saltIndex + 1) % this.EncryptSalt.Length;
                        break;
                }
            }
            return result.ToString();
        }

        // fluffcounts + SHA-256::encryptedPayloadInBase64
        private string MakeEncryptedPayload(string payload)
        {
            var mySHA256 = (SHA256Managed)SHA256Managed.Create();
            var rnd = new Random(DateTime.Now.Millisecond);
            byte preFluffCount = (byte)rnd.Next(1, 255);
            byte postFluffCount = (byte)rnd.Next(1, 255);
            byte[] sha256 = new byte[34];
            sha256[0] = postFluffCount;
            sha256[1] = preFluffCount;

            mySHA256.ComputeHash(Encoding.UTF8.GetBytes(payload)).CopyTo(sha256, 2);

            StringBuilder payloadRaw = new StringBuilder();
            while (preFluffCount > 0)
            {
                payloadRaw.Append((char)(byte)rnd.Next(32, 122));
                preFluffCount--;
            }
            payloadRaw.Append(payload);
            while (postFluffCount > 0)
            {
                payloadRaw.Append((char)(byte)rnd.Next(32, 122));
                postFluffCount--;
            }

            return
                SHA256toString(sha256)
                + "::"
                + new CipherUtility().Encrypt(payloadRaw.ToString(), this.EncryptKey, this.EncryptSalt, Base64FormattingOptions.InsertLineBreaks)
                + ":::";
        }

        private string ReadEncryptedPayload(string payload)
        {
            // Guard: two-colons at offset 64, and three colons as the last three characters.

            if (payload.IndexOf("::") != 68)
            {
                throw new InvalidPayloadException("Missing expected delimiter (::) at offset 68 of sequence.");
            }
            if (payload.Length < 73 || payload.LastIndexOf(":::") != payload.Length - 3)
            {
                throw new InvalidPayloadException("Missing expected delimiter (:::) at end of sequence.");
            }
            var mySHA256 = (SHA256Managed)SHA256Managed.Create();
            var Sha256FingerPrint = payload.Substring(0, 68);
            for (int index = 0; index < Sha256FingerPrint.Length; index++)
                if ("0123456789ABCDEF".IndexOf(char.ToUpper(Sha256FingerPrint[index])) == -1)
                    throw new InvalidPayloadException($"Invalid hex character (${Sha256FingerPrint[index]}) at offset ${index}");

            var preFluffCount = byte.Parse(Sha256FingerPrint.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            var postFluffCount = byte.Parse(Sha256FingerPrint.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            var fluffyPayload = new CipherUtility().Decrypt(payload.Substring(70, payload.Length - 73), this.EncryptKey, this.EncryptSalt);
            var recoveredPayload = fluffyPayload.Substring(preFluffCount, fluffyPayload.Length - (preFluffCount + postFluffCount));
            var receivedHash = new byte[34];
            receivedHash[0] = postFluffCount;
            receivedHash[1] = preFluffCount;

            mySHA256.ComputeHash(Encoding.UTF8.GetBytes(recoveredPayload)).CopyTo(receivedHash, 2);
            var recoveredPayloadHash = SHA256toString(receivedHash);
            if (string.Compare(recoveredPayloadHash, Sha256FingerPrint, true) != 0)
                throw new InvalidPayloadException($"Sha256 mismatch on payload");

            return recoveredPayload;
        }
        #endregion

        #region Helpers (Public)

        /// <summary>
        /// Decodes a string containing Base64 to an unencoded string
        /// </summary>
        /// <param name="inputStr">Base64 compliant string</param>
        /// <returns></returns>
        public string Base64DecodeString(string inputStr)
        {
            byte[] decodedByteArray = Convert.FromBase64CharArray(inputStr.ToCharArray(), 0, inputStr.Length);
            StringBuilder s = new StringBuilder();
            for (int i = 0; i < decodedByteArray.Length; i++)
            {
                s.Append((char)decodedByteArray[i]);
            }
            return (s.ToString());
        }

        /// <summary>
        /// Encodes a string into Base64
        /// </summary>
        /// <param name="inputStr">the string value to encode</param>
        /// <param name="insertLineBreaks">Whether the resulting Base64 should be broken into separate lines.</param>
        /// <returns>Base64</returns>
        public string Base64EncodeString(string inputStr, bool insertLineBreaks)
        {
            byte[] rawByteArray = new byte[inputStr.Length];
            char[] encodedArray = new char[inputStr.Length * 2];

            for (int i = 0; i < inputStr.Length; i++)
                rawByteArray[i] = (byte)inputStr[i];

            Convert.ToBase64CharArray(
                rawByteArray,
                0,
                inputStr.Length,
                encodedArray,
                0,
                insertLineBreaks ?
                    Base64FormattingOptions.InsertLineBreaks : Base64FormattingOptions.None);
            string EncodedString = new string(encodedArray);
            int ActualLength = EncodedString.Length;
            while (ActualLength-- > 0 && EncodedString[ActualLength] == '\0')
                ;
            return EncodedString.Substring(0, ActualLength + 1);
        }
        #endregion

    }
}