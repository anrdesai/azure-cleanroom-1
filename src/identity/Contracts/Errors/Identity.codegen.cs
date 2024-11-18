// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Resources;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

// Specify that the fallback language is also inside a satellite assembly.
[assembly: NeutralResourcesLanguage("en", UltimateResourceFallbackLocation.Satellite)]

namespace Microsoft.Azure.CleanRoomSidecar.Identity.Errors
{
    /// <summary>
    /// Enumeration of display name to code.
    /// </summary>
    public enum IdentityErrorCode
    {
        InternalError = 500,
        InvalidClientId = 501,
        InvalidConfiguration = 502,
    }

    /// <summary>
    /// Class definition.
    /// </summary>
    public partial class IdentityException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IdentityException"/> class.
        /// This constructor is used to create an instance of <see cref="IdentityException"/>
        /// with an inner exception.
        /// </summary>
        /// <param name="exception">Instance of <see cref="IdentityException"/> from which
        /// this exception is to be initialized.</param>
        /// <param name="innerException">Inner exception to be associated with this instance.
        /// </param>
        public IdentityException(
            IdentityException exception,
            Exception innerException)
            : base(string.Empty, innerException)
        {
            if (exception != null)
            {
                this.Id = exception.Id;
                this.Parameters = exception.Parameters;
                this.Initialize();
            }
        }

        /// <summary>
        /// Gets the ID associated with the error.
        /// </summary>
        [JsonInclude]
        public IdentityErrorCode Id { get; private set; }

        /// <summary>
        /// Gets the parameters needed to form resource strings.
        /// </summary>
        [JsonInclude]
        public Dictionary<string, string> Parameters { get; private set; }

        /// <summary>
        /// Gets the tags associated with the error.
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, string> Tags { get; private set; }

        /// <summary>
        /// Gets the error code associated with the error.
        /// </summary>
        [JsonIgnore]
        public IdentityErrorCode ErrorCode => this.Id;

        /// <summary>
        /// Gets the error message. Use GetMessage if you want to specify culture.
        /// </summary>
        [JsonIgnore]
        public override string Message => this.GetMessageSafe();

        /// <summary>
        /// Perform any custom initialization of the class properties here.
        /// </summary>
        internal void Initialize()
        {
            this.Data.Add("ErrorCode", this.Id);
            this.Data.Add("Level", IdentityErrorCodeSeverityMap[this.Id]);
        }
    }

    /// <summary>
    /// Constructors (one per code) that take required parameters and return initialized object.
    /// </summary>
    public partial class IdentityException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IdentityException"/> class.
        /// </summary>
        internal IdentityException(
            IdentityErrorCode id,
            Dictionary<string, string> parameters,
            Dictionary<string, string> tags)
        {
            this.Id = id;
            this.Parameters = parameters;
            this.Tags = tags;
            this.Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IdentityException"/> class.
        /// </summary>
        /// <remarks>This constructor is used for JSON deserialization.</remarks>
        [JsonConstructor]
        protected IdentityException()
        {
        }

        public static IdentityException InternalError()
        {
            return new IdentityException(
                IdentityErrorCode.InternalError,
                new System.Collections.Generic.Dictionary<string, string>()
                {
                },
                new System.Collections.Generic.Dictionary<string, string>()
                {
                });
        }

        public static IdentityException InvalidClientId(string clientId)
        {
            return new IdentityException(
                IdentityErrorCode.InvalidClientId,
                new System.Collections.Generic.Dictionary<string, string>()
                {
                    { "ClientId", clientId },
                },
                new System.Collections.Generic.Dictionary<string, string>()
                {
                });
        }

        public static IdentityException InvalidConfiguration(string message)
        {
            return new IdentityException(
                IdentityErrorCode.InvalidConfiguration,
                new System.Collections.Generic.Dictionary<string, string>()
                {
                    { "Message", message },
                },
                new System.Collections.Generic.Dictionary<string, string>()
                {
                });
        }
    }

    /// <summary>
    /// Accessors for resource string that take culture as input and return localized string.
    /// </summary>
    public partial class IdentityException
    {
        public string GetMessage(string culture = null)
        {
            culture = culture ?? CultureInfo.InstalledUICulture.Name;
            string resourceId = $"Error_{(int)this.Id}_Message";
            return MessageFormatter.GetFormattedMessageString(
                resourceId,
                this.Parameters,
                culture);
        }

        public string GetMessageSafe(string culture = null)
        {
            try
            {
                return this.GetMessage(culture);
            }
            catch (Exception)
            {
                return $"Unable to retrieve event message.";
            }
        }

        public string GetPossibleCauses(string culture = null)
        {
            culture = culture ?? CultureInfo.InstalledUICulture.Name;
            string resourceId = $"Error_{(int)this.Id}_PossibleCauses";
            return MessageFormatter.GetFormattedMessageString(
                resourceId,
                this.Parameters,
                culture);
        }

        public string GetPossibleCausesSafe(string culture = null)
        {
            try
            {
                return this.GetPossibleCauses(culture);
            }
            catch (Exception)
            {
                return $"Unable to retrieve event message.";
            }
        }

        public string GetRecommendedAction(string culture = null)
        {
            culture = culture ?? CultureInfo.InstalledUICulture.Name;
            string resourceId = $"Error_{(int)this.Id}_RecommendedAction";
            return MessageFormatter.GetFormattedMessageString(
                resourceId,
                this.Parameters,
                culture);
        }

        public string GetRecommendedActionSafe(string culture = null)
        {
            try
            {
                return this.GetRecommendedAction(culture);
            }
            catch (Exception)
            {
                return $"Unable to retrieve event message.";
            }
        }

        /// <summary>
        /// Message formatter class for creating messages in the requested culture.
        /// </summary>
        private class MessageFormatter
        {
            /// <summary>
            /// If a message parameter is not passed, it will be replaced with this string.
            /// </summary>
            private const string NoParameterMarker = "";

            /// <summary>
            /// For having a % character in the message, its better to use %% in the message.
            /// The %% in the message is replaced with this for safe parsing, and replaced back
            /// with %.
            /// </summary>
            private const string ANSIIMarkerForPercent = "&#37;";

            /// <summary>
            /// Regular expression for matching parameter names (e.g. %PrimaryCloudName;) in the
            /// message.
            /// </summary>
            private static readonly Regex ParameterRegex = new Regex(
                @"%[^%]*?;",
                RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

            /// <summary>
            /// Resource manager instance for fetching the messages from the resource file.
            /// </summary>
            private static ResourceManager resourceManager;

            /// <summary>
            /// Initializes static members of the <see cref="MessageFormatter"/> class.
            /// </summary>
            static MessageFormatter()
            {
                resourceManager =
                    new ResourceManager(
                        "Microsoft.Azure.CleanRoomSidecar.Identity.Resources.Errors",
                        typeof(IdentityException).Assembly);
            }

            /// <summary>
            /// Formats and returns the message string in the given locale.
            /// </summary>
            /// <param name="resourceId">The resource id.</param>
            /// <param name="parameters">Collection of named parameters for creating message.
            /// </param>
            /// <param name="culture">The culture, in which the string is needed.</param>
            /// <returns>
            /// Formatted message in the given locale. Empty string if resource not found.
            /// </returns>
            public static string GetFormattedMessageString(
                string resourceId,
                IDictionary<string, string> parameters,
                string culture)
            {
                string formatString = GetResourceString(resourceId, culture);
                return FormatString(formatString, parameters);
            }

            /// <summary>
            /// Get resource string for the specified resource id. Empty string if resource is
            /// not found.
            /// </summary>
            /// <param name="resourceId">The resource id.</param>
            /// <param name="culture">The culture, in which the string is needed.</param>
            /// <returns>Resource string in the given locale. Null if resource not found.
            /// </returns>
            private static string GetResourceString(
                string resourceId,
                string culture)
            {
                CultureInfo cultureInfo = null;
                try
                {
                    // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Accept-Language
                    if (culture != null && culture.Contains(","))
                    {
                        culture = culture.Split(',')[0];
                    }

                    cultureInfo = new CultureInfo(culture ?? string.Empty);
                }
                catch (CultureNotFoundException)
                {
                    // Client can send a locale which is valid on client system, but is not
                    // supported by the OS/.NET versions of the Gateway web/worker roles.
                    // Unsupported culture in OS/.NET will default to 'en-US'.
                    cultureInfo = new CultureInfo("en-US");
                }

                return resourceManager.GetString(resourceId, cultureInfo);
            }

            /// <summary>
            /// Replace all the message parameters with that in the list of parameters passed.
            /// </summary>
            /// <param name="formatString">The formatted message in which the values need to be
            /// replaced.</param>
            /// <param name="parameters">A key value pair of message parameters and values.</param>
            /// <returns>The string with all the parameters replaced.</returns>
            private static string FormatString(
                string formatString,
                IDictionary<string, string> parameters)
            {
                if (string.IsNullOrWhiteSpace(formatString))
                {
                    return string.Empty;
                }

                StringBuilder sb = new StringBuilder();

                sb.Append(formatString);

                // hide percentage escape characters using their ANSI marker
                sb.Replace("%%", ANSIIMarkerForPercent);

                foreach (Match param in MessageFormatter.ParameterRegex.Matches(formatString))
                {
                    string key = param.Value.Substring(1, param.Length - 2);
                    string parameter;
                    if (parameters == null || !parameters.TryGetValue(key, out parameter))
                    {
                        parameter = MessageFormatter.NoParameterMarker;
                    }

                    sb.Replace(param.Value, parameter);
                }

                sb.Replace(ANSIIMarkerForPercent, "%");

                return sb.ToString();
            }
        }
    }

    /// <summary>
    /// Accessors for Severity.
    /// </summary>
    public partial class IdentityException
    {
        private static Dictionary<IdentityErrorCode, string> IdentityErrorCodeSeverityMap { get; } =
            new Dictionary<IdentityErrorCode, string>
            {
                { IdentityErrorCode.InternalError, "Error" },
                { IdentityErrorCode.InvalidClientId, "Error" },
                { IdentityErrorCode.InvalidConfiguration, "Error" },
            };

        public static string GetSeverity(IdentityErrorCode id)
        {
            return IdentityErrorCodeSeverityMap[id];
        }

        public static bool TryGetSeverity(IdentityErrorCode id, out string value)
        {
            return IdentityErrorCodeSeverityMap.TryGetValue(id, out value);
        }
    }

    /// <summary>
    /// Accessors for HttpStatusCode.
    /// </summary>
    public partial class IdentityException
    {
        private static Dictionary<IdentityErrorCode, string> IdentityErrorCodeHttpStatusCodeMap { get; } =
            new Dictionary<IdentityErrorCode, string>
            {
                { IdentityErrorCode.InternalError, "InternalServerError" },
                { IdentityErrorCode.InvalidClientId, "BadRequest" },
                { IdentityErrorCode.InvalidConfiguration, "InternalServerError" },
            };

        public static string GetHttpStatusCode(IdentityErrorCode id)
        {
            return IdentityErrorCodeHttpStatusCodeMap[id];
        }

        public static bool TryGetHttpStatusCode(IdentityErrorCode id, out string value)
        {
            return IdentityErrorCodeHttpStatusCodeMap.TryGetValue(id, out value);
        }
    }
}
