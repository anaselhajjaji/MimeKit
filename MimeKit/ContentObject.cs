//
// ContentObject.cs
//
// Author: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2013 Jeffrey Stedfast
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.IO;
using System.Threading;

using MimeKit.IO;
using MimeKit.IO.Filters;

namespace MimeKit {
	/// <summary>
	/// Encapsulates a content stream used by <see cref="MimeKit.MimePart"/>.
	/// </summary>
	public sealed class ContentObject : IContentObject
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MimeKit.ContentObject"/> class.
		/// </summary>
		/// <param name="stream">The content stream.</param>
		/// <param name="encoding">The stream encoding.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="stream"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <para><paramref name="stream"/> does not support reading.</para>
		/// <para>-or-</para>
		/// <para><paramref name="stream"/> does not support seeking.</para>
		/// </exception>
		/// <remarks>
		/// When creating new <see cref="MimeKit.MimePart"/>s, the <paramref name="encoding"/>
		/// should typically be <see cref="MimeKit.ContentEncoding.Default"/> unless the
		/// <paramref name="stream"/> has already been encoded.
		/// </remarks>
		public ContentObject (Stream stream, ContentEncoding encoding)
		{
			if (stream == null)
				throw new ArgumentNullException ("stream");

			if (!stream.CanRead || !stream.CanSeek)
				throw new ArgumentException ("stream");

			Encoding = encoding;
			Stream = stream;
		}

		#region IContentObject implementation

		/// <summary>
		/// Gets or sets the content encoding.
		/// </summary>
		/// <value>The content encoding.</value>
		public ContentEncoding Encoding {
			get; private set;
		}

		/// <summary>
		/// Gets or sets the content stream.
		/// </summary>
		/// <value>The content stream.</value>
		public Stream Stream {
			get; private set;
		}

		/// <summary>
		/// Copies the content stream to the specified output stream.
		/// </summary>
		/// <param name="stream">The output stream.</param>
		/// <param name="token">A cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="stream"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		public void WriteTo (Stream stream, CancellationToken token)
		{
			if (stream == null)
				throw new ArgumentNullException ("stream");

			token.ThrowIfCancellationRequested ();

			byte[] buf = new byte[4096];
			int nread;

			Stream.Seek (0, SeekOrigin.Begin);

			try {
				do {
					token.ThrowIfCancellationRequested ();
					if ((nread = Stream.Read (buf, 0, buf.Length)) <= 0)
						break;

					token.ThrowIfCancellationRequested ();
					stream.Write (buf, 0, nread);
				} while (true);

				Stream.Seek (0, SeekOrigin.Begin);
			} catch (OperationCanceledException) {
				// try and reset the stream

				try {
					Stream.Seek (0, SeekOrigin.Begin);
				} catch (IOException) {
				}

				throw;
			}
		}

		/// <summary>
		/// Copies the content stream to the specified output stream.
		/// </summary>
		/// <param name="stream">The output stream.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="stream"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		public void WriteTo (Stream stream)
		{
			WriteTo (stream, CancellationToken.None);
		}

		/// <summary>
		/// Decodes the content stream into another stream.
		/// </summary>
		/// <param name="stream">The output stream.</param>
		/// <param name="token">A cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="stream"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		public void DecodeTo (Stream stream, CancellationToken token)
		{
			if (stream == null)
				throw new ArgumentNullException ("stream");

			using (var filtered = new FilteredStream (stream)) {
				filtered.Add (DecoderFilter.Create (Encoding));
				WriteTo (filtered, token);
				filtered.Flush ();
			}
		}

		/// <summary>
		/// Decodes the content stream into another stream.
		/// </summary>
		/// <param name="stream">The output stream.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="stream"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		public void DecodeTo (Stream stream)
		{
			DecodeTo (stream, CancellationToken.None);
		}

		#endregion
	}
}
