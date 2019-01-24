/*
Copyright 2013 Google Inc

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Google.Apis.Discovery;
using Google.Apis.Http;
using Google.Apis.Json;
using Google.Apis.Logging;
using Google.Apis.Requests;
using Google.Apis.Util;
using Google.Apis.Testing;
using System.Linq;

namespace Google.Apis.Services
{
    /// <summary>
    /// A base class for a client service which provides common mechanism for all services, like 
    /// serialization and GZip support.  It should be safe to use a single service instance to make server requests
    /// concurrently from multiple threads. 
    /// This class adds a special <see cref="Google.Apis.Http.IHttpExecuteInterceptor"/> to the 
    /// <see cref="Google.Apis.Http.ConfigurableMessageHandler"/> execute interceptor list, which uses the given 
    /// Authenticator. It calls to its applying authentication method, and injects the "Authorization" header in the 
    /// request.
    /// If the given Authenticator implements <see cref="Google.Apis.Http.IHttpUnsuccessfulResponseHandler"/>, this 
    /// class adds the Authenticator to the <see cref="Google.Apis.Http.ConfigurableMessageHandler"/>'s unsuccessful 
    /// response handler list.
    /// </summary>
    public abstract partial class BaseClientService : IClientService
    {
        /// <summary>The class logger.</summary>
        private static readonly ILogger Logger = ApplicationContext.Logger.ForType<BaseClientService>();

        /// <summary>The default maximum allowed length of a URL string for GET requests.</summary>
        [VisibleForTestOnly]
        public const uint DefaultMaxUrlLength = 2048;

#region BaseClientServiceInitializer

        #endregion

        /// <summary>Constructs a new base client with the specified baseClientServiceInitializer.</summary>
        protected BaseClientService(BaseClientServiceInitializer baseClientServiceInitializer)
        {
            baseClientServiceInitializer.Validate();
            // Set the right properties by the baseClientServiceInitializer's properties.
            GZipEnabled = baseClientServiceInitializer.GZipEnabled;
            Serializer = baseClientServiceInitializer.Serializer;
            ApiKey = baseClientServiceInitializer.ApiKey;
            ApplicationName = baseClientServiceInitializer.ApplicationName;
            if (ApplicationName == null)
            {
                Logger.Warning("Application name is not set. Please set BaseClientServiceInitializer.ApplicationName property");
            }
            HttpClientInitializer = baseClientServiceInitializer.HttpClientInitializer;

            // Create a HTTP client for this service.
            HttpClient = CreateHttpClient(baseClientServiceInitializer);
        }

        /// <summary>Returns <c>true</c> if this service contains the specified feature.</summary>
        private bool HasFeature(Features feature)
        {
            return Features.Contains(Utilities.GetEnumStringValue(feature));
        }

        private ConfigurableHttpClient CreateHttpClient(BaseClientServiceInitializer baseClientServiceInitializer)
        {
            // If factory wasn't set use the default HTTP client factory.
            var factory = baseClientServiceInitializer.HttpClientFactory ?? new HttpClientFactory();
            var args = new CreateHttpClientArgs
                {
                    GZipEnabled = GZipEnabled,
                    ApplicationName = ApplicationName,
                };

            // Add the user's input baseClientServiceInitializer.
            if (HttpClientInitializer != null)
            {
                args.Initializers.Add(HttpClientInitializer);
            }

            // Add exponential back-off baseClientServiceInitializer if necessary.
            if (baseClientServiceInitializer.DefaultExponentialBackOffPolicy != ExponentialBackOffPolicy.None)
            {
                args.Initializers.Add(new ExponentialBackOffInitializer(baseClientServiceInitializer.DefaultExponentialBackOffPolicy,
                    CreateBackOffHandler));
            }

            var httpClient = factory.CreateHttpClient(args);
            if (baseClientServiceInitializer.MaxUrlLength > 0)
            {
                httpClient.MessageHandler.AddExecuteInterceptor(new MaxUrlLengthInterceptor(baseClientServiceInitializer.MaxUrlLength));
            }
            return httpClient;
        }

        /// <summary>
        /// Creates the back-off handler with <see cref="Google.Apis.Util.ExponentialBackOff"/>. 
        /// Overrides this method to change the default behavior of back-off handler (e.g. you can change the maximum
        /// waited request's time span, or create a back-off handler with you own implementation of 
        /// <see cref="Google.Apis.Util.IBackOff"/>).
        /// </summary>
        protected virtual BackOffHandler CreateBackOffHandler()
        {
            // TODO(peleyal): consider return here interface and not the concrete class
            return new BackOffHandler(new ExponentialBackOff());
        }

        #region IClientService Members

        /// <inheritdoc/>
        public ConfigurableHttpClient HttpClient { get; private set; }

        /// <inheritdoc/>
        public IConfigurableHttpClientInitializer HttpClientInitializer { get; private set; }

        /// <inheritdoc/>
        public bool GZipEnabled { get; private set; }

        /// <inheritdoc/>
        public string ApiKey { get; private set; }

        /// <inheritdoc/>
        public string ApplicationName { get; private set; }

        /// <inheritdoc/>
        public void SetRequestSerailizedContent(HttpRequestMessage request, object body)
        {
            request.SetRequestSerailizedContent(this, body, GZipEnabled);
        }

        #region Serialization

        /// <inheritdoc/>
        public ISerializer Serializer { get; private set; }

        /// <inheritdoc/>
        public virtual string SerializeObject(object obj) => Serializer.Serialize(obj);

        /// <inheritdoc/>
        public virtual async Task<T> DeserializeResponse<T>(HttpResponseMessage response)
        {
            //Debug.WriteLine("DeserializeResponse 001 ");
            var text = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            //Debug.WriteLine("DeserializeResponse 002 " + text);


            // If a string is request, don't parse the response.
            if (Type.Equals(typeof(T), typeof(string)))
            {
                //Debug.WriteLine("DeserializeResponse 003 ");
                return (T)(object)text;
            }

            //Debug.WriteLine("DeserializeResponse 004 ");

            // Check if there was an error returned. The error node is returned in both paths
            // Deserialize the stream based upon the format of the stream.
            if (HasFeature(Discovery.Features.LegacyDataResponse))
            {
                //Debug.WriteLine("DeserializeResponse 005 ");
                // Legacy path (deprecated!)
                StandardResponse<T> sr = null;
                try
                {
                    //Debug.WriteLine("DeserializeResponse 006 ");
                    sr = Serializer.Deserialize<StandardResponse<T>>(text);
                    //Debug.WriteLine("DeserializeResponse 007 ");
                }
                catch (JsonReaderException ex)
                {
                    //Debug.WriteLine("DeserializeResponse 008 ");
                    throw new GoogleApiException(Name,
                        "Failed to parse response from server as json [" + text + "]", ex);
                }

                //Debug.WriteLine("DeserializeResponse 009 ");
                if (sr.Error != null)
                {
                    //Debug.WriteLine("DeserializeResponse 010 ");
                    throw new GoogleApiException(Name, "Server error - " + sr.Error)
                    {
                        Error = sr.Error
                    };
                }

                //Debug.WriteLine("DeserializeResponse 011 ");
                if (sr.Data == null)
                {
                    //Debug.WriteLine("DeserializeResponse 012 ");
                    throw new GoogleApiException(Name, "The response could not be deserialized.");
                }

                //Debug.WriteLine("DeserializeResponse 013 ");
                return sr.Data;
            }

            //Debug.WriteLine("DeserializeResponse 014 ");
            // New path: Deserialize the object directly.
            T result = default(T);
            //Debug.WriteLine("DeserializeResponse 015 ");
            try
            {
                Debug.WriteLine($"DeserializeResponse 016 {Serializer != null} {Serializer?.GetType().FullName}");
                result = Serializer.Deserialize<T>(text);
                //Debug.WriteLine("DeserializeResponse 017 ");
            }
            catch (JsonReaderException ex)
            {
                //Debug.WriteLine("DeserializeResponse 018 ");
                throw new GoogleApiException(Name, "Failed to parse response from server as json [" + text + "]", ex);
            }

            // TODO(peleyal): is this the right place to check ETag? it isn't part of deserialization!
            // If this schema/object provides an error container, check it.
            var eTag = response.Headers.ETag != null ? response.Headers.ETag.Tag : null;

            //Debug.WriteLine("DeserializeResponse 019 ");

            if (result is IDirectResponseSchema && eTag != null)
            {
                //Debug.WriteLine("DeserializeResponse 020 ");
                (result as IDirectResponseSchema).ETag = eTag;
                //Debug.WriteLine("DeserializeResponse 021 ");
            }

            //Debug.WriteLine("DeserializeResponse 022 ");
            return result;
        }

        /// <inheritdoc/>
        public virtual async Task<RequestError> DeserializeError(HttpResponseMessage response)
        {
            StandardResponse<object> errorResponse = null;
            try
            {
                var str = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                errorResponse = Serializer.Deserialize<StandardResponse<object>>(str);
                if (errorResponse.Error == null)
                {
                    throw new GoogleApiException(Name, "error response is null");
                }
            }
            catch (Exception ex)
            {
                // exception will be thrown in case the response content is empty or it can't be deserialized to 
                // Standard response (which contains data and error properties)
                throw new GoogleApiException(Name,
                    "An Error occurred, but the error response could not be deserialized", ex);
            }

            return errorResponse.Error;
        }

        #endregion

        #region Abstract Members

        /// <inheritdoc/>
        public abstract string Name { get; }

        /// <inheritdoc/>
        public abstract string BaseUri { get; }

        /// <inheritdoc/>
        public abstract string BasePath { get; }

        /// <summary>The URI used for batch operations.</summary>
        public virtual string BatchUri { get { return null; } }

        /// <summary>The path used for batch operations.</summary>
        public virtual string BatchPath { get { return null; } }

        /// <inheritdoc/>
        public abstract IList<string> Features { get; }

        #endregion

        #endregion

        /// <inheritdoc/>
        public virtual void Dispose()
        {
            if (HttpClient != null)
            {
                HttpClient.Dispose();
            }
        }
    }
}
