﻿using System;

namespace HTTPlease
{
	/// <summary>
	///		Extension methods for <see cref="HttpRequestFactory2"/>.
	/// </summary>
	public static class FactoryExtensions2
    {
		/// <summary>
		///		Create a new HTTP request with the specified request URI.
		/// </summary>
		/// <param name="requestFactory">
		///		The HTTP request factory.
		/// </param>
		/// <param name="requestUri">
		///		The request URI (can be relative or absolute).
		/// </param>
		/// <returns>
		///		The new <see cref="HttpRequest"/>.
		/// </returns>
		public static HttpRequest2 Create(this HttpRequestFactory2 requestFactory, string requestUri)
		{
			if (requestFactory == null)
				throw new ArgumentNullException(nameof(requestFactory));

			if (String.IsNullOrWhiteSpace(requestUri))
				throw new ArgumentException("Argument cannot be null, empty, or composed entirely of whitespace: 'requestUri'.", nameof(requestUri));

			return requestFactory.Create(
				new Uri(requestUri, UriKind.RelativeOrAbsolute)
			);
		}
	}
}
