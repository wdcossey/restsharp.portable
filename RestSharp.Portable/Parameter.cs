﻿using System;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;

namespace RestSharp.Portable
{
    /// <summary>
    /// Parameter container for REST requests
    /// </summary>
    public class Parameter
    {
        private static readonly CultureInfo s_cultureUS = new CultureInfo("en-US");

        /// <summary>
        /// Initializes a new instance of the <see cref="Parameter" /> class.
        /// </summary>
        public Parameter()
        {
            ValidateOnAdd = true;
        }

        /// <summary>
        /// Gets or sets the name of the parameter
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the value of the parameter
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Gets or sets the type of the parameter
        /// </summary>
        public ParameterType Type { get; set; }

        /// <summary>
        /// Gets or sets the content type of the parameter
        /// </summary>
        /// <remarks>
        /// Applies to the following parameter types:
        /// - RequestBody
        /// </remarks>
        public MediaTypeHeaderValue ContentType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this parameter should be validated?
        /// </summary>
        /// <remarks>
        /// Applies to the following parameter types:
        /// - HttpHeader
        /// </remarks>
        public bool ValidateOnAdd { get; set; }

        /// <summary>
        /// Gets or sets the encoding of this parameters value
        /// </summary>
        /// <remarks>
        /// Applies to the following parameter types:
        /// - GetOrPost
        /// - QueryString
        /// - UrlSegment
        /// </remarks>
        public Encoding Encoding { get; set; }

        /// <summary>
        /// Returns the parameter value as string
        /// </summary>
        /// <returns>Returns the value as string</returns>
        internal string AsString()
        {
            var byteArray = Value as byte[];
            if (byteArray != null)
                return Convert.ToBase64String(byteArray);
            return string.Format(s_cultureUS, "{0}", Value);
        }
    }
}
