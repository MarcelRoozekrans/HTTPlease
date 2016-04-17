﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;

namespace HTTPlease
{
	using ValueProviders;

	using RequestProperties = ImmutableDictionary<string, object>;

	/// <summary>
	///		Helper methods for <see cref="HttpRequest"/> / <see cref="IHttpRequest"/> extensions.
	/// </summary>
	public static partial class RequestExtensions
    {
		/// <summary>
		///		Configure the request URI (and template status) in the request properties.
		/// </summary>
		/// <param name="requestProperties">
		///		The request properties to modify.
		/// </param>
		/// <param name="requestUri">
		///		The request URI.
		/// </param>
		static void SetRequestUri(this RequestProperties.Builder requestProperties, Uri requestUri)
		{
			if (requestProperties == null)
				throw new ArgumentNullException(nameof(requestProperties));

			if (requestUri == null)
				throw new ArgumentNullException(nameof(requestUri));

			requestProperties[nameof(IHttpRequest.RequestUri)] = requestUri;
			requestProperties[nameof(IHttpRequest.IsUriTemplate)] = UriTemplate.IsTemplate(requestUri);
		}

		/// <summary>
		///		Ensure that the specified string is surrounted by quotes.
		/// </summary>
		/// <param name="str">
		///		The string to examine.
		/// </param>
		/// <returns>
		///		The string, with quotes prepended / appended as required.
		/// </returns>
		/// <remarks>
		///		Some HTTP headers (such as If-Match) require their values to be quoted.
		/// </remarks>
		static string EnsureQuoted(string str)
		{
			if (str == null)
				throw new ArgumentNullException(nameof(str));

			if (str.Length == 0)
				return "\"\"";

			StringBuilder quotedStringBuilder = new StringBuilder(str);

			if (quotedStringBuilder[0] != '\"')
				quotedStringBuilder.Insert(0, '\"');

			if (quotedStringBuilder[quotedStringBuilder.Length - 1] != '\"')
				quotedStringBuilder.Append('\"');

			return quotedStringBuilder.ToString();
		}

		/// <summary>
		///		Convert the specified object's properties to deferred parameters.
		/// </summary>
		/// <typeparam name="TContext">
		///		The type of object used by the request when resolving deferred template parameters.
		/// </typeparam>
		/// <typeparam name="TParameters">
		///		The type of object whose properties will form the parameters.
		/// </typeparam>
		/// <param name="parameters">
		///		The object whose properties will form the parameters.
		/// </param>
		/// <returns>
		///		A sequence of key / value pairs representing the parameters.
		/// </returns>
		static IEnumerable<KeyValuePair<string, IValueProvider<TContext, string>>> CreateDeferredParameters<TContext, TParameters>(this TParameters parameters)
		{
			if (Equals(parameters, null))
				throw new ArgumentNullException(nameof(parameters));

			// Yes yes yes, reflection might be "slow", but it's still blazingly fast compared to making a request over the network.
			foreach (PropertyInfo property in typeof(TParameters).GetProperties(BindingFlags.Instance | BindingFlags.Public))
			{
				// Ignore write-only properties.
				if (!property.CanRead)
					continue;

				yield return new KeyValuePair<string, IValueProvider<TContext, string>>(
					property.Name,
					ValueProvider<TContext>.FromSelector(
						context => property.GetValue(parameters)
					)
					.Convert().ValueToString()
				);
			}
		}
	}
}