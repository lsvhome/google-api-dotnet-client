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
using System.Linq;
using Google.Apis;
using Google.Apis.Http;
using Google.Apis.Json;

namespace EncFSFileProvider.GoogleDrive
{
        /// <summary>An baseClientServiceInitializer class for the client service.</summary>
        public class BaseClientServiceInitializer
        {
            /// <summary>
            /// Gets or sets the factory for creating <see cref="System.Net.Http.HttpClient"/> instance. If this 
            /// property is not set the service uses a new <see cref="Google.Apis.Http.HttpClientFactory"/> instance.
            /// </summary>
            public IHttpClientFactory HttpClientFactory { get; set; }

            /// <summary>
            /// Gets or sets a HTTP client baseClientServiceInitializer which is able to customize properties on 
            /// <see cref="Google.Apis.Http.ConfigurableHttpClient"/> and 
            /// <see cref="Google.Apis.Http.ConfigurableMessageHandler"/>.
            /// </summary>
            public IConfigurableHttpClientInitializer HttpClientInitializer { get; set; }

            /// <summary>
            /// Get or sets the exponential back-off policy used by the service. Default value is 
            /// <c>UnsuccessfulResponse503</c>, which means that exponential back-off is used on 503 abnormal HTTP
            /// response.
            /// If the value is set to <c>None</c>, no exponential back-off policy is used, and it's up to the user to
            /// configure the <see cref="Google.Apis.Http.ConfigurableMessageHandler"/> in an
            /// <see cref="Google.Apis.Http.IConfigurableHttpClientInitializer"/> to set a specific back-off
            /// implementation (using <see cref="Google.Apis.Http.BackOffHandler"/>).
            /// </summary>
            public ExponentialBackOffPolicy DefaultExponentialBackOffPolicy { get; set; }

            /// <summary>Gets or sets whether this service supports GZip. Default value is <c>true</c>.</summary>
            public bool GZipEnabled { get; set; }

            /// <summary>
            /// Gets or sets the serializer. Default value is <see cref="Google.Apis.Json.NewtonsoftJsonSerializer"/>.
            /// </summary>
            public ISerializer Serializer { get; set; }

            /// <summary>Gets or sets the API Key. Default value is <c>null</c>.</summary>
            public string ApiKey { get; set; }

            /// <summary>
            /// Gets or sets Application name to be used in the User-Agent header. Default value is <c>null</c>. 
            /// </summary>
            public string ApplicationName { get; set; }

            /// <summary>
            /// Maximum allowed length of a URL string for GET requests. Default value is <c>2048</c>. If the value is
            /// set to <c>0</c>, requests will never be modified due to URL string length.
            /// </summary>
            public uint MaxUrlLength { get; set; }

            /// <summary>Constructs a new baseClientServiceInitializer with default values.</summary>
            public BaseClientServiceInitializer()
            {
                //GZipEnabled = true;
                //Serializer = new NewtonsoftJsonSerializer();
                //DefaultExponentialBackOffPolicy = ExponentialBackOffPolicy.UnsuccessfulResponse503;
                //MaxUrlLength = BaseClientService.DefaultMaxUrlLength;
            }

            // HttpRequestMessage.Headers fails if any of these characters are included in a User-Agent header.
            private const string InvalidApplicationNameCharacters = "\"(),:;<=>?@[\\]{}";

            internal void Validate()
            {
                if (ApplicationName != null && ApplicationName.Any(c => InvalidApplicationNameCharacters.Contains(c)))
                {
                    throw new ArgumentException("Invalid Application name", nameof(ApplicationName));
                }
            }
        }
}
