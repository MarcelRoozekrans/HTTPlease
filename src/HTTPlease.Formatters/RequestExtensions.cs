﻿using System;
using System.Collections.Immutable;
using System.Net.Http;
using System.Text;

namespace HTTPlease
{
	using Formatters;

	/// <summary>
	///		Extension methods for working with <see cref="HttpRequest"/>s.
	/// </summary>
	public static class RequestExtensions
    {
		/// <summary>
		///		Create a copy of the <see cref="HttpRequest"/>, configuring it to accept the JSON ("application/json") media type.
		/// </summary>
		/// <param name="request">
		///		The <see cref="HttpRequest"/>.
		/// </param>
		/// <returns>
		///		The new <see cref="HttpRequest"/>.
		/// </returns>
		public static HttpRequest ExpectJson(this HttpRequest request)
		{
			if (request == null)
				throw new ArgumentNullException(nameof(request));

			return request.AcceptMediaType(WellKnownMediaTypes.Json);
		}

		/// <summary>
		///		Create a copy of the <see cref="HttpRequest"/>, configuring it to accept the JSON ("application/json") media type.
		/// </summary>
		/// <typeparam name="TContext">
		///		The type of object used as a context for resolving deferred parameters.
		/// </typeparam>
		/// <param name="request">
		///		The <see cref="HttpRequest"/>.
		/// </param>
		/// <returns>
		///		The new <see cref="HttpRequest"/>.
		/// </returns>
		public static HttpRequest<TContext> ExpectJson<TContext>(this HttpRequest<TContext> request)
		{
			if (request == null)
				throw new ArgumentNullException(nameof(request));

			return request.AcceptMediaType(WellKnownMediaTypes.Json);
		}

		/// <summary>
		///		Create a copy of the <see cref="HttpRequest"/>, configuring it to accept the XML ("application/json") media type.
		/// </summary>
		/// <param name="request">
		///		The <see cref="HttpRequest"/>.
		/// </param>
		/// <returns>
		///		The new <see cref="HttpRequest"/>.
		/// </returns>
		public static HttpRequest ExpectXml(this HttpRequest request)
		{
			if (request == null)
				throw new ArgumentNullException(nameof(request));

			return request.AcceptMediaType(WellKnownMediaTypes.Xml);
		}

		/// <summary>
		///		Create a copy of the <see cref="HttpRequest"/>, configuring it to accept the XML ("application/json") media type.
		/// </summary>
		/// <typeparam name="TContext">
		///		The type of object used as a context for resolving deferred parameters.
		/// </typeparam>
		/// <param name="request">
		///		The <see cref="HttpRequest"/>.
		/// </param>
		/// <returns>
		///		The new <see cref="HttpRequest"/>.
		/// </returns>
		public static HttpRequest<TContext> ExpectXml<TContext>(this HttpRequest<TContext> request)
		{
			if (request == null)
				throw new ArgumentNullException(nameof(request));

			return request.AcceptMediaType(WellKnownMediaTypes.Xml);
		}
		
		/// <summary>
		///		Build an HTTP request message, selecting an appropriate content formatter to serialise its body content.
		/// </summary>
		/// <param name="request">
		///		The <see cref="HttpRequest"/>.
		/// </param>
		/// <param name="httpMethod">
		///		The HTTP request method.
		/// </param>
		/// <param name="bodyContent">
		///		The request body content.
		/// </param>
		/// <param name="mediaType">
		///		The request body media type to use.
		/// </param>
		/// <param name="baseUri">
		///		An optional base URI to use if the request does not already have an absolute request URI.
		/// </param>
		/// <returns>
		///		The configured <see cref="HttpRequestMessage"/>.
		/// </returns>
		public static HttpRequestMessage BuildRequestMessage(this HttpRequest request, HttpMethod httpMethod, object bodyContent, string mediaType, Uri baseUri = null)
		{
			if (request == null)
				throw new ArgumentNullException(nameof(request));

			return request.BuildRequestMessage(httpMethod, bodyContent, mediaType, Encoding.UTF8, baseUri);
		}

		/// <summary>
		///		Build an HTTP request message, selecting an appropriate content formatter to serialise its body content.
		/// </summary>
		/// <param name="request">
		///		The <see cref="HttpRequest"/>.
		/// </param>
		/// <param name="httpMethod">
		///		The HTTP request method.
		/// </param>
		/// <param name="bodyContent">
		///		The request body content.
		/// </param>
		/// <param name="mediaType">
		///		The request body media type to use.
		/// </param>
		/// <param name="encoding">
		///		The request body encoding to use.
		/// </param>
		/// <param name="baseUri">
		///		An optional base URI to use if the request does not already have an absolute request URI.
		/// </param>
		/// <returns>
		///		The configured <see cref="HttpRequestMessage"/>.
		/// </returns>
		public static HttpRequestMessage BuildRequestMessage(this HttpRequest request, HttpMethod httpMethod, object bodyContent, string mediaType, Encoding encoding, Uri baseUri = null)
		{
			if (request == null)
				throw new ArgumentNullException(nameof(request));

			HttpContent httpContent = bodyContent as HttpContent;
			if (httpContent == null && bodyContent != null)
			{
				IFormatterCollection formatters = request.CreateFormatterCollection();

				OutputFormatterContext writeContext = new OutputFormatterContext(bodyContent, bodyContent.GetType(), mediaType, encoding);
				IOutputFormatter writeFormatter = formatters.FindOutputFormatter(writeContext);
				if (writeFormatter == null)
					throw new HttpRequestException($"None of the supplied formatters can write data of type '{writeContext.DataType.FullName}' to media type '{writeContext.MediaType}'.");

				httpContent = new FormattedObjectContent(writeFormatter, writeContext);
			}

			return request.BuildRequestMessage(httpMethod, httpContent, baseUri);
		}

		/// <summary>
		///		Create a copy of the <see cref="HttpRequest"/>, adding the specified content formatter.
		/// </summary>
		/// <param name="request">
		///		The <see cref="HttpRequest"/>.
		/// </param>
		/// <param name="formatter">
		///		The content formatter to add.
		/// </param>
		/// <returns>
		///		The new <see cref="HttpRequest"/>.
		/// </returns>
		public static HttpRequest WithFormatter(this HttpRequest request, IFormatter formatter)
		{
			if (request == null)
				throw new ArgumentNullException(nameof(request));

			Type formatterType = formatter.GetType();

			ImmutableDictionary<Type, IFormatter> formatters = request.GetFormatters();
			bool isFirstFormatter = formatters.Count == 0;

			formatters = formatters.SetItem(formatterType, formatter);

			return request.Clone(properties =>
			{
				properties[MessageProperties.ContentFormatters] = formatters;

				// If this is the first formatter we're adding, then make sure that we'll populate the formatter collection for each outgoing request.
				if (isFirstFormatter)
				{
					properties[nameof(request.RequestActions)] = request.RequestActions.Add((requestMessage, context) =>
					{
						requestMessage.Properties[MessageProperties.ContentFormatters] = new FormatterCollection(formatters.Values);
					});
				}
			});
		}

		/// <summary>
		///		Create a copy of the <see cref="HttpRequest"/>, adding the specified content formatter.
		/// </summary>
		/// <param name="request">
		///		The <see cref="HttpRequest"/>.
		/// </param>
		/// <param name="formatterType">
		///		The type of content formatter to remove.
		/// </param>
		/// <returns>
		///		The new <see cref="HttpRequest"/>.
		/// </returns>
		public static HttpRequest WithoutFormatter(this HttpRequest request, Type formatterType)
		{
			if (request == null)
				throw new ArgumentNullException(nameof(request));

			if (formatterType == null)
				throw new ArgumentNullException(nameof(formatterType));

			ImmutableDictionary<Type, IFormatter> formatters = request.GetFormatters();
			if (formatters == null)
				return request;

			if (!formatters.ContainsKey(formatterType))
				return request;

			return request.Clone(properties =>
			{
				properties[MessageProperties.ContentFormatters] = formatters.Remove(formatterType);
			});
		}

		/// <summary>
		///		Get the collection formatters used by the <see cref="HttpRequest"/>.
		/// </summary>
		/// <param name="request">
		///		The <see cref="HttpRequest"/>.
		/// </param>
		/// <returns>
		///		An immutable dictionary of formatters, keyed by type.
		/// </returns>
		public static ImmutableDictionary<Type, IFormatter> GetFormatters(this HttpRequest request)
		{
			if (request == null)
				throw new ArgumentNullException(nameof(request));

			object formatters;
			if (request.Properties.TryGetValue(MessageProperties.ContentFormatters, out formatters))
				return (ImmutableDictionary<Type, IFormatter>)formatters;

			return ImmutableDictionary<Type, IFormatter>.Empty;
		}

		/// <summary>
		///		Create an <see cref="IFormatterCollection"/> from the request's registered formatters.
		/// </summary>
		/// <param name="request">
		///		The <see cref="HttpRequest"/>.
		/// </param>
		/// <returns>
		///		An <see cref="IFormatterCollection"/> representing the formatter collection.
		/// </returns>
		public static IFormatterCollection CreateFormatterCollection(this HttpRequest request)
		{
			if (request == null)
				throw new ArgumentNullException(nameof(request));

			return new FormatterCollection(
				request.GetFormatters().Values
			);
		}
	}
}
